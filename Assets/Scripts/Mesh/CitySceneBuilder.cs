using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 城市场景构建器。
/// </summary>
public class CitySceneBuilder : MonoBehaviour
{
    [Header("材质")]
    // 建筑墙体材质。
    public Material buildingMat;

    // 玻璃材质。
    public Material glassMat;

    // 地形材质。
    public Material terrainMat;

    // 地面材质。
    public Material groundMat;

    // 街道物体材质。
    public Material streetMat;

    // 当前已生成对象列表。
    private readonly List<GameObject> _objects = new();

    /// <summary>
    /// 构建完整城市场景。
    /// </summary>
    /// <param name="scene">城市场景数据。</param>
    public void Build(CityScene scene)
    {
        // 清理上一轮生成的对象。
        foreach (var go in _objects) Destroy(go);
        _objects.Clear();

        double cLat = scene.Center.Latitude;
        double cLon = scene.Center.Longitude;

        // 创建基础地面。
        var gnd = GameObject.CreatePrimitive(PrimitiveType.Plane);
        gnd.name = "Ground";
        gnd.transform.localScale = Vector3.one * 80;
        gnd.GetComponent<MeshRenderer>().material = groundMat;
        _objects.Add(gnd);

        // 创建建筑群。
        foreach (var b in scene.Buildings)
        {
            var go = BuildingMeshGen.Create(b, cLat, cLon, buildingMat, glassMat);
            _objects.Add(go);
        }

        // 创建程序化地形。
        _objects.Add(TerrainGen.Create(scene.Terrain, terrainMat));

        // 创建街道物体。
        foreach (var s in scene.StreetObjects)
            _objects.Add(StreetObjectGen.Create(s, streetMat));

        Debug.Log($"[城市三维] 场景已构建：{scene.Buildings.Count} 栋建筑");
    }
}
