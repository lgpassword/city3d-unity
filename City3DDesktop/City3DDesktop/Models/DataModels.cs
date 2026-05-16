namespace City3DDesktop.Models;

public class GpsCoordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public GpsCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public override string ToString() => $"{Latitude}, {Longitude}";
}

public class LocationRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SceneRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OsmData
{
    public List<Building> Buildings { get; set; } = new();
    public List<Road> Roads { get; set; } = new();
    public List<Terrain> Terrains { get; set; } = new();
}

public class Building
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GpsCoordinate> Coordinates { get; set; } = new();
    public double Height { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class Road
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GpsCoordinate> Coordinates { get; set; } = new();
    public string Type { get; set; } = string.Empty;
}

public class Terrain
{
    public GpsCoordinate Coordinate { get; set; } = new(0, 0);
    public double Elevation { get; set; }
}
