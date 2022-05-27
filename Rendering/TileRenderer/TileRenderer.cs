using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public static class TileRenderer
{
    public static BaseShape Tessellate(this MapFeatureData feature, ref BoundingBox boundingBox, ref PriorityQueue<BaseShape, int> shapes)
    {
        BaseShape? baseShape = null;

        var featureType = feature.Type;
        var coordinates = feature.Coordinates;
        if (Road.isRoad(feature))
        {
            baseShape = new Road(coordinates);
        }
        else if (Waterway.isWaterway(feature))
        {
            baseShape = new Waterway(coordinates, feature.Type == GeometryType.Polygon);
        }
        else if (Border.isBorder(feature))
        {
            baseShape = new Border(coordinates);
        }
        else if (PopulatedPlace.isPopulatedPlace(feature))
        {
            baseShape = new PopulatedPlace(coordinates, feature);
        }
        else if (Railway.isRailway(feature))
        {
            baseShape = new Railway(coordinates);
        }
        else if (GeoFeature.isNatural(feature))
        {
            baseShape = new GeoFeature(coordinates, feature);
        }
        else if (GeoFeature.isForest(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
        }
        else if (GeoFeature.isLanduseForestOrOrchad(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
        }
        else if (GeoFeature.isLanduseResidential(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
        }
        else if (GeoFeature.isLandusePlain(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Plain);
        }
        else if (GeoFeature.isWater(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Water);
        }
        else if (GeoFeature.isBuilding(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
        }
        else if (GeoFeature.isLeisure(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
        }
        else if (GeoFeature.isAmenity(feature))
        {
            baseShape = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
        }

        if (baseShape != null)
        {
            shapes.Enqueue(baseShape, baseShape.ZIndex);
            for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j)
            {
                boundingBox.MinX = Math.Min(boundingBox.MinX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MaxX = Math.Max(boundingBox.MaxX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MinY = Math.Min(boundingBox.MinY, baseShape.ScreenCoordinates[j].Y);
                boundingBox.MaxY = Math.Max(boundingBox.MaxY, baseShape.ScreenCoordinates[j].Y);
            }
        }

        return baseShape;
    }

    public static Image<Rgba32> Render(this PriorityQueue<BaseShape, int> shapes, BoundingBox boundingBox, int width, int height)
    {
        var canvas = new Image<Rgba32>(width, height);

        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (boundingBox.MaxX - boundingBox.MinX);
        var scaleY = canvas.Height / (boundingBox.MaxY - boundingBox.MinY);
        var scale = Math.Min(scaleX, scaleY);

        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));
        while (shapes.Count > 0)
        {
            var entry = shapes.Dequeue();
            entry.TranslateAndScale(boundingBox.MinX, boundingBox.MinY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }

    public struct BoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
