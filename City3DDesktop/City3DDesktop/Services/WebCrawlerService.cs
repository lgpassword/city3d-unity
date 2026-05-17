using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace City3DDesktop.Services;

/// <summary>
/// 网络爬虫服务 - 从多个来源获取物体元数据
/// </summary>
public class WebCrawlerService
{
    private readonly HttpClient _httpClient;

    public WebCrawlerService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// 收集物体的完整元数据
    /// </summary>
    public async Task<ObjectMetadata> CollectMetadataAsync(
        ObjectRecognitionResult recognition,
        Action<string, int> onProgress = null)
    {
        var metadata = new ObjectMetadata
        {
            ObjectName = recognition.Name,
            ObjectNameEn = recognition.NameEn,
            Category = recognition.Category,
            Features = recognition.Features
        };

        onProgress?.Invoke("开始收集数据...", 0);

        // 1. 维基百科/百度百科
        onProgress?.Invoke("正在搜索百科数据...", 10);
        metadata.WikiData = await SearchWikipediaAsync(recognition.NameEn, recognition.Name);

        // 2. 3D模型网站元数据
        onProgress?.Invoke("正在搜索3D模型信息...", 30);
        metadata.ModelSiteData = await SearchModelSitesAsync(recognition.NameEn);

        // 3. 技术规格
        onProgress?.Invoke("正在搜索技术规格...", 50);
        metadata.TechnicalSpecs = await SearchTechnicalSpecsAsync(recognition);

        // 4. 多角度参考图片
        onProgress?.Invoke("正在搜索参考图片...", 70);
        metadata.ReferenceImages = await SearchReferenceImagesAsync(recognition.NameEn);

        // 5. 结构化数据提取
        onProgress?.Invoke("正在提取结构化数据...", 90);
        metadata.Dimensions = ExtractDimensions(metadata);
        metadata.StructuralInfo = ExtractStructuralInfo(metadata);

        onProgress?.Invoke("数据收集完成", 100);

        return metadata;
    }

    /// <summary>
    /// 搜索维基百科和百度百科
    /// </summary>
    private async Task<WikiData> SearchWikipediaAsync(string nameEn, string nameCn)
    {
        var wikiData = new WikiData();

        try
        {
            // 1. 英文维基百科
            var wikiUrl = $"https://en.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(nameEn)}";
            var response = await _httpClient.GetAsync(wikiUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                wikiData.Summary = json["extract"]?.ToString();
                wikiData.ThumbnailUrl = json["thumbnail"]?["source"]?.ToString();
                wikiData.WikiUrl = json["content_urls"]?["desktop"]?["page"]?.ToString();
            }

            // 2. 获取完整页面内容（提取信息框数据）
            if (!string.IsNullOrEmpty(wikiData.WikiUrl))
            {
                var pageHtml = await _httpClient.GetStringAsync(wikiData.WikiUrl);
                var doc = new HtmlDocument();
                doc.LoadHtml(pageHtml);

                // 提取信息框（Infobox）
                var infobox = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'infobox')]");
                if (infobox != null)
                {
                    wikiData.InfoboxData = ParseInfobox(infobox);
                }
            }

