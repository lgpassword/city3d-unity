# City3D Unity 工程说明 / Project Overview

## 1. 项目定位 / Project Purpose

### 中文

City3D Unity 是一个完整的 Unity 城市场景生成工程。它将图片识别、GPS 解析、在线地图建筑数据、海拔数据、本地数据库缓存和 Unity 程序化建模整合到一个可运行示例中。项目目标是让开发者可以快速查看“从图片和位置生成 3D 城市场景”的完整链路。

### English

City3D Unity is a complete Unity city scene generation project. It integrates image recognition, GPS parsing, online map building data, elevation data, local database caching, and procedural modeling in Unity into one runnable example. The goal is to provide a complete pipeline for generating a 3D city scene from an image and a location.

## 2. 总体架构 / Architecture

### 中文

工程由 Unity 主程序和本地 Python 识别服务组成。Unity 负责 UI、网络请求、数据库、GPS 转换、网格生成和场景交互；Python 服务负责图片零样本识别。外部网络依赖包括 OSM Overpass API 和 Open-Elevation API。

### English

The project consists of the Unity application and a local Python recognition service. Unity handles UI, network requests, database access, GPS conversion, mesh generation, and scene interaction. The Python service handles zero-shot image recognition. External network dependencies include OSM Overpass API and Open-Elevation API.

```text
Unity Application
├── Local HTTP -> Python recognition service
├── HTTP -> OSM Overpass API
├── HTTP -> Open-Elevation API
├── SQLite -> local cache and saved scenes
├── GPS -> local Unity coordinate conversion
├── Procedural mesh generation
├── Runtime scene bootstrapping
└── UGUI control panel
```

## 3. 主要模块 / Main Modules

### Unity 脚本 / Unity Scripts

```text
Assets/Scripts/AppManager.cs
```

中文：应用总控，负责初始化数据库、网络客户端、识别客户端，并协调图片加载、场景生成、保存和加载。

English: The central application controller. It initializes the database, network clients, and recognition client, then coordinates image loading, scene generation, saving, and loading.

```text
Assets/Scripts/Bootstrap/RuntimeSceneBootstrap.cs
```

中文：运行时自动引导器。空场景进入 Play 后自动创建相机、灯光、管理器、材质和 UI 控制面板。

English: Runtime bootstrapper. It automatically creates the camera, light, managers, materials, and UI control panel when Play starts in an empty scene.

```text
Assets/Scripts/Network/
```

中文：包含 AI 服务客户端、OSM 数据获取器和海拔获取器。

English: Contains the AI service client, OSM data fetcher, and elevation fetcher.

```text
Assets/Scripts/Database/
```

中文：包含 sqlite-net 单文件 ORM、数据库记录模型和数据库管理器。

English: Contains the sqlite-net single-file ORM, database record models, and database manager.

```text
Assets/Scripts/Geo/
```

中文：包含 EXIF GPS 解析器和 GPS 到 Unity 本地坐标的转换工具。

English: Contains the EXIF GPS reader and GPS-to-Unity local coordinate converter.

```text
Assets/Scripts/Mesh/
```

中文：包含建筑、地形、街道物体和城市场景的程序化生成逻辑。

English: Contains procedural generation logic for buildings, terrain, street objects, and the full city scene.

```text
Assets/Scripts/UI/
```

中文：包含控制面板、建筑选择和信息面板逻辑。

English: Contains control panel, building selection, and information panel logic.

```text
services/recognition/
```

中文：Python FastAPI 服务，使用 CLIP 做图片零样本分类。

English: Python FastAPI service that uses CLIP for zero-shot image classification.

## 4. 数据流程 / Data Flow

### 中文

1. 用户输入图片路径并加载图片。
2. Unity 读取图片字节，并尝试解析 EXIF GPS。
3. 用户确认或手动输入经纬度和查询半径。
4. Unity 并行调用本地 AI 服务、OSM Overpass API 和 Open-Elevation API。
5. OSM 建筑轮廓被转换为 Unity 本地坐标。
6. Unity 生成地面、地形、建筑主体、窗户和街道物体。
7. 用户可保存场景 JSON 或收藏位置到 SQLite。
8. 二次查询同一区域时，OSM 数据优先从 SQLite 缓存读取。

### English

1. The user enters an image path and loads the image.
2. Unity reads image bytes and tries to parse EXIF GPS data.
3. The user confirms or manually enters latitude, longitude, and query radius.
4. Unity calls the local AI service, OSM Overpass API, and Open-Elevation API in parallel.
5. OSM building footprints are converted into Unity local coordinates.
6. Unity generates the ground, terrain, building bodies, windows, and street objects.
7. The user can save scene JSON or favorite locations into SQLite.
8. When querying the same area again, OSM data is read from SQLite cache first.

## 5. 运行依赖 / Runtime Dependencies

### 中文

- Unity 2022.3 LTS 或更高版本。
- Python 3.10 或更高版本。
- `Assets/Plugins/x86_64/sqlite3.dll` 用于 Windows x64 SQLite 原生调用。
- `services/recognition/requirements.txt` 中列出的 Python 包。
- 首次启动识别服务时需要下载 CLIP 模型。

### English

- Unity 2022.3 LTS or newer.
- Python 3.10 or newer.
- `Assets/Plugins/x86_64/sqlite3.dll` for Windows x64 SQLite native calls.
- Python packages listed in `services/recognition/requirements.txt`.
- The CLIP model is downloaded on the first recognition service startup.

## 6. 可运行性说明 / Runability Notes

### 中文

仓库包含 Unity 运行所需的脚本、材质、SQLite 原生库、包配置和 Python 服务代码。拉取代码后不需要手动创建脚本文件。打开 Unity 后，运行时引导脚本会自动创建默认场景对象和 UI 控件。

仍需手动启动 Python 识别服务，因为 Unity 通过 `http://localhost:8000` 调用该服务。

### English

The repository contains the Unity scripts, materials, SQLite native library, package configuration, and Python service code needed to run the project. After cloning, no script files need to be created manually. Once opened in Unity, the runtime bootstrap script automatically creates the default scene objects and UI controls.

The Python recognition service still needs to be started manually because Unity calls it through `http://localhost:8000`.

## 7. 开源协议 / License

### 中文

本工程使用 MIT License，可用于学习、修改、分发和二次开发。

### English

This project is licensed under the MIT License and can be used for learning, modification, distribution, and further development.
