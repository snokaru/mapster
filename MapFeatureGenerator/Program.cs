using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommandLine;
using Mapster.Common;
using OSMDataParser;
using OSMDataParser.Elements;

namespace MapFeatureGenerator;

public static class Program
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Input osm.pbf file")]
        public string? OsmPbfFilePath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output binary file")]
        public string? OutputFilePath { get; set; }
    }

    readonly struct MapData
    {
        public ImmutableDictionary<long, AbstractNode> Nodes { get; init; }
        public ImmutableArray<Way> Ways { get; init; }
    }

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
                        {
                            switch (primitiveGroup.ContainedType)
                            {
                                case PrimitiveGroup.ElementType.Node:
                                    foreach (var node in primitiveGroup)
                                    {
                                        nodes[node.Id] = (AbstractNode)node;
                                    }
                                    break;

                                case PrimitiveGroup.ElementType.Way:
                                    foreach (var way in primitiveGroup)
                                    {
                                        ways.Add((Way)way);
                                    }
                                    break;
                            }
                        }
                        break;
                    }
            }
        });

        return new MapData() { Nodes = nodes.ToImmutableDictionary(), Ways = ways.ToImmutableArray() };
    }

    public static void Main(string[] args)
    {
        Options? arguments = null;
        var argParseResult = Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
        {
            arguments = options;
        });

        if (argParseResult.Errors.Any())
        {
            Environment.Exit(-1);
        }

        var mapData = LoadOSMFile(arguments!.OsmPbfFilePath);
        mapData.Ways.Select(way =>
        {
            var result = new MapFeature()
            {
                Id = way.Id,
                Label = string.Empty,
                Type = MapFeature.GeometryType.Polyline,
                Properties = way.Tags.ToDictionary(x => x.Key, x => x.Value),
                Coordinates = way.NodeIds.Select(nodeId =>
                {
                    var node = mapData.Nodes[nodeId];
                    return new Coordinate(node.Latitude, node.Longitude);
                }).ToArray(),
            };

            if (result.Coordinates[0] == result.Coordinates[^1])
            {
                result.Type = MapFeature.GeometryType.Polygon;
            }
            
            if (result.Properties.TryGetValue("name", out var value))
            {
                result.Label = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            };

            return result;
        }).Save(arguments!.OutputFilePath!);
    }
}