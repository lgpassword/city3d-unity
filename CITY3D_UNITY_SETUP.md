# City3D Unity 编辑器配置

## 场景层级

创建 `CityScene` 场景，并按以下层级配置：

```text
CityScene
├── AppManager（空物体）
│   ├── AppManager.cs：拖入 CitySceneBuilder 和 UIManager
│   └── CitySceneBuilder.cs：拖入 BuildingMat、GlassMat、TerrainMat、GroundMat、StreetMat
├── Main Camera
│   └── OrbitCamera.cs
├── Directional Light（X=-50，Y=30）
└── Canvas（Screen Space Overlay）
    ├── StatusText（Text，顶部）
    ├── ProgressBar（Image，默认隐藏）
    ├── LeftPanel（Panel）
    │   ├── PathInput（InputField）
    │   ├── LoadBtn（Button，文字：加载图片）
    │   ├── Preview（RawImage）
    │   ├── LatInput（InputField，默认：31.2304）
    │   ├── LonInput（InputField，默认：121.4737）
    │   ├── RadiusSlider（Slider，范围：100 到 800）
    │   ├── RadiusLabel（Text）
    │   ├── GenerateBtn（Button，文字：生成城市场景）
    │   ├── SceneNameInput（InputField）
    │   ├── SaveSceneBtn（Button，文字：保存场景）
    │   └── SaveLocBtn（Button，文字：收藏位置）
    ├── RightPanel（Panel）
    │   ├── LocationsContent（ScrollView 内容区）
    │   └── ScenesContent（ScrollView 内容区）
    └── InfoPanel（Panel，默认隐藏）
        ├── InfoPanel.cs
        ├── NameText（Text）
        └── InfoText（Text）
```

`UIManager.cs` 挂在 `Canvas` 上，并把所有 UI 组件拖入对应字段。`ListItemPrefab` 使用包含 `Button` 和 `Text` 的预制体。

## 材质参数

已创建以下材质文件：

```text
Assets/Materials/BuildingMat.mat：RGB(85,125,160)，Smoothness 0.3
Assets/Materials/GlassMat.mat：RGB(140,200,240,透明度80)，Transparent
Assets/Materials/TerrainMat.mat：RGB(50,90,48)，Smoothness 0.1
Assets/Materials/GroundMat.mat：RGB(20,28,40)，Smoothness 0.0
Assets/Materials/StreetMat.mat：RGB(210,155,45)，Smoothness 0.2
```

## 启动

```bash
cd services/recognition
pip install -r requirements.txt
python main.py
```

然后在 Unity 中打开项目并点击 Play。

## 验证清单

```text
[ ] http://localhost:8000/health 返回 {"status":"ok"}
[ ] Unity Play 无报错
[ ] city3d.db 文件生成于 Application.persistentDataPath
[ ] 输入图片路径后点击加载，预览显示
[ ] 有 GPS 的手机照片会自动填入经纬度
[ ] 点击生成城市场景后出现 3D 建筑
[ ] 鼠标左键拖动可旋转
[ ] 鼠标右键拖动可平移
[ ] 滚轮可缩放
[ ] 点击建筑后显示楼名和高度
[ ] 保存场景后右侧列表出现
[ ] 点击已保存场景后重新加载渲染
[ ] 收藏位置后点击位置列表可自动填入经纬度
[ ] OSM 断网时使用示例建筑且不崩溃
[ ] 同一地区二次查询会从 SQLite 缓存读取
```
