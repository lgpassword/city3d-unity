using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

/// <summary>
/// 开放海拔服务查询器。
/// </summary>
public class ElevationFetcher
{
    // 复用 HTTP 客户端查询开放海拔接口。
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    /// <summary>
    /// 获取指定经纬度的海拔。
    /// </summary>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    /// <returns>海拔，单位米；失败时返回 0。</returns>
    public async Task<double> FetchAsync(double lat, double lon)
    {
        try
        {
            // 调用开放海拔接口并读取第一个查询结果。
            var resp = await Http.GetAsync(
                $"https://api.open-elevation.com/api/v1/lookup?locations={lat},{lon}");
            resp.EnsureSuccessStatusCode();
            var root = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return root["results"]![0]!["elevation"]!.Value<double>();
        }
        catch
        {
            return 0;
        }
    }
}
