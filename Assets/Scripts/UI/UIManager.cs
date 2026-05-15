using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity UGUI 控制面板管理器。
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("图片")]
    // 图片路径输入框。
    public InputField PathInput;

    // 图片预览控件。
    public RawImage Preview;

    // 加载图片按钮。
    public Button LoadBtn;

    [Header("GPS")]
    // 纬度和经度输入框。
    public InputField LatInput, LonInput;

    // 查询半径滑块。
    public Slider RadiusSlider;

    // 查询半径文本。
    public Text RadiusLabel;

    // 生成场景按钮。
    public Button GenerateBtn;

    [Header("保存")]
    // 场景或位置名称输入框。
    public InputField SceneNameInput;

    // 保存场景按钮。
    public Button SaveSceneBtn;

    // 收藏位置按钮。
    public Button SaveLocBtn;

    [Header("列表")]
    // 收藏位置列表容器。
    public Transform LocationsContent;

    // 已保存场景列表容器。
    public Transform ScenesContent;

    // 列表项预制体。
    public GameObject ListItemPrefab;

    [Header("状态")]
    // 状态文本。
    public Text StatusText;

    // 进度条对象。
    public GameObject ProgressBar;

    [Header("建筑信息")]
    // 建筑信息面板。
    public InfoPanel infoPanel;

    // 注册 UI 事件并设置初始状态。
    private void Start()
    {
        LoadBtn.onClick.AddListener(() => AppManager.I.LoadImage(PathInput.text));
        GenerateBtn.onClick.AddListener(() => AppManager.I.GenerateScene());
        SaveSceneBtn.onClick.AddListener(() => AppManager.I.SaveScene(SceneNameInput.text));
        SaveLocBtn.onClick.AddListener(() => AppManager.I.SaveLocation(SceneNameInput.text));
        RadiusSlider.onValueChanged.AddListener(v => RadiusLabel.text = $"{(int)v} m");
        RadiusSlider.value = 300;
        EnableSave(false);
    }

    /// <summary>
    /// 设置 GPS 输入框。
    /// </summary>
    /// <param name="lat">纬度。</param>
    /// <param name="lon">经度。</param>
    public void SetGps(double lat, double lon)
    {
        LatInput.text = lat.ToString("F6");
        LonInput.text = lon.ToString("F6");
    }

    /// <summary>
    /// 获取纬度输入值。
    /// </summary>
    /// <returns>纬度。</returns>
    public double GetLat()
    {
        double.TryParse(LatInput.text, out double v);
        return v;
    }

    /// <summary>
    /// 获取经度输入值。
    /// </summary>
    /// <returns>经度。</returns>
    public double GetLon()
    {
        double.TryParse(LonInput.text, out double v);
        return v;
    }

    /// <summary>
    /// 获取查询半径。
    /// </summary>
    /// <returns>查询半径，单位米。</returns>
    public int GetRadius() => (int)RadiusSlider.value;

    /// <summary>
    /// 设置图片预览。
    /// </summary>
    /// <param name="bytes">图片字节数据。</param>
    public void SetImagePreview(byte[] bytes)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        Preview.texture = tex;
    }

    /// <summary>
    /// 设置状态文本。
    /// </summary>
    /// <param name="msg">状态消息。</param>
    public void SetStatus(string msg)
    {
        StatusText.text = msg;
        Debug.Log($"[界面] {msg}");
    }

    /// <summary>
    /// 设置忙碌状态。
    /// </summary>
    /// <param name="busy">是否正在执行生成任务。</param>
    public void SetBusy(bool busy)
    {
        ProgressBar.SetActive(busy);
        GenerateBtn.interactable = !busy;
    }

    /// <summary>
    /// 设置保存场景按钮可用状态。
    /// </summary>
    /// <param name="v">是否可用。</param>
    public void EnableSave(bool v) => SaveSceneBtn.interactable = v;

    /// <summary>
    /// 刷新收藏位置和保存场景列表。
    /// </summary>
    /// <param name="locs">收藏位置列表。</param>
    /// <param name="scenes">保存场景列表。</param>
    public void RefreshLists(List<LocationRecord> locs, List<SceneRecord> scenes)
    {
        Refresh(LocationsContent, locs, rec =>
        {
            var lr = rec as LocationRecord;
            return (lr.ToString(), () => AppManager.I.LoadLocation(lr));
        });
        Refresh(ScenesContent, scenes, rec =>
        {
            var sr = rec as SceneRecord;
            return (sr.ToString(), () => AppManager.I.LoadScene(sr));
        });
    }

    // 刷新指定列表容器中的按钮项。
    private void Refresh<T>(Transform parent, List<T> items, Func<T, (string label, Action onClick)> selector)
    {
        // 清空旧列表项。
        foreach (Transform c in parent) Destroy(c.gameObject);

        // 为每条记录创建按钮并绑定点击事件。
        foreach (var item in items)
        {
            var (label, action) = selector(item);
            var go = Instantiate(ListItemPrefab, parent);
            // 激活由隐藏模板克隆出来的列表项。
            go.SetActive(true);
            var txt = go.GetComponentInChildren<Text>();
            if (txt) txt.text = label;
            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => action());
        }
    }
}
