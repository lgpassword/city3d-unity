using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using City3DDesktop.Services;
using City3DDesktop.Models;
using System.IO;

namespace City3DDesktop;

public partial class MainWindow : Window
{
    private readonly DatabaseService _databaseService;
    private readonly AiService _aiService;
    private readonly OsmService _osmService;
    private readonly ElevationService _elevationService;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _unityInitialized = false;

    public MainWindow()
    {
        InitializeComponent();

        _databaseService = new DatabaseService();
        _aiService = new AiService();
        _osmService = new OsmService();
        _elevationService = new ElevationService();

        LoadSavedData();
        UpdateStatus("就绪");

        // 异步初始化Unity
        Loaded += async (s, e) => await InitializeUnityAsync();
    }

    private async Task InitializeUnityAsync()
    {
        try
        {
            UpdateStatus("正在启动Unity渲染引擎...");

            // Unity可执行文件路径（需要先构建Unity项目）
            string unityExePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "UnityRenderer",
                "City3D.exe"
            );

            // 如果Unity可执行文件不存在，显示提示
            if (!File.Exists(unityExePath))
            {
                UpdateStatus("Unity渲染引擎未找到，请先构建Unity项目");
                MessageBox.Show(
                    "Unity渲染引擎未找到！\n\n" +
                    "请按照以下步骤操作：\n" +
                    "1. 在Unity中打开主项目\n" +
                    "2. 选择 File > Build Settings\n" +
                    "3. 选择 Windows 平台\n" +
                    "4. 构建到: City3DDesktop/City3DDesktop/bin/Debug/net8.0-windows/UnityRenderer/\n" +
                    "5. 重新启动此应用\n\n" +
                    "当前查找路径: " + unityExePath,
                    "Unity渲染引擎未找到",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            bool success = await UnityView.StartUnityAsync(unityExePath);
            if (success)
            {
                _unityInitialized = true;
                UpdateStatus("Unity渲染引擎已就绪");
            }
            else
            {
                UpdateStatus("Unity渲染引擎启动失败");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"Unity初始化失败: {ex.Message}");
            MessageBox.Show($"Unity渲染引擎初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSavedData()
    {
        try
        {
            var locations = _databaseService.GetAllLocations();
            foreach (var location in locations)
            {
                LocationsList.Items.Add($"{location.Name} ({location.Latitude:F4}, {location.Longitude:F4})");
            }

            var scenes = _databaseService.GetAllScenes();
            foreach (var scene in scenes)
            {
                ScenesList.Items.Add($"{scene.Name} - {scene.CreatedAt:yyyy-MM-dd HH:mm}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"加载数据失败: {ex.Message}");
        }
    }

    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
            Title = "选择图片"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            PathInput.Text = openFileDialog.FileName;
            try
            {
                var bitmap = new BitmapImage(new Uri(openFileDialog.FileName));
                PreviewImage.Source = bitmap;
                UpdateStatus($"已加载图片: {System.IO.Path.GetFileName(openFileDialog.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("图片加载失败");
            }
        }
    }

    private async void GenerateScene_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PathInput.Text))
        {
            MessageBox.Show("请先加载图片", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(LatInput.Text, out double lat) || !double.TryParse(LonInput.Text, out double lon))
        {
            MessageBox.Show("请输入有效的GPS坐标", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        GenerateBtn.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            UpdateStatus("正在识别图片位置...");
            string locationName = await _aiService.IdentifyLocationFromImage(PathInput.Text, _cancellationTokenSource.Token);

            UpdateStatus($"识别结果: {locationName}，正在获取OSM数据...");
            double radius = RadiusSlider.Value;
            OsmData osmData = await _osmService.FetchOsmData(lat, lon, radius, _cancellationTokenSource.Token);

            UpdateStatus($"获取到 {osmData.Buildings.Count} 个建筑物和 {osmData.Roads.Count} 条道路，正在获取海拔数据...");
            double elevation = await _elevationService.GetElevation(lat, lon, _cancellationTokenSource.Token);

            UpdateStatus($"场景生成完成！建筑物: {osmData.Buildings.Count}, 道路: {osmData.Roads.Count}, 海拔: {elevation:F1}m");

            // 如果Unity已初始化，发送场景数据
            if (_unityInitialized)
            {
                UpdateStatus("正在发送场景数据到Unity渲染引擎...");
                var sceneData = new
                {
                    location = locationName,
                    latitude = lat,
                    longitude = lon,
                    radius = radius,
                    elevation = elevation,
                    buildings = osmData.Buildings,
                    roads = osmData.Roads
                };

                bool sent = await UnityView.SendSceneDataAsync(sceneData);
                if (sent)
                {
                    UpdateStatus("场景已发送到Unity渲染引擎");
                }
                else
                {
                    UpdateStatus("发送场景数据失败");
                }
            }

            MessageBox.Show(
                $"场景生成完成！\n\n" +
                $"位置: {locationName}\n" +
                $"建筑物: {osmData.Buildings.Count}\n" +
                $"道路: {osmData.Roads.Count}\n" +
                $"海拔: {elevation:F1}m",
                "成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("场景生成已取消");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成场景失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("场景生成失败");
        }
        finally
        {
            GenerateBtn.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void SaveScene_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SceneNameInput.Text))
        {
            MessageBox.Show("请输入场景名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(LatInput.Text, out double lat) || !double.TryParse(LonInput.Text, out double lon))
        {
            MessageBox.Show("请输入有效的GPS坐标", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var scene = new SceneRecord
            {
                Name = SceneNameInput.Text,
                ImagePath = PathInput.Text,
                Latitude = lat,
                Longitude = lon,
                Radius = RadiusSlider.Value,
                CreatedAt = DateTime.Now
            };

            _databaseService.SaveScene(scene);
            ScenesList.Items.Add($"{scene.Name} - {scene.CreatedAt:yyyy-MM-dd HH:mm}");

            UpdateStatus($"已保存场景: {scene.Name}");
            MessageBox.Show("场景已保存", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存场景失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveLocation_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(LatInput.Text, out double lat) || !double.TryParse(LonInput.Text, out double lon))
        {
            MessageBox.Show("请输入有效的GPS坐标", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string locationName = string.IsNullOrWhiteSpace(SceneNameInput.Text) ? "未命名位置" : SceneNameInput.Text;

        try
        {
            var location = new LocationRecord
            {
                Name = locationName,
                Latitude = lat,
                Longitude = lon,
                CreatedAt = DateTime.Now
            };

            _databaseService.SaveLocation(location);
            LocationsList.Items.Add($"{location.Name} ({location.Latitude:F4}, {location.Longitude:F4})");

            UpdateStatus($"已收藏位置: {location.Name}");
            MessageBox.Show("位置已收藏", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存位置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateStatus(string message)
    {
        StatusText.Text = message;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        UnityView.StopUnity();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        base.OnClosing(e);
    }
}