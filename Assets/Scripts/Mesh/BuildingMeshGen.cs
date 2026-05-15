using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 建筑网格生成器。
/// </summary>
public static class BuildingMeshGen
{
    /// <summary>
    /// 根据建筑数据生成建筑游戏对象。
    /// </summary>
    /// <param name="b">建筑数据。</param>
    /// <param name="cLat">场景中心纬度。</param>
    /// <param name="cLon">场景中心经度。</param>
    /// <param name="wallMat">墙体材质。</param>
    /// <param name="glassMat">玻璃材质。</param>
    /// <returns>建筑根对象。</returns>
    public static GameObject Create(BuildingData b, double cLat, double cLon, Material wallMat, Material glassMat)
    {
        var fp = GpsConverter.FootprintToLocal(b.Footprint, cLat, cLon);
        if (fp.Count < 3) return new GameObject(b.Name);

        var root = new GameObject(b.Name);
        var sel = root.AddComponent<BuildingSelector>();
        sel.data = b;

        // 创建建筑主体并赋予墙体材质。
        var body = CreateBody(b.Name, fp, (float)b.HeightM);
        body.transform.SetParent(root.transform);
        body.GetComponent<MeshRenderer>().material = wallMat;

        // 创建玻璃窗层并赋予玻璃材质。
        var wins = CreateWindows(fp, (float)b.HeightM, b.Floors);
        wins.transform.SetParent(root.transform);
        wins.GetComponent<MeshRenderer>().material = glassMat;

        return root;
    }

    // 创建建筑主体网格。
    private static GameObject CreateBody(string name, List<Vector2> fp, float h)
    {
        var go = new GameObject("Body");
        go.AddComponent<MeshCollider>();
        var mf = go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();

        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();
        int n = fp.Count;

        // 计算建筑轮廓重心，用于生成顶面和底面的扇形三角面。
        float cx = 0, cz = 0;
        foreach (var p in fp)
        {
            cx += p.x;
            cz += p.y;
        }
        cx /= n;
        cz /= n;

        // 顶面。
        int topBase = verts.Count;
        verts.Add(new Vector3(cx, h, cz));
        uvs.Add(Vector2.zero);
        foreach (var p in fp)
        {
            verts.Add(new Vector3(p.x, h, p.y));
            uvs.Add(new Vector2(p.x / 10, p.y / 10));
        }
        for (int i = 0; i < n; i++)
        {
            tris.Add(topBase);
            tris.Add(topBase + 1 + (i + 1) % n);
            tris.Add(topBase + 1 + i);
        }

        // 底面。
        int botBase = verts.Count;
        verts.Add(new Vector3(cx, 0, cz));
        uvs.Add(Vector2.zero);
        foreach (var p in fp)
        {
            verts.Add(new Vector3(p.x, 0, p.y));
            uvs.Add(new Vector2(p.x / 10, p.y / 10));
        }
        for (int i = 0; i < n; i++)
        {
            tris.Add(botBase);
            tris.Add(botBase + 1 + i);
            tris.Add(botBase + 1 + (i + 1) % n);
        }

        // 侧墙。
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            var b0 = new Vector3(fp[i].x, 0, fp[i].y);
            var b1 = new Vector3(fp[j].x, 0, fp[j].y);
            var t0 = new Vector3(fp[i].x, h, fp[i].y);
            var t1 = new Vector3(fp[j].x, h, fp[j].y);
            int idx = verts.Count;
            verts.AddRange(new[] { b0, b1, t1, t0 });
            uvs.AddRange(new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            });
            tris.AddRange(new[] { idx, idx + 1, idx + 2, idx, idx + 2, idx + 3 });
        }

        var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = go.GetComponent<MeshCollider>().sharedMesh = mesh;
        return go;
    }

    // 创建建筑窗户网格。
    private static GameObject CreateWindows(List<Vector2> fp, float h, int floors)
    {
        var go = new GameObject("Windows");
        var mf = go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        int n = fp.Count;
        float fh = h / Mathf.Max(floors, 1);
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var uvs = new List<Vector2>();

        // 在每一侧墙体和每一层上铺设一条窗户四边形。
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            var dir = new Vector2(fp[j].x - fp[i].x, fp[j].y - fp[i].y);
            float wlen = dir.magnitude;
            dir.Normalize();
            for (int f = 0; f < floors; f++)
            {
                float y0 = fh * f + fh * .25f, y1 = fh * f + fh * .72f;
                var p0 = new Vector3(fp[i].x, y0, fp[i].y) + new Vector3(dir.x, 0, dir.y) * .4f;
                var p1 = new Vector3(fp[i].x, y0, fp[i].y) + new Vector3(dir.x, 0, dir.y) * (wlen - .4f);
                var p2 = p1 + Vector3.up * (y1 - y0);
                var p3 = p0 + Vector3.up * (y1 - y0);
                int idx = verts.Count;
                verts.AddRange(new[] { p0, p1, p2, p3 });
                uvs.AddRange(new[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(0, 1)
                });
                tris.AddRange(new[] { idx, idx + 1, idx + 2, idx, idx + 2, idx + 3 });
            }
        }

        var mesh = new Mesh { name = "Windows", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        return go;
    }
}
