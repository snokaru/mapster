using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommandLine;
using Mapster.Common;
using Mapster.Common.MemoryMappedTypes;
using OSMDataParser;
using OSMDataParser.Elements;

namespace MapFeatureGenerator;

public static class Program
{

    private static MapData LoadOSMFile(ReadOnlySpan<char> osmFilePath)
    {
        var nodes = new ConcurrentDictionary<long, AbstractNode>();
        var ways = new ConcurrentBag<Way>();

        Parallel.ForEach(new PBFFile(osmFilePath), (blob, loopState) =>
        {
            switch (blob.Type)
            {
                case BlobType.Primitive:
                    {
                        var primitiveBlock = blob.ToPrimitiveBlock();
                        foreach (var primitiveGroup in primitiveBlock)
                            switch (primitiveGroup.ContainedType)
                            {
                                case PrimitiveGroup.ElementType.Node:
                                    foreach (var node in primitiveGroup) nodes[node.Id] = (AbstractNode)node;
                                    break;

                                case PrimitiveGroup.ElementType.Way:
                                    foreach (var way in primitiveGroup) ways.Add((Way)way);
                                    break;
                            }

                        break;
                    }
            }
        });

        var tiles = new Dictionary<int, List<long>>();
        foreach (var (id, node) in nodes)
        {
            var tileId = TiligSystem.GetTile(new Coordinate(node.Latitude, node.Longitude));
            if (tiles.TryGetValue(tileId, out var nodeIds))
            {
                nodeIds.Add(id);
            }
            else
            {
                tiles[tileId] = new List<long> { id };
            }
        }

        return new MapData
        {
            Nodes = nodes.ToImmutableDictionary(), Tiles = tiles.ToImmutableDictionary(), Ways = ways.ToImmutableArray()
        };
    }

    private static void CreateMapDataFile(ref MapData mapData, string filePath)
    {
        var usedNodes = new HashSet<long>();
        var labels = new List<int>();
        var propKeys = new HashSet<string>();
        var propValues = new HashSet<string>();
        var featureIds = new List<long>();
        var geometryTypes = new List<GeometryType>();
        var coordinates = new Dictionary<long, (int offset, List<Coordinate> coordinates)>();

        using var fileWriter = new BinaryWriter(File.OpenWrite(filePath));
        var offsets = new List<long>(mapData.Tiles.Count);

        // Write FileHeader
        fileWriter.Write((long)1);
        fileWriter.Write(mapData.Tiles.Count);

        // Write TileHeaderEntry
        foreach (var tile in mapData.Tiles)
        {
            fileWriter.Write(tile.Key);
            fileWriter.Write((long)0);
        }

        foreach (var (tileId, nodeIds) in mapData.Tiles)
        {
            usedNodes.Clear();
            labels.Clear();
            propKeys.Clear();
            propValues.Clear();
            featureIds.Clear();
            geometryTypes.Clear();
            coordinates.Clear();

            var totalCoordinateCount = 0;

            foreach (var way in mapData.Ways)
            {
                var featureCoordinates = new List<Coordinate>();

                featureIds.Add(way.Id);
                var geometryType = GeometryType.Polyline;

                labels.Add(-1);
                for (var i = 0; i < way.Tags.Count; ++i)
                {
                    var tag = way.Tags[i];
                    if (!propKeys.Contains(tag.Key))
                    {
                        if (tag.Key == "name")
                        {
                            labels[^1] = propKeys.Count - 1;
                        }

                        propKeys.Add(tag.Key);
                        propValues.Add(tag.Value);
                    }
                }

                foreach (var nodeId in way.NodeIds)
                {
                    var node = mapData.Nodes[nodeId];
                    usedNodes.Add(nodeId);

                    foreach (var (key, value) in node.Tags)
                    {
                        if (!propKeys.Contains(key))
                        {
                            propKeys.Add(key);
                            propValues.Add(value);
                        }
                    }

                    featureCoordinates.Add(new Coordinate(node.Latitude, node.Longitude));
                }

                if (featureCoordinates[0] == featureCoordinates[^1])
                {
                    geometryType = GeometryType.Polygon;
                }
                geometryTypes.Add(geometryType);
                coordinates.Add(way.Id, (totalCoordinateCount, featureCoordinates));
                totalCoordinateCount += featureCoordinates.Count;
            }

            foreach (var (nodeId, node) in mapData.Nodes.Where(n => !usedNodes.Contains(n.Key)))
            {
                featureIds.Add(nodeId);
                geometryTypes.Add(GeometryType.Point);

                labels.Add(-1);
                for (var i = 0; i < node.Tags.Count; ++i)
                {
                    var tag = node.Tags[i];
                    if (!propKeys.Contains(tag.Key))
                    {
                        if (tag.Key == "name")
                        {
                            labels[^1] = propKeys.Count - 1;
                        }

                        propKeys.Add(tag.Key);
                        propValues.Add(tag.Value);
                    }
                }

                coordinates.Add(nodeId, (totalCoordinateCount, new List<Coordinate> { new Coordinate(node.Latitude, node.Longitude) }));
                ++totalCoordinateCount;
            }

            offsets.Add(fileWriter.BaseStream.Position);

            // Write TileBlockHeader
            fileWriter.Write(featureIds.Count); // TileBlockHeader: FeatureCount
            fileWriter.Write(totalCoordinateCount); // TileBlockHeader: CoordinateCount
            fileWriter.Write(propKeys.Count + propValues.Count); // TileBlockHeader: StringCount
            fileWriter.Write(0); //TileBlockHeader: CharactersCount

            // Write MapFeatures
            for (var i = 0; i < featureIds.Count; ++i)
            {
                fileWriter.Write(featureIds[i]); // MapFeature: Id
                fileWriter.Write(labels[i]); // MapFeature: LabelOffset
                fileWriter.Write((byte)geometryTypes[i]); // MapFeature: GeometryType
                fileWriter.Write(coordinates[featureIds[i]].offset); // MapFeature: CoordinateOffset
                fileWriter.Write(coordinates[featureIds[i]].coordinates.Count); // MapFeature: CoordinateCount
            }

            fileWriter.Write(fileWriter.BaseStream.Position); // TileBlockHeader: CoordinatesOffsetInBytes
            foreach (var (_, (_, coordinateList)) in coordinates)
            {
                foreach (var c in coordinateList)
                {
                    fileWriter.Write(c.Latitude); // Coordinate: Latitude
                    fileWriter.Write(c.Longitude); // Coordinate: Longitude
                }
            }

            fileWriter.Write(fileWriter.BaseStream.Position); // TileBlockHeader: StringsOffsetInBytes
            foreach (var key in propKeys)
            {
                fileWriter.Write(); // Coordinate: Latitude
                fileWriter.Write(coordinate.Longitude); // Coordinate: Longitude
            }

            fileWriter.Write(fileWriter.BaseStream.Position); // TileBlockHeader: CharactersOffsetInBytes

        }
    }

