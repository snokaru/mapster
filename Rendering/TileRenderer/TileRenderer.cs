using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public static class TileRenderer
{
    public static Image<Rgba32> Render(this MapFeature feature, string label, ReadOnlySpan<Coordinate> coordinates, int width, int height)
    {
        var features = new PriorityQueue<BaseShape, int>();

        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;

        BaseShape? baseShape = null;
        if (tile.Value[i].Properties
            .Any(p => p.Key == "highway" && MapFeature.HighwayTypes.Any(v => p.Value.StartsWith(v))))
        {
            var coordinates = tile.Value[i].Coordinates;
            var road = new Road(coordinates);
            baseShape = road;
            features.Enqueue(road, road.ZIndex);
        }

        else if (tile.Value[i].Properties.Any(p => p.Key.StartsWith("water")) &&
                 tile.Value[i].Type != MapFeature.GeometryType.Point)
        {
            var coordinates = tile.Value[i].Coordinates;

            var waterway = new Waterway(coordinates, tile.Value[i].Type == MapFeature.GeometryType.Polygon);
            baseShape = waterway;
            features.Enqueue(waterway, waterway.ZIndex);
        }
        else if (Border.ShouldBeBorder(tile.Value[i]))
        {
            var coordinates = tile.Value[i].Coordinates;
            var border = new Border(coordinates);
            baseShape = border;
            features.Enqueue(border, border.ZIndex);
        }

        else if (PopulatedPlace.ShouldBePopulatedPlace(tile.Value[i]))
        {
            var coordinates = tile.Value[i].Coordinates;
            var popPlace = new PopulatedPlace(coordinates, tile.Value[i]);
            baseShape = popPlace;
            features.Enqueue(popPlace, popPlace.ZIndex);
        }

        else if (tile.Value[i].Properties.Any(p => p.Key.StartsWith("railway")))
        {
            var coordinates = tile.Value[i].Coordinates;
            var railway = new Railway(coordinates);
            baseShape = railway;
            features.Enqueue(railway, railway.ZIndex);
        }

        else if (tile.Value[i].Properties.Any(p =>
                     p.Key.StartsWith("natural") && tile.Value[i].Type == MapFeature.GeometryType.Polygon))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, tile.Value[i]);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Properties.Any(p => p.Key.StartsWith("boundary") && p.Value.StartsWith("forest")))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Properties.Any(p =>
                     p.Key.StartsWith("landuse") &&
                     (p.Value.StartsWith("forest") || p.Value.StartsWith("orchard"))))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon && tile.Value[i].Properties.Any(p =>
                     p.Key.StartsWith("landuse") && (p.Value.StartsWith("residential") ||
                                                     p.Value.StartsWith("cemetery") ||
                                                     p.Value.StartsWith("industrial") ||
                                                     p.Value.StartsWith("commercial") ||
                                                     p.Value.StartsWith("square") ||
                                                     p.Value.StartsWith("construction") ||
                                                     p.Value.StartsWith("military") ||
                                                     p.Value.StartsWith("quarry") ||
                                                     p.Value.StartsWith("brownfield"))))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon && tile.Value[i].Properties.Any(p =>
                     p.Key.StartsWith("landuse") && (p.Value.StartsWith("farm") || p.Value.StartsWith("meadow") ||
                                                     p.Value.StartsWith("grass") ||
                                                     p.Value.StartsWith("greenfield") ||
                                                     p.Value.StartsWith("recreation_ground") ||
                                                     p.Value.StartsWith("winter_sports") ||
                                                     p.Value.StartsWith("allotments"))))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Plain);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon && tile.Value[i].Properties.Any(p =>
                     p.Key.StartsWith("landuse") &&
                     (p.Value.StartsWith("reservoir") || p.Value.StartsWith("basin"))))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Water);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon &&
                 tile.Value[i].Properties.Any(p => p.Key.StartsWith("building")))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon &&
                 tile.Value[i].Properties.Any(p => p.Key.StartsWith("leisure")))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }

        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon &&
                 tile.Value[i].Properties.Any(p => p.Key.StartsWith("amenity")))
        {
            var coordinates = tile.Value[i].Coordinates;
            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
            baseShape = geoFeature;
            features.Enqueue(geoFeature, geoFeature.ZIndex);
        }


        else if (tile.Value[i].Type == MapFeature.GeometryType.Polygon)
        {
        }

        if (baseShape != null)
        {
            for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j)
            {
                minX = Math.Min(minX, baseShape.ScreenCoordinates[j].X);
                maxX = Math.Max(maxX, baseShape.ScreenCoordinates[j].X);
                minY = Math.Min(minY, baseShape.ScreenCoordinates[j].Y);
                maxY = Math.Max(maxY, baseShape.ScreenCoordinates[j].Y);
            }
        }

        var canvas = new Image<Rgba32>(width, height);
        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (maxX - minX);
        var scaleY = canvas.Height / (maxY - minY);
        var scale = Math.Min(scaleX, scaleY);


        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));

        while (features.Count > 0)
        {
            var entry = features.Dequeue();
            entry.TranslateAndScale(minX, minY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }
}
