using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 多边形耳切三角化工具。
/// </summary>
public static class EarClipper
{
    /// <summary>
    /// 将二维多边形三角化。
    /// </summary>
    /// <param name="poly">多边形顶点列表。</param>
    /// <returns>三角形索引列表。</returns>
    public static List<int> Triangulate(List<Vector2> poly)
    {
        var result = new List<int>();
        var indices = Enumerable.Range(0, poly.Count).ToList();
        if (SignedArea(poly) < 0) indices.Reverse();

        int max = poly.Count * poly.Count + 10;
        for (int iter = 0; indices.Count > 3 && iter < max; iter++)
        {
            bool found = false;

            // 寻找一个凸耳并裁剪掉该顶点。
            for (int i = 0; i < indices.Count; i++)
            {
                int p = indices[(i - 1 + indices.Count) % indices.Count];
                int c = indices[i];
                int n = indices[(i + 1) % indices.Count];
                if (!IsConvex(poly[p], poly[c], poly[n])) continue;
                if (AnyInside(poly, indices, p, c, n)) continue;
                result.Add(p);
                result.Add(c);
                result.Add(n);
                indices.RemoveAt(i);
                found = true;
                break;
            }

            if (!found) break;
        }

        if (indices.Count == 3)
        {
            result.Add(indices[0]);
            result.Add(indices[1]);
            result.Add(indices[2]);
        }

        return result;
    }

    // 计算多边形有向面积。
    private static float SignedArea(List<Vector2> p)
    {
        float a = 0;
        for (int i = 0, j = p.Count - 1; i < p.Count; j = i++)
            a += (p[j].x + p[i].x) * (p[j].y - p[i].y);
        return a / 2;
    }

    // 判断三个点是否形成凸角。
    private static bool IsConvex(Vector2 p, Vector2 c, Vector2 n)
    {
        var d1 = c - p;
        var d2 = n - c;
        return d1.x * d2.y - d1.y * d2.x >= 0;
    }

    // 判断其他顶点是否落在候选耳三角形内部。
    private static bool AnyInside(List<Vector2> poly, List<int> idx, int a, int b, int c)
    {
        foreach (int i in idx)
        {
            if (i == a || i == b || i == c) continue;
            if (InTriangle(poly[i], poly[a], poly[b], poly[c])) return true;
        }

        return false;
    }

    // 判断点是否在三角形内部。
    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b), d2 = Sign(p, b, c), d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0, hasPos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNeg && hasPos);
    }

    // 计算点相对有向边的符号面积。
    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
}
