namespace Lab6.Utilities;

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