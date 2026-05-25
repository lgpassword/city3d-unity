using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace City3DDesktop.Services;

/// <summary>
/// 车辆生成策略 - 工业级精度
/// </summary>
public class VehicleGenerationStrategy : IGenerationStrategy
{
    public async Task<Mesh> GenerateBaseShapeAsync(
        ObjectMetadata metadata,
        GenerationConfig config,
        Action<string, int> onProgress)
    {
        onProgress?.Invoke("正在生成车身基础形状...", 0);

        var mesh = new Mesh();
        var dims = metadata.Dimensions;

        // 如果没有尺寸数据，使用默认比例
        if (dims.Length == 0)
        {
            dims.Length = 4500; // 默认轿车长度 4.5m
            dims.Width = 1800;
            dims.Height = 1400;
            dims.Wheelbase = 2700;
        }

        // 转换为米（归一化）
        double length = dims.Length / 1000.0;
        double width = dims.Width / 1000.0;
        double height = dims.Height / 1000.0;
        double wheelbase = dims.Wheelbase > 0 ? dims.Wheelbase / 1000.0 : length * 0.6;

        onProgress?.Invoke("正在生成车身主体...", 20);

        // 1. 生成车身主体（流线型）
        await Task.Run(() =>
        {
            GenerateVehicleBody(mesh, length, width, height, config.Resolution);
        });

        onProgress?.Invoke("正在生成车轮...", 40);

        // 2. 生成车轮
        await Task.Run(() =>
        {
            GenerateWheels(mesh, length, width, wheelbase, config.Resolution);
        });

        onProgress?.Invoke("正在生成车窗...", 60);

        // 3. 生成车窗
        await Task.Run(() =>
        {
            GenerateWindows(mesh, length, width, height, config.Resolution);
        });

        onProgress?.Invoke("正在生成细节部件...", 80);

        // 4. 生成细节（车灯、格栅等）
        await Task.Run(() =>
        {
            GenerateVehicleDetails(mesh, length, width, height, metadata.Features);
        });

        onProgress?.Invoke("基础形状生成完成", 100);

        return mesh;
    }

    public async Task<Mesh> SculptDetailsAsync(
        Mesh baseMesh,
        string imagePath,
        ObjectMetadata metadata,
        GenerationConfig config,
        Action<string, int> onProgress)
    {
        if (!config.EnableDetailSculpting)
            return baseMesh;

        onProgress?.Invoke("正在分析图片轮廓...", 0);

        // 1. 提取图片轮廓
        var contours = await ExtractContoursAsync(imagePath);

        onProgress?.Invoke("正在调整车身形状...", 30);

        // 2. 根据轮廓调整车身
        await Task.Run(() =>
        {
            AdjustBodyShape(baseMesh, contours, metadata.Features);
        });

        onProgress?.Invoke("正在细分网格...", 60);

        // 3. 细分网格（工业级）
        if (config.Quality >= QualityLevel.High)
        {
            await Task.Run(() =>
            {
                SubdivideMesh(baseMesh, config.SubdivisionLevel);
            });
        }

        onProgress?.Invoke("正在平滑表面...", 80);

        // 4. 平滑处理
        await Task.Run(() =>
        {
            SmoothMesh(baseMesh, iterations: 5);
        });

        onProgress?.Invoke("细节雕刻完成", 100);

        return baseMesh;
    }

    public async Task<Mesh> ApplyTextureAsync(
        Mesh mesh,
        string imagePath,
        ObjectMetadata metadata,
        GenerationConfig config,
        Action<string, int> onProgress)
    {
        if (!config.EnableTextureMapping)
            return mesh;

        onProgress?.Invoke("正在生成UV坐标...", 0);

        // 1. UV展开
        await Task.Run(() =>
        {
            GenerateUVCoordinates(mesh);
        });

        onProgress?.Invoke("正在提取纹理...", 50);

        // 2. 从原图提取纹理
        await Task.Run(() =>
        {
            ExtractTextureFromImage(mesh, imagePath);
        });

        onProgress?.Invoke("纹理应用完成", 100);

        return mesh;
    }

    // ========== 私有方法 ==========

