using System.Collections.Generic;
using Newtonsoft.Json;

namespace City3DDesktop.Models;

/// <summary>
/// GPS坐标（用于场景中心点）
/// </summary>
public class GpsCoordinate
{
    /// <summary>纬度</summary>
    [JsonProperty("Latitude")]
    public double Latitude { get; set; }

    /// <summary>经度</summary>
    [JsonProperty("Longitude")]
    public double Longitude { get; set; }
}

/// <summary>
/// GPS点（用于轮廓、路径等坐标列表）
/// </summary>
public class GpsPoint
{
    /// <summary>纬度</summary>
    [JsonProperty("Lat")]
    public double Lat { get; set; }

    /// <summary>经度</summary>
    [JsonProperty("Lon")]
    public double Lon { get; set; }
}

/// <summary>
/// 建筑数据
/// </summary>
public class BuildingData
{
    /// <summary>建筑名称</summary>
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>质心纬度</summary>
    [JsonProperty("CentroidLat")]
    public double CentroidLat { get; set; }

    /// <summary>质心经度</summary>
    [JsonProperty("CentroidLon")]
    public double CentroidLon { get; set; }

    /// <summary>宽度（米）</summary>
    [JsonProperty("WidthM")]
    public double WidthM { get; set; }

    /// <summary>进深（米）</summary>
    [JsonProperty("DepthM")]
    public double DepthM { get; set; }

    /// <summary>高度（米）</summary>
    [JsonProperty("HeightM")]
    public double HeightM { get; set; }

    /// <summary>楼层数</summary>
    [JsonProperty("Floors")]
    public int Floors { get; set; }

    /// <summary>建筑轮廓坐标点列表</summary>
    [JsonProperty("Footprint")]
    public List<GpsPoint> Footprint { get; set; } = new();
}

/// <summary>
/// 地形配置
/// </summary>
public class TerrainConfig
{
    /// <summary>地形尺寸（米）</summary>
    [JsonProperty("SizeM")]
    public double SizeM { get; set; }

    /// <summary>最大高度（米）</summary>
    [JsonProperty("MaxHeightM")]
    public double MaxHeightM { get; set; }

    /// <summary>分辨率</summary>
    [JsonProperty("Resolution")]
    public int Resolution { get; set; }

    /// <summary>随机种子</summary>
    [JsonProperty("Seed")]
    public int Seed { get; set; }
}

/// <summary>
/// 道路数据
/// </summary>
public class RoadData
{
    /// <summary>道路名称</summary>
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>道路类型（trunk/primary/secondary等）</summary>
    [JsonProperty("RoadType")]
    public string RoadType { get; set; } = string.Empty;

    /// <summary>路径坐标点列表</summary>
    [JsonProperty("Points")]
    public List<GpsPoint> Points { get; set; } = new();

    /// <summary>道路宽度（米）</summary>
    [JsonProperty("WidthM")]
    public double WidthM { get; set; }

    /// <summary>车道数</summary>
    [JsonProperty("Lanes")]
    public int Lanes { get; set; }

    /// <summary>是否单行道</summary>
    [JsonProperty("OneWay")]
    public bool OneWay { get; set; }
}

/// <summary>
/// 水域数据
/// </summary>
public class WaterData
{
    /// <summary>水域名称</summary>
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>水域类型（river/lake等）</summary>
    [JsonProperty("WaterType")]
    public string WaterType { get; set; } = string.Empty;

    /// <summary>水域边界坐标点列表</summary>
    [JsonProperty("Boundary")]
    public List<GpsPoint> Boundary { get; set; } = new();
}

/// <summary>
/// 绿地数据
/// </summary>
public class GreenData
{
    /// <summary>绿地名称</summary>
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>绿地类型（park/garden等）</summary>
    [JsonProperty("GreenType")]
    public string GreenType { get; set; } = string.Empty;

    /// <summary>绿地边界坐标点列表</summary>
    [JsonProperty("Boundary")]
    public List<GpsPoint> Boundary { get; set; } = new();
}

/// <summary>
/// 兴趣点数据
/// </summary>
public class POIData
{
    /// <summary>POI名称</summary>
    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>分类（museum/landmark/park/shopping等）</summary>
    [JsonProperty("Category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>纬度</summary>
    [JsonProperty("Lat")]
    public double Lat { get; set; }

    /// <summary>经度</summary>
    [JsonProperty("Lon")]
    public double Lon { get; set; }

    /// <summary>图标名称</summary>
    [JsonProperty("IconName")]
    public string IconName { get; set; } = string.Empty;

    /// <summary>附加属性（键值对）</summary>
    [JsonProperty("Properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}

/// <summary>
/// 数字孪生场景（顶层数据结构，对应 beijing_center.json）
/// </summary>
public class DigitalTwinScene
{
    /// <summary>场景中心点GPS坐标</summary>
    [JsonProperty("Center")]
    public GpsCoordinate Center { get; set; } = new();

    /// <summary>海拔高度（米）</summary>
    [JsonProperty("ElevationM")]
    public double ElevationM { get; set; }

    /// <summary>地形配置</summary>
    [JsonProperty("Terrain")]
    public TerrainConfig Terrain { get; set; } = new();

    /// <summary>建筑列表</summary>
    [JsonProperty("Buildings")]
    public List<BuildingData> Buildings { get; set; } = new();

    /// <summary>道路列表</summary>
    [JsonProperty("Roads")]
    public List<RoadData> Roads { get; set; } = new();

    /// <summary>水域列表</summary>
    [JsonProperty("Waters")]
    public List<WaterData> Waters { get; set; } = new();

    /// <summary>绿地列表</summary>
    [JsonProperty("Greens")]
    public List<GreenData> Greens { get; set; } = new();

    /// <summary>兴趣点列表</summary>
    [JsonProperty("POIs")]
    public List<POIData> POIs { get; set; } = new();

    /// <summary>街道设施对象列表（预留）</summary>
    [JsonProperty("StreetObjects")]
    public List<object> StreetObjects { get; set; } = new();
}
