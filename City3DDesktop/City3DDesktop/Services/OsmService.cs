using System.Net.Http;
using Newtonsoft.Json.Linq;
using City3DDesktop.Models;

namespace City3DDesktop.Services;

public class OsmService
{
    private readonly HttpClient _httpClient;
    private const string OverpassApiUrl = "https://overpass-api.de/api/interpreter";

    public OsmService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<OsmData> FetchOsmData(double lat, double lon, double radiusMeters, CancellationToken cancellationToken = default)
    {
        try
        {
            // 将米转换为度数（近似）
            double radiusDegrees = radiusMeters / 111320.0;

            string query = $@"
                [out:json][timeout:25];
                (
                  way[""building""]({lat - radiusDegrees},{lon - radiusDegrees},{lat + radiusDegrees},{lon + radiusDegrees});
                  way[""highway""]({lat - radiusDegrees},{lon - radiusDegrees},{lat + radiusDegrees},{lon + radiusDegrees});
                );
                out body;
                >;
                out skel qt;
            ";

            var content = new StringContent(query);
            var response = await _httpClient.PostAsync(OverpassApiUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseOsmData(jsonResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OSM数据获取失败: {ex.Message}");
            return new OsmData();
        }
    }

    private OsmData ParseOsmData(string json)
    {
        var osmData = new OsmData();
        var root = JObject.Parse(json);
        var elements = root["elements"] as JArray;

        if (elements == null) return osmData;

        // 创建节点字典
        var nodes = new Dictionary<long, GpsCoordinate>();
        foreach (var element in elements)
        {
            if (element["type"]?.ToString() == "node")
            {
                long id = element["id"]?.Value<long>() ?? 0;
                double lat = element["lat"]?.Value<double>() ?? 0;
                double lon = element["lon"]?.Value<double>() ?? 0;
                nodes[id] = new GpsCoordinate(lat, lon);
            }
        }

        // 解析建筑物和道路
        foreach (var element in elements)
        {
            if (element["type"]?.ToString() != "way") continue;

            var tags = element["tags"] as JObject;
            if (tags == null) continue;

            var nodeIds = element["nodes"] as JArray;
            if (nodeIds == null) continue;

            var coordinates = new List<GpsCoordinate>();
            foreach (var nodeId in nodeIds)
            {
                long id = nodeId.Value<long>();
                if (nodes.ContainsKey(id))
                {
                    coordinates.Add(nodes[id]);
                }
            }

            if (coordinates.Count == 0) continue;

            if (tags["building"] != null)
            {
                osmData.Buildings.Add(new Building
                {
                    Id = element["id"]?.ToString() ?? "",
                    Name = tags["name"]?.ToString() ?? "",
                    Coordinates = coordinates,
                    Height = ParseHeight(tags["height"]?.ToString()),
                    Type = tags["building"]?.ToString() ?? "yes"
                });
            }
            else if (tags["highway"] != null)
            {
                osmData.Roads.Add(new Road
                {
                    Id = element["id"]?.ToString() ?? "",
                    Name = tags["name"]?.ToString() ?? "",
                    Coordinates = coordinates,
                    Type = tags["highway"]?.ToString() ?? "road"
                });
            }
        }

        return osmData;
    }

    private double ParseHeight(string? heightStr)
    {
        if (string.IsNullOrEmpty(heightStr)) return 10.0; // 默认高度

        // 尝试解析数字
        if (double.TryParse(heightStr.Replace("m", "").Trim(), out double height))
        {
            return height;
        }

        return 10.0;
    }
}
