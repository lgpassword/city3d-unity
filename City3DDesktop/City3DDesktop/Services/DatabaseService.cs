using System.Data.SQLite;
using City3DDesktop.Models;

namespace City3DDesktop.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string dbPath = "city3d.db")
    {
        _connectionString = $"Data Source={dbPath};Version=3;";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string createLocationsTable = @"
            CREATE TABLE IF NOT EXISTS Locations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                CreatedAt TEXT NOT NULL
            )";

        string createScenesTable = @"
            CREATE TABLE IF NOT EXISTS Scenes (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                ImagePath TEXT NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Radius REAL NOT NULL,
                CreatedAt TEXT NOT NULL
            )";

        using (var cmd = new SQLiteCommand(createLocationsTable, connection))
        {
            cmd.ExecuteNonQuery();
        }

        using (var cmd = new SQLiteCommand(createScenesTable, connection))
        {
            cmd.ExecuteNonQuery();
        }
    }

    public void SaveLocation(LocationRecord location)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string sql = @"INSERT INTO Locations (Name, Latitude, Longitude, CreatedAt)
                      VALUES (@Name, @Latitude, @Longitude, @CreatedAt)";

        using var cmd = new SQLiteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Name", location.Name);
        cmd.Parameters.AddWithValue("@Latitude", location.Latitude);
        cmd.Parameters.AddWithValue("@Longitude", location.Longitude);
        cmd.Parameters.AddWithValue("@CreatedAt", location.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public List<LocationRecord> GetAllLocations()
    {
        var locations = new List<LocationRecord>();

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string sql = "SELECT * FROM Locations ORDER BY CreatedAt DESC";

        using var cmd = new SQLiteCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            locations.Add(new LocationRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Latitude = reader.GetDouble(2),
                Longitude = reader.GetDouble(3),
                CreatedAt = DateTime.Parse(reader.GetString(4))
            });
        }

        return locations;
    }

    public void SaveScene(SceneRecord scene)
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string sql = @"INSERT INTO Scenes (Name, ImagePath, Latitude, Longitude, Radius, CreatedAt)
                      VALUES (@Name, @ImagePath, @Latitude, @Longitude, @Radius, @CreatedAt)";

        using var cmd = new SQLiteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Name", scene.Name);
        cmd.Parameters.AddWithValue("@ImagePath", scene.ImagePath);
        cmd.Parameters.AddWithValue("@Latitude", scene.Latitude);
        cmd.Parameters.AddWithValue("@Longitude", scene.Longitude);
        cmd.Parameters.AddWithValue("@Radius", scene.Radius);
        cmd.Parameters.AddWithValue("@CreatedAt", scene.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public List<SceneRecord> GetAllScenes()
    {
        var scenes = new List<SceneRecord>();

        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        string sql = "SELECT * FROM Scenes ORDER BY CreatedAt DESC";

        using var cmd = new SQLiteCommand(sql, connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            scenes.Add(new SceneRecord
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ImagePath = reader.GetString(2),
                Latitude = reader.GetDouble(3),
                Longitude = reader.GetDouble(4),
                Radius = reader.GetDouble(5),
                CreatedAt = DateTime.Parse(reader.GetString(6))
            });
        }

        return scenes;
    }
}
