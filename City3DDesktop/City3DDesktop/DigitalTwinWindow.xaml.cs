using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using City3DDesktop.Models;
using City3DDesktop.Services;

namespace City3DDesktop;

/// <summary>
/// 数字孪生窗口 - 北京中心区域 3D 城市可视化
/// </summary>
public partial class DigitalTwinWindow : Window
{
    private DigitalTwinScene? _scene;
    private CitySceneRenderer? _renderer;

    // 各图层的 Model3DGroup
    private Model3DGroup _buildingsGroup = new();
    private Model3DGroup _roadsGroup = new();
    private Model3DGroup _waterGroup = new();
    private Model3DGroup _greenGroup = new();
    private Model3DGroup _poiGroup = new();

    // 建筑索引（用于点击选中）
    private readonly Dictionary<GeometryModel3D, BuildingData> _buildingIndex = new();

    public DigitalTwinWindow()
    {
        InitializeComponent();

        // 将图层 Model3DGroup 绑定到容器
        BuildingsContainer.Content = _buildingsGroup;
        RoadsContainer.Content = _roadsGroup;
        WaterContainer.Content = _waterGroup;
        GreenContainer.Content = _greenGroup;
        POIContainer.Content = _poiGroup;

        // 启动时自动加载数据
        Loaded += (_, _) =>
        {
            try { LoadSceneData(); }
            catch (Exception ex) { MessageBox.Show($"加载异常：{ex.Message}\n{ex.StackTrace}"); }
        };
    }

    /// <summary>
    /// 加载场景数据并渲染
    /// </summary>
    private void LoadSceneData()
    {
        try
        {
            StatusMessage.Text = "正在加载数据...";

            var service = new SceneDataService();
            _scene = service.LoadDefault();

            if (_scene == null)
            {
                MessageBox.Show("数据加载失败，请确认 Data/beijing_center.json 文件存在。",
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _renderer = new CitySceneRenderer(_buildingsGroup);
            RenderAllLayers();
            UpdateStats();

            StatusMessage.Text = $"加载完成 - {_scene.Buildings?.Count ?? 0} 栋建筑";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载数据时出错：{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage.Text = "加载失败";
        }
    }

    /// <summary>
    /// 渲染所有图层
    /// </summary>
    private void RenderAllLayers()
    {
        if (_scene == null || _renderer == null) return;

        double cLat = _scene.Center?.Latitude ?? 39.9042;
        double cLon = _scene.Center?.Longitude ?? 116.4074;

        // 清除旧数据
        _buildingsGroup.Children.Clear();
        _roadsGroup.Children.Clear();
        _waterGroup.Children.Clear();
        _greenGroup.Children.Clear();
        _poiGroup.Children.Clear();
        _buildingIndex.Clear();

        // 渲染地面
        _renderer.RenderGround(1600);

        // 渲染建筑
        if (_scene.Buildings != null)
        {
            _renderer.RenderBuildings(_scene.Buildings, cLat, cLon);
        }

        // 渲染道路
        if (_scene.Roads != null)
        {
            var roadsRenderer = new CitySceneRenderer(_roadsGroup);
            roadsRenderer.RenderRoads(_scene.Roads, cLat, cLon);
        }

        // 渲染水域
        if (_scene.Waters != null)
        {
            var waterRenderer = new CitySceneRenderer(_waterGroup);
            waterRenderer.RenderWater(_scene.Waters, cLat, cLon);
        }

        // 渲染绿地
        if (_scene.Greens != null)
        {
            var greenRenderer = new CitySceneRenderer(_greenGroup);
            greenRenderer.RenderGreen(_scene.Greens, cLat, cLon);
        }
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStats()
    {
        if (_scene == null) return;

        StatBuildings.Text = (_scene.Buildings?.Count ?? 0).ToString();
        StatRoads.Text = (_scene.Roads?.Count ?? 0).ToString();
        StatWater.Text = (_scene.Waters?.Count ?? 0).ToString();
        StatGreen.Text = (_scene.Greens?.Count ?? 0).ToString();
        StatPOI.Text = (_scene.POIs?.Count ?? 0).ToString();
    }

    // ===== 事件处理 =====

    private void LoadData_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON 文件|*.json|所有文件|*.*",
            Title = "选择城市数据文件"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var service = new SceneDataService();
                _scene = service.LoadFromFile(dialog.FileName);
                _renderer = new CitySceneRenderer(_buildingsGroup);
                RenderAllLayers();
                UpdateStats();
                StatusMessage.Text = $"已加载: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败：{ex.Message}");
            }
        }
    }

    private void ToggleLayers_Click(object sender, RoutedEventArgs e)
    {
        // 切换左侧面板可见性（简单实现）
    }

