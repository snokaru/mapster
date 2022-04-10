using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security;
using Mapster.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using TileRenderer;

namespace TestTileRenderer;

[TestClass]
public class TestTileRenderer
{
    private static Dictionary<int, MapFeature[]> LoadBinaryData()
    {
        using var stream = File.OpenRead(@"MapData/andorra-10032022.bin");
        using var reader = new BinaryReader(stream);

        var tileCount = reader.ReadInt32();
        var result = new Dictionary<int, MapFeature[]>(tileCount);

        var tileOffsets = new (int tileId, long offset)[tileCount];
        for (int i = 0; i < tileCount; ++i)
        {
            tileOffsets[i] = (reader.ReadInt32(), reader.ReadInt64());
        }

        foreach (var (tileId, offset) in tileOffsets)
        {
            stream.Position = offset;

            var featureCount = reader.ReadInt32();
            var features = new MapFeature[featureCount];

            for (int i = 0; i < featureCount; ++i)
            {
                var featureId = reader.ReadInt64();
                var featureLabel = reader.ReadString();
                var featureType = (MapFeature.GeometryType) reader.ReadByte();
                var coordinates = new Coordinate[reader.ReadInt32()];
                for (int j = 0; j < coordinates.Length; ++j)
                {
                    coordinates[j] = new Coordinate(reader.ReadDouble(), reader.ReadDouble());
                }

                var propertyCount = reader.ReadInt32();
                var properties = new Dictionary<string, string>(propertyCount);
                for (int j = 0; j < propertyCount; ++j)
                {
                    properties[reader.ReadString()] = reader.ReadString();
                }

                features[i] = new MapFeature()
                {
                    Id = featureId,
                    Label = featureLabel,
                    Type = featureType,
                    Coordinates = coordinates,
                    Properties = properties,
                };
            }

            result[tileId] = features;
        }

        return result;
    }
    
    private static MapFeature[] GetCoordinates(string path)
    {
        // Open the coordinates file for reading
        using var coordinateFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(coordinateFileStream);

        // Set up out result containers
        var resultCoordinates = new List<Coordinate>();

        // Read the first line and discard it since it's a table header
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            // Read the line and split it by the <TAB> character
            var line = reader.ReadLine()!.Split('\t');
            // Create a new coordinate instance and add it to the result list
            var c = new Coordinate(double.Parse(line[1]),double.Parse(line[2]));
            resultCoordinates.Add(c);
        }

        // Return the two lists as a tuple of arrays
        return new MapFeature[] {new MapFeature() {Type = MapFeature.GeometryType.Polygon, Coordinates = resultCoordinates.ToArray() }};
    }
    
    [TestMethod]
    public void TestRendering()
    {
        var tiles = LoadBinaryData();
        foreach (var tile in tiles)
        {
            var image = tile.Render(2000, 2000);
            image.SaveAsPng($"/var/home/kicsyromy/workspace/dotnet_labs_2022/{tile.Key}.png");
        }
    }
}