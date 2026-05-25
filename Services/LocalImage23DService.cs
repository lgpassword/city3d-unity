using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace City3DDesktop.Services;

/// <summary>
/// 基于传统计算机视觉算法的图片转3D服务
/// 使用像素深度图、边缘检测、轮廓挤出等算法
/// 完全本地运行，无需AI或网络
/// </summary>
public class LocalImage23DService
{
    public event Action<string, int>? ProgressChanged;

    public enum Algorithm
    {
        HeightMap,          // 高度图：灰度值→高度（适合任何图片，浮雕效果）
        ContourExtrusion,   // 轮廓挤出：边缘检测后挤出（适合简单物体）
        Relief,             // 浮雕：增强对比度的高度图（艺术效果）
        Voxel               // 体素化：基于像素的方块堆叠（像素艺术风格）
    }

    public class GenerationOptions
    {
        public Algorithm Algorithm { get; set; } = Algorithm.HeightMap;
        public int Resolution { get; set; } = 128;          // 网格分辨率
        public float MaxHeight { get; set; } = 1.0f;        // 最大高度（相对单位）
        public float Smoothness { get; set; } = 1.0f;       // 平滑度
        public bool Invert { get; set; } = false;           // 反转高度
        public float ContrastBoost { get; set; } = 1.0f;    // 对比度增强
    }

    /// <summary>
    /// 从图片生成3D模型（OBJ格式）
    /// </summary>
    public async Task<string> GenerateAsync(string imagePath, GenerationOptions options, CancellationToken ct = default)
    {
        Report("正在分析图片...", 5);

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"图片不存在: {imagePath}");

