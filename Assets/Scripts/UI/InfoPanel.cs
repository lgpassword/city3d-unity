using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 建筑信息面板。
/// </summary>
public class InfoPanel : MonoBehaviour
{
    // 名称文本控件。
    public Text nameText;

    // 详情文本控件。
    public Text infoText;

    // 启动时隐藏信息面板。
    private void Start() => gameObject.SetActive(false);

    /// <summary>
    /// 显示建筑信息。
    /// </summary>
    /// <param name="name">建筑名称。</param>
    /// <param name="info">建筑详情。</param>
    public void Show(string name, string info)
    {
        nameText.text = name;
        infoText.text = info;
        gameObject.SetActive(true);
    }

    // 按下 Escape 时关闭信息面板。
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) gameObject.SetActive(false);
    }
}
