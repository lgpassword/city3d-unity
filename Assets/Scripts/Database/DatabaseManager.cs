using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 本地 SQLite 数据库管理器。
/// </summary>
public class DatabaseManager
{
    // SQLite 连接实例。
    private SQLiteConnection _db;

    /// <summary>
    /// 初始化数据库连接、数据表和默认产品规格。
    /// </summary>
    public void Initialize()
    {
        var path = Path.Combine(Application.persistentDataPath, "city3d.db");
        _db = new SQLiteConnection(path);
        _db.CreateTable<LocationRecord>();
        _db.CreateTable<SceneRecord>();
        _db.CreateTable<OsmCacheRecord>();
        _db.CreateTable<ProductSpecRecord>();
        SeedSpecs();
        Debug.Log($"[数据库] 数据库就绪：{path}");
    }

    // 初始化默认产品规格，避免重复插入。
    private void SeedSpecs()
    {
        if (_db.Table<ProductSpecRecord>().Count() > 0) return;

        _db.InsertAll(new[]
        {
            new ProductSpecRecord { NamePattern = "street lamp", LengthM = 0.3f, WidthM = 0.3f, HeightM = 6.0f },
            new ProductSpecRecord { NamePattern = "路灯", LengthM = 0.3f, WidthM = 0.3f, HeightM = 6.0f },
            new ProductSpecRecord { NamePattern = "bench", LengthM = 1.8f, WidthM = 0.55f, HeightM = 0.85f },
            new ProductSpecRecord { NamePattern = "长椅", LengthM = 1.8f, WidthM = 0.55f, HeightM = 0.85f },
            new ProductSpecRecord { NamePattern = "car", LengthM = 4.5f, WidthM = 1.8f, HeightM = 1.5f },
            new ProductSpecRecord { NamePattern = "汽车", LengthM = 4.5f, WidthM = 1.8f, HeightM = 1.5f },
            new ProductSpecRecord { NamePattern = "bus", LengthM = 12f, WidthM = 2.5f, HeightM = 3.2f },
        });
    }

    /// <summary>
    /// 保存收藏位置。
    /// </summary>
    /// <param name="name">位置名称。</param>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    /// <param name="r">查询半径，单位米。</param>
    public void SaveLocation(string name, double lat, double lon, int r)
        => _db.Insert(new LocationRecord { Name = name, Latitude = lat, Longitude = lon, RadiusM = r });

    /// <summary>
    /// 获取最近保存的位置列表。
    /// </summary>
    /// <returns>位置记录列表。</returns>
    public List<LocationRecord> GetLocations()
        => _db.Table<LocationRecord>().OrderByDescending(x => x.Id).Take(30).ToList();

    /// <summary>
    /// 删除指定位置记录。
    /// </summary>
    /// <param name="id">位置主键。</param>
    public void DeleteLocation(int id)
        => _db.Delete<LocationRecord>(id);

    /// <summary>
    /// 保存城市场景。
    /// </summary>
    /// <param name="name">场景名称。</param>
    /// <param name="lat">中心纬度。</param>
    /// <param name="lon">中心经度。</param>
    /// <param name="buildings">建筑数量。</param>
    /// <param name="elev">海拔，单位米。</param>
    /// <param name="json">场景 JSON 数据。</param>
    public void SaveScene(string name, double lat, double lon, int buildings, double elev, string json)
        => _db.Insert(new SceneRecord
        {
            Name = name,
            Latitude = lat,
            Longitude = lon,
            Buildings = buildings,
            ElevationM = elev,
            SceneJson = json
        });

    /// <summary>
    /// 获取最近保存的场景列表。
    /// </summary>
    /// <returns>场景记录列表。</returns>
    public List<SceneRecord> GetScenes()
        => _db.Table<SceneRecord>().OrderByDescending(x => x.Id).Take(20).ToList();

    /// <summary>
    /// 获取指定场景的 JSON 数据。
    /// </summary>
    /// <param name="id">场景主键。</param>
    /// <returns>场景 JSON 数据，未找到时返回空。</returns>
    public string GetSceneJson(int id)
        => _db.Find<SceneRecord>(id)?.SceneJson;

    /// <summary>
    /// 删除指定场景记录。
    /// </summary>
    /// <param name="id">场景主键。</param>
    public void DeleteScene(int id)
        => _db.Delete<SceneRecord>(id);

    /// <summary>
    /// 读取 OSM 查询缓存。
    /// </summary>
    /// <param name="lat">中心纬度。</param>
    /// <param name="lon">中心经度。</param>
    /// <param name="r">查询半径，单位米。</param>
    /// <returns>缓存 JSON 数据，未命中时返回空。</returns>
    public string GetOsmCache(double lat, double lon, int r)
    {
        double g = 0.002;
        var now = DateTime.Now.ToString("s");

        // 使用经纬度近似网格和半径判断缓存是否可复用。
        return _db.Table<OsmCacheRecord>()
            .Where(x => Math.Abs(x.CenterLat - lat) < g &&
                        Math.Abs(x.CenterLon - lon) < g &&
                        x.RadiusM == r &&
                        x.ExpireAt.CompareTo(now) > 0)
            .OrderByDescending(x => x.Id)
            .FirstOrDefault()?.DataJson;
    }

    /// <summary>
    /// 保存 OSM 查询缓存。
    /// </summary>
    /// <param name="lat">中心纬度。</param>
    /// <param name="lon">中心经度。</param>
    /// <param name="r">查询半径，单位米。</param>
    /// <param name="json">OSM 数据 JSON。</param>
    public void SaveOsmCache(double lat, double lon, int r, string json)
        => _db.Insert(new OsmCacheRecord
        {
            CenterLat = lat,
            CenterLon = lon,
            RadiusM = r,
            DataJson = json,
            ExpireAt = DateTime.Now.AddHours(48).ToString("s")
        });

    /// <summary>
    /// 根据名称匹配产品规格。
    /// </summary>
    /// <param name="name">识别出的物体名称。</param>
    /// <returns>长度、宽度和高度。</returns>
    public (float l, float w, float h) GetSpec(string name)
    {
        var n = name.ToLower();

        // 按名称模式查找最接近的产品尺寸。
        foreach (var s in _db.Table<ProductSpecRecord>().ToList())
            if (n.Contains(s.NamePattern.ToLower()))
                return (s.LengthM, s.WidthM, s.HeightM);

        return (1f, 1f, 1.5f);
    }
}