        return options.Algorithm switch
        {
            Algorithm.HeightMap => await GenerateHeightMapAsync(imagePath, options, ct),
            Algorithm.Relief => await GenerateReliefAsync(imagePath, options, ct),
            Algorithm.ContourExtrusion => await GenerateContourAsync(imagePath, options, ct),
            Algorithm.Voxel => await GenerateVoxelAsync(imagePath, options, ct),
            _ => await GenerateHeightMapAsync(imagePath, options, ct)
        };
    }

    /// <summary>
    /// 算法1：高度图法 - 将像素灰度值映射为3D高度
    /// 适合：任何图片，会产生类似浮雕/地形的效果
    /// </summary>
    private async Task<string> GenerateHeightMapAsync(string imagePath, GenerationOptions opt, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            Report("正在读取图像数据...", 10);
            using var bitmap = new Bitmap(imagePath);
            ct.ThrowIfCancellationRequested();

            // 缩放到指定分辨率（提升性能）
            Report("正在缩放图像...", 20);
            using var resized = ResizeImage(bitmap, opt.Resolution);

            Report("正在计算高度图...", 35);
            var heights = ExtractHeightMap(resized, opt);
            ct.ThrowIfCancellationRequested();

            Report("正在生成3D网格...", 60);
            var (vertices, indices, normals, uvs) = BuildHeightMapMesh(heights, opt);
            ct.ThrowIfCancellationRequested();

            Report("正在导出OBJ文件...", 85);
            var path = SaveAsObj(vertices, indices, normals, uvs, imagePath, "heightmap");

            Report("生成完成", 100);
            return path;
        }, ct);
    }

    /// <summary>
    /// 算法2：浮雕效果 - 增强对比度的高度图
    /// </summary>
    private async Task<string> GenerateReliefAsync(string imagePath, GenerationOptions opt, CancellationToken ct)
    {
        // 使用增强参数运行高度图算法
        var enhanced = new GenerationOptions
        {
            Algorithm = Algorithm.HeightMap,
            Resolution = Math.Max(opt.Resolution, 256),  // 浮雕需要更高分辨率
            MaxHeight = opt.MaxHeight * 0.3f,             // 浮雕通常较薄
            Smoothness = opt.Smoothness,
            Invert = opt.Invert,
            ContrastBoost = opt.ContrastBoost * 2.0f      // 增强对比度
        };
        return await GenerateHeightMapAsync(imagePath, enhanced, ct);
    }

    /// <summary>
    /// 算法3：体素化 - 像素艺术风格的3D
    /// </summary>
    private async Task<string> GenerateVoxelAsync(string imagePath, GenerationOptions opt, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            Report("正在读取图像...", 10);
            using var bitmap = new Bitmap(imagePath);

            // 体素分辨率较低（保持像素感）
            int voxelRes = Math.Min(opt.Resolution, 64);
            using var resized = ResizeImage(bitmap, voxelRes);

            Report("正在生成体素...", 40);
            var heights = ExtractHeightMap(resized, opt);

            Report("正在构建立方体网格...", 70);
            var (vertices, indices, normals, uvs) = BuildVoxelMesh(heights, opt);

            Report("正在导出...", 90);
            var path = SaveAsObj(vertices, indices, normals, uvs, imagePath, "voxel");

            Report("生成完成", 100);
            return path;
        }, ct);
    }

    /// <summary>
    /// 算法4：轮廓挤出 - 检测图片轮廓并挤出为3D
    /// </summary>
    private async Task<string> GenerateContourAsync(string imagePath, GenerationOptions opt, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            Report("正在读取图像...", 10);
            using var bitmap = new Bitmap(imagePath);
            using var resized = ResizeImage(bitmap, opt.Resolution);

            Report("正在边缘检测 (Sobel算子)...", 30);
            var edges = SobelEdgeDetection(resized);

            Report("正在二值化...", 50);
            var binary = Threshold(edges, 50);

            Report("正在挤出3D...", 70);
            var (vertices, indices, normals, uvs) = BuildExtrusionMesh(binary, opt);

            Report("正在导出...", 90);
            var path = SaveAsObj(vertices, indices, normals, uvs, imagePath, "contour");

            Report("生成完成", 100);
            return path;
        }, ct);
    }

    // ===== 图像处理工具方法 =====

    private Bitmap ResizeImage(Bitmap source, int targetSize)
    {
        // 保持宽高比的缩放
        float ratio = (float)source.Width / source.Height;
        int w, h;
        if (ratio > 1)
        {
            w = targetSize;
            h = (int)(targetSize / ratio);
        }
        else
        {
            h = targetSize;
            w = (int)(targetSize * ratio);
        }

        var result = new Bitmap(w, h);
        using (var g = Graphics.FromImage(result))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(source, 0, 0, w, h);
        }
        return result;
    }

    /// <summary>
    /// 提取高度图：将每个像素的亮度转换为0-1的高度值
    /// </summary>
    private float[,] ExtractHeightMap(Bitmap bitmap, GenerationOptions opt)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        var heights = new float[w, h];

        // 锁定位图数据以快速访问
        var data = bitmap.LockBits(
            new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly,
            PixelFormat.Format24bppRgb);

        try
        {
            int stride = data.Stride;
            byte[] buffer = new byte[stride * h];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * stride + x * 3;
                    byte b = buffer[idx];
                    byte g = buffer[idx + 1];
                    byte r = buffer[idx + 2];

                    // 计算亮度（使用人眼感知权重）
                    float brightness = (0.299f * r + 0.587f * g + 0.114f * b) / 255f;

                    // 应用对比度增强
                    if (opt.ContrastBoost != 1.0f)
                    {
                        brightness = (brightness - 0.5f) * opt.ContrastBoost + 0.5f;
                        brightness = Math.Clamp(brightness, 0f, 1f);
                    }

                    // 反转
                    if (opt.Invert) brightness = 1f - brightness;

                    heights[x, y] = brightness * opt.MaxHeight;
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(data);
        }

        // 平滑处理
        if (opt.Smoothness > 1.0f)
        {
            heights = SmoothHeightMap(heights, (int)opt.Smoothness);
        }

        return heights;
    }

    /// <summary>
    /// 高斯平滑高度图
    /// </summary>
    private float[,] SmoothHeightMap(float[,] heights, int passes)
    {
        int w = heights.GetLength(0);
        int h = heights.GetLength(1);
        var result = (float[,])heights.Clone();

        for (int p = 0; p < passes; p++)
        {
            var temp = new float[w, h];
            for (int y = 1; y < h - 1; y++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    // 3x3 均值滤波
                    float sum = 0;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            sum += result[x + dx, y + dy];
                    temp[x, y] = sum / 9f;
                }
            }
            // 边界保持原值
            for (int x = 0; x < w; x++) { temp[x, 0] = result[x, 0]; temp[x, h - 1] = result[x, h - 1]; }
            for (int y = 0; y < h; y++) { temp[0, y] = result[0, y]; temp[w - 1, y] = result[w - 1, y]; }
            result = temp;
        }
        return result;
    }

    /// <summary>
    /// Sobel边缘检测算子
    /// </summary>
    private float[,] SobelEdgeDetection(Bitmap bitmap)
    {
        int w = bitmap.Width;
        int h = bitmap.Height;
        var gray = new float[w, h];

        // 转灰度
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = bitmap.GetPixel(x, y);
                gray[x, y] = (0.299f * c.R + 0.587f * c.G + 0.114f * c.B);
            }

        // Sobel算子
        var edges = new float[w, h];
        for (int y = 1; y < h - 1; y++)
        {
            for (int x = 1; x < w - 1; x++)
            {
                float gx = -gray[x - 1, y - 1] - 2 * gray[x - 1, y] - gray[x - 1, y + 1]
                          + gray[x + 1, y - 1] + 2 * gray[x + 1, y] + gray[x + 1, y + 1];
                float gy = -gray[x - 1, y - 1] - 2 * gray[x, y - 1] - gray[x + 1, y - 1]
                          + gray[x - 1, y + 1] + 2 * gray[x, y + 1] + gray[x + 1, y + 1];
                edges[x, y] = (float)Math.Sqrt(gx * gx + gy * gy);
            }
        }
        return edges;
    }

    /// <summary>
    /// 二值化阈值处理
    /// </summary>
    private bool[,] Threshold(float[,] data, float threshold)
    {
        int w = data.GetLength(0);
        int h = data.GetLength(1);
        var result = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result[x, y] = data[x, y] > threshold;
        return result;
    }

    // ===== 网格构建方法 =====

    /// <summary>
    /// 从高度图构建3D网格
    /// </summary>
    private (List<Vec3> verts, List<int> idx, List<Vec3> normals, List<Vec2> uvs)
        BuildHeightMapMesh(float[,] heights, GenerationOptions opt)
    {
        int w = heights.GetLength(0);
        int h = heights.GetLength(1);
        var verts = new List<Vec3>();
        var idx = new List<int>();
        var normals = new List<Vec3>();
        var uvs = new List<Vec2>();

        // 归一化坐标到 [-1, 1]
        float scale = 2f / Math.Max(w, h);

        // 生成顶点
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float vx = (x - w / 2f) * scale;
                float vz = (y - h / 2f) * scale;
                float vy = heights[x, y];

                verts.Add(new Vec3(vx, vy, vz));
                uvs.Add(new Vec2((float)x / (w - 1), 1f - (float)y / (h - 1)));

                // 计算法线（基于相邻像素）
                float dx = (x < w - 1 ? heights[x + 1, y] : vy) - (x > 0 ? heights[x - 1, y] : vy);
                float dz = (y < h - 1 ? heights[x, y + 1] : vy) - (y > 0 ? heights[x, y - 1] : vy);
                var normal = new Vec3(-dx, 2f * scale, -dz).Normalized();
                normals.Add(normal);
            }
        }

        // 生成三角面（每个网格单元两个三角形）
        for (int y = 0; y < h - 1; y++)
        {
            for (int x = 0; x < w - 1; x++)
            {
                int i00 = y * w + x;
                int i10 = y * w + x + 1;
                int i01 = (y + 1) * w + x;
                int i11 = (y + 1) * w + x + 1;

                // 三角形1
                idx.Add(i00); idx.Add(i01); idx.Add(i10);
                // 三角形2
                idx.Add(i10); idx.Add(i01); idx.Add(i11);
            }
        }

        return (verts, idx, normals, uvs);
    }

    /// <summary>
    /// 体素化网格（每个像素一个立方体）
    /// </summary>
    private (List<Vec3>, List<int>, List<Vec3>, List<Vec2>)
        BuildVoxelMesh(float[,] heights, GenerationOptions opt)
    {
        int w = heights.GetLength(0);
        int h = heights.GetLength(1);
        var verts = new List<Vec3>();
        var idx = new List<int>();
        var normals = new List<Vec3>();
        var uvs = new List<Vec2>();

        float scale = 2f / Math.Max(w, h);
        float voxelSize = scale;

        // 阈值：高度大于此值才生成立方体
        float threshold = 0.05f;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (heights[x, y] < threshold) continue;

                float vx = (x - w / 2f) * scale;
                float vz = (y - h / 2f) * scale;
                float vh = heights[x, y];

                // 添加一个立方体
                AddCube(verts, idx, normals, uvs, vx, 0, vz, voxelSize, vh);
            }
        }

        return (verts, idx, normals, uvs);
    }

    private void AddCube(List<Vec3> verts, List<int> idx, List<Vec3> normals, List<Vec2> uvs,
        float x, float y, float z, float size, float height)
    {
        int baseIdx = verts.Count;
        float s = size / 2f;

        // 8个立方体顶点
        verts.Add(new Vec3(x - s, y, z - s));
        verts.Add(new Vec3(x + s, y, z - s));
        verts.Add(new Vec3(x + s, y, z + s));
        verts.Add(new Vec3(x - s, y, z + s));
        verts.Add(new Vec3(x - s, y + height, z - s));
        verts.Add(new Vec3(x + s, y + height, z - s));
        verts.Add(new Vec3(x + s, y + height, z + s));
        verts.Add(new Vec3(x - s, y + height, z + s));

        for (int i = 0; i < 8; i++)
        {
            normals.Add(new Vec3(0, 1, 0));
            uvs.Add(new Vec2(0, 0));
        }

        // 12个三角形（6个面）
        int[] cubeIndices = {
            // 顶部
            4, 5, 6, 4, 6, 7,
            // 底部
            0, 2, 1, 0, 3, 2,
            // 前
            0, 1, 5, 0, 5, 4,
            // 后
            2, 3, 7, 2, 7, 6,
            // 左
            3, 0, 4, 3, 4, 7,
            // 右
            1, 2, 6, 1, 6, 5
        };

        foreach (var i in cubeIndices)
            idx.Add(baseIdx + i);
    }

    private (List<Vec3>, List<int>, List<Vec3>, List<Vec2>)
        BuildExtrusionMesh(bool[,] mask, GenerationOptions opt)
    {
        int w = mask.GetLength(0);
        int h = mask.GetLength(1);
        var verts = new List<Vec3>();
        var idx = new List<int>();
        var normals = new List<Vec3>();
        var uvs = new List<Vec2>();

        float scale = 2f / Math.Max(w, h);
        float voxelSize = scale;
        float extrudeHeight = opt.MaxHeight;

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[x, y]) continue;
                float vx = (x - w / 2f) * scale;
                float vz = (y - h / 2f) * scale;
                AddCube(verts, idx, normals, uvs, vx, 0, vz, voxelSize, extrudeHeight);
            }

        return (verts, idx, normals, uvs);
    }

    // ===== OBJ导出 =====

    private string SaveAsObj(List<Vec3> verts, List<int> idx, List<Vec3> normals, List<Vec2> uvs,
        string imagePath, string algoTag)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Generated by City3DDesktop Local Algorithm");
        sb.AppendLine($"# Algorithm: {algoTag}");
        sb.AppendLine($"# Source: {Path.GetFileName(imagePath)}");
        sb.AppendLine($"# Vertices: {verts.Count}, Faces: {idx.Count / 3}");
        sb.AppendLine();
        sb.AppendLine($"o {Path.GetFileNameWithoutExtension(imagePath)}_{algoTag}");
        sb.AppendLine();

        // 顶点
        foreach (var v in verts)
            sb.AppendLine($"v {v.X:F6} {v.Y:F6} {v.Z:F6}");

        // UV坐标
        foreach (var uv in uvs)
            sb.AppendLine($"vt {uv.X:F6} {uv.Y:F6}");

        // 法线
        foreach (var n in normals)
            sb.AppendLine($"vn {n.X:F6} {n.Y:F6} {n.Z:F6}");

        sb.AppendLine();
        sb.AppendLine("s 1");

        // 面（OBJ索引从1开始）
        for (int i = 0; i < idx.Count; i += 3)
        {
            int a = idx[i] + 1;
            int b = idx[i + 1] + 1;
            int c = idx[i + 2] + 1;
            sb.AppendLine($"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}");
        }

        var dir = Path.Combine(Path.GetTempPath(), "City3D_Models");
        Directory.CreateDirectory(dir);
        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var path = Path.Combine(dir, $"{fileName}_{algoTag}_{DateTime.Now:yyyyMMddHHmmss}.obj");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private void Report(string msg, int pct) => ProgressChanged?.Invoke(msg, pct);

    // ===== 辅助类型 =====

    public record struct Vec3(float X, float Y, float Z)
    {
        public Vec3 Normalized()
        {
            float len = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
            return len > 0 ? new Vec3(X / len, Y / len, Z / len) : this;
        }
    }

    public record struct Vec2(float X, float Y);
}
