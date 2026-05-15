# City3D Unity

City3D Unity 是一个 Unity 单进程城市场景生成示例项目。项目通过本地 Python 识别服务、OSM Overpass、Open-Elevation 和 SQLite，把图片、GPS、建筑轮廓和简单街道物体组合成可交互的 3D 城市场景。

## 功能

- 从图片读取 EXIF GPS 坐标。
- 调用本地 Python CLIP 服务识别图片对象。
- 调用 OSM Overpass API 获取建筑轮廓。
- 调用 Open-Elevation API 获取海拔。
- 使用 SQLite 缓存 OSM 查询和保存场景。
- 在 Unity 中程序化生成地面、地形、建筑和街道物体。
- 支持轨道相机旋转、平移和缩放。
- 支持点击建筑查看高度、楼层和坐标。

## 环境要求

- Unity 2022.3 LTS 或更高版本。
- Windows x64。
- Python 3.10 或更高版本。
- 可访问 OSM Overpass API、Open-Elevation API 和 Hugging Face 模型下载源。

## 运行

1. 克隆仓库。

```bash
git clone <仓库地址>
cd 3d
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

## 使用

1. 在左侧输入图片路径。
2. 点击“加载图片”。
3. 如果图片有 GPS，纬度和经度会自动填入；也可以手动输入。
4. 选择查询半径。
5. 点击“生成城市场景”。
6. 使用鼠标左键旋转、右键平移、滚轮缩放。
7. 点击建筑查看详情。
8. 输入名称后可保存场景或收藏位置。

## 目录

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

## 说明

- `Assets/Plugins/x86_64/sqlite3.dll` 是 Windows x64 运行所需的 SQLite 原生库。
- 首次启动 Python 识别服务会下载 CLIP 模型，耗时取决于网络。
- OSM 查询失败时会使用示例建筑，Unity 场景不会中断。
