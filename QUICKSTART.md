# 城市3D场景生成器 - 快速启动指南

## 🚀 如何运行项目

### 方法1：Unity编辑器运行（推荐）

#### 1. 安装Unity
- 下载并安装 **Unity 2022.3.62f3 LTS**
- 下载地址：https://unity.com/releases/editor/archive
- 或通过Unity Hub安装

#### 2. 打开项目
1. 打开 Unity Hub
2. 点击"添加" → 选择此项目文件夹
3. 等待Unity导入项目（首次打开需要3-5分钟）

#### 3. 运行项目
1. 在Unity编辑器中，点击顶部的 **▶ Play** 按钮
2. 界面会自动生成（无需创建场景文件）
3. 程序会自动加载默认配置

### 方法2：构建独立应用程序

#### 1. 在Unity中构建
1. 菜单：`File` → `Build Settings`
2. 选择平台：`Windows` / `Mac` / `Linux`
3. 点击 `Build` 并选择输出文件夹
4. 等待构建完成（约2-5分钟）

#### 2. 运行构建的程序
- Windows: 双击 `.exe` 文件
- Mac: 双击 `.app` 文件
- Linux: 运行可执行文件

---

## 📋 系统要求

### 开发环境
- **Unity**: 2022.3.62f3 或更高版本
- **Visual Studio**: 2019/2022（用于代码编辑）
- **.NET**: 4.x 或更高
- **操作系统**: Windows 10/11, macOS 10.15+, Ubuntu 20.04+

### 运行时依赖
- **本地AI服务**（可选）: http://localhost:8000
  - 如果AI服务不可用，会使用默认识别结果
- **网络连接**: 用于查询OSM建筑数据和海拔信息
  - 离线模式会使用缓存和示例数据

---

## 🎮 使用说明

### 基本流程
1. **加载图片**
   - 输入图片完整路径
   - 点击"加载图片"按钮
   - 如果图片包含GPS信息，会自动填充经纬度

2. **设置位置**
   - 手动输入纬度和经度
   - 或从"收藏位置"列表中选择

3. **生成场景**
   - 调整查询半径（100-800米）
   - 点击"生成城市场景"按钮
   - 等待并行查询完成（OSM + 海拔 + AI识别）

4. **保存场景**
   - 输入场景名称
   - 点击"保存场景"或"收藏位置"

### 相机控制
- **旋转**: 鼠标右键拖拽
- **缩放**: 鼠标滚轮
- **平移**: 鼠标中键拖拽

### 建筑信息
- 点击场景中的建筑物查看详细信息
- 显示建筑名称、高度、楼层数等

---

## ⚙️ 配置说明

### AppConfig 配置文件
位置：`Assets/Resources/DefaultAppConfig.asset`

可配置项：
```
AI服务配置:
- aiServiceUrl: AI识别服务地址（默认: http://localhost:8000）
- aiTimeoutSeconds: AI请求超时时间（默认: 10秒）

网络配置:
- httpTimeoutSeconds: HTTP超时时间（默认: 25秒）
- osmOverpassUrl: OSM API地址
- elevationApiUrl: 海拔查询API地址

缓存配置:
- osmCacheExpiryHours: OSM缓存过期时间（默认: 48小时）
- cacheGridPrecision: 缓存网格精度（默认: 0.002度）

地理常量:
- earthMetersPerDegreeLon: 经度米数转换（默认: 111320）
- earthMetersPerDegreeLat: 纬度米数转换（默认: 110540）
- defaultFloorHeight: 默认楼层高度（默认: 3.2米）

限制:
- maxBuildingCount: 最大建筑数量（默认: 80）
- maxLocationListCount: 位置列表最大数量（默认: 30）
- maxSceneListCount: 场景列表最大数量（默认: 20）
```

### 修改配置
1. 在Unity编辑器中打开 `Assets/Resources/DefaultAppConfig.asset`
2. 在Inspector面板中修改参数
3. 保存后重新运行项目

