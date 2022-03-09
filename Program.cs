using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RenderingProjection
{
    // Repesents a geographical coordinate
    public struct Coordinate
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    // Represents a pixel on a canvas
    public struct Pixel
    {
        public float X { get; init; }
        public float Y { get; init; }

        public Pixel()
        {
            X = 0;
            Y = 0;
        }

        public Pixel(Coordinate c)
        {
            X = (float)MercatorProjection.lonToX(c.Longitude);
            Y = (float)MercatorProjection.latToY(c.Latitude);
        }
    }

    public class Program
    {
        // Read coordinates from a file retuns them, as well as their equivalent pixels
        // Also calculates the pixel deltas for the X and Y axes
        public static (Coordinate[], Pixel[]) GetCoordinates(string path, out (float minX, float maxX, float minY, float maxY) pixelLimits)
        {
            // Open the coordinates file for reading
            using FileStream coordinateFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using StreamReader reader = new StreamReader(coordinateFileStream);

            // Set up out result containers
            List<Coordinate> resultCoordinates = new List<Coordinate>();
            List<Pixel> resultPixels = new List<Pixel>();

            // Initialize the min and max values
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            // Read the first line and discard it since it's a table header
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                // Read the line and split it by the <TAB> character
                var line = reader.ReadLine()!.Split('\t');
                // Create a new coordinate instance and add it to the result list
                var c = new Coordinate()
                {
                    Latitude = double.Parse(line[1]),
                    Longitude = double.Parse(line[2])
                };
                resultCoordinates.Add(c);

                // Create a Pixel instance and adjust the min and max values
                var pixel = new Pixel(c);
                minX = Math.Min(minX, pixel.X);
                maxX = Math.Max(maxX, pixel.X);
                minY = Math.Min(minY, pixel.Y);
                maxY = Math.Max(maxY, pixel.Y);

                // Add the pixel to the result list
                resultPixels.Add(pixel);
            }

            // Set the pixelLimits output variable to the appropriate values
            pixelLimits = (minX, maxX, minY, maxY);
            // Return the two lists as a tuple of arrays
            return (resultCoordinates.ToArray(), resultPixels.ToArray());
        }

        public static void Main()
        {
            var (coordinates, pixels) = GetCoordinates(@"<path_to_coordinates.txt>", out var pixelLimits);

            // Set up a canvas to draw on
            // The resulting image will use 3 color channels, R, G and B, each consisting of values from 0 -> 255
            Image<Rgb24> canvas = new Image<Rgb24>(800, 800);
            // Calculate the scale for each pixel, essentially applying a normalization
            var scaleX = canvas.Width / (pixelLimits.maxX - pixelLimits.minX);
            var scaleY = canvas.Height / (pixelLimits.maxY - pixelLimits.minY);
            var scale = Math.Min(scaleX, scaleY);

            // Build out the path that describes our polygon
            PathBuilder outline = new PathBuilder();
            // Apply a scaling transformation to each pixel by using the .Select() LINQ extension method (equivalent to map() in JS or Rust)
            outline.AddLines(pixels.Select(p => new PointF((Math.Abs(pixelLimits.minX) + p.X) * scale, canvas.Height - (Math.Abs(pixelLimits.minY) + p.Y) * scale)));

            // Render the polygon to the canvas
            canvas.Mutate(context => context.Fill(Color.BurlyWood).Fill(Color.DarkCyan, outline.Build()));

            // Save the final image as a .png
            canvas.SaveAsPng("rendered-tile.png");
        }
    }
}
