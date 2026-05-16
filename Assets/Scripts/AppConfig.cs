using UnityEngine;

/// <summary>
/// 应用配置 ScriptableObject。
/// </summary>
[CreateAssetMenu(fileName = "AppConfig", menuName = "City3D/App Config")]
public class AppConfig : ScriptableObject
{
    [Header("AI 服务配置")]
    [Tooltip("本地 AI 识别服务地址")]
    public string aiServiceUrl = "http://localhost:8000";

    [Tooltip("AI 服务超时时间（秒）")]
    public int aiTimeoutSeconds = 10;

    [Header("网络请求配置")]
    [Tooltip("HTTP 请求超时时间（秒）")]
    public int httpTimeoutSeconds = 25;

    [Tooltip("海拔查询超时时间（秒）")]
    public int elevationTimeoutSeconds = 10;

    [Header("缓存配置")]
    [Tooltip("OSM 缓存过期时间（小时）")]
    public int osmCacheExpiryHours = 48;

    [Tooltip("缓存网格精度（度）")]
    public double cacheGridPrecision = 0.002;

    [Header("地理计算常量")]
    [Tooltip("地球每度经度对应的米数（赤道）")]
    public double earthMetersPerDegreeLon = 111320;

    [Tooltip("地球每度纬度对应的米数")]
    public double earthMetersPerDegreeLat = 110540;

    [Tooltip("默认楼层高度（米）")]
    public float defaultFloorHeight = 3.2f;

    [Tooltip("最小建筑尺寸（米）")]
    public float minBuildingSize = 4f;

    [Header("OSM 查询配置")]
    [Tooltip("OSM Overpass API 地址")]
    public string osmOverpassUrl = "https://overpass-api.de/api/interpreter";

    [Tooltip("OSM 查询超时时间（秒）")]
    public int osmQueryTimeoutSeconds = 20;

    [Tooltip("最大建筑数量限制")]
    public int maxBuildingCount = 80;

    [Header("海拔服务配置")]
    [Tooltip("开放海拔 API 地址")]
    public string elevationApiUrl = "https://api.open-elevation.com/api/v1/lookup";

    [Header("数据库配置")]
    [Tooltip("数据库文件名")]
    public string databaseFileName = "city3d.db";

    [Tooltip("位置列表最大显示数量")]
    public int maxLocationListCount = 30;

    [Tooltip("场景列表最大显示数量")]
    public int maxSceneListCount = 20;

    /// <summary>
    /// 获取完整的数据库路径。
    /// </summary>
    public string GetDatabasePath() =>
        System.IO.Path.Combine(Application.persistentDataPath, databaseFileName);
}
