using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using City3DDesktop.Services;

namespace City3DDesktop;

public partial class MainWindow : Window
{
    private readonly Image23DService _aiService;
    private readonly ModelExportService _exportService;

    private string? _imagePath;
    private string? _generatedModelPath;
    private CancellationTokenSource? _cts;
    private bool _wireframeMode = false;

    // 配置存储
    private string _tripoApiKey = "";
    private string _meshyApiKey = "";

    public MainWindow()
    {
        InitializeComponent();
        _aiService = new Image23DService();
        _exportService = new ModelExportService();

        _aiService.ProgressChanged += OnProgressChanged;
        LoadSettings();
    }

    // ===== 步骤1: 加载图片 =====
    private void LoadImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|JPEG|*.jpg;*.jpeg|PNG|*.png|所有文件|*.*",
            Title = "选择要转换的图片"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _imagePath = dialog.FileName;

            // 加载图片预览
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(_imagePath);
            bitmap.EndInit();
            bitmap.Freeze();

            PreviewImage.Source = bitmap;
            ImagePlaceholder.Visibility = Visibility.Collapsed;
            ImagePathText.Text = Path.GetFileName(_imagePath);
            UpdateStatus($"已加载图片: {Path.GetFileName(_imagePath)} ({bitmap.PixelWidth}x{bitmap.PixelHeight})");
        }
        catch (Exception ex)
        {
            ShowError($"加载图片失败: {ex.Message}");
        }
    }

    // ===== 步骤2: 生成3D模型 =====
    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_imagePath))
        {
            ShowWarning("请先选择一张图片");
            return;
        }

        // 配置AI服务
        var providerTag = ((ComboBoxItem)AiProviderCombo.SelectedItem).Tag.ToString();
        var provider = providerTag switch
        {
            "Tripo3D" => Image23DService.AiProvider.Tripo3D,
            "Meshy" => Image23DService.AiProvider.Meshy,
            _ => Image23DService.AiProvider.Demo
        };

        var apiKey = provider switch
        {
            Image23DService.AiProvider.Tripo3D => _tripoApiKey,
            Image23DService.AiProvider.Meshy => _meshyApiKey,
            _ => ""
        };

        if (provider != Image23DService.AiProvider.Demo && string.IsNullOrEmpty(apiKey))
        {
            ShowWarning($"请先在'设置'中配置 {provider} 的 API Key\n\n或选择'演示模式'体验完整流程");
            return;
        }

        _aiService.Configure(provider, apiKey);

        // 准备UI
        GenerateBtn.IsEnabled = false;
        GenerateProgress.Visibility = Visibility.Visible;
        ProgressText.Visibility = Visibility.Visible;
        GenerateProgress.Value = 0;
        ExportBtn.IsEnabled = false;

        _cts = new CancellationTokenSource();

        try
        {
            _generatedModelPath = await _aiService.GenerateModelAsync(_imagePath, _cts.Token);

            // 加载并显示生成的模型
            LoadModel(_generatedModelPath);
            ExportBtn.IsEnabled = true;
            UpdateStatus($"✅ 3D模型生成成功: {Path.GetFileName(_generatedModelPath)}");
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("操作已取消");
        }
        catch (Exception ex)
        {
            ShowError($"生成失败: {ex.Message}");
            UpdateStatus("❌ 生成失败");
        }
        finally
        {
            GenerateBtn.IsEnabled = true;
            GenerateProgress.Visibility = Visibility.Collapsed;
            ProgressText.Visibility = Visibility.Collapsed;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnProgressChanged(string message, int percent)
    {
        Dispatcher.Invoke(() =>
        {
            GenerateProgress.Value = percent;
            ProgressText.Text = $"{message} ({percent}%)";
            UpdateStatus(message);
        });
    }

    // ===== 步骤3: 导出模型 =====
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_generatedModelPath) || !File.Exists(_generatedModelPath))
        {
            ShowWarning("请先生成3D模型");
            return;
        }

        var formatTag = ((ComboBoxItem)ExportFormatCombo.SelectedItem).Tag.ToString();
        var format = formatTag switch
        {
            "STL" => ModelExportService.ExportFormat.STL,
            "GLTF" => ModelExportService.ExportFormat.GLTF,
            "FBX" => ModelExportService.ExportFormat.FBX,
            _ => ModelExportService.ExportFormat.OBJ
        };

        var ext = ModelExportService.GetExtension(format);
        var dialog = new SaveFileDialog
        {
            Filter = $"{format}文件|*{ext}|所有文件|*.*",
            FileName = $"{Path.GetFileNameWithoutExtension(_imagePath)}_3D{ext}",
            Title = $"导出为 {format} 格式"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            UpdateStatus($"正在导出为 {format}...");
            var success = await _exportService.ExportAsync(_generatedModelPath, dialog.FileName, format);

            if (success)
            {
                UpdateStatus($"✅ 已导出: {dialog.FileName}");
                var result = MessageBox.Show(
                    $"导出成功！\n\n文件位置:\n{dialog.FileName}\n\n是否打开所在文件夹？",
                    "导出成功",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{dialog.FileName}\"");
                }
            }
            else
            {
                ShowError("导出失败");
            }
        }
        catch (Exception ex)
        {
            ShowError($"导出错误: {ex.Message}");
        }
    }

    // ===== 3D模型加载与显示 =====
    private void LoadModel(string modelPath)
    {
        try
        {
            ModelContainer.Children.Clear();

            // 仅支持OBJ格式预览（GLB/GLTF需要其他库）
            if (!modelPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            {
                EmptyState.Visibility = Visibility.Visible;
                UpdateStatus("⚠️ 当前格式不支持预览，但可以导出");
                return;
            }

            var reader = new ObjReader();
            var model = reader.Read(modelPath);

            if (model == null)
            {
                ShowError("无法读取生成的3D模型");
                return;
            }

            var visual = new ModelVisual3D { Content = model };
            ModelContainer.Children.Add(visual);

            EmptyState.Visibility = Visibility.Collapsed;
            Viewport3D.ZoomExtents();
        }
        catch (Exception ex)
        {
            ShowError($"加载模型失败: {ex.Message}");
        }
    }

    // ===== 视图控制 =====
    private void ResetView_Click(object sender, RoutedEventArgs e)
    {
        Viewport3D.ZoomExtents();
        UpdateStatus("视角已重置");
    }

    private void ToggleWireframe_Click(object sender, RoutedEventArgs e)
    {
        _wireframeMode = !_wireframeMode;
        // 简化版本：切换背景色作为视觉反馈
        Viewport3D.Background = _wireframeMode
            ? System.Windows.Media.Brushes.White
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x1a, 0x1a, 0x2e));
        UpdateStatus(_wireframeMode ? "线框模式" : "实体模式");
    }

    // ===== 设置 =====
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new Window
        {
            Title = "API设置",
            Width = 500,
            Height = 380,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(20) };

        panel.Children.Add(new TextBlock
        {
            Text = "AI服务API配置",
            FontSize = 18,
            FontWeight = FontWeights.Medium,
            Margin = new Thickness(0, 0, 0, 16)
        });

        panel.Children.Add(new TextBlock { Text = "Tripo3D API Key:", Margin = new Thickness(0, 8, 0, 4) });
        var tripoBox = new TextBox { Text = _tripoApiKey, Padding = new Thickness(8) };
        panel.Children.Add(tripoBox);

        panel.Children.Add(new TextBlock { Text = "Meshy.ai API Key:", Margin = new Thickness(0, 12, 0, 4) });
        var meshyBox = new TextBox { Text = _meshyApiKey, Padding = new Thickness(8) };
        panel.Children.Add(meshyBox);

        panel.Children.Add(new TextBlock
        {
            Text = "💡 提示：\n• Tripo3D: https://platform.tripo3d.ai/\n• Meshy: https://www.meshy.ai/api\n• 演示模式无需API key",
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 16, 0, 16),
            TextWrapping = TextWrapping.Wrap
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var saveBtn = new Button
        {
            Content = "保存",
            Padding = new Thickness(20, 8, 20, 8),
            Margin = new Thickness(8, 0, 0, 0),
            IsDefault = true
        };
        saveBtn.Click += (s, e) =>
        {
            _tripoApiKey = tripoBox.Text;
            _meshyApiKey = meshyBox.Text;
            SaveSettings();
            UpdateStatus("✅ 设置已保存");
            settingsWindow.Close();
        };

        var cancelBtn = new Button
        {
            Content = "取消",
            Padding = new Thickness(20, 8, 20, 8),
            IsCancel = true
        };

        buttonPanel.Children.Add(cancelBtn);
        buttonPanel.Children.Add(saveBtn);
        panel.Children.Add(buttonPanel);

        settingsWindow.Content = panel;
        settingsWindow.ShowDialog();
    }

    private void LoadSettings()
    {
        try
        {
            var settingsPath = GetSettingsPath();
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
                if (settings != null)
                {
                    _tripoApiKey = settings.TripoApiKey ?? "";
                    _meshyApiKey = settings.MeshyApiKey ?? "";
                }
            }
        }
        catch { /* ignore */ }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new { TripoApiKey = _tripoApiKey, MeshyApiKey = _meshyApiKey };
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
            File.WriteAllText(GetSettingsPath(), json);
        }
        catch { /* ignore */ }
    }

    private string GetSettingsPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "City3DDesktop");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    // ===== 工具方法 =====
    private void UpdateStatus(string message) => StatusText.Text = message;

    private void ShowError(string message)
    {
        MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _cts?.Cancel();
        base.OnClosing(e);
    }
}
