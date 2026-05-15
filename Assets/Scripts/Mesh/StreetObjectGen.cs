using UnityEngine;

/// <summary>
/// 街道物体生成器。
/// </summary>
public static class StreetObjectGen
{
    /// <summary>
    /// 根据街道物体数据创建游戏对象。
    /// </summary>
    /// <param name="s">街道物体数据。</param>
    /// <param name="mat">物体材质。</param>
    /// <returns>街道物体根对象。</returns>
    public static GameObject Create(StreetObject s, Material mat)
    {
        var root = new GameObject(s.Name);
        root.transform.position = new Vector3(s.PosX, 0, s.PosZ);
        var n = s.Name.ToLower();

        // 根据识别名称选择简单几何体组合。
        if (n.Contains("lamp") || n.Contains("路灯"))
        {
            Cylinder(root, s.HeightM * .9f, .08f, mat);
            Sphere(root, s.HeightM, .3f, mat);
        }
        else if (n.Contains("bench") || n.Contains("长椅"))
        {
            Box(root, 0, s.HeightM * .55f, 0, s.LengthM, .06f, s.WidthM, mat);
        }
        else
        {
            Box(root, 0, s.HeightM / 2, 0, s.LengthM, s.HeightM, s.WidthM, mat);
        }

        return root;
    }

    // 创建立方体部件。
    private static void Box(GameObject r, float x, float y, float z, float w, float h, float d, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(r.transform);
        go.transform.localPosition = new Vector3(x, y, z);
        go.transform.localScale = new Vector3(w, h, d);
        go.GetComponent<MeshRenderer>().material = m;
    }

    // 创建圆柱部件。
    private static void Cylinder(GameObject r, float h, float rad, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(r.transform);
        go.transform.localPosition = new Vector3(0, h / 2, 0);
        go.transform.localScale = new Vector3(rad * 2, h / 2, rad * 2);
        go.GetComponent<MeshRenderer>().material = m;
    }

    // 创建球体部件。
    private static void Sphere(GameObject r, float y, float rad, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(r.transform);
        go.transform.localPosition = new Vector3(0, y, 0);
        go.transform.localScale = Vector3.one * rad * 2;
        go.GetComponent<MeshRenderer>().material = m;
    }
}
