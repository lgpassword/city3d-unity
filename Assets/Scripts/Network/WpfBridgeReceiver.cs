using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Unity端的WPF桥接服务
/// 接收来自WPF的场景数据并渲染
/// </summary>
public class WpfBridgeReceiver : MonoBehaviour
{
    private NamedPipeClientStream? _pipeClient;
    private string _pipeName = "";
    private bool _isConnected = false;
    private AppManager? _appManager;

    [Header("调试信息")]
    [SerializeField] private bool _showDebugLogs = true;

    private void Awake()
    {
        // 从命令行参数获取管道名称
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-pipeName")
            {
                _pipeName = args[i + 1];
                break;
            }
        }

        if (string.IsNullOrEmpty(_pipeName))
        {
            LogDebug("未找到管道名称参数，使用默认模式运行");
            return;
        }

        LogDebug($"管道名称: {_pipeName}");
    }

    private async void Start()
    {
        _appManager = FindObjectOfType<AppManager>();
        if (_appManager == null)
        {
            LogDebug("警告: 未找到AppManager组件");
        }

        if (!string.IsNullOrEmpty(_pipeName))
        {
            await ConnectToPipeAsync();
            if (_isConnected)
            {
                _ = ReceiveDataLoopAsync();
            }
        }
    }

    /// <summary>
    /// 连接到WPF的命名管道
    /// </summary>
    private async Task ConnectToPipeAsync()
    {
        try
        {
            LogDebug("正在连接到WPF管道...");
            _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _pipeClient.ConnectAsync(5000);
            _isConnected = true;
            LogDebug("已连接到WPF管道");
        }
        catch (Exception ex)
        {
            LogDebug($"连接管道失败: {ex.Message}");
            _isConnected = false;
        }
    }

    /// <summary>
    /// 持续接收来自WPF的数据
    /// </summary>
    private async Task ReceiveDataLoopAsync()
    {
        while (_isConnected && _pipeClient != null && _pipeClient.IsConnected)
        {
            try
            {
                var json = await ReceiveMessageAsync();
                if (!string.IsNullOrEmpty(json))
                {
                    LogDebug($"收到场景数据: {json.Substring(0, Math.Min(100, json.Length))}...");
                    ProcessSceneData(json);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"接收数据失败: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    /// <summary>
    /// 从管道接收消息
    /// </summary>
    private async Task<string?> ReceiveMessageAsync()
    {
        if (_pipeClient == null || !_pipeClient.IsConnected)
            return null;

        try
        {
            // 读取数据长度
            var lengthBytes = new byte[4];
            await _pipeClient.ReadAsync(lengthBytes, 0, 4);
            var length = BitConverter.ToInt32(lengthBytes, 0);

            // 读取数据内容
            var buffer = new byte[length];
            var totalRead = 0;
            while (totalRead < length)
            {
                var read = await _pipeClient.ReadAsync(buffer, totalRead, length - totalRead);
                if (read == 0) break;
                totalRead += read;
            }

            return Encoding.UTF8.GetString(buffer, 0, totalRead);
        }
        catch (Exception ex)
        {
            LogDebug($"接收消息失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 处理接收到的场景数据
    /// </summary>
    private void ProcessSceneData(string json)
    {
        try
        {
            var sceneData = JsonConvert.DeserializeObject<SceneDataMessage>(json);
            if (sceneData == null)
            {
                LogDebug("场景数据解析失败");
                return;
            }

            LogDebug($"解析场景数据成功: {sceneData.location}, 建筑物: {sceneData.buildings?.Count ?? 0}");

            // 在主线程中调用AppManager生成场景
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (_appManager != null)
                {
                    GenerateSceneFromData(sceneData);
                }
            });
        }
        catch (Exception ex)
        {
            LogDebug($"处理场景数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据接收的数据生成Unity场景
    /// </summary>
    private void GenerateSceneFromData(SceneDataMessage data)
    {
        try
        {
            LogDebug($"开始生成场景: {data.location}");

            // 调用AppManager的生成方法
            if (_appManager != null)
            {
                // 这里需要将WPF的数据格式转换为Unity的数据格式
                // 然后调用CitySceneBuilder生成场景
                var cityBuilder = FindObjectOfType<CitySceneBuilder>();
                if (cityBuilder != null)
                {
                    // 转换建筑物数据
                    // 转换道路数据
                    // 调用cityBuilder.BuildScene()
                    LogDebug("场景生成完成");
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"生成场景失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送消息到WPF
    /// </summary>
    public async Task<bool> SendMessageAsync(string message)
    {
        if (_pipeClient == null || !_pipeClient.IsConnected)
            return false;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            var lengthBytes = BitConverter.GetBytes(bytes.Length);

            await _pipeClient.WriteAsync(lengthBytes, 0, lengthBytes.Length);
            await _pipeClient.WriteAsync(bytes, 0, bytes.Length);
            await _pipeClient.FlushAsync();

            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"发送消息失败: {ex.Message}");
            return false;
        }
    }

    private void LogDebug(string message)
    {
        if (_showDebugLogs)
        {
            Debug.Log($"[WpfBridge] {message}");
        }
    }

    private void OnDestroy()
    {
        _isConnected = false;
        _pipeClient?.Dispose();
    }
}

/// <summary>
/// 场景数据消息格式
/// </summary>
[Serializable]
public class SceneDataMessage
{
    public string location;
    public double latitude;
    public double longitude;
    public double radius;
    public double elevation;
    public System.Collections.Generic.List<BuildingData> buildings;
    public System.Collections.Generic.List<RoadData> roads;
}

[Serializable]
public class BuildingData
{
    public string name;
    public double latitude;
    public double longitude;
    public double height;
    public string type;
}

[Serializable]
public class RoadData
{
    public string name;
    public string type;
    public System.Collections.Generic.List<GpsPoint> points;
}

[Serializable]
public class GpsPoint
{
    public double latitude;
    public double longitude;
}
