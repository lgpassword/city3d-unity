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
    private readonly HttpClient _http;

    // 应用配置。
    private readonly AppConfig _config;

    /// <summary>
    /// 创建海拔查询器。
    /// </summary>
    /// <param name="config">应用配置。</param>
    public ElevationFetcher(AppConfig config)
    {
        _config = config;
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(config.elevationTimeoutSeconds)
        };
    }

    /// <summary>
    /// 获取指定经纬度的海拔。
    /// </summary>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>海拔，单位米；失败时返回 0。</returns>
    public async Task<double> FetchAsync(double lat, double lon, CancellationToken cancellationToken = default)
    {
        try
        {
            // 调用开放海拔接口并读取第一个查询结果。
            var resp = await _http.GetAsync(
                $"{_config.elevationApiUrl}?locations={lat},{lon}", cancellationToken);
            resp.EnsureSuccessStatusCode();
            var root = JObject.Parse(await resp.Content.ReadAsStringAsync());
            return root["results"]![0]!["elevation"]!.Value<double>();
        }
        catch (OperationCanceledException)
        {
            UnityEngine.Debug.Log("[海拔查询] 操作已取消");
            throw;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[海拔查询] 查询失败 ({lat:F4},{lon:F4})：{ex.GetType().Name} - {ex.Message}");
            return 0;
        }
    }
}
