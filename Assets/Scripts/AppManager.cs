using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 应用总控管理器。
/// </summary>
public class AppManager : MonoBehaviour
{
    // 全局单例。
    public static AppManager I { get; private set; }

    [Header("场景引用")]
    // 场景构建器。
    public CitySceneBuilder sceneBuilder;

    // UI 管理器。
    public UIManager ui;

    // AI 服务客户端。
    private AiClient _ai;

    // OSM 查询器。
    private OsmFetcher _osm;

    // 海拔查询器。
    private ElevationFetcher _elev;

    // 数据库管理器。
    private DatabaseManager _db;

    // 当前加载的图片字节。
    private byte[] _imageBytes;

    // 最近一次生成或加载的场景。
    private CityScene _lastScene;

    // 初始化核心服务。
    private void Awake()
    {
        I = this;
        _db = new DatabaseManager();
        _db.Initialize();
        _ai = new AiClient();
        _osm = new OsmFetcher(_db);
        _elev = new ElevationFetcher();
    }

    // 启动后刷新收藏列表和场景列表。
    private async void Start() => await RefreshListsAsync();

    /// <summary>
    /// 从本地路径加载图片。
    /// </summary>
    /// <param name="path">图片路径。</param>
    public void LoadImage(string path)
    {
        try
        {
            // 读取图片并尝试从 EXIF 中提取 GPS。
            _imageBytes = File.ReadAllBytes(path);
            var gps = ExifGpsReader.Read(_imageBytes);
            if (gps != null) ui.SetGps(gps.Latitude, gps.Longitude);
            ui.SetImagePreview(_imageBytes);
            ui.SetStatus($"图片已加载：{Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ui.SetStatus($"读取图片失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 根据当前图片和 GPS 生成城市场景。
    /// </summary>
    public async void GenerateScene()
    {
        if (_imageBytes == null)
        {
            ui.SetStatus("请先加载图片");
            return;
        }

        double lat = ui.GetLat(), lon = ui.GetLon();
        int radius = ui.GetRadius();

        ui.SetBusy(true);
        ui.SetStatus("正在并行查询 OSM、海拔和 AI 识别");
        try
        {
            // 并行执行 AI 识别、OSM 建筑查询和海拔查询。
            var t1 = _ai.RecognizeAsync(_imageBytes);
            var t2 = _osm.FetchAsync(lat, lon, radius);
            var t3 = _elev.FetchAsync(lat, lon);
            await Task.WhenAll(t1, t2, t3);

            var obj = t1.Result;
            var (l, w, h) = _db.GetSpec(obj.Name);

            // 组装完整场景数据。
            _lastScene = new CityScene
            {
                Center = new GpsCoordinate(lat, lon),
                Buildings = t2.Result,
                ElevationM = t3.Result,
                Terrain = new TerrainConfig
                {
                    SizeM = 400,
                    Resolution = 24,
                    MaxHeightM = Mathf.Max(5, (float)t3.Result * .05f),
                    Seed = (int)(lat * 100)
                },
                StreetObjects = new List<StreetObject>
                {
                    new() { Name = obj.Name, LengthM = l, WidthM = w, HeightM = h, PosX = 0, PosZ = 0 }
                }
            };

            sceneBuilder.Build(_lastScene);
            ui.SetStatus($"{t2.Result.Count} 栋建筑，海拔 {t3.Result:F0}m，识别：{obj.Name}");
            ui.EnableSave(true);
        }
        catch (Exception ex)
        {
            ui.SetStatus($"生成场景失败：{ex.Message}");
        }
        finally
        {
            ui.SetBusy(false);
        }
    }

    /// <summary>
    /// 保存当前场景到数据库。
    /// </summary>
    /// <param name="name">场景名称。</param>
    public async void SaveScene(string name)
    {
        if (_lastScene == null) return;
        var json = JsonConvert.SerializeObject(_lastScene);
        await Task.Run(() => _db.SaveScene(name,
            _lastScene.Center.Latitude, _lastScene.Center.Longitude,
            _lastScene.Buildings.Count, _lastScene.ElevationM, json));
        await RefreshListsAsync();
        ui.SetStatus("场景已保存到数据库");
    }

    /// <summary>
    /// 保存当前位置到数据库。
    /// </summary>
    /// <param name="name">位置名称。</param>
    public async void SaveLocation(string name)
    {
        await Task.Run(() => _db.SaveLocation(name, ui.GetLat(), ui.GetLon(), ui.GetRadius()));
        await RefreshListsAsync();
        ui.SetStatus("位置已收藏");
    }

    /// <summary>
    /// 将收藏位置加载到 GPS 输入框。
    /// </summary>
    /// <param name="loc">收藏位置记录。</param>
    public void LoadLocation(LocationRecord loc)
        => ui.SetGps(loc.Latitude, loc.Longitude);

    /// <summary>
    /// 从数据库加载已保存场景。
    /// </summary>
    /// <param name="rec">场景记录。</param>
    public async void LoadScene(SceneRecord rec)
    {
        var json = await Task.Run(() => _db.GetSceneJson(rec.Id));
        if (json == null) return;
        _lastScene = JsonConvert.DeserializeObject<CityScene>(json);
        if (_lastScene != null)
        {
            sceneBuilder.Build(_lastScene);
            ui.SetStatus($"已加载「{rec.Name}」");
        }
    }

    // 刷新 UI 中的位置列表和场景列表。
    private async Task RefreshListsAsync()
    {
        var locs = await Task.Run(() => _db.GetLocations());
        var scenes = await Task.Run(() => _db.GetScenes());
        ui.RefreshLists(locs, scenes);
    }
}
