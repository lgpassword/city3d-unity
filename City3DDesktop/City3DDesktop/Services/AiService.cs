using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace City3DDesktop.Services;

public class AiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public AiService(string apiUrl = "http://localhost:11434/api/generate")
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _apiUrl = apiUrl;
    }

    public async Task<string> IdentifyLocationFromImage(string imagePath, CancellationToken cancellationToken = default)
    {
        try
        {
            // 读取图片并转换为Base64
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath, cancellationToken);
            string base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new
            {
                model = "llava",
                prompt = "请识别这张图片中的地理位置，只返回城市名称和地标名称，格式：城市-地标",
                images = new[] { base64Image },
                stream = false
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonConvert.DeserializeObject<dynamic>(responseBody);

            return result?.response?.ToString() ?? "未识别";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI识别失败: {ex.Message}");
            return "识别失败";
        }
    }
}
