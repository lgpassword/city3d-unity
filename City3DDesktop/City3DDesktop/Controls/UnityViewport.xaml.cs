using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using City3DDesktop.Services;

namespace City3DDesktop.Controls;

/// <summary>
/// Unity渲染视口控件
/// 负责显示嵌入的Unity窗口
/// </summary>
public partial class UnityViewport : UserControl
{
    private UnityBridgeService? _unityBridge;
    private IntPtr _containerHandle;

    public UnityBridgeService? UnityBridge => _unityBridge;

    public UnityViewport()
    {
        InitializeComponent();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        // 获取容器的窗口句柄
        var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
        if (hwndSource != null)
        {
            _containerHandle = hwndSource.Handle;
        }
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // 当WPF控件大小改变时，调整Unity窗口大小
        if (_unityBridge != null && _unityBridge.IsUnityRunning)
        {
            var width = (int)ActualWidth;
            var height = (int)ActualHeight;
            _unityBridge.ResizeUnityWindow(width, height);
        }
    }

    /// <summary>
    /// 启动Unity渲染引擎
    /// </summary>
    public async Task<bool> StartUnityAsync(string unityExePath)
    {
        try
        {
            LoadingPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;

            // 创建Unity桥接服务
            _unityBridge = new UnityBridgeService(unityExePath);

            // 启动Unity进程
            var started = await _unityBridge.StartUnityAsync();
            if (!started)
            {
                ShowError("无法启动Unity进程，请检查Unity可执行文件路径");
                return false;
            }

            // 等待Unity窗口准备就绪
            await Task.Delay(1000);

            // 嵌入Unity窗口
            var width = (int)ActualWidth;
            var height = (int)ActualHeight;
            var embedded = _unityBridge.EmbedUnityWindow(_containerHandle, width, height);

            if (!embedded)
            {
                ShowError("无法嵌入Unity窗口，请重试");
                return false;
            }

            LoadingPanel.Visibility = Visibility.Collapsed;
            return true;
        }
        catch (Exception ex)
        {
            ShowError($"启动Unity失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 发送场景数据到Unity
    /// </summary>
    public async Task<bool> SendSceneDataAsync(object sceneData)
    {
        if (_unityBridge == null || !_unityBridge.IsUnityRunning)
        {
            ShowError("Unity未运行");
            return false;
        }

        return await _unityBridge.SendSceneDataAsync(sceneData);
    }

    /// <summary>
    /// 停止Unity渲染引擎
    /// </summary>
    public void StopUnity()
    {
        _unityBridge?.StopUnity();
        _unityBridge?.Dispose();
        _unityBridge = null;
    }

    /// <summary>
    /// 显示错误信息
    /// </summary>
    private void ShowError(string message)
    {
        LoadingPanel.Visibility = Visibility.Collapsed;
        ErrorPanel.Visibility = Visibility.Visible;
        ErrorText.Text = message;
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void Cleanup()
    {
        StopUnity();
    }
}
