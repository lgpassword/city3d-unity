using UnityEngine;

/// <summary>
/// 建筑点击选择器。
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    // 当前建筑绑定的数据。
    public BuildingData data;

    // 鼠标点击建筑时显示详情面板。
    private void OnMouseDown()
    {
        var panel = FindObjectOfType<InfoPanel>();
        if (panel == null || data == null) return;
        panel.Show(data.Name,
            $"高度：{data.HeightM:F1}m\n楼层：{data.Floors}层\n坐标：{data.CentroidLat:F4},{data.CentroidLon:F4}");
    }
}
