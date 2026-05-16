using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace City3DDesktop.Services;

public class ElevationService
{
    private readonly HttpClient _httpClient;
    private const string ElevationApiUrl = "https://api.open-elevation.com/api/v1/lookup";

    public ElevationService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<double> GetElevation(double lat, double lon, CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"{ElevationApiUrl}?locations={lat},{lon}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var root = JObject.Parse(jsonResponse);
            var results = root["results"] as JArray;

            if (results != null && results.Count > 0)
            {
                return results[0]["elevation"]?.Value<double>() ?? 0.0;
            }

            return 0.0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"海拔数据获取失败: {ex.Message}");
            return 0.0;
        }
    }

    public async Task<Dictionary<(double, double), double>> GetElevationGrid(
        double centerLat,
        double centerLon,
        double radiusMeters,
        int gridSize = 10,
        CancellationToken cancellationToken = default)
    {
        var elevations = new Dictionary<(double, double), double>();

        try
        {
            // 计算网格点
            double radiusDegrees = radiusMeters / 111320.0;
            double step = (radiusDegrees * 2) / gridSize;

            var locations = new List<(double lat, double lon)>();
            for (int i = 0; i <= gridSize; i++)
            {
                for (int j = 0; j <= gridSize; j++)
                {
                    double lat = centerLat - radiusDegrees + (i * step);
                    double lon = centerLon - radiusDegrees + (j * step);
                    locations.Add((lat, lon));
                }
            }

            // 批量查询（API限制每次最多100个点）
            for (int i = 0; i < locations.Count; i += 100)
            {
                var batch = locations.Skip(i).Take(100).ToList();
                var batchElevations = await GetElevationBatch(batch, cancellationToken);

                foreach (var kvp in batchElevations)
                {
                    elevations[kvp.Key] = kvp.Value;
                }

                // 避免API限流
                if (i + 100 < locations.Count)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"海拔网格数据获取失败: {ex.Message}");
        }

        return elevations;
    }

    private async Task<Dictionary<(double, double), double>> GetElevationBatch(
        List<(double lat, double lon)> locations,
        CancellationToken cancellationToken)
    {
        var elevations = new Dictionary<(double, double), double>();

        try
        {
            string locationsParam = string.Join("|", locations.Select(l => $"{l.lat},{l.lon}"));
            string url = $"{ElevationApiUrl}?locations={locationsParam}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var root = JObject.Parse(jsonResponse);
            var results = root["results"] as JArray;

            if (results != null)
            {
                for (int i = 0; i < results.Count && i < locations.Count; i++)
                {
                    double elevation = results[i]["elevation"]?.Value<double>() ?? 0.0;
                    elevations[locations[i]] = elevation;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"批量海拔数据获取失败: {ex.Message}");
        }

        return elevations;
    }
}
