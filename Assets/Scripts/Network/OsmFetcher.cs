using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// OSM Overpass 建筑数据查询器。
/// </summary>
public class OsmFetcher
{
    // 复用 HTTP 客户端访问 Overpass API。
    private readonly HttpClient _http;

    // 数据库管理器用于读取和保存 OSM 缓存。
    private readonly DatabaseManager _db;

    // 应用配置。
    private readonly AppConfig _config;

    /// <summary>
    /// 创建 OSM 查询器。
    /// </summary>
    /// <param name="db">数据库管理器。</param>
    /// <param name="config">应用配置。</param>
    public OsmFetcher(DatabaseManager db, AppConfig config)
    {
        _db = db;
        _config = config;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(config.httpTimeoutSeconds)
        };
    }

    /// <summary>
    /// 查询指定位置半径内的建筑数据。
    /// </summary>
    /// <param name="lat">中心纬度。</param>
    /// <param name="lon">中心经度。</param>
    /// <param name="radiusM">查询半径，单位米。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>建筑数据列表。</returns>
    public async Task<List<BuildingData>> FetchAsync(double lat, double lon, int radiusM, CancellationToken cancellationToken = default)
    {
        // 先查本地缓存，命中后避免重复访问外部 API。
        var cached = _db.GetOsmCache(lat, lon, radiusM);
        if (cached != null)
        {
            Debug.Log("[OSM] 从缓存加载");
            return Newtonsoft.Json.JsonConvert
                .DeserializeObject<List<BuildingData>>(cached)
                ?? new List<BuildingData>();
        }

        var query = $"[out:json][timeout:{_config.osmQueryTimeoutSeconds}];(way[\"building\"]" +
                    $"(around:{radiusM},{lat},{lon}););out geom;";
        try
        {
            // 使用表单提交 Overpass 查询语句。
            var content = new FormUrlEncodedContent(
                new[] { new KeyValuePair<string, string>("data", query) });
            var resp = await _http.PostAsync(_config.osmOverpassUrl, content, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            var result = Parse(json);
            _db.SaveOsmCache(lat, lon, radiusM,
                Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[OSM] 操作已取消");
            throw;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[OSM] 查询失败 ({lat:F4},{lon:F4}, 半径{radiusM}m)：{ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null)
                Debug.LogWarning($"[OSM] 内部异常：{ex.InnerException.Message}");
            return Fallback(lat, lon);
        }
    }

    // 解析 Overpass 返回的建筑元素。
    private List<BuildingData> Parse(string json)
    {
        var result = new List<BuildingData>();
        var elems = (JObject.Parse(json)["elements"] as JArray) ?? new JArray();

        foreach (var el in elems.Take(_config.maxBuildingCount))
        {
            var tags = el["tags"] as JObject ?? new JObject();
            var geo = el["geometry"] as JArray;
            if (geo == null || geo.Count < 3) continue;

            // 提取建筑轮廓点并计算中心、宽深和高度。
            var pts = geo.Select(g => new GpsPoint(
                g["lat"]!.Value<double>(), g["lon"]!.Value<double>())).ToList();
            var lats = pts.Select(p => p.Lat).ToList();
            var lons = pts.Select(p => p.Lon).ToList();
            double cLat = lats.Average(), cLon = lons.Average();
            double mLon = _config.earthMetersPerDegreeLon * Math.Cos(cLat * Math.PI / 180);
            double w = Math.Max(_config.minBuildingSize, (lons.Max() - lons.Min()) * mLon);
            double d = Math.Max(_config.minBuildingSize, (lats.Max() - lats.Min()) * _config.earthMetersPerDegreeLat);
            int fl = tags["building:levels"]?.Value<int>() ?? 0;
            double h = tags["height"]?.Value<double>() ?? 0;
            if (h == 0) h = fl > 0 ? fl * _config.defaultFloorHeight : 8 + new System.Random().NextDouble() * 24;

            result.Add(new BuildingData
            {
                Name = tags["name"]?.ToString() ?? "Building",
                CentroidLat = cLat,
                CentroidLon = cLon,
                WidthM = w,
                DepthM = d,
                HeightM = h,
                Floors = Math.Max(1, (int)(h / _config.defaultFloorHeight)),
                Footprint = pts
            });
        }

        return result;
    }

    // 生成断网或查询失败时使用的示例建筑。
    private List<BuildingData> Fallback(double lat, double lon)
    {
        var rng = new System.Random();
        return Enumerable.Range(0, 8).Select(i =>
        {
            double ilat = lat + (rng.NextDouble() - .5) * .004;
            double ilon = lon + (rng.NextDouble() - .5) * .004;
            double h = 8 + rng.NextDouble() * 40;
            double w = 8 + rng.NextDouble() * 20;
            double d = 8 + rng.NextDouble() * 16;
            double mLon = _config.earthMetersPerDegreeLon * Math.Cos(ilat * Math.PI / 180);
            double hw = w / 2 / mLon;
            double hd = d / 2 / _config.earthMetersPerDegreeLat;
            return new BuildingData
            {
                Name = $"示例楼{i + 1}",
                CentroidLat = ilat,
                CentroidLon = ilon,
                WidthM = w,
                DepthM = d,
                HeightM = h,
                Floors = Math.Max(1, (int)(h / _config.defaultFloorHeight)),
                Footprint = new List<GpsPoint>
                {
                    new(ilat - hd, ilon - hw),
                    new(ilat - hd, ilon + hw),
                    new(ilat + hd, ilon + hw),
                    new(ilat + hd, ilon - hw)
                }
            };
        }).ToList();
    }
}