    private void GenerateVehicleBody(Mesh mesh, double length, double width, double height, int resolution)
    {
        // 生成车身轮廓（侧视图）
        var profilePoints = new List<Vector3>();

        // 前部（引擎盖）
        for (int i = 0; i <= resolution / 4; i++)
        {
            double t = (double)i / (resolution / 4);
            double x = length * 0.15 * t;
            double y = height * 0.5 * (1 - Math.Cos(t * Math.PI / 2));
            profilePoints.Add(new Vector3(x, y, 0));
        }

        // 车顶
        for (int i = 0; i <= resolution / 2; i++)
        {
            double t = (double)i / (resolution / 2);
            double x = length * 0.15 + length * 0.7 * t;
            double y = height * (0.5 + 0.4 * Math.Sin(t * Math.PI));
            profilePoints.Add(new Vector3(x, y, 0));
        }

        // 后部（行李箱）
        for (int i = 0; i <= resolution / 4; i++)
        {
            double t = (double)i / (resolution / 4);
            double x = length * 0.85 + length * 0.15 * t;
            double y = height * 0.5 * Math.Cos(t * Math.PI / 2);
            profilePoints.Add(new Vector3(x, y, 0));
        }

        // 旋转生成3D车身（沿X轴）
        int slices = resolution;
        for (int i = 0; i < profilePoints.Count; i++)
        {
            var point = profilePoints[i];

            for (int j = 0; j <= slices; j++)
            {
                double angle = (double)j / slices * Math.PI; // 只生成上半部分
                double z = width / 2 * Math.Sin(angle);
                double y = point.Y + (width / 2 * (1 - Math.Cos(angle)) * 0.3); // 车身侧面弧度

                mesh.Vertices.Add(new Vector3(point.X, y, z));
            }
        }

        // 生成面
        for (int i = 0; i < profilePoints.Count - 1; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                int v0 = i * (slices + 1) + j;
                int v1 = i * (slices + 1) + j + 1;
                int v2 = (i + 1) * (slices + 1) + j;
                int v3 = (i + 1) * (slices + 1) + j + 1;

                mesh.Faces.Add(new Face(v0, v2, v1));
                mesh.Faces.Add(new Face(v1, v2, v3));
            }
        }

