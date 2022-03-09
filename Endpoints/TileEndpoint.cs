using Lab6.Utilities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Lab6.Endpoints;

class TileEndpoint
{
    private readonly Coordinate[] _coordinates;
    private readonly Pixel[] _pixels;
    private readonly (float minX, float maxX, float minY, float maxY) _pixelLimits;

    public TileEndpoint(Coordinate[] coordinates, Pixel[] pixels, (float, float, float, float) pixelLimits)
    {
        _coordinates = coordinates;
        _pixels = pixels;
        _pixelLimits = pixelLimits;
    }

    // Main request handler, async method
    public async Task SendPngResponse(HttpContext context, int width, int height)
    {
        var response = context.Response;

        var canvas = new Image<Rgb24>(width, height);
        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (_pixelLimits.maxX - _pixelLimits.minX);
        var scaleY = canvas.Height / (_pixelLimits.maxY - _pixelLimits.minY);
        var scale = Math.Min(scaleX, scaleY);

        // Run potentially CPU intensive work as a CPU-bound task
        await Task.Run(() => {
            // Build out the path that describes our polygon
            var outline = new PathBuilder();
            // Apply a scaling transformation to each pixel by using the .Select() LINQ extension method (equivalent to map() in JS or Rust)
            outline.AddLines(_pixels.Select(p => new PointF((Math.Abs(_pixelLimits.minX) + p.X) * scale, canvas.Height - (Math.Abs(_pixelLimits.minY) + p.Y) * scale)));

            // Render the polygon to the canvas
            canvas.Mutate(context => context.Fill(Color.BurlyWood).Fill(Color.DarkCyan, outline.Build()));
        });

        // Set the content type of the response as "image/png"
        response.ContentType = "image/png";
        // Use non-blocking async I/O to send the response back to the client
        await canvas.SaveAsPngAsync(response.BodyWriter.AsStream());
    }
}