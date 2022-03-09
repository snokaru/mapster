using Lab6.Utilities;
using Lab6.Endpoints;

namespace Lab6;

internal static class Program
{
    public static (Coordinate[], Pixel[]) GetCoordinates(string path,
        out (float minX, float maxX, float minY, float maxY) pixelLimits)
    {
        // Open the coordinates file for reading
        using var coordinateFileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(coordinateFileStream);

        // Set up out result containers
        var resultCoordinates = new List<Coordinate>();
        var resultPixels = new List<Pixel>();

        // Initialize the min and max values
        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minY = float.MaxValue;
        var maxY = float.MinValue;

        // Read the first line and discard it since it's a table header
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            // Read the line and split it by the <TAB> character
            var line = reader.ReadLine()!.Split('\t');
            // Create a new coordinate instance and add it to the result list
            var c = new Coordinate
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

    // Set up Main as an async method
    private static async Task Main(string[] args)
    {
        var (coordinates, pixels) =
            GetCoordinates(@"/var/home/kicsyromy/workspace/Lab6/coordinates.txt", out var pixelLimits);

        // Set up a builder to help register our endpoint handlers
        var appBuilder = WebApplication.CreateBuilder();
        // Register TileEndpoint as a singleton instance that will be lazily instantiated
        appBuilder.Services.AddSingleton<TileEndpoint>(_ => new TileEndpoint(coordinates, pixels, pixelLimits));

        // Create the application instance
        var app = appBuilder.Build();
        // Set up to serve on localhost's unpriviledged 8080 port
        app.Urls.Add("http://localhost:8080");

        // Map HTTP GET requests to TileEndpoint
        // Set up the request as parameterized on 'boundingBox', 'width' and 'height'
        app.MapGet("/getTile/{boundingBox}/{width}x{height}", HandleTileRequest);
        async Task HandleTileRequest(HttpContext context, string boundingBox, int width, int height, TileEndpoint tileEndpoint)
        {
            await tileEndpoint.SendPngResponse(context, width, height);
        }

        await app.RunAsync();
    }
}