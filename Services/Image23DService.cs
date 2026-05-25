using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace City3DDesktop.Services;

/// <summary>
/// 图片转3D模型服务
/// 支持多种AI后端：Tripo3D、Meshy.ai、本地模拟
/// </summary>
public class Image23DService
{
    private readonly HttpClient _httpClient;
    private string _apiKey = "";
    private AiProvider _provider = AiProvider.Demo;

    public enum AiProvider
    {
        Demo,       // 演示模式（生成示例立方体）
        Tripo3D,    // Tripo3D API
        Meshy       // Meshy.ai API
    }

    public Image23DService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public void Configure(AiProvider provider, string apiKey = "")
    {
        _provider = provider;
        _apiKey = apiKey;
    }

    /// <summary>
    /// 进度回调
    /// </summary>
    public event Action<string, int>? ProgressChanged;

    /// <summary>
    /// 从图片生成3D模型
    /// </summary>
    /// <returns>生成的OBJ文件路径</returns>
    public async Task<string> GenerateModelAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        ReportProgress("正在准备图片...", 5);

        if (!File.Exists(imagePath))
            throw new FileNotFoundException($"图片不存在: {imagePath}");

        return _provider switch
        {
            AiProvider.Demo => await GenerateDemoModelAsync(imagePath, cancellationToken),
            AiProvider.Tripo3D => await GenerateWithTripo3DAsync(imagePath, cancellationToken),
            AiProvider.Meshy => await GenerateWithMeshyAsync(imagePath, cancellationToken),
            _ => throw new NotSupportedException()
        };
    }

    /// <summary>
    /// 演示模式：生成一个示例立方体OBJ
    /// 这样用户即使没有API key也能体验完整流程
    /// </summary>
    private async Task<string> GenerateDemoModelAsync(string imagePath, CancellationToken ct)
    {
        ReportProgress("演示模式：生成示例3D模型...", 30);
        await Task.Delay(1000, ct);

        ReportProgress("正在构建网格...", 60);
        await Task.Delay(800, ct);

        var outputDir = Path.Combine(Path.GetTempPath(), "City3D_Models");
        Directory.CreateDirectory(outputDir);

        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var objPath = Path.Combine(outputDir, $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.obj");

        // 生成一个示例OBJ文件（更复杂的形状，不只是立方体）
        var objContent = GenerateSampleObjContent();
        await File.WriteAllTextAsync(objPath, objContent, ct);

        ReportProgress("生成完成", 100);
        return objPath;
    }

    /// <summary>
    /// Tripo3D API 调用
    /// 文档: https://platform.tripo3d.ai/
    /// </summary>
    private async Task<string> GenerateWithTripo3DAsync(string imagePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("请先在设置中配置Tripo3D API Key");

        ReportProgress("上传图片到Tripo3D...", 10);

        // 步骤1: 上传图片
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(imageBytes), "file", Path.GetFileName(imagePath));

        var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.tripo3d.ai/v2/openapi/upload");
        uploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        uploadRequest.Content = uploadContent;

        var uploadResp = await _httpClient.SendAsync(uploadRequest, ct);
        uploadResp.EnsureSuccessStatusCode();
        var uploadJson = JObject.Parse(await uploadResp.Content.ReadAsStringAsync(ct));
        var imageToken = uploadJson["data"]?["image_token"]?.ToString();

        ReportProgress("提交生成任务...", 25);

        // 步骤2: 创建任务
        var taskBody = new
        {
            type = "image_to_model",
            file = new { type = "jpg", file_token = imageToken }
        };
        var taskRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.tripo3d.ai/v2/openapi/task");
        taskRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        taskRequest.Content = new StringContent(JsonConvert.SerializeObject(taskBody), Encoding.UTF8, "application/json");

        var taskResp = await _httpClient.SendAsync(taskRequest, ct);
        taskResp.EnsureSuccessStatusCode();
        var taskJson = JObject.Parse(await taskResp.Content.ReadAsStringAsync(ct));
        var taskId = taskJson["data"]?["task_id"]?.ToString();

        // 步骤3: 轮询任务状态
        ReportProgress("AI正在生成3D模型...", 40);
        string? modelUrl = null;
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(5000, ct);

            var statusRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.tripo3d.ai/v2/openapi/task/{taskId}");
            statusRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var statusResp = await _httpClient.SendAsync(statusRequest, ct);
            var statusJson = JObject.Parse(await statusResp.Content.ReadAsStringAsync(ct));
            var status = statusJson["data"]?["status"]?.ToString();
            var progress = statusJson["data"]?["progress"]?.Value<int>() ?? 0;

            ReportProgress($"AI生成中: {progress}%", 40 + (progress * 50 / 100));

            if (status == "success")
            {
                modelUrl = statusJson["data"]?["output"]?["model"]?.ToString();
                break;
            }
            else if (status == "failed")
            {
                throw new Exception("Tripo3D生成失败");
            }
        }

        if (string.IsNullOrEmpty(modelUrl))
            throw new Exception("生成超时");

        // 步骤4: 下载模型
        ReportProgress("下载3D模型...", 95);
        var outputDir = Path.Combine(Path.GetTempPath(), "City3D_Models");
        Directory.CreateDirectory(outputDir);
        var fileName = Path.GetFileNameWithoutExtension(imagePath);
        var objPath = Path.Combine(outputDir, $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.glb");

        var modelBytes = await _httpClient.GetByteArrayAsync(modelUrl, ct);
        await File.WriteAllBytesAsync(objPath, modelBytes, ct);

        ReportProgress("生成完成", 100);
        return objPath;
    }

    /// <summary>
    /// Meshy.ai API 调用
    /// </summary>
    private async Task<string> GenerateWithMeshyAsync(string imagePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("请先在设置中配置Meshy API Key");

        ReportProgress("提交到Meshy.ai...", 20);

        // 转换图片为base64
        var imageBytes = await File.ReadAllBytesAsync(imagePath, ct);
        var base64 = Convert.ToBase64String(imageBytes);
        var ext = Path.GetExtension(imagePath).TrimStart('.').ToLower();
        var dataUri = $"data:image/{ext};base64,{base64}";

        var body = new
        {
            image_url = dataUri,
            enable_pbr = true
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.meshy.ai/v1/image-to-3d");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        var resp = await _httpClient.SendAsync(request, ct);
        resp.EnsureSuccessStatusCode();
        var json = JObject.Parse(await resp.Content.ReadAsStringAsync(ct));
        var taskId = json["result"]?.ToString();

        // 轮询任务
        ReportProgress("AI生成中...", 30);
        for (int i = 0; i < 60; i++)
        {
            await Task.Delay(5000, ct);
            var statusReq = new HttpRequestMessage(HttpMethod.Get, $"https://api.meshy.ai/v1/image-to-3d/{taskId}");
            statusReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            var statusResp = await _httpClient.SendAsync(statusReq, ct);
            var statusJson = JObject.Parse(await statusResp.Content.ReadAsStringAsync(ct));
            var status = statusJson["status"]?.ToString();
            var progress = statusJson["progress"]?.Value<int>() ?? 0;

            ReportProgress($"AI生成中: {progress}%", 30 + (progress * 60 / 100));

            if (status == "SUCCEEDED")
            {
                var modelUrl = statusJson["model_urls"]?["obj"]?.ToString();
                if (!string.IsNullOrEmpty(modelUrl))
                {
                    var outputDir = Path.Combine(Path.GetTempPath(), "City3D_Models");
                    Directory.CreateDirectory(outputDir);
                    var fileName = Path.GetFileNameWithoutExtension(imagePath);
                    var objPath = Path.Combine(outputDir, $"{fileName}_{DateTime.Now:yyyyMMddHHmmss}.obj");
                    var modelBytes = await _httpClient.GetByteArrayAsync(modelUrl, ct);
                    await File.WriteAllBytesAsync(objPath, modelBytes, ct);
                    ReportProgress("生成完成", 100);
                    return objPath;
                }
            }
            else if (status == "FAILED")
            {
                throw new Exception("Meshy生成失败");
            }
        }

        throw new Exception("生成超时");
    }

    private void ReportProgress(string message, int percent)
    {
        ProgressChanged?.Invoke(message, percent);
    }

    /// <summary>
    /// 生成示例OBJ内容（一个程序化的建筑样式模型）
    /// </summary>
    private string GenerateSampleObjContent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Generated by City3DDesktop Demo Mode");
        sb.AppendLine("# A simple house-like structure");
        sb.AppendLine();
        sb.AppendLine("o House");
        sb.AppendLine();

        // 立方体底部 + 屋顶（金字塔）顶点
        // 底部立方体 8 个顶点
        sb.AppendLine("v -1.0 0.0 -1.0");  // 1
        sb.AppendLine("v  1.0 0.0 -1.0");  // 2
        sb.AppendLine("v  1.0 0.0  1.0");  // 3
        sb.AppendLine("v -1.0 0.0  1.0");  // 4
        sb.AppendLine("v -1.0 1.5 -1.0");  // 5
        sb.AppendLine("v  1.0 1.5 -1.0");  // 6
        sb.AppendLine("v  1.0 1.5  1.0");  // 7
        sb.AppendLine("v -1.0 1.5  1.0");  // 8
        // 屋顶顶点
        sb.AppendLine("v  0.0 2.5  0.0");  // 9
        sb.AppendLine();

        // 法线
        sb.AppendLine("vn  0.0 -1.0  0.0");
        sb.AppendLine("vn  0.0  0.0 -1.0");
        sb.AppendLine("vn  1.0  0.0  0.0");
        sb.AppendLine("vn  0.0  0.0  1.0");
        sb.AppendLine("vn -1.0  0.0  0.0");
        sb.AppendLine("vn  0.0  1.0  0.0");
        sb.AppendLine();

        // 面（带法线）
        // 底部
        sb.AppendLine("f 1//1 2//1 3//1 4//1");
        // 侧面 4 个
        sb.AppendLine("f 1//2 5//2 6//2 2//2");
        sb.AppendLine("f 2//3 6//3 7//3 3//3");
        sb.AppendLine("f 3//4 7//4 8//4 4//4");
        sb.AppendLine("f 4//5 8//5 5//5 1//5");
        // 屋顶 4 个三角面
        sb.AppendLine("f 5//6 9//6 6//6");
        sb.AppendLine("f 6//6 9//6 7//6");
        sb.AppendLine("f 7//6 9//6 8//6");
        sb.AppendLine("f 8//6 9//6 5//6");

        return sb.ToString();
    }
}
