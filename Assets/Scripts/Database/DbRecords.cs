using SQLite;
using System;

/// <summary>
/// 收藏位置数据库记录。
/// </summary>
[Table("locations")]
public class LocationRecord
{
    // 位置主键。
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 位置名称。
    public string Name { get; set; } = "";

    // 纬度。
    public double Latitude { get; set; }

    // 经度。
    public double Longitude { get; set; }

    // 查询半径，单位米。
    public int RadiusM { get; set; } = 300;

    // 创建时间。
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 返回位置记录的显示文本。
    /// </summary>
    /// <returns>位置名称和坐标文本。</returns>
    public override string ToString() => $"{Name} ({Latitude:F4},{Longitude:F4})";
}

/// <summary>
/// 保存场景数据库记录。
/// </summary>
[Table("saved_scenes")]
public class SceneRecord
{
    // 场景主键。
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 场景名称。
    public string Name { get; set; } = "";

    // 场景中心纬度。
    public double Latitude { get; set; }

    // 场景中心经度。
    public double Longitude { get; set; }

    // 建筑数量。
    public int Buildings { get; set; }

    // 海拔，单位米。
    public double ElevationM { get; set; }

    // 场景 JSON 数据。
    public string SceneJson { get; set; } = "";

    // 创建时间。
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    /// <summary>
    /// 返回场景记录的显示文本。
    /// </summary>
    /// <returns>场景名称、建筑数量和日期文本。</returns>
    public override string ToString() => $"{Name}，{Buildings} 栋（{CreatedAt[..10]}）";
}

/// <summary>
/// OSM 查询缓存数据库记录。
/// </summary>
[Table("osm_cache")]
public class OsmCacheRecord
{
    // 缓存主键。
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 缓存中心纬度。
    public double CenterLat { get; set; }

    // 缓存中心经度。
    public double CenterLon { get; set; }

    // 查询半径，单位米。
    public int RadiusM { get; set; }

    // OSM 数据 JSON。
    public string DataJson { get; set; } = "";

    // 缓存过期时间。
    public string ExpireAt { get; set; } = "";
}

/// <summary>
/// 标准产品规格数据库记录。
/// </summary>
[Table("product_specs")]
public class ProductSpecRecord
{
    // 规格主键。
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // 名称匹配模式。
    public string NamePattern { get; set; } = "";

    // 长度，单位米。
    public float LengthM { get; set; }

    // 宽度，单位米。
    public float WidthM { get; set; }

    // 高度，单位米。
    public float HeightM { get; set; }
}
