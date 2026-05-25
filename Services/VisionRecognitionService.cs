using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace City3DDesktop.Services;

/// <summary>
/// 视觉识别服务 - 支持豆包和DeepSeek免费API
/// </summary>
public class VisionRecognitionService
{
    private readonly HttpClient _httpClient;

    public enum Provider
    {
        Doubao,    // 豆包（字节跳动）
        DeepSeek   // DeepSeek
    }

    public VisionRecognitionService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    /// <summary>
    /// 识别图片中的物体
    /// </summary>
    public async Task<ObjectRecognitionResult> RecognizeAsync(
        string imagePath,
        Provider provider = Provider.Doubao,
        string apiKey = null)
    {
        var imageBase64 = ConvertImageToBase64(imagePath);

        return provider switch
        {
            Provider.Doubao => await RecognizeWithDoubaoAsync(imageBase64, apiKey),
            Provider.DeepSeek => await RecognizeWithDeepSeekAsync(imageBase64, apiKey),
            _ => throw new NotSupportedException($"Provider {provider} not supported")
        };
    }

    /// <summary>
    /// 使用豆包API识别
    /// </summary>
    private async Task<ObjectRecognitionResult> RecognizeWithDoubaoAsync(string imageBase64, string apiKey)
    {
        // 豆包 API endpoint
        var endpoint = "https://ark.cn-beijing.volces.com/api/v3/chat/completions";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = "doubao-vision-pro",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = @"请详细分析这张图片中的主要物体，并以JSON格式返回以下信息：
{
  ""name"": ""物体的具体名称（中文）"",
  ""nameEn"": ""物体的英文名称"",
  ""category"": ""物体类别（Vehicle/Building/Character/Product/Animal/Furniture/Other）"",
  ""subCategory"": ""子类别（如Vehicle.Car.SportsCar）"",
  ""confidence"": 0.95,
  ""features"": [""特征1"", ""特征2"", ""特征3""],
  ""description"": ""详细描述物体的外观、结构、特点""
}

只返回JSON，不要其他解释。"
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{imageBase64}" }
                        }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"豆包API调用失败: {response.StatusCode} - {responseText}");
        }

        return ParseRecognitionResponse(responseText);
    }

    /// <summary>
    /// 使用DeepSeek API识别
    /// </summary>
    private async Task<ObjectRecognitionResult> RecognizeWithDeepSeekAsync(string imageBase64, string apiKey)
    {
        // DeepSeek API endpoint
        var endpoint = "https://api.deepseek.com/v1/chat/completions";

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var payload = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = @"请详细分析这张图片中的主要物体，并以JSON格式返回以下信息：
{
  ""name"": ""物体的具体名称（中文）"",
  ""nameEn"": ""物体的英文名称"",
  ""category"": ""物体类别（Vehicle/Building/Character/Product/Animal/Furniture/Other）"",
  ""subCategory"": ""子类别（如Vehicle.Car.SportsCar）"",
  ""confidence"": 0.95,
  ""features"": [""特征1"", ""特征2"", ""特征3""],
  ""description"": ""详细描述物体的外观、结构、特点""
}

只返回JSON，不要其他解释。"
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = $"data:image/jpeg;base64,{imageBase64}" }
                        }
                    }
                }
            }
        };

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"DeepSeek API调用失败: {response.StatusCode} - {responseText}");
        }

        return ParseRecognitionResponse(responseText);
    }

    /// <summary>
    /// 解析API响应
    /// </summary>
    private ObjectRecognitionResult ParseRecognitionResponse(string responseText)
    {
        try
        {
            var json = JObject.Parse(responseText);
            var content = json["choices"]?[0]?["message"]?["content"]?.ToString();

            if (string.IsNullOrEmpty(content))
            {
                throw new Exception("API返回内容为空");
            }

            // 提取JSON部分（可能包含markdown代码块）
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var result = JsonConvert.DeserializeObject<ObjectRecognitionResult>(jsonContent);
                return result;
            }

            throw new Exception("无法从响应中提取JSON数据");
        }
        catch (Exception ex)
        {
            throw new Exception($"解析识别结果失败: {ex.Message}\n原始响应: {responseText}");
        }
    }

    /// <summary>
    /// 将图片转换为Base64
    /// </summary>
    private string ConvertImageToBase64(string imagePath)
    {
        var imageBytes = File.ReadAllBytes(imagePath);
        return Convert.ToBase64String(imageBytes);
    }
}

/// <summary>
/// 物体识别结果
/// </summary>
public class ObjectRecognitionResult
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("nameEn")]
    public string NameEn { get; set; }

    [JsonProperty("category")]
    public string Category { get; set; }

    [JsonProperty("subCategory")]
    public string SubCategory { get; set; }

    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    [JsonProperty("features")]
    public List<string> Features { get; set; } = new();

    [JsonProperty("description")]
    public string Description { get; set; }
}
