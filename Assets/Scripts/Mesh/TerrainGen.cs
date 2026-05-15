using UnityEngine;

/// <summary>
/// 程序化地形生成器。
/// </summary>
public static class TerrainGen
{
    /// <summary>
    /// 根据地形配置创建地形对象。
    /// </summary>
    /// <param name="cfg">地形配置。</param>
    /// <param name="mat">地形材质。</param>
    /// <returns>地形游戏对象。</returns>
    public static GameObject Create(TerrainConfig cfg, Material mat)
    {
        var go = new GameObject("Terrain");
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        mr.material = mat;

        int res = cfg.Resolution;
        float sz = cfg.SizeM, half = sz / 2;
        float cell = sz / (res - 1);
        var rng = new System.Random(cfg.Seed);

        var verts = new Vector3[res * res];
        var uvs = new Vector2[res * res];

        // 按网格采样生成山丘式高度场。
        for (int z = 0; z < res; z++)
        for (int x = 0; x < res; x++)
        {
            float nx = (float)x / (res - 1) * 2 - 1, nz = (float)z / (res - 1) * 2 - 1;
            float d = Mathf.Sqrt(nx * nx + nz * nz);
            float h = cfg.MaxHeightM *
                (0.7f * Mathf.Exp(-d * d * 6) +
                 0.4f * Mathf.Exp(-((nx - .2f) * (nx - .2f) + (nz + .1f) * (nz + .1f)) * 14));
            h += Mathf.Max(0, (float)(rng.NextDouble() - .5) * cfg.MaxHeightM * .1f);
            int i = z * res + x;
            verts[i] = new Vector3(-half + x * cell, Mathf.Max(0, h), -half + z * cell);
            uvs[i] = new Vector2((float)x / (res - 1), (float)z / (res - 1));
        }

        var tris = new int[(res - 1) * (res - 1) * 6];
        int t = 0;

        // 为每个网格单元生成两个三角形。
        for (int z = 0; z < res - 1; z++)
        for (int x = 0; x < res - 1; x++)
        {
            int i = z * res + x;
            tris[t++] = i;
            tris[t++] = i + res;
            tris[t++] = i + 1;
            tris[t++] = i + 1;
            tris[t++] = i + res;
            tris[t++] = i + res + 1;
        }

        var mesh = new Mesh
        {
            name = "Terrain",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
            vertices = verts,
            triangles = tris,
            uv = uvs
        };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        return go;
    }
}
