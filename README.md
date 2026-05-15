# City3D Unity

中文 | [English](#english)

## 中文

City3D Unity 是一个 Unity 单进程城市场景生成工程。项目通过本地 Python 图像识别服务、OSM Overpass API、Open-Elevation API 和 SQLite，把图片、GPS、建筑轮廓、海拔和标准街道物体组合成可交互的 3D 城市场景。

本工程面向城市可视化、地理信息展示、Unity 程序化建模和 AI 识别集成示例。克隆后用 Unity 打开仓库根目录，启动本地识别服务，即可点击 Play 运行。

### 核心功能

- 从图片读取 EXIF GPS 坐标。
- 调用本地 Python CLIP 服务识别图片对象。
- 调用 OSM Overpass API 获取建筑轮廓。
- 调用 Open-Elevation API 获取海拔。
- 使用 SQLite 缓存 OSM 查询和保存场景。
- 在 Unity 中程序化生成地面、地形、建筑和街道物体。
- 支持轨道相机旋转、平移和缩放。
- 支持点击建筑查看高度、楼层和坐标。
- 支持保存场景和收藏位置。
- 支持空场景运行时自动创建相机、灯光、管理器和 UI 控制面板。

### 环境要求

- Unity 2022.3 LTS 或更高版本。
- Windows x64。
- Python 3.10 或更高版本。
- 可访问 OSM Overpass API、Open-Elevation API 和 Hugging Face 模型下载源。

### 快速运行

1. 克隆仓库。

```bash
git clone https://github.com/lgpassword/city3d-unity.git
cd city3d-unity
```

2. 启动本地识别服务。

```bash
cd services/recognition
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

3. 用 Unity 打开仓库根目录。

4. 点击 Play。

项目包含运行时自动引导逻辑。即使当前场景为空，点击 Play 后也会自动创建相机、灯光、AppManager、CitySceneBuilder 和完整 UGUI 控制面板。

### 使用方式

1. 在左侧输入图片路径。
2. 点击“加载图片”。
3. 如果图片有 GPS，纬度和经度会自动填入；也可以手动输入。
4. 选择查询半径。
5. 点击“生成城市场景”。
6. 使用鼠标左键旋转、右键平移、滚轮缩放。
7. 点击建筑查看详情。
8. 输入名称后可保存场景或收藏位置。

### 工程目录

```text
Assets/
├── Materials/
├── Plugins/x86_64/sqlite3.dll
└── Scripts/
    ├── Bootstrap/
    ├── Camera/
    ├── Database/
    ├── Geo/
    ├── Mesh/
    ├── Network/
    └── UI/
services/recognition/
├── main.py
└── requirements.txt
```

更完整的工程说明见 [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md)。

### 许可证

本项目使用 MIT License。

---

## English

City3D Unity is a Unity-based city scene generation project that runs as a single Unity application. It combines a local Python image recognition service, OSM Overpass API, Open-Elevation API, and SQLite to turn images, GPS coordinates, building footprints, elevation data, and simple street objects into an interactive 3D city scene.

This project is designed as a practical example for city visualization, GIS-style presentation, procedural modeling in Unity, and AI recognition integration. Clone the repository, open the root folder with Unity, start the local recognition service, and press Play.

### Features

- Reads EXIF GPS coordinates from images.
- Uses a local Python CLIP service for image recognition.
- Fetches building footprints from OSM Overpass API.
- Fetches elevation data from Open-Elevation API.
- Uses SQLite for OSM caching and saved scenes.
- Procedurally generates ground, terrain, buildings, and street objects in Unity.
- Supports orbit camera rotation, panning, and zooming.
- Supports clicking buildings to inspect height, floor count, and coordinates.
- Supports saving scenes and favorite locations.
- Automatically creates the camera, light, managers, and UGUI control panel at runtime when the scene is empty.

### Requirements

- Unity 2022.3 LTS or newer.
- Windows x64.
- Python 3.10 or newer.
- Network access to OSM Overpass API, Open-Elevation API, and Hugging Face model downloads.

### Quick Start

1. Clone the repository.

```bash
git clone https://github.com/lgpassword/city3d-unity.git
cd city3d-unity
```

2. Start the local recognition service.

```bash
cd services/recognition
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python main.py
```

3. Open the repository root folder with Unity.

4. Press Play.

The project includes runtime bootstrapping. Even if the scene is empty, pressing Play automatically creates the camera, light, AppManager, CitySceneBuilder, and the full UGUI control panel.

### Usage

1. Enter an image path in the left panel.
2. Click "加载图片" to load the image.
3. If the image contains GPS data, latitude and longitude are filled automatically. You can also enter them manually.
4. Choose the query radius.
5. Click "生成城市场景" to generate the city scene.
6. Use left mouse drag to rotate, right mouse drag to pan, and the mouse wheel to zoom.
7. Click a building to inspect details.
8. Enter a name to save the scene or favorite the location.

### Project Layout

```text
Assets/
├── Materials/
├── Plugins/x86_64/sqlite3.dll
└── Scripts/
    ├── Bootstrap/
    ├── Camera/
    ├── Database/
    ├── Geo/
    ├── Mesh/
    ├── Network/
    └── UI/
services/recognition/
├── main.py
└── requirements.txt
```

See [PROJECT_OVERVIEW.md](PROJECT_OVERVIEW.md) for the full bilingual project overview.

### License

This project is licensed under the MIT License.