    private void ViewAngle_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ViewAngleCombo?.SelectedItem is not ComboBoxItem item) return;
        var tag = item.Tag?.ToString() ?? "Perspective";

        var camera = Viewport3D?.Camera as PerspectiveCamera;
        if (camera == null) return;

        switch (tag)
        {
            case "TopDown":
                camera.Position = new Point3D(0, 800, 0);
                camera.LookDirection = new Vector3D(0, -1, 0);
                camera.UpDirection = new Vector3D(0, 0, -1);
                break;
            case "Front":
                camera.Position = new Point3D(0, 200, 800);
                camera.LookDirection = new Vector3D(0, -0.3, -1);
                camera.UpDirection = new Vector3D(0, 1, 0);
                break;
            case "Side":
                camera.Position = new Point3D(800, 200, 0);
                camera.LookDirection = new Vector3D(-1, -0.3, 0);
                camera.UpDirection = new Vector3D(0, 1, 0);
                break;
            default: // Perspective
                camera.Position = new Point3D(0, 500, 500);
                camera.LookDirection = new Vector3D(0, -1, -1);
                camera.UpDirection = new Vector3D(0, 1, 0);
                break;
        }
    }

    private void Layer_Changed(object sender, RoutedEventArgs e)
    {
        if (BuildingsContainer != null)
            BuildingsContainer.SetValue(UIElement.VisibilityProperty,
                LayerBuildings.IsChecked == true ? Visibility.Visible : Visibility.Collapsed);
        if (RoadsContainer != null)
            RoadsContainer.SetValue(UIElement.VisibilityProperty,
                LayerRoads.IsChecked == true ? Visibility.Visible : Visibility.Collapsed);
        if (WaterContainer != null)
            WaterContainer.SetValue(UIElement.VisibilityProperty,
                LayerWater.IsChecked == true ? Visibility.Visible : Visibility.Collapsed);
        if (GreenContainer != null)
            GreenContainer.SetValue(UIElement.VisibilityProperty,
                LayerGreen.IsChecked == true ? Visibility.Visible : Visibility.Collapsed);
        if (POIContainer != null)
            POIContainer.SetValue(UIElement.VisibilityProperty,
                LayerPOI.IsChecked == true ? Visibility.Visible : Visibility.Collapsed);
    }

    private void Viewport3D_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var point = e.GetPosition(Viewport3D);
        var hits = Viewport3D.Viewport.FindHits(point);

        if (hits.Count > 0)
        {
            var hit = hits[0];
            var model = hit.Model as GeometryModel3D;

            // 尝试从建筑索引中查找
            if (model != null && _scene?.Buildings != null)
            {
                // 简单实现：根据命中点位置找最近的建筑
                var hitPoint = hit.Position;
                BuildingData? closest = null;
                double minDist = double.MaxValue;

                double cLat = _scene.Center?.Latitude ?? 39.9042;
                double cLon = _scene.Center?.Longitude ?? 116.4074;

                foreach (var b in _scene.Buildings)
                {
                    double bx = (b.CentroidLon - cLon) * 111320 * Math.Cos(cLat * Math.PI / 180);
                    double bz = (b.CentroidLat - cLat) * 111320;
                    double dist = Math.Sqrt(Math.Pow(hitPoint.X - bx, 2) + Math.Pow(hitPoint.Z - bz, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = b;
                    }
                }

                if (closest != null && minDist < 50)
                {
                    ShowBuildingProperties(closest);
                    return;
                }
            }

            // 未命中建筑
            HideProperties();
        }
        else
        {
            HideProperties();
        }
    }

    private void ShowBuildingProperties(BuildingData building)
    {
        NoSelectionPanel.Visibility = Visibility.Collapsed;
        SelectionPanel.Visibility = Visibility.Visible;

        SelectedObjectName.Text = building.Name;
        PropType.Text = "建筑";
        PropHeight.Text = $"{building.HeightM:F1} m";
        PropArea.Text = $"{building.WidthM * building.DepthM:F0} m²";
        PropPosition.Text = $"{building.CentroidLat:F4}, {building.CentroidLon:F4}";
        PropFloors.Text = building.Floors.ToString();
        PropUsage.Text = building.Name.Contains("殿") || building.Name.Contains("宫")
            ? "历史建筑" : building.Name.Contains("商业") ? "商业办公" : "综合";
        PropId.Text = $"BLD-{Math.Abs(building.Name.GetHashCode()) % 10000:D4}";

        StatusCoord.Text = $"坐标: {building.CentroidLat:F4}, {building.CentroidLon:F4}";
    }

    private void HideProperties()
    {
        NoSelectionPanel.Visibility = Visibility.Visible;
        SelectionPanel.Visibility = Visibility.Collapsed;
    }
}