            // 3. 百度百科（备用）
            try
            {
                var baiduUrl = $"https://baike.baidu.com/item/{Uri.EscapeDataString(nameCn)}";
                var baiduHtml = await _httpClient.GetStringAsync(baiduUrl);
                var baiduDoc = new HtmlDocument();
                baiduDoc.LoadHtml(baiduHtml);

                // 提取基本信息模块
                var basicInfo = baiduDoc.DocumentNode.SelectSingleNode("//div[@class='basic-info']");
                if (basicInfo != null)
                {
                    wikiData.BaiduData = ParseBaiduBasicInfo(basicInfo);
                }
            }
            catch
            {
                // 百度百科失败不影响整体流程
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"维基百科搜索失败: {ex.Message}");
        }

        return wikiData;
    }

    /// <summary>
    /// 搜索3D模型网站元数据（不下载模型）
    /// </summary>
    private async Task<List<ModelSiteData>> SearchModelSitesAsync(string objectName)
    {
        var results = new List<ModelSiteData>();

        // 1. Sketchfab
        try
        {
            var sketchfabUrl = $"https://api.sketchfab.com/v3/search?type=models&q={Uri.EscapeDataString(objectName)}&count=5";
            var response = await _httpClient.GetAsync(sketchfabUrl);

            if (response.IsSuccessStatusCode)
            {
                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                var models = json["results"] as JArray;

                foreach (var model in models ?? new JArray())
                {
                    results.Add(new ModelSiteData
                    {
                        Source = "Sketchfab",
                        Name = model["name"]?.ToString(),
                        Description = model["description"]?.ToString(),
                        PreviewUrl = model["thumbnails"]?["images"]?[0]?["url"]?.ToString(),
                        VertexCount = model["vertexCount"]?.ToObject<int>() ?? 0,
                        FaceCount = model["faceCount"]?.ToObject<int>() ?? 0,
                        Tags = model["tags"]?.Select(t => t["name"]?.ToString()).ToList()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sketchfab搜索失败: {ex.Message}");
        }

        // 2. TurboSquid（爬虫）
        try
        {
            var turbosquidUrl = $"https://www.turbosquid.com/Search/3D-Models?keyword={Uri.EscapeDataString(objectName)}";
            var html = await _httpClient.GetStringAsync(turbosquidUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var products = doc.DocumentNode.SelectNodes("//div[contains(@class, 'product')]");
            if (products != null)
            {
                foreach (var product in products.Take(5))
                {
                    var nameNode = product.SelectSingleNode(".//h3");
                    var imgNode = product.SelectSingleNode(".//img");
                    var descNode = product.SelectSingleNode(".//p[contains(@class, 'description')]");

                    results.Add(new ModelSiteData
                    {
                        Source = "TurboSquid",
                        Name = nameNode?.InnerText.Trim(),
                        Description = descNode?.InnerText.Trim(),
                        PreviewUrl = imgNode?.GetAttributeValue("src", null)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TurboSquid搜索失败: {ex.Message}");
        }

        // 3. Free3D（爬虫）
        try
        {
            var free3dUrl = $"https://free3d.com/3d-models/{Uri.EscapeDataString(objectName)}";
            var html = await _httpClient.GetStringAsync(free3dUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var models = doc.DocumentNode.SelectNodes("//div[contains(@class, 'model-item')]");
            if (models != null)
            {
                foreach (var model in models.Take(5))
                {
                    var nameNode = model.SelectSingleNode(".//h4");
                    var imgNode = model.SelectSingleNode(".//img");

                    results.Add(new ModelSiteData
                    {
                        Source = "Free3D",
                        Name = nameNode?.InnerText.Trim(),
                        PreviewUrl = imgNode?.GetAttributeValue("src", null)
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Free3D搜索失败: {ex.Message}");
        }

        return results;
    }

    /// <summary>
    /// 搜索技术规格
    /// </summary>
    private async Task<TechnicalSpecs> SearchTechnicalSpecsAsync(ObjectRecognitionResult recognition)
    {
        var specs = new TechnicalSpecs();

        try
        {
            // 根据类别选择搜索策略
            if (recognition.Category == "Vehicle")
            {
                specs = await SearchVehicleSpecsAsync(recognition.NameEn);
            }
            else if (recognition.Category == "Building")
            {
                specs = await SearchBuildingSpecsAsync(recognition.NameEn);
            }
            else
            {
                // 通用搜索
                specs = await SearchGeneralSpecsAsync(recognition.NameEn);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"技术规格搜索失败: {ex.Message}");
        }

        return specs;
    }

    /// <summary>
    /// 搜索车辆规格
    /// </summary>
    private async Task<TechnicalSpecs> SearchVehicleSpecsAsync(string vehicleName)
    {
        var specs = new TechnicalSpecs();

        try
        {
            // 搜索汽车之家、懂车帝等
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(vehicleName + " specifications dimensions")}";
            var html = await _httpClient.GetStringAsync(searchUrl);

            // 使用正则提取尺寸数据
            specs.Properties["length"] = ExtractDimension(html, @"length[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
            specs.Properties["width"] = ExtractDimension(html, @"width[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
            specs.Properties["height"] = ExtractDimension(html, @"height[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
            specs.Properties["wheelbase"] = ExtractDimension(html, @"wheelbase[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"车辆规格搜索失败: {ex.Message}");
        }

        return specs;
    }

    /// <summary>
    /// 搜索建筑规格
    /// </summary>
    private async Task<TechnicalSpecs> SearchBuildingSpecsAsync(string buildingName)
    {
        var specs = new TechnicalSpecs();

        try
        {
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(buildingName + " height floors dimensions")}";
            var html = await _httpClient.GetStringAsync(searchUrl);

            specs.Properties["height"] = ExtractDimension(html, @"height[:\s]+(\d+\.?\d*)\s*(m|ft|meter|feet)");
            specs.Properties["floors"] = ExtractNumber(html, @"(\d+)\s*floors?");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"建筑规格搜索失败: {ex.Message}");
        }

        return specs;
    }

    /// <summary>
    /// 通用规格搜索
    /// </summary>
    private async Task<TechnicalSpecs> SearchGeneralSpecsAsync(string objectName)
    {
        var specs = new TechnicalSpecs();

        try
        {
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(objectName + " dimensions size specifications")}";
            var html = await _httpClient.GetStringAsync(searchUrl);

            specs.Properties["length"] = ExtractDimension(html, @"length[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
            specs.Properties["width"] = ExtractDimension(html, @"width[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
            specs.Properties["height"] = ExtractDimension(html, @"height[:\s]+(\d+\.?\d*)\s*(mm|cm|m|inch|ft)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"通用规格搜索失败: {ex.Message}");
        }

        return specs;
    }

    /// <summary>
    /// 搜索多角度参考图片
    /// </summary>
    private async Task<List<string>> SearchReferenceImagesAsync(string objectName)
    {
        var images = new List<string>();

        try
        {
            // 使用Bing Image Search（无需API key的爬虫方式）
            var searchUrl = $"https://www.bing.com/images/search?q={Uri.EscapeDataString(objectName + " multiple angles views")}";
            var html = await _httpClient.GetStringAsync(searchUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // 提取图片URL
            var imgNodes = doc.DocumentNode.SelectNodes("//img[@class='mimg']");
            if (imgNodes != null)
            {
                images.AddRange(imgNodes
                    .Select(img => img.GetAttributeValue("src", null))
                    .Where(src => !string.IsNullOrEmpty(src))
                    .Take(10));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"参考图片搜索失败: {ex.Message}");
        }

        return images;
    }

    // ========== 辅助方法 ==========

    private Dictionary<string, string> ParseInfobox(HtmlNode infobox)
    {
        var data = new Dictionary<string, string>();

        var rows = infobox.SelectNodes(".//tr");
        if (rows != null)
        {
            foreach (var row in rows)
            {
                var header = row.SelectSingleNode(".//th");
                var value = row.SelectSingleNode(".//td");

                if (header != null && value != null)
                {
                    data[header.InnerText.Trim()] = value.InnerText.Trim();
                }
            }
        }

        return data;
    }

    private Dictionary<string, string> ParseBaiduBasicInfo(HtmlNode basicInfo)
    {
        var data = new Dictionary<string, string>();

        var items = basicInfo.SelectNodes(".//dt | .//dd");
        if (items != null)
        {
            for (int i = 0; i < items.Count - 1; i += 2)
            {
                var key = items[i].InnerText.Trim();
                var value = items[i + 1].InnerText.Trim();
                data[key] = value;
            }
        }

        return data;
    }

    private string ExtractDimension(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value + " " + match.Groups[2].Value : null;
    }

    private string ExtractNumber(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private Dimensions ExtractDimensions(ObjectMetadata metadata)
    {
        var dims = new Dimensions();

        // 从多个来源提取尺寸
        var sources = new List<Dictionary<string, string>>();

        if (metadata.WikiData?.InfoboxData != null)
            sources.Add(metadata.WikiData.InfoboxData);

        if (metadata.WikiData?.BaiduData != null)
            sources.Add(metadata.WikiData.BaiduData);

        if (metadata.TechnicalSpecs?.Properties != null)
            sources.Add(metadata.TechnicalSpecs.Properties);

        foreach (var source in sources)
        {
            if (dims.Length == 0)
                dims.Length = ParseDimensionValue(source.GetValueOrDefault("length") ?? source.GetValueOrDefault("长度"));

            if (dims.Width == 0)
                dims.Width = ParseDimensionValue(source.GetValueOrDefault("width") ?? source.GetValueOrDefault("宽度"));

            if (dims.Height == 0)
                dims.Height = ParseDimensionValue(source.GetValueOrDefault("height") ?? source.GetValueOrDefault("高度"));
        }

        return dims;
    }

    private double ParseDimensionValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        // 提取数字
        var match = Regex.Match(value, @"(\d+\.?\d*)");
        if (!match.Success)
            return 0;

        var number = double.Parse(match.Groups[1].Value);

        // 转换单位为毫米
        if (value.Contains("m") && !value.Contains("mm"))
            number *= 1000;
        else if (value.Contains("cm"))
            number *= 10;
        else if (value.Contains("inch"))
            number *= 25.4;
        else if (value.Contains("ft"))
            number *= 304.8;

        return number;
    }

    private StructuralInfo ExtractStructuralInfo(ObjectMetadata metadata)
    {
        var info = new StructuralInfo();

        // 从特征列表提取结构信息
        foreach (var feature in metadata.Features)
        {
            info.KeyFeatures.Add(feature);
        }

        // 从模型网站数据提取
        foreach (var modelData in metadata.ModelSiteData)
        {
            if (modelData.Tags != null)
            {
                info.Tags.AddRange(modelData.Tags);
            }

            if (modelData.VertexCount > 0)
            {
                info.ComplexityReference = Math.Max(info.ComplexityReference, modelData.VertexCount);
            }
        }

        return info;
    }
}

// ========== 数据模型 ==========

public class ObjectMetadata
{
    public string ObjectName { get; set; }
    public string ObjectNameEn { get; set; }
    public string Category { get; set; }
    public List<string> Features { get; set; } = new();
    public WikiData WikiData { get; set; }
    public List<ModelSiteData> ModelSiteData { get; set; } = new();
    public TechnicalSpecs TechnicalSpecs { get; set; }
    public List<string> ReferenceImages { get; set; } = new();
    public Dimensions Dimensions { get; set; }
    public StructuralInfo StructuralInfo { get; set; }
}

public class WikiData
{
    public string Summary { get; set; }
    public string ThumbnailUrl { get; set; }
    public string WikiUrl { get; set; }
    public Dictionary<string, string> InfoboxData { get; set; } = new();
    public Dictionary<string, string> BaiduData { get; set; } = new();
}

public class ModelSiteData
{
    public string Source { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string PreviewUrl { get; set; }
    public int VertexCount { get; set; }
    public int FaceCount { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class TechnicalSpecs
{
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class Dimensions
{
    public double Length { get; set; }  // mm
    public double Width { get; set; }   // mm
    public double Height { get; set; }  // mm
    public double Depth { get; set; }   // mm
    public double Wheelbase { get; set; } // mm (车辆专用)
}

public class StructuralInfo
{
    public List<string> KeyFeatures { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int ComplexityReference { get; set; } // 参考顶点数
}
