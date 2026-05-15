using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPS 坐标与 Unity 本地坐标转换工具。
/// </summary>
public static class GpsConverter
{
    // 根据纬度计算每经度对应的米数。
    private static double MPerLon(double lat) => 111320 * Math.Cos(lat * Math.PI / 180);

    /// <summary>
    /// 将经纬度转换为以中心点为原点的本地二维坐标。
    /// </summary>
    /// <param name="lat">目标纬度。</param>
    /// <param name="lon">目标经度。</param>
    /// <param name="cLat">中心纬度。</param>
    /// <param name="cLon">中心经度。</param>
    /// <returns>Unity 平面坐标。</returns>
    public static Vector2 ToLocal(double lat, double lon, double cLat, double cLon)
        => new((float)((lon - cLon) * MPerLon(cLat)), (float)(-(lat - cLat) * 110540));

    /// <summary>
    /// 将建筑轮廓 GPS 点转换为本地二维坐标列表。
    /// </summary>
    /// <param name="fp">建筑轮廓 GPS 点。</param>
    /// <param name="cLat">中心纬度。</param>
    /// <param name="cLon">中心经度。</param>
    /// <returns>本地二维坐标列表。</returns>
    public static List<Vector2> FootprintToLocal(List<GpsPoint> fp, double cLat, double cLon)
    {
        var result = new List<Vector2>();
        foreach (var p in fp)
            result.Add(ToLocal(p.Lat, p.Lon, cLat, cLon));
        return result;
    }
}
