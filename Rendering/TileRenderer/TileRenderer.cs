using Mapster.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace TileRenderer;

public static class TileRenderer
{
    public static Image<Rgba32> Render(this KeyValuePair<int, MapFeature[]> tile, int width, int height)
    {
        var shapes = new Pixel[tile.Value.Length][];
        
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;

        for (int i = 0; i < tile.Value.Length; ++i)
        {
            var coordinates = tile.Value[i].Coordinates;
            shapes[i] = new Pixel[coordinates.Length];

            for (int j = 0; j < coordinates.Length; ++j)
            {
                var pixel = new Pixel(coordinates[j]);
                minX = Math.Min(minX, pixel.X);
                minY = Math.Min(minY, pixel.Y);
                shapes[i][j] = pixel;
            }
        }

        for (int i = 0; i < shapes.Length; ++i)
        {
            for (int j = 0; j < shapes[i].Length; ++j)
            {
                shapes[i][j].X = shapes[i][j].X - minX;
                shapes[i][j].Y = shapes[i][j].Y - minY;
                
                maxX = Math.Max(maxX, shapes[i][j].X);
                maxY = Math.Max(maxY, shapes[i][j].Y);
            }
        }
        
        var canvas = new Image<Rgba32>(width, height);
        
        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / maxX;
        var scaleY = canvas.Height / maxY;
        var scale = Math.Min(scaleX, scaleY);

        for (int i = 0 ; i < shapes.Length; ++i)
        {
            var shape = shapes[i];
            var points = shape.Select(p => new PointF(p.X * scale, canvas.Height - p.Y * scale)).ToArray();
            if (points.Length < 2)
            {
                continue;
            }
            
            // Build out the path that describes our polygon
            var outline = new PathBuilder();
            // Apply a scaling transformation to each pixel by using the .Select() LINQ extension method (equivalent to map() in JS or Rust)
            outline.AddLines(points);
            
            // Render the polygon to the canvas
            canvas.Mutate(context =>
            {
                if (tile.Value[i].Type == MapFeature.GeometryType.Polygon)
                {
                    context.Fill(Color.DarkCyan, outline.Build());
                }
                else
                {
                    var pen = new Pen(Color.Black, 5.0f);
                    context.DrawLines(pen, points);
                }
            });
        }

        return canvas;
    }
}