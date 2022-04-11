using Mapster.Common;

namespace TileRenderer;

public struct Pixel
{
    public float X { get; set; }
    public float Y { get; set; }

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

