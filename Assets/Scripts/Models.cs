using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPS 经纬度坐标。
/// </summary>
[Serializable]
public class GpsCoordinate
{
    // 纬度。
    public double Latitude;

    // 经度。
    public double Longitude;

    /// <summary>
    /// 创建默认 GPS 坐标。
    /// </summary>
    public GpsCoordinate() {}

    /// <summary>
    /// 使用纬度和经度创建 GPS 坐标。
    /// </summary>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    public GpsCoordinate(double lat, double lon)
    {
        Latitude = lat;
        Longitude = lon;
    }
}

/// <summary>
/// 建筑轮廓中的 GPS 点。
/// </summary>
[Serializable]
public class GpsPoint
{
    // 纬度。
    public double Lat;

    // 经度。
    public double Lon;

    /// <summary>
    /// 创建默认 GPS 点。
    /// </summary>
    public GpsPoint() {}

    /// <summary>
    /// 使用纬度和经度创建 GPS 点。
    /// </summary>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    public GpsPoint(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }
}

/// <summary>
/// 建筑数据模型。
/// </summary>
[Serializable]
public class BuildingData
{
    // 建筑名称。
    public string Name = "";

    // 建筑中心点纬度。
    public double CentroidLat;

    // 建筑中心点经度。
    public double CentroidLon;

    // 建筑宽度，单位米。
    public double WidthM;

    // 建筑深度，单位米。
    public double DepthM;

    // 建筑高度，单位米。
    public double HeightM;

    // 建筑楼层数。
    public int Floors = 1;

    // 建筑轮廓点列表。
    public List<GpsPoint> Footprint = new();
}

/// <summary>
/// 程序化地形配置。
/// </summary>
[Serializable]
public class TerrainConfig
{
    // 地形尺寸，单位米。
    public float SizeM = 400;

    // 最大高度，单位米。
    public float MaxHeightM = 20;

    // 网格分辨率。
    public int Resolution = 24;

    // 随机种子。
    public int Seed;
}

/// <summary>
/// 街道物体数据模型。
/// </summary>
[Serializable]
public class StreetObject
{
    // 物体名称。
    public string Name = "";

    // 物体长度，单位米。
    public float LengthM;

    // 物体宽度，单位米。
    public float WidthM;

    // 物体高度，单位米。
    public float HeightM;

    // 本地 X 坐标。
    public float PosX;

    // 本地 Z 坐标。
    public float PosZ;
}

/// <summary>
/// 城市场景数据模型。
/// </summary>
[Serializable]
public class CityScene
{
    // 场景中心 GPS 坐标。
    public GpsCoordinate Center = new();

    // 建筑列表。
    public List<BuildingData> Buildings = new();

    // 地形配置。
    public TerrainConfig Terrain = new();

    // 街道物体列表。
    public List<StreetObject> StreetObjects = new();

    // 场景海拔，单位米。
    public double ElevationM;
}

/// <summary>
/// AI 识别结果。
/// </summary>
public class RecognizedObject
{
    // 识别名称。
    public string Name;

    // 识别类别。
    public string Category;

    // 置信度。
    public double Confidence;

    /// <summary>
    /// 创建 AI 识别结果。
    /// </summary>
    /// <param name="n">识别名称。</param>
    /// <param name="c">识别类别。</param>
    /// <param name="conf">置信度。</param>
    public RecognizedObject(string n, string c, double conf)
    {
        Name = n;
        Category = c;
        Confidence = conf;
    }
}
