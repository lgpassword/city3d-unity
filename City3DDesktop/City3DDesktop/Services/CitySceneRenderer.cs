using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using City3DDesktop.Models;

namespace City3DDesktop.Services;

/// <summary>
/// 3D 城市场景渲染器 - 使用 WPF 原生 3D 构建城市模型
/// </summary>
public class CitySceneRenderer
{
    private readonly Model3DGroup _modelGroup;

    public CitySceneRenderer(Model3DGroup modelGroup)
    {
        _modelGroup = modelGroup;
    }

    public void RenderScene(DigitalTwinScene scene)
    {
        ClearScene();
        double cLat = scene.Center?.Latitude ?? 39.9042;
        double cLon = scene.Center?.Longitude ?? 116.4074;
        RenderGround(1600);
        RenderBuildings(scene.Buildings, cLat, cLon);
        RenderRoads(scene.Roads, cLat, cLon);
        RenderWater(scene.Waters, cLat, cLon);
        RenderGreen(scene.Greens, cLat, cLon);
    }

    public void RenderBuildings(List<BuildingData>? buildings, double centerLat, double centerLon)
    {
        if (buildings == null) return;
        foreach (var b in buildings)
        {
            double x = (b.CentroidLon - centerLon) * 111320 * Math.Cos(centerLat * Math.PI / 180);
            double z = (b.CentroidLat - centerLat) * 111320;
            double h = b.HeightM;

            var mesh = CreateBox(x, h / 2, z, b.WidthM, h, b.DepthM);
            var material = GetBuildingMaterial(b.Name, h);
            var model = new GeometryModel3D(mesh, material);
            model.BackMaterial = material;
            _modelGroup.Children.Add(model);
        }
    }

