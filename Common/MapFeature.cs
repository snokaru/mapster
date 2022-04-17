namespace Mapster.Common;

public struct MapFeature
{
    // https://wiki.openstreetmap.org/wiki/Key:highway
    public static string[] HighwayTypes = new string[] {
        "motorway", "trunk", "primary", "secondary", "tertiary", "unclassified", "residential", "road"
    };

    public enum GeometryType : byte
    {
        Polyline,
        Polygon,
        Point,
    }

    public long Id { get; set; }
    public string Label { get; set; }
    public Coordinate[] Coordinates { get; set; }
    public GeometryType Type { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}