    public static void Main(string[] args)
    {
        Options? arguments = null;
        var argParseResult =
            Parser.Default.ParseArguments<Options>(args).WithParsed(options => { arguments = options; });

        if (argParseResult.Errors.Any())
        {
            Environment.Exit(-1);
        }

        var mapData = LoadOSMFile(arguments!.OsmPbfFilePath);
        var usedNodes = new HashSet<long>();
        var features = mapData.Ways.Select(way =>
        {
            var properties = way.Tags.ToDictionary(x => x.Key, x => x.Value);
            var result = new MapFeature
            {
                Id = way.Id,
                Label = string.Empty,
                Type = MapFeature.GeometryType.Polyline,
                Properties = null,
                Coordinates = way.NodeIds.Select(nodeId =>
                {
                    var node = mapData.Nodes[nodeId];

                    foreach (var (key, value) in node.Tags)
                        if (!properties.ContainsKey(key))
                        {
                            properties[key] = value;
                        }
                    usedNodes.Add(nodeId);

                    return new Coordinate(node.Latitude, node.Longitude);
                }).ToArray()
            };
            result.Properties = properties;

            if (result.Coordinates[0] == result.Coordinates[^1])
            {
                result.Type = MapFeature.GeometryType.Polygon;
            }

            if (result.Properties.TryGetValue("name", out var value))
            {
                result.Label = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            }
            ;

            return result;
        }).ToList();

        foreach (var (nodeId, node) in mapData.Nodes.Where(n => !usedNodes.Contains(n.Key)))
        {
            var nodeFeature = new MapFeature
            {
                Id = nodeId,
                Label = string.Empty,
                Type = MapFeature.GeometryType.Point,
                Properties = node.Tags.ToDictionary(tag => tag.Key, tag => tag.Value),
                Coordinates = new[] { new(node.Latitude, node.Longitude) }
            };
            if (nodeFeature.Properties.TryGetValue("name", out var value))
            {
                nodeFeature.Label = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            }
            ;
            features.Add(nodeFeature);
        }

        features.Save(arguments!.OutputFilePath!);
    }

    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input osm.pbf file")]
        public string? OsmPbfFilePath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output binary file")]
        public string? OutputFilePath { get; set; }
    }

    private readonly struct MapData
    {
        public ImmutableDictionary<long, AbstractNode> Nodes { get; init; }
        public ImmutableDictionary<int, List<long>> Tiles { get; init; }
        public ImmutableArray<Way> Ways { get; init; }
    }
}