    public void RenderRoads(List<RoadData>? roads, double centerLat, double centerLon)
    {
        if (roads == null) return;
        foreach (var road in roads)
        {
            if (road.Points == null || road.Points.Count < 2) continue;
            for (int i = 0; i < road.Points.Count - 1; i++)
            {
                var p1 = road.Points[i];
                var p2 = road.Points[i + 1];
                double x1 = (p1.Lon - centerLon) * 111320 * Math.Cos(centerLat * Math.PI / 180);
                double z1 = (p1.Lat - centerLat) * 111320;
                double x2 = (p2.Lon - centerLon) * 111320 * Math.Cos(centerLat * Math.PI / 180);
                double z2 = (p2.Lat - centerLat) * 111320;

                double cx = (x1 + x2) / 2;
                double cz = (z1 + z2) / 2;
                double length = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));

                var mesh = CreateBox(cx, 0.05, cz, road.WidthM, 0.1, length);
                var material = GetRoadMaterial(road.RoadType);
                var model = new GeometryModel3D(mesh, material);
                model.BackMaterial = material;
                _modelGroup.Children.Add(model);
            }
        }
    }

    public void RenderWater(List<WaterData>? waters, double centerLat, double centerLon)
    {
        if (waters == null) return;
        foreach (var water in waters)
        {
            if (water.Boundary == null || water.Boundary.Count < 3) continue;
            var lats = water.Boundary.Select(p => p.Lat).ToList();
            var lons = water.Boundary.Select(p => p.Lon).ToList();
            double cx = (lons.Average() - centerLon) * 111320 * Math.Cos(centerLat * Math.PI / 180);
            double cz = (lats.Average() - centerLat) * 111320;
            double w = (lons.Max() - lons.Min()) * 111320 * Math.Cos(centerLat * Math.PI / 180);
            double d = (lats.Max() - lats.Min()) * 111320;

            var mesh = CreateBox(cx, 0.02, cz, Math.Max(w, 5), 0.04, Math.Max(d, 5));
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(180, 30, 144, 255)));
            var model = new GeometryModel3D(mesh, material);
            model.BackMaterial = material;
            _modelGroup.Children.Add(model);
        }
    }

    public void RenderGreen(List<GreenData>? greens, double centerLat, double centerLon)
    {
        if (greens == null) return;
        foreach (var green in greens)
        {
            if (green.Boundary == null || green.Boundary.Count < 3) continue;
            var lats = green.Boundary.Select(p => p.Lat).ToList();
            var lons = green.Boundary.Select(p => p.Lon).ToList();
            double cx = (lons.Average() - centerLon) * 111320 * Math.Cos(centerLat * Math.PI / 180);
            double cz = (lats.Average() - centerLat) * 111320;
            double w = (lons.Max() - lons.Min()) * 111320 * Math.Cos(centerLat * Math.PI / 180);
            double d = (lats.Max() - lats.Min()) * 111320;

            var mesh = CreateBox(cx, 0.01, cz, Math.Max(w, 5), 0.02, Math.Max(d, 5));
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(34, 139, 34)));
            var model = new GeometryModel3D(mesh, material);
            model.BackMaterial = material;
            _modelGroup.Children.Add(model);
        }
    }

    public void RenderGround(float size)
    {
        var mesh = CreateBox(0, -0.1, 0, size, 0.2, size);
        var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(220, 220, 220)));
        var model = new GeometryModel3D(mesh, material);
        model.BackMaterial = material;
        _modelGroup.Children.Add(model);
    }

    public void ClearScene()
    {
        _modelGroup.Children.Clear();
    }

    /// <summary>
    /// 创建一个 Box 的 MeshGeometry3D（中心点 + 尺寸）
    /// </summary>
    private static MeshGeometry3D CreateBox(double cx, double cy, double cz, double width, double height, double depth)
    {
        double hw = width / 2, hh = height / 2, hd = depth / 2;
        var mesh = new MeshGeometry3D();

        // 8 个顶点
        mesh.Positions.Add(new Point3D(cx - hw, cy - hh, cz - hd)); // 0
        mesh.Positions.Add(new Point3D(cx + hw, cy - hh, cz - hd)); // 1
        mesh.Positions.Add(new Point3D(cx + hw, cy + hh, cz - hd)); // 2
        mesh.Positions.Add(new Point3D(cx - hw, cy + hh, cz - hd)); // 3
        mesh.Positions.Add(new Point3D(cx - hw, cy - hh, cz + hd)); // 4
        mesh.Positions.Add(new Point3D(cx + hw, cy - hh, cz + hd)); // 5
        mesh.Positions.Add(new Point3D(cx + hw, cy + hh, cz + hd)); // 6
        mesh.Positions.Add(new Point3D(cx - hw, cy + hh, cz + hd)); // 7

        // 12 个三角形（6 个面）
        int[] indices = {
            0,2,1, 0,3,2, // 前
            5,6,4, 4,6,7, // 后
            4,3,0, 4,7,3, // 左
            1,2,5, 5,2,6, // 右
            3,7,6, 3,6,2, // 上
            0,1,5, 0,5,4  // 下
        };

        foreach (var i in indices)
            mesh.TriangleIndices.Add(i);

        return mesh;
    }

    private Material GetBuildingMaterial(string name, double height)
    {
        Color color;
        if (name.Contains("殿") || name.Contains("门") || name.Contains("堂") || name.Contains("宫"))
        {
            color = Color.FromRgb(218, 165, 32);
        }
        else
        {
            byte r = (byte)Math.Max(60, 180 - height * 1.5);
            byte g = (byte)Math.Max(60, 190 - height * 1.2);
            byte b = (byte)Math.Min(255, 200 + height * 0.7);
            color = Color.FromRgb(r, g, b);
        }
        return new DiffuseMaterial(new SolidColorBrush(color));
    }

    private Material GetRoadMaterial(string? roadType)
    {
        var color = roadType switch
        {
            "trunk" or "motorway" => Color.FromRgb(60, 60, 60),
            "primary" => Color.FromRgb(90, 90, 90),
            "secondary" => Color.FromRgb(120, 120, 120),
            "tertiary" => Color.FromRgb(150, 150, 150),
            _ => Color.FromRgb(180, 180, 180)
        };
        return new DiffuseMaterial(new SolidColorBrush(color));
    }
}
