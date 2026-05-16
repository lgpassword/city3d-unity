using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Newtonsoft.Json;

namespace City3DDesktop.Services;

/// <summary>
/// Unity渲染引擎桥接服务
/// 负责启动Unity进程、窗口嵌入和进程间通信
/// </summary>
public class UnityBridgeService : IDisposable
{
    // Win32 API 导入
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_STYLE = -16;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_MINIMIZE = 0x20000000;
    private const int WS_MAXIMIZE = 0x01000000;
    private const int WS_SYSMENU = 0x00080000;

    private Process? _unityProcess;
    private NamedPipeServerStream? _pipeServer;
    private readonly string _pipeName;
    private readonly string _unityExePath;
    private IntPtr _unityWindowHandle = IntPtr.Zero;

    public bool IsUnityRunning => _unityProcess != null && !_unityProcess.HasExited;
    public IntPtr UnityWindowHandle => _unityWindowHandle;

    public UnityBridgeService(string unityExePath)
    {
        _unityExePath = unityExePath;
        _pipeName = $"City3D_Unity_Pipe_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 启动Unity渲染进程
    /// </summary>
    public async Task<bool> StartUnityAsync()
    {
        try
        {
            if (!File.Exists(_unityExePath))
            {
                throw new FileNotFoundException($"Unity可执行文件不存在: {_unityExePath}");
            }

            // 创建命名管道服务器
            _pipeServer = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            // 启动Unity进程，传递管道名称
            var startInfo = new ProcessStartInfo
            {
                FileName = _unityExePath,
                Arguments = $"-pipeName {_pipeName} -popupwindow",
                UseShellExecute = false,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            };

            _unityProcess = Process.Start(startInfo);
            if (_unityProcess == null)
            {
                throw new Exception("无法启动Unity进程");
            }

            // 等待Unity连接到管道
            await _pipeServer.WaitForConnectionAsync();

            // 等待Unity窗口创建
            await Task.Delay(2000);

            // 查找Unity窗口句柄
            _unityWindowHandle = _unityProcess.MainWindowHandle;
            if (_unityWindowHandle == IntPtr.Zero)
            {
                // 尝试刷新并重新获取
                _unityProcess.Refresh();
                _unityWindowHandle = _unityProcess.MainWindowHandle;
            }

            return _unityWindowHandle != IntPtr.Zero;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"启动Unity失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 将Unity窗口嵌入到WPF容器中
    /// </summary>
    public bool EmbedUnityWindow(IntPtr wpfContainerHandle, int width, int height)
    {
        try
        {
            if (_unityWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            // 移除Unity窗口的标题栏和边框
            int style = GetWindowLong(_unityWindowHandle, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZE | WS_MAXIMIZE | WS_SYSMENU);
            SetWindowLong(_unityWindowHandle, GWL_STYLE, style);

            // 将Unity窗口设为WPF容器的子窗口
            SetParent(_unityWindowHandle, wpfContainerHandle);

            // 调整Unity窗口大小和位置
            MoveWindow(_unityWindowHandle, 0, 0, width, height, true);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"嵌入Unity窗口失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 调整Unity窗口大小
    /// </summary>
    public void ResizeUnityWindow(int width, int height)
    {
        if (_unityWindowHandle != IntPtr.Zero)
        {
            MoveWindow(_unityWindowHandle, 0, 0, width, height, true);
        }
    }

    /// <summary>
    /// 发送场景数据到Unity
    /// </summary>
    public async Task<bool> SendSceneDataAsync(object sceneData)
    {
        try
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                return false;
            }

            var json = JsonConvert.SerializeObject(sceneData);
            var bytes = Encoding.UTF8.GetBytes(json);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);

            // 先发送数据长度
            await _pipeServer.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            // 再发送数据内容
            await _pipeServer.WriteAsync(bytes, 0, bytes.Length);
            await _pipeServer.FlushAsync();

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"发送场景数据失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从Unity接收消息
    /// </summary>
    public async Task<string?> ReceiveMessageAsync()
    {
        try
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                return null;
            }

            // 读取数据长度
            var lengthBytes = new byte[4];
            await _pipeServer.ReadAsync(lengthBytes, 0, 4);
            var length = BitConverter.ToInt32(lengthBytes, 0);

            // 读取数据内容
            var buffer = new byte[length];
            var totalRead = 0;
            while (totalRead < length)
            {
                var read = await _pipeServer.ReadAsync(buffer, totalRead, length - totalRead);
                if (read == 0) break;
                totalRead += read;
            }

            return Encoding.UTF8.GetString(buffer, 0, totalRead);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"接收Unity消息失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 停止Unity进程
    /// </summary>
    public void StopUnity()
    {
        try
        {
            if (_unityProcess != null && !_unityProcess.HasExited)
            {
                _unityProcess.Kill();
                _unityProcess.WaitForExit(3000);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"停止Unity进程失败: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopUnity();
        _pipeServer?.Dispose();
        _unityProcess?.Dispose();
    }
}
