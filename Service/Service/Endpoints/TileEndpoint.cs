using Mapster.Common;
using Mapster.Rendering;
using SixLabors.ImageSharp;
using Newtonsoft.Json.Bson;

namespace Mapster.Service.Endpoints;

internal class TileEndpoint
{
    Dictionary<int, MapFeature[]> _tileSet = new Dictionary<int, MapFeature[]>();

    public TileEndpoint(string mapDataPath)
    {
        foreach (var binaryFile in Directory.EnumerateFiles(mapDataPath))
        {
            LoadBinaryData(binaryFile, _tileSet);
        }
    }

    public static void Register(WebApplication app)
    {
        // Map HTTP GET requests to this
        // Set up the request as parameterized on 'boundingBox', 'width' and 'height'
        app.MapGet("/render", HandleTileRequest);
        async Task HandleTileRequest(HttpContext context, double minLat, double minLon, double maxLat, double maxLon, int? size, TileEndpoint tileEndpoint)
        {
            if (size == null)
            {
                size = 800;
            }

            var tileIds = TiligSystem.GetTilesForBoundingBox(minLat, minLon, maxLat, maxLon);

            var dummyStream = new MemoryStream();
            context.Response.ContentType = "application/bson";
            await tileEndpoint.SerializeBson(context.Response.BodyWriter.AsStream(), tileIds, size.Value);
        }
    }

    private async Task SerializeBson(Stream outputStream, int[] tileIds, int size)
    {
        var responseWriter = new BsonDataWriter(outputStream);

        await responseWriter.WriteStartObjectAsync();
        {
            // Used to store encoded image data
            var imageMemoryStream = new MemoryStream();

            // Write the number of tiles
            await responseWriter.WritePropertyNameAsync("tileCount");
            await responseWriter.WriteValueAsync(tileIds.Length);

            await responseWriter.WritePropertyNameAsync("imageData");
            await responseWriter.WriteStartArrayAsync();
            {
                for (int i = 0; i < tileIds.Length; ++i)
                {
                    imageMemoryStream.Position = 0;
                    await RenderPng(imageMemoryStream, tileIds[i], size, size);
                    await responseWriter.WriteValueAsync(imageMemoryStream.ToArray());
                }
            }
            await responseWriter.WriteEndArrayAsync();
        }
        await responseWriter.WriteEndObjectAsync();
    }

    private async Task RenderPng(Stream outputStream, int tileId, int width, int height)
    {
        KeyValuePair<int, MapFeature[]> tileData = new KeyValuePair<int, MapFeature[]>(tileId, new MapFeature[0]);
        if (_tileSet.TryGetValue(tileId, out var features))
        {
            tileData = new KeyValuePair<int, MapFeature[]>(tileId, features);
        }

        var canvas = await Task.Run(() =>
        {
            return tileData.Render(width, height);
        });

        await canvas.SaveAsPngAsync(outputStream);
    }

    private static Dictionary<int, MapFeature[]> LoadBinaryData(string path, Dictionary<int, MapFeature[]> result)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var tileCount = reader.ReadInt32();

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
                var featureType = (MapFeature.GeometryType)reader.ReadByte();
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
}