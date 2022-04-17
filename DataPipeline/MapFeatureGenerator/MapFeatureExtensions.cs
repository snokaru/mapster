using Mapster.Common;

namespace MapFeatureGenerator;

public static class MapFeatureExtensions
{
    // Serializes one feature to a binary representation and writes it out to a file
    public static void WriteBinary(this MapFeature feature, BinaryWriter bWriter)
    {
        bWriter.Write(feature.Id);
        bWriter.Write(feature.Label);
        bWriter.Write((byte)feature.Type);
        bWriter.Write(feature.Coordinates.Length);
        foreach (var coord in feature.Coordinates)
        {
            bWriter.Write(coord.Latitude);
            bWriter.Write(coord.Longitude);
        }

        bWriter.Write(feature.Properties.Count);
        foreach (var entry in feature.Properties)
        {
            bWriter.Write(entry.Key);
            bWriter.Write(entry.Value);
        }
    }

    // Writes a list of features to a file
    public static void Save(this IEnumerable<MapFeature> features, ReadOnlySpan<char> path)
    {
        // Writes a placeholder header into the file to reserve the necessary space for the real header
        void WriteDummyHeader(BinaryWriter bWriter, Dictionary<int, List<MapFeature>> data)
        {
            foreach (var entry in data)
            {
                bWriter.Write(entry.Key);
                bWriter.Write((long)0);
            }
        }

        long globalId = 0;
        var organizedData = new Dictionary<int, List<MapFeature>>();
        var offsets = new Dictionary<int, long>();
        foreach (var feature in features)
        {
            globalId = OrganizeFeature(feature, organizedData, globalId);
        }

        {
            using var fStream = File.OpenWrite(path.ToString());
            fStream.SetLength(0);
            using var bWriter = new BinaryWriter(fStream);

            bWriter.Write(organizedData.Count); // Number of entries in the header.
            WriteDummyHeader(bWriter, organizedData); // Allocates the space in the file for the header.
            foreach (var (tile, featureList) in organizedData)
            {
                // Take note of the offset for this entry
                offsets[tile] = fStream.Position;
                bWriter.Write(featureList.Count); // Number of features in the tile.
                foreach (var feature in featureList)
                {
                    feature.WriteBinary(bWriter);
                }
            }

            bWriter.Flush();

            // Seek back to the beginning of the file and overwrite the dummy header with actual data
            fStream.Seek(sizeof(int), SeekOrigin.Begin);
            foreach (var entry in offsets)
            {
                bWriter.Write(entry.Key);
                bWriter.Write(entry.Value);
            }
        }
    }

    public static void SaveGeoJson(this IEnumerable<MapFeature> features, ReadOnlySpan<char> path, int limit = int.MaxValue)
    {
        long globalId = 0;
        var organizedData = new Dictionary<int, List<MapFeature>>();
        var offsets = new Dictionary<int, long>();
        foreach (var feature in features)
        {
            globalId = OrganizeFeature(feature, organizedData, globalId);
        }

        {
            using var fStream = File.OpenWrite(path.ToString());
            fStream.SetLength(0);
            using var writer = new StreamWriter(fStream);

            writer.WriteLine("{ \"type\": \"FeatureCollection\", \"features\": [");
            int counter = 0;
            foreach (var (_, featureList) in organizedData)
            {
                if (counter == limit)
                {
                    break;
                }

                foreach (var feature in featureList)
                {
                    if (counter > 0)
                    {
                        writer.WriteLine(",");
                    }

                    writer.WriteLine(
                        "{ \"type\": \"Feature\", \"properties\": [");
                    bool isFirstProperty = true;
                    foreach (var (key, value) in feature.Properties)
                    {
                        if (!isFirstProperty)
                        {
                            writer.Write(",");
                        }
                        else
                        {
                            isFirstProperty = false;
                        }
                        writer.WriteLine($"{{\"{key}\": \"{value}\"}}");
                    }
                    writer.WriteLine($"], \"geometry\": {{ \"type\": \"{(feature.Type == MapFeature.GeometryType.Polygon ? "Polygon" : "LineString")}\", \"coordinates\": [");
                    if (feature.Type == MapFeature.GeometryType.Polygon)
                    {
                        writer.WriteLine("[");
                    }

                    bool isFirstCoordinate = true;
                    foreach (var coordinate in feature.Coordinates)
                    {
                        if (!isFirstCoordinate)
                        {
                            writer.Write(",");
                        }
                        else
                        {
                            isFirstCoordinate = false;
                        }

                        writer.WriteLine($"[{coordinate.Longitude},{coordinate.Latitude}]");
                    }
                    if (feature.Type == MapFeature.GeometryType.Polygon)
                    {
                        writer.WriteLine("]");
                    }
                    writer.WriteLine("]}}");

                    ++counter;
                    if (counter == limit)
                    {
                        break;
                    }
                }
            }
            writer.WriteLine("]}");

            writer.Flush();
        }
    }

    // Organize and split features into their respective tiles
    private static long OrganizeFeature(MapFeature feature, Dictionary<int, List<MapFeature>> storage, long guid)
    {
        Dictionary<int, MapFeature> splitByTile = new Dictionary<int, MapFeature>();
        foreach (var coord in feature.Coordinates)
        {
            var tileId = TiligSystem.GetTile(coord);
            MapFeature newFeature = new MapFeature();
            newFeature.Properties = feature.Properties;
            newFeature.Label = feature.Label;
            newFeature.Id = guid;
            newFeature.Type = feature.Type;
            if (splitByTile.ContainsKey(tileId))
            {
                newFeature.Coordinates = new Coordinate[splitByTile[tileId].Coordinates.Length + 1];
                Array.Copy(splitByTile[tileId].Coordinates, newFeature.Coordinates,
                    splitByTile[tileId].Coordinates.Length);
                newFeature.Coordinates[splitByTile[tileId].Coordinates.Length] = coord;
                splitByTile[tileId] = newFeature;
            }
            else
            {
                newFeature.Coordinates = new Coordinate[1];
                newFeature.Coordinates[0] = coord;
                splitByTile[tileId] = newFeature;
            }
        }

        foreach (var (tile, feat) in splitByTile)
        {
            if (storage.TryGetValue(tile, out var featureList))
            {
                featureList.Add(feat);
            }
            else
            {
                storage[tile] = new List<MapFeature> { feat };
            }
        }

        return guid + 1;
    }
}