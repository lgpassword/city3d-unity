using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// 本地 AI 识别服务客户端。
/// </summary>
public class AiClient
{
    // 复用 HTTP 客户端连接本地识别服务。
    private readonly HttpClient _http;

    /// <summary>
    /// 创建 AI 客户端。
    /// </summary>
    /// <param name="config">应用配置。</param>
    public AiClient(AppConfig config)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(config.aiServiceUrl),
            Timeout = TimeSpan.FromSeconds(config.aiTimeoutSeconds)
        };
    }

    /// <summary>
    /// 识别图片中的主要对象。
    /// </summary>
    /// <param name="imageBytes">图片字节数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>识别结果。</returns>
    public async Task<RecognizedObject> RecognizeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            // 使用 multipart/form-data 上传图片到本地 AI 服务。
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(imageBytes), "file", "image.jpg");
            var resp = await _http.PostAsync("/recognize", form, cancellationToken);
            resp.EnsureSuccessStatusCode();
            var dto = JsonConvert.DeserializeObject<Dto>(
                await resp.Content.ReadAsStringAsync());
            return new RecognizedObject(dto!.name, dto.category, dto.confidence);
        }
        catch (OperationCanceledException)
        {
            UnityEngine.Debug.Log("[AI识别] 操作已取消");
            throw;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[AI识别] 识别失败，使用默认值：{ex.GetType().Name} - {ex.Message}");
            return new RecognizedObject("building", "Building", 0.5);
        }
    }

    /// <summary>
    /// AI 服务返回的数据传输对象。
    /// </summary>
    private class Dto
    {
        // 识别名称。
        public string name = "";

        // 识别类别。
        public string category = "";

        // 置信度。
        public double confidence;
    }
}
