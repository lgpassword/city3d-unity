# 架构与扩展指南

## 整体架构

```
┌─────────────────────────────────────────────────────┐
│                    City3DDesktop                      │
├──────────────────────┬──────────────────────────────┤
│   MainWindow         │   DigitalTwinWindow           │
│   (图片转3D工具)      │   (数字孪生查看器)            │
├──────────────────────┴──────────────────────────────┤
│                    Services 层                        │
├─────────────────────────────────────────────────────┤
│  HelixToolkit.Wpf (3D渲染)  │  System.Drawing (图像) │
└─────────────────────────────────────────────────────┘
```

应用采用 Code-Behind 模式（非 MVVM），两个独立窗口各自管理状态和服务实例。

## 服务层设计

### 图片转 3D 管线

```
图片输入
  ├─→ LocalImage23DService        (本地 CPU 算法)
  ├─→ Image23DService             (云端 API: Tripo3D / Meshy / Demo)
  └─→ IntelligentModelGenerationService (AI 智能管线)
        │
        ├─ VisionRecognitionService   (图像识别 → 物体类别)
        ├─ WebCrawlerService          (抓取尺寸/规格数据)
        ├─ IGenerationStrategy        (按类别生成网格)
        │    ├─ VehicleGenerationStrategy
        │    ├─ BuildingGenerationStrategy
        │    ├─ CharacterGenerationStrategy
        │    ├─ ProductGenerationStrategy
        │    ├─ AnimalGenerationStrategy
        │    ├─ FurnitureGenerationStrategy
        │    └─ GenericGenerationStrategy
        └─ ModelExportService         (导出 OBJ/STL/glTF)
```

### 数字孪生管线

```
beijing_center.json
  → SceneDataService.LoadDefault()    (反序列化)
  → CitySceneRenderer                 (GPS→本地坐标→3D几何体)
      ├─ RenderBuildings()
      ├─ RenderRoads()
      ├─ RenderWater()
      ├─ RenderGreen()
      └─ RenderGround()
```

## 各服务职责

| 服务 | 职责 | 输入 | 输出 |
|------|------|------|------|
| `Image23DService` | 调用云端 API 生成 3D | 图片路径 + API Key | .obj/.glb 文件路径 |
| `LocalImage23DService` | 本地图像→网格算法 | 图片路径 + 参数 | .obj 文件路径 |
| `IntelligentModelGenerationService` | 编排 AI 智能生成全流程 | 图片路径 | .obj 文件路径 |
| `VisionRecognitionService` | 调用豆包/DeepSeek 识别图片内容 | 图片 Base64 | 物体名称/类别/特征 |
| `WebCrawlerService` | 从 Wikipedia 等抓取物体规格 | 物体名称 | 尺寸/材质/技术参数 |
| `ModelExportService` | 格式转换导出 | 内存网格 | OBJ/STL/glTF 文件 |
| `CitySceneRenderer` | 将城市数据渲染为 3D 几何体 | DigitalTwinScene | Model3DGroup |
| `SceneDataService` | 加载 JSON 城市数据 | 文件路径 | DigitalTwinScene |

## 数字孪生数据格式

`Data/beijing_center.json` 结构：

```json
{
  "Center": { "Latitude": 39.9042, "Longitude": 116.4074 },
  "ElevationM": 43.5,
  "Terrain": { "SizeM": 1600, "MaxHeightM": 10, "Resolution": 32, "Seed": 42 },
  "Buildings": [
    {
      "Name": "太和殿",
      "CentroidLat": 39.9163, "CentroidLon": 116.3972,
      "WidthM": 63.96, "DepthM": 37.2, "HeightM": 35.05,
      "Floors": 3,
      "Footprint": [{ "Lat": ..., "Lon": ... }, ...]
    }
  ],
  "Roads": [{ "Name": "...", "RoadType": "trunk", "Points": [...] }],
  "Waters": [{ "Name": "...", "Polygon": [...] }],
  "Greens": [{ "Name": "...", "Polygon": [...] }],
  "POIs": [{ "Name": "...", "Lat": ..., "Lon": ..., "Type": "..." }],
  "StreetObjects": [...]
}
```

坐标系转换：GPS (WGS84) → 本地米制坐标，以 `Center` 为原点，使用等距矩形投影。

## 扩展指南

### 添加新的生成策略

1. 在 `Services/Strategies/` 下创建新类，实现 `IGenerationStrategy` 接口：

```csharp
public class MyStrategy : IGenerationStrategy
{
    public string Category => "MyCategory";
    
    public Task<Mesh> GenerateBaseShapeAsync(ObjectMetadata metadata, GenerationConfig config, ...) { ... }
    public Task SculptDetailsAsync(Mesh mesh, ObjectMetadata metadata, ...) { ... }
    public Task ApplyTextureAsync(Mesh mesh, ObjectMetadata metadata, ...) { ... }
}
```

2. 在 `IntelligentModelGenerationService` 构造函数中注册：

```csharp
_strategies["MyCategory"] = new MyStrategy();
```

### 添加新的城市数据

1. 按照 `Data/beijing_center.json` 的 schema 准备新城市的 JSON 数据
2. 数据来源建议：OpenStreetMap 导出 → 转换脚本 → JSON
3. 在 `SceneDataService` 中添加加载方法，或修改 `LoadDefault()` 支持选择城市

### 添加新的导出格式

在 `ModelExportService.cs` 中添加新的导出分支：

```csharp
case "新格式":
    await ExportToNewFormat(mesh, outputPath);
    break;
```

同时在 `MainWindow.xaml` 的导出格式下拉框中添加对应选项。

### 添加新的本地算法

在 `LocalImage23DService.cs` 中：

1. 在 `Algorithm` 枚举中添加新值
2. 实现对应的 `Generate*Mesh` 方法
3. 在 `GenerateAsync` 的 switch 中添加路由

### 添加新的云端 API

在 `Image23DService.cs` 中：

1. 在 `AiProvider` 枚举中添加新值
2. 实现 `GenerateWith*Async` 方法（上传→创建任务→轮询→下载）
3. 在 MainWindow 的设置面板中添加对应 API Key 输入

## 输出目录

| 来源 | 输出路径 |
|------|----------|
| 云端 API / Demo / 本地算法 | `%TEMP%\City3D_Models\` |
| AI 智能生成 | `%USERPROFILE%\Documents\City3D\Generated\` |
| 用户手动导出 | 用户选择的路径 |

## 依赖说明

| 包 | 用途 |
|----|------|
| HelixToolkit.Wpf | 3D 视口、OBJ 加载、模型交互 |
| MaterialDesignThemes + Colors | UI 主题（当前主要使用自定义样式） |
| Newtonsoft.Json | JSON 序列化/反序列化 |
| HtmlAgilityPack | HTML 解析（WebCrawlerService 抓取网页） |
| System.Drawing.Common | 像素级图像处理（本地算法） |
| System.Data.SQLite.Core | 预留（当前未使用，可移除） |