        // 生成底盘
        GenerateChassis(mesh, length, width);
    }

    private void GenerateChassis(Mesh mesh, double length, double width)
    {
        int baseIndex = mesh.Vertices.Count;

        // 底盘四个角
        mesh.Vertices.Add(new Vector3(0, 0, -width / 2));
        mesh.Vertices.Add(new Vector3(length, 0, -width / 2));
        mesh.Vertices.Add(new Vector3(length, 0, width / 2));
        mesh.Vertices.Add(new Vector3(0, 0, width / 2));

        // 底盘面
        mesh.Faces.Add(new Face(baseIndex, baseIndex + 1, baseIndex + 2));
        mesh.Faces.Add(new Face(baseIndex, baseIndex + 2, baseIndex + 3));
    }

    private void GenerateWheels(Mesh mesh, double length, double width, double wheelbase, int resolution)
    {
        double wheelRadius = width * 0.35;
        double wheelWidth = width * 0.25;

        // 前轮位置
        double frontWheelX = (length - wheelbase) / 2;
        // 后轮位置
        double rearWheelX = frontWheelX + wheelbase;

        // 生成4个轮子
        var wheelPositions = new[]
        {
            new Vector3(frontWheelX, wheelRadius, -width / 2 - wheelWidth / 2),  // 左前
            new Vector3(frontWheelX, wheelRadius, width / 2 + wheelWidth / 2),   // 右前
            new Vector3(rearWheelX, wheelRadius, -width / 2 - wheelWidth / 2),   // 左后
            new Vector3(rearWheelX, wheelRadius, width / 2 + wheelWidth / 2)     // 右后
        };

        foreach (var pos in wheelPositions)
        {
            GenerateWheel(mesh, pos, wheelRadius, wheelWidth, resolution / 2);
        }
    }

    private void GenerateWheel(Mesh mesh, Vector3 center, double radius, double width, int segments)
    {
        int baseIndex = mesh.Vertices.Count;

        // 生成轮子圆环
        for (int i = 0; i <= segments; i++)
        {
            double angle = (double)i / segments * 2 * Math.PI;
            double x = center.X;
            double y = center.Y + radius * Math.Cos(angle);
            double z = center.Z + radius * Math.Sin(angle);

            // 外侧
            mesh.Vertices.Add(new Vector3(x - width / 2, y, z));
            // 内侧
            mesh.Vertices.Add(new Vector3(x + width / 2, y, z));
        }

        // 生成轮子侧面
        for (int i = 0; i < segments; i++)
        {
            int v0 = baseIndex + i * 2;
            int v1 = baseIndex + i * 2 + 1;
            int v2 = baseIndex + (i + 1) * 2;
            int v3 = baseIndex + (i + 1) * 2 + 1;

            mesh.Faces.Add(new Face(v0, v2, v1));
            mesh.Faces.Add(new Face(v1, v2, v3));
        }
    }

    private void GenerateWindows(Mesh mesh, double length, double width, double height, int resolution)
    {
        // 前挡风玻璃
        GenerateWindshield(mesh, length * 0.2, length * 0.4, width, height, true);

        // 后挡风玻璃
        GenerateWindshield(mesh, length * 0.6, length * 0.8, width, height, false);

        // 侧窗
        GenerateSideWindows(mesh, length, width, height);
    }

    private void GenerateWindshield(Mesh mesh, double startX, double endX, double width, double height, bool isFront)
    {
        int baseIndex = mesh.Vertices.Count;

        double topY = height * 0.9;
        double bottomY = height * 0.6;

        // 4个角点
        mesh.Vertices.Add(new Vector3(startX, bottomY, -width / 3));
        mesh.Vertices.Add(new Vector3(endX, topY, -width / 3));
        mesh.Vertices.Add(new Vector3(endX, topY, width / 3));
        mesh.Vertices.Add(new Vector3(startX, bottomY, width / 3));

        // 玻璃面
        mesh.Faces.Add(new Face(baseIndex, baseIndex + 1, baseIndex + 2));
        mesh.Faces.Add(new Face(baseIndex, baseIndex + 2, baseIndex + 3));
    }

    private void GenerateSideWindows(Mesh mesh, double length, double width, double height)
    {
        // 简化实现：侧窗作为车身的一部分
        // 在实际工业级实现中，这里会生成独立的玻璃几何体
    }

    private void GenerateVehicleDetails(Mesh mesh, double length, double width, double height, List<string> features)
    {
        // 根据特征生成细节
        if (features.Any(f => f.Contains("headlight") || f.Contains("车灯")))
        {
            GenerateHeadlights(mesh, length, width, height);
        }

        if (features.Any(f => f.Contains("grille") || f.Contains("格栅")))
        {
            GenerateGrille(mesh, length, width, height);
        }

        if (features.Any(f => f.Contains("mirror") || f.Contains("后视镜")))
        {
            GenerateMirrors(mesh, length, width, height);
        }
    }

    private void GenerateHeadlights(Mesh mesh, double length, double width, double height)
    {
        // 前大灯（简化为椭圆）
        double headlightY = height * 0.4;
        double headlightZ = width * 0.35;

        GenerateEllipsoid(mesh, new Vector3(length * 0.05, headlightY, -headlightZ), 0.15, 0.1, 0.1, 16);
        GenerateEllipsoid(mesh, new Vector3(length * 0.05, headlightY, headlightZ), 0.15, 0.1, 0.1, 16);
    }

    private void GenerateGrille(Mesh mesh, double length, double width, double height)
    {
        // 进气格栅（简化为矩形网格）
        int baseIndex = mesh.Vertices.Count;

        double grilleWidth = width * 0.6;
        double grilleHeight = height * 0.2;
        double grilleX = length * 0.02;
        double grilleY = height * 0.3;

        // 格栅框架
        mesh.Vertices.Add(new Vector3(grilleX, grilleY, -grilleWidth / 2));
        mesh.Vertices.Add(new Vector3(grilleX, grilleY + grilleHeight, -grilleWidth / 2));
        mesh.Vertices.Add(new Vector3(grilleX, grilleY + grilleHeight, grilleWidth / 2));
        mesh.Vertices.Add(new Vector3(grilleX, grilleY, grilleWidth / 2));

        mesh.Faces.Add(new Face(baseIndex, baseIndex + 1, baseIndex + 2));
        mesh.Faces.Add(new Face(baseIndex, baseIndex + 2, baseIndex + 3));
    }

    private void GenerateMirrors(Mesh mesh, double length, double width, double height)
    {
        // 后视镜（简化为小盒子）
        double mirrorX = length * 0.3;
        double mirrorY = height * 0.7;
        double mirrorZ = width * 0.55;

        GenerateBox(mesh, new Vector3(mirrorX, mirrorY, -mirrorZ), 0.1, 0.08, 0.15);
        GenerateBox(mesh, new Vector3(mirrorX, mirrorY, mirrorZ), 0.1, 0.08, 0.15);
    }

    private void GenerateEllipsoid(Mesh mesh, Vector3 center, double rx, double ry, double rz, int segments)
    {
        int baseIndex = mesh.Vertices.Count;

        for (int i = 0; i <= segments; i++)
        {
            double theta = (double)i / segments * Math.PI;
            for (int j = 0; j <= segments; j++)
            {
                double phi = (double)j / segments * 2 * Math.PI;

                double x = center.X + rx * Math.Sin(theta) * Math.Cos(phi);
                double y = center.Y + ry * Math.Sin(theta) * Math.Sin(phi);
                double z = center.Z + rz * Math.Cos(theta);

                mesh.Vertices.Add(new Vector3(x, y, z));
            }
        }

        // 生成面
        for (int i = 0; i < segments; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int v0 = baseIndex + i * (segments + 1) + j;
                int v1 = baseIndex + i * (segments + 1) + j + 1;
                int v2 = baseIndex + (i + 1) * (segments + 1) + j;
                int v3 = baseIndex + (i + 1) * (segments + 1) + j + 1;

                mesh.Faces.Add(new Face(v0, v2, v1));
                mesh.Faces.Add(new Face(v1, v2, v3));
            }
        }
    }

    private void GenerateBox(Mesh mesh, Vector3 center, double width, double height, double depth)
    {
        int baseIndex = mesh.Vertices.Count;

        // 8个顶点
        mesh.Vertices.Add(new Vector3(center.X - width / 2, center.Y - height / 2, center.Z - depth / 2));
        mesh.Vertices.Add(new Vector3(center.X + width / 2, center.Y - height / 2, center.Z - depth / 2));
        mesh.Vertices.Add(new Vector3(center.X + width / 2, center.Y + height / 2, center.Z - depth / 2));
        mesh.Vertices.Add(new Vector3(center.X - width / 2, center.Y + height / 2, center.Z - depth / 2));
        mesh.Vertices.Add(new Vector3(center.X - width / 2, center.Y - height / 2, center.Z + depth / 2));
        mesh.Vertices.Add(new Vector3(center.X + width / 2, center.Y - height / 2, center.Z + depth / 2));
        mesh.Vertices.Add(new Vector3(center.X + width / 2, center.Y + height / 2, center.Z + depth / 2));
        mesh.Vertices.Add(new Vector3(center.X - width / 2, center.Y + height / 2, center.Z + depth / 2));

        // 6个面
        int[] indices = { 0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2, 2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0 };
        for (int i = 0; i < indices.Length; i += 3)
        {
            mesh.Faces.Add(new Face(baseIndex + indices[i], baseIndex + indices[i + 1], baseIndex + indices[i + 2]));
        }
    }

    private async Task<List<Vector3>> ExtractContoursAsync(string imagePath)
    {
        // 简化实现：从图片提取轮廓点
        return await Task.Run(() =>
        {
            var contours = new List<Vector3>();
            // TODO: 实现边缘检测算法（Canny等）
            return contours;
        });
    }

    private void AdjustBodyShape(Mesh mesh, List<Vector3> contours, List<string> features)
    {
        // 根据轮廓调整车身形状
        // TODO: 实现形状匹配算法
    }

    private void SubdivideMesh(Mesh mesh, int level)
    {
        // Catmull-Clark细分算法
        for (int i = 0; i < level; i++)
        {
            var newVertices = new List<Vector3>(mesh.Vertices);
            var newFaces = new List<Face>();

            // 为每个面生成中心点
            var facePoints = new Dictionary<int, Vector3>();
            for (int faceIdx = 0; faceIdx < mesh.Faces.Count; faceIdx++)
            {
                var face = mesh.Faces[faceIdx];
                var center = (mesh.Vertices[face.V1] + mesh.Vertices[face.V2] + mesh.Vertices[face.V3]) / 3.0;
                facePoints[faceIdx] = center;
                newVertices.Add(center);
            }

            // 为每条边生成中点
            // TODO: 完整的Catmull-Clark实现

            mesh.Vertices = newVertices;
            mesh.Faces = newFaces;
        }
    }

    private void SmoothMesh(Mesh mesh, int iterations)
    {
        // Laplacian平滑
        for (int iter = 0; iter < iterations; iter++)
        {
            var smoothed = new List<Vector3>(mesh.Vertices);

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var neighbors = FindNeighbors(mesh, i);
                if (neighbors.Count > 0)
                {
                    var sum = new Vector3(0, 0, 0);
                    foreach (var n in neighbors)
                    {
                        sum = sum + mesh.Vertices[n];
                    }
                    smoothed[i] = sum / neighbors.Count;
                }
            }

            mesh.Vertices = smoothed;
        }
    }

    private List<int> FindNeighbors(Mesh mesh, int vertexIndex)
    {
        var neighbors = new HashSet<int>();

        foreach (var face in mesh.Faces)
        {
            if (face.V1 == vertexIndex)
            {
                neighbors.Add(face.V2);
                neighbors.Add(face.V3);
            }
            else if (face.V2 == vertexIndex)
            {
                neighbors.Add(face.V1);
                neighbors.Add(face.V3);
            }
            else if (face.V3 == vertexIndex)
            {
                neighbors.Add(face.V1);
                neighbors.Add(face.V2);
            }
        }

        return neighbors.ToList();
    }

    private void GenerateUVCoordinates(Mesh mesh)
    {
        // 简化的UV展开（球面投影）
        mesh.UVs.Clear();

        foreach (var vertex in mesh.Vertices)
        {
            double u = 0.5 + Math.Atan2(vertex.Z, vertex.X) / (2 * Math.PI);
            double v = 0.5 - Math.Asin(vertex.Y / vertex.Length()) / Math.PI;
            mesh.UVs.Add(new Vector2(u, v));
        }
    }

    private void ExtractTextureFromImage(Mesh mesh, string imagePath)
    {
        // TODO: 从原图提取纹理并映射到UV
    }
}
