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
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:8000"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// 识别图片中的主要对象。
    /// </summary>
    /// <param name="imageBytes">图片字节数据。</param>
    /// <returns>识别结果。</returns>
    public async Task<RecognizedObject> RecognizeAsync(byte[] imageBytes)
    {
        try
        {
            // 使用 multipart/form-data 上传图片到本地 AI 服务。
            using var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(imageBytes), "file", "image.jpg");
            var resp = await Http.PostAsync("/recognize", form);
            resp.EnsureSuccessStatusCode();
            var dto = JsonConvert.DeserializeObject<Dto>(
                await resp.Content.ReadAsStringAsync());
            return new RecognizedObject(dto!.name, dto.category, dto.confidence);
        }
        catch
        {
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
