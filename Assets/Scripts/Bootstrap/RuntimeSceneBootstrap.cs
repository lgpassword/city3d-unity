using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 运行时场景自动引导器。
/// </summary>
public static class RuntimeSceneBootstrap
{
    // 默认字体资源路径。
    private const string DefaultFontPath = "LegacyRuntime.ttf";

    /// <summary>
    /// 场景加载后自动创建 City3D 所需对象。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Bootstrap()
    {
        if (Object.FindObjectOfType<AppManager>() != null) return;

        var font = Resources.GetBuiltinResource<Font>(DefaultFontPath);
        var materials = CreateMaterials();
        var canvas = CreateCanvas();
        var ui = canvas.gameObject.AddComponent<UIManager>();

        CreateCamera();
        CreateLight();
        CreateEventSystem();
        CreateManagers(ui, materials);
        CreateControlPanel(canvas.transform, ui, font);

        Debug.Log("[启动] City3D 运行时场景已自动配置");
    }

    // 创建相机并绑定轨道控制脚本。
    private static void CreateCamera()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<OrbitCamera>();
        cameraObject.transform.position = new Vector3(80, 80, 80);
        cameraObject.transform.LookAt(Vector3.zero);
    }

    // 创建方向光。
    private static void CreateLight()
    {
        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    // 创建 UI 事件系统。
    private static void CreateEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null) return;

        var eventObject = new GameObject("EventSystem");
        eventObject.AddComponent<EventSystem>();
        eventObject.AddComponent<StandaloneInputModule>();
    }

    // 创建应用管理器和场景构建器。
    private static void CreateManagers(UIManager ui, BootstrapMaterials materials)
    {
        var managerObject = new GameObject("AppManager");
        var builder = managerObject.AddComponent<CitySceneBuilder>();
        builder.buildingMat = materials.Building;
        builder.glassMat = materials.Glass;
        builder.terrainMat = materials.Terrain;
        builder.groundMat = materials.Ground;
        builder.streetMat = materials.Street;

        var manager = managerObject.AddComponent<AppManager>();
        manager.sceneBuilder = builder;
        manager.ui = ui;
        // 配置会在Awake中自动加载
    }

    // 创建屏幕空间画布。
    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // 创建完整控制面板并绑定到 UIManager。
    private static void CreateControlPanel(Transform root, UIManager ui, Font font)
    {
        var status = CreateText(root, "StatusText", "请输入图片路径并生成城市场景", font, 15, TextAnchor.MiddleLeft);
        SetAnchor(status.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(12, -36), new Vector2(-12, -8));
        ui.StatusText = status;

        var progress = CreatePanel(root, "ProgressBar", new Color(0.18f, 0.55f, 0.85f, 0.85f));
        SetAnchor(progress.GetComponent<RectTransform>(), new Vector2(0.35f, 1), new Vector2(0.65f, 1), new Vector2(0, -58), new Vector2(0, -44));
        progress.SetActive(false);
        ui.ProgressBar = progress;

        var leftPanel = CreatePanel(root, "LeftPanel", new Color(0.05f, 0.07f, 0.09f, 0.88f));
        SetAnchor(leftPanel.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 1), new Vector2(12, 12), new Vector2(340, -68));

        var rightPanel = CreatePanel(root, "RightPanel", new Color(0.05f, 0.07f, 0.09f, 0.88f));
        SetAnchor(rightPanel.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-330, 12), new Vector2(-12, -68));

        ui.PathInput = CreateInput(leftPanel.transform, "PathInput", "", "图片完整路径", font, 18, -42);
        ui.LoadBtn = CreateButton(leftPanel.transform, "LoadBtn", "加载图片", font, 18, -86);
        ui.Preview = CreatePreview(leftPanel.transform, "Preview", 18, -246);
        ui.LatInput = CreateInput(leftPanel.transform, "LatInput", "31.2304", "纬度", font, 18, -292);
        ui.LonInput = CreateInput(leftPanel.transform, "LonInput", "121.4737", "经度", font, 18, -336);
        ui.RadiusSlider = CreateSlider(leftPanel.transform, "RadiusSlider", 18, -386);
        ui.RadiusLabel = CreateText(leftPanel.transform, "RadiusLabel", "300 m", font, 14, TextAnchor.MiddleRight);
        SetRect(ui.RadiusLabel.rectTransform, 236, -386, 76, 28);
        ui.GenerateBtn = CreateButton(leftPanel.transform, "GenerateBtn", "生成城市场景", font, 18, -438);
        ui.SceneNameInput = CreateInput(leftPanel.transform, "SceneNameInput", "我的场景", "场景或位置名称", font, 18, -486);
        ui.SaveSceneBtn = CreateButton(leftPanel.transform, "SaveSceneBtn", "保存场景", font, 18, -530);
        ui.SaveLocBtn = CreateButton(leftPanel.transform, "SaveLocBtn", "收藏位置", font, 174, -530);

        CreateText(rightPanel.transform, "LocationsTitle", "收藏位置", font, 16, TextAnchor.MiddleLeft).rectTransform.anchoredPosition = new Vector2(16, -24);
        ui.LocationsContent = CreateListContent(rightPanel.transform, "LocationsContent", 16, -190);
        CreateText(rightPanel.transform, "ScenesTitle", "保存场景", font, 16, TextAnchor.MiddleLeft).rectTransform.anchoredPosition = new Vector2(16, -224);
        ui.ScenesContent = CreateListContent(rightPanel.transform, "ScenesContent", 16, -398);
        ui.ListItemPrefab = CreateListItemPrefab(root, font);

        var infoPanel = CreatePanel(root, "InfoPanel", new Color(0.04f, 0.05f, 0.07f, 0.92f));
        SetAnchor(infoPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-220, 24), new Vector2(220, 128));
        var info = infoPanel.AddComponent<InfoPanel>();
        info.nameText = CreateText(infoPanel.transform, "NameText", "", font, 18, TextAnchor.MiddleLeft);
        SetRect(info.nameText.rectTransform, 16, -28, 408, 28);
        info.infoText = CreateText(infoPanel.transform, "InfoText", "", font, 14, TextAnchor.UpperLeft);
        SetRect(info.infoText.rectTransform, 16, -62, 408, 72);
        ui.infoPanel = info;
        infoPanel.SetActive(false);
    }

    // 创建可复用列表项模板。
    private static GameObject CreateListItemPrefab(Transform root, Font font)
    {
        var item = CreatePanel(root, "ListItemPrefab", new Color(0.12f, 0.16f, 0.2f, 0.95f));
        item.AddComponent<Button>();
        var text = CreateText(item.transform, "Text", "列表项", font, 13, TextAnchor.MiddleLeft);
        SetAnchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        item.SetActive(false);
        return item;
    }

    // 创建列表内容容器。
    private static Transform CreateListContent(Transform parent, string name, float x, float y)
    {
        var content = new GameObject(name, typeof(RectTransform));
        content.transform.SetParent(parent, false);
        var rect = content.GetComponent<RectTransform>();
        SetRect(rect, x, y, 286, 154);
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.childForceExpandHeight = false;
        layout.childControlHeight = true;
        return content.transform;
    }

    // 创建图片预览区域。
    private static RawImage CreatePreview(Transform parent, string name, float x, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect(go.GetComponent<RectTransform>(), x, y, 294, 132);
        var image = go.AddComponent<RawImage>();
        image.color = new Color(0.02f, 0.025f, 0.03f, 1f);
        return image;
    }

    // 创建输入框。
    private static InputField CreateInput(Transform parent, string name, string value, string placeholder, Font font, float x, float y)
    {
        var go = CreatePanel(parent, name, new Color(0.12f, 0.14f, 0.16f, 1f));
        SetRect(go.GetComponent<RectTransform>(), x, y, 294, 34);

        var input = go.AddComponent<InputField>();
        var text = CreateText(go.transform, "Text", "", font, 14, TextAnchor.MiddleLeft);
        SetAnchor(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        var hint = CreateText(go.transform, "Placeholder", placeholder, font, 14, TextAnchor.MiddleLeft);
        hint.color = new Color(1, 1, 1, 0.45f);
        SetAnchor(hint.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0));
        input.textComponent = text;
        input.placeholder = hint;
        input.text = value;
        return input;
    }

    // 创建按钮。
    private static Button CreateButton(Transform parent, string name, string label, Font font, float x, float y)
    {
        var go = CreatePanel(parent, name, new Color(0.18f, 0.42f, 0.68f, 1f));
        SetRect(go.GetComponent<RectTransform>(), x, y, 138, 34);
        var button = go.AddComponent<Button>();
        var text = CreateText(go.transform, "Text", label, font, 14, TextAnchor.MiddleCenter);
        SetAnchor(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    // 创建半径滑块。
    private static Slider CreateSlider(Transform parent, string name, float x, float y)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect(go.GetComponent<RectTransform>(), x, y, 210, 28);

        var slider = go.AddComponent<Slider>();
        slider.minValue = 100;
        slider.maxValue = 800;
        slider.value = 300;

        var background = CreatePanel(go.transform, "Background", new Color(0.18f, 0.2f, 0.23f, 1f));
        SetAnchor(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var fill = CreatePanel(go.transform, "Fill", new Color(0.18f, 0.55f, 0.85f, 1f));
        SetAnchor(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var handle = CreatePanel(go.transform, "Handle", new Color(0.95f, 0.95f, 0.95f, 1f));
        SetRect(handle.GetComponent<RectTransform>(), 0, 0, 18, 28);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = handle.GetComponent<Image>();
        return slider;
    }

    // 创建文本控件。
    private static Text CreateText(Transform parent, string name, string value, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        SetRect(text.rectTransform, 16, -24, 280, 28);
        return text;
    }

    // 创建带图片组件的面板。
    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }

    // 设置矩形尺寸和位置。
    private static void SetRect(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(w, h);
    }

    // 设置锚点和偏移。
    private static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // 创建默认运行时材质。
    private static BootstrapMaterials CreateMaterials()
    {
        return new BootstrapMaterials
        {
            Building = CreateMaterial("BuildingMat", new Color32(85, 125, 160, 255), 0.3f, false),
            Glass = CreateMaterial("GlassMat", new Color32(140, 200, 240, 80), 0.5f, true),
            Terrain = CreateMaterial("TerrainMat", new Color32(50, 90, 48, 255), 0.1f, false),
            Ground = CreateMaterial("GroundMat", new Color32(20, 28, 40, 255), 0f, false),
            Street = CreateMaterial("StreetMat", new Color32(210, 155, 45, 255), 0.2f, false)
        };
    }

    // 创建单个材质并配置透明模式。
    private static Material CreateMaterial(string name, Color color, float smoothness, bool transparent)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader) { name = name, color = color };
        material.SetFloat("_Glossiness", smoothness);
        if (transparent)
        {
            material.SetFloat("_Mode", 2);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 3000;
        }
        return material;
    }

    /// <summary>
    /// 运行时默认材质集合。
    /// </summary>
    private class BootstrapMaterials
    {
        // 建筑材质。
        public Material Building;

        // 玻璃材质。
        public Material Glass;

        // 地形材质。
        public Material Terrain;

        // 地面材质。
        public Material Ground;

        // 街道物体材质。
        public Material Street;
    }
}