---

## 🐛 常见问题

### Q: 为什么不能通过Visual Studio运行？
**A:** 这是Unity项目，必须通过Unity编辑器运行。Visual Studio只是代码编辑器。

### Q: 界面没有显示？
**A:** 
- 确保在Unity编辑器中点击了Play按钮
- 检查Console面板是否有错误信息
- 确认 `RuntimeSceneBootstrap.cs` 正常工作

### Q: AI识别失败？
**A:** 
- 检查本地AI服务是否运行在 http://localhost:8000
- 如果服务不可用，程序会使用默认识别结果（"building"）
- 可以在配置中修改AI服务地址

### Q: OSM查询失败？
**A:** 
- 检查网络连接
- 查询失败时会自动使用示例建筑数据
- 成功的查询会缓存48小时

### Q: 数据库文件在哪里？
**A:** 
- Windows: `C:\Users\<用户名>\AppData\LocalLow\DefaultCompany\3d\city3d.db`
- Mac: `~/Library/Application Support/DefaultCompany/3d/city3d.db`
- Linux: `~/.config/unity3d/DefaultCompany/3d/city3d.db`

---

## 📦 项目结构

```
Assets/
├── Scripts/
│   ├── AppManager.cs           # 应用总控
│   ├── AppConfig.cs            # 配置ScriptableObject
│   ├── Models.cs               # 数据模型
│   ├── Bootstrap/
│   │   └── RuntimeSceneBootstrap.cs  # 运行时自动引导
│   ├── Camera/
│   │   └── OrbitCamera.cs      # 轨道相机控制
│   ├── Database/
│   │   ├── DatabaseManager.cs  # SQLite数据库管理
│   │   ├── DbRecords.cs        # 数据库表定义
│   │   └── SQLite.cs           # SQLite ORM
│   ├── Geo/
│   │   ├── ExifGpsReader.cs    # EXIF GPS提取
│   │   └── GpsConverter.cs     # GPS坐标转换
│   ├── Mesh/
│   │   ├── BuildingMeshGen.cs  # 建筑网格生成
│   │   ├── CitySceneBuilder.cs # 场景构建器
│   │   ├── EarClipper.cs       # 多边形三角化
│   │   ├── StreetObjectGen.cs  # 街道物体生成
│   │   └── TerrainGen.cs       # 程序化地形生成
│   ├── Network/
│   │   ├── AiClient.cs         # AI识别客户端
│   │   ├── ElevationFetcher.cs # 海拔查询
│   │   └── OsmFetcher.cs       # OSM数据查询
│   └── UI/
│       ├── UIManager.cs        # UI管理器
│       ├── InfoPanel.cs        # 建筑信息面板
│       └── BuildingSelector.cs # 建筑选择器
├── Resources/
│   └── DefaultAppConfig.asset  # 默认配置
└── Plugins/
    └── x86_64/
        └── sqlite3.dll         # SQLite原生库
```

---

## 🔧 开发说明

### 代码改进（已完成）
✅ 添加详细的异常日志记录  
✅ 提取硬编码配置到ScriptableObject  
✅ 添加网络请求取消令牌支持  
✅ 修复日期存储为DateTime类型  

### 技术栈
- **Unity 2022.3 LTS** - 游戏引擎
- **Universal Render Pipeline (URP)** - 渲染管线
- **Newtonsoft.Json** - JSON序列化
- **SQLite** - 本地数据库
- **C# 8.0+** - 编程语言

### 架构模式
- **单例模式**: AppManager
- **依赖注入**: 所有服务类接受AppConfig
- **异步编程**: async/await模式
- **运行时引导**: 无需预创建场景

---

## 📄 许可证

请参考项目根目录的 LICENSE 文件。

---

## 🤝 贡献

欢迎提交Issue和Pull Request！

---

**祝你使用愉快！** 🎉
