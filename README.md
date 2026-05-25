# City3DDesktop

基于 WPF + HelixToolkit 的 3D 桌面应用，提供两大核心功能：

1. **图片转 3D 模型** — 支持本地算法、云端 API、AI 智能生成三种方式将图片转换为 3D 模型
2. **数字孪生** — 加载真实城市地理数据，渲染北京中心区域 3D 场景

## 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | .NET 8.0 / WPF |
| 3D 渲染 | HelixToolkit.Wpf 3.1.2 |
| UI 风格 | MaterialDesign + 自定义主题 |
| 数据格式 | JSON (Newtonsoft.Json) |
| 图像处理 | System.Drawing.Common |
| 网页抓取 | HtmlAgilityPack |

## 快速开始

### 环境要求

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (推荐) 或 VS Code

### 构建运行

```bash
dotnet restore
dotnet build
dotnet run
```

或用 Visual Studio 打开 `City3DDesktop.sln`，按 F5 运行。

## 项目结构

```
├── City3DDesktop.sln          # 解决方案文件
├── City3DDesktop.csproj       # 项目文件
├── App.xaml                   # 应用入口、全局样式
├── MainWindow.xaml/.cs        # 主窗口 - 图片转3D工具
├── DigitalTwinWindow.xaml/.cs # 数字孪生窗口
├── Models/
│   └── DigitalTwinModels.cs   # 城市场景数据模型
├── Services/
│   ├── Image23DService.cs     # 云端 API (Tripo3D/Meshy/Demo)
│   ├── LocalImage23DService.cs # 本地算法 (HeightMap/Relief/Voxel/Contour)
│   ├── IntelligentModelGenerationService.cs  # AI 智能生成管线
│   ├── VisionRecognitionService.cs  # 视觉识别 (豆包/DeepSeek)
│   ├── WebCrawlerService.cs   # 网页元数据抓取
│   ├── ModelExportService.cs  # 模型导出 (OBJ/STL/glTF)
│   ├── CitySceneRenderer.cs   # 城市3D场景渲染器
│   ├── SceneDataService.cs    # 场景数据加载
│   └── Strategies/            # 智能生成策略
│       ├── VehicleGenerationStrategy.cs
│       └── OtherStrategies.cs
├── Data/
│   └── beijing_center.json    # 北京中心区域城市数据
└── docs/
    └── ARCHITECTURE.md        # 架构与扩展文档
```

## 功能说明

### 图片转 3D 模型

| 模式 | 说明 | 需要 API Key |
|------|------|:---:|
| Demo | 生成示例房屋模型，用于测试流程 | 否 |
| 本地算法 | HeightMap / Relief / Voxel / Contour 四种纯 CPU 算法 | 否 |
| Tripo3D | 云端图片转 3D API | 是 |
| Meshy | 云端图片转 3D API | 是 |
| AI 智能生成 | 视觉识别 → 网页抓取 → 策略生成 | 是 (豆包/DeepSeek) |

### 数字孪生

加载 `Data/beijing_center.json` 中的北京中心区域数据，渲染建筑、道路、水域、绿地、POI 五个图层，支持：
- 图层开关
- 建筑点选查看属性
- 3D 视角旋转缩放

## API 配置

点击主窗口右上角「设置」按钮，填入对应 API Key：
- **Tripo3D**: 从 [tripo3d.ai](https://tripo3d.ai) 获取
- **Meshy**: 从 [meshy.ai](https://meshy.ai) 获取
- **AI 智能生成**: 需要豆包或 DeepSeek 的 Vision API Key

配置保存在 `%APPDATA%\City3DDesktop\settings.json`。

## 许可证

MIT License
