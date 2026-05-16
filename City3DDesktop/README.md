# 城市3D场景生成器 - WPF桌面应用

这是一个现代化的Windows桌面应用程序，用于从图片和GPS坐标自动生成城市3D场景。

## 功能特性

- 📸 **图片加载与预览** - 支持JPG、PNG、BMP等常见图片格式
- 🤖 **AI位置识别** - 使用AI模型自动识别图片中的地理位置
- 🗺️ **OSM数据获取** - 从OpenStreetMap获取建筑物和道路数据
- 🏔️ **海拔数据** - 获取真实的地形海拔信息
- 💾 **数据持久化** - 保存场景和收藏位置到本地数据库
- 🎨 **现代化UI** - 简洁美观的用户界面

## 技术栈

- **.NET 8.0** - 最新的.NET框架
- **WPF** - Windows Presentation Foundation
- **SQLite** - 轻量级数据库
- **HttpClient** - 网络请求
- **Newtonsoft.Json** - JSON处理

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
- 网络连接（用于获取OSM和海拔数据）

## 快速开始

### 1. 编译项目

```bash
cd City3DDesktop/City3DDesktop
dotnet build
```

### 2. 运行应用

```bash
dotnet run
```

或直接运行编译后的可执行文件：
```bash
bin/Debug/net8.0-windows/City3DDesktop.exe
```

### 3. 使用步骤

1. **加载图片**
   - 点击"📁 加载图片"按钮选择图片
   - 或直接在文本框中输入图片路径

2. **设置GPS坐标**
   - 输入纬度和经度
   - 调整查询半径（100-800米）

3. **生成场景**
   - 点击"⚡ 生成城市场景"按钮
   - 等待AI识别、OSM数据获取和海拔数据处理

4. **保存数据**
   - 输入场景名称
   - 点击"💾 保存场景"保存完整场景
   - 点击"⭐ 收藏位置"保存GPS坐标

## 项目结构

```
City3DDesktop/
├── Models/
│   └── DataModels.cs          # 数据模型
├── Services/
│   ├── DatabaseService.cs     # 数据库服务
│   ├── AiService.cs           # AI识别服务
│   ├── OsmService.cs          # OSM数据服务
│   └── ElevationService.cs    # 海拔数据服务
├── MainWindow.xaml            # 主窗口UI
├── MainWindow.xaml.cs         # 主窗口逻辑
├── App.xaml                   # 应用资源
└── City3DDesktop.csproj       # 项目配置
```

## API配置

### AI服务
默认使用本地Ollama服务（http://localhost:11434）。如需修改：

```csharp
// 在 MainWindow.xaml.cs 中
_aiService = new AiService("http://your-api-url");
```

### OSM服务
使用公共Overpass API（https://overpass-api.de/api/interpreter）

### 海拔服务
使用Open-Elevation API（https://api.open-elevation.com/api/v1/lookup）

## 数据库

应用使用SQLite数据库（city3d.db）存储：
- 收藏的位置（Locations表）
- 保存的场景（Scenes表）

数据库文件位于应用程序运行目录。

## 依赖包

- `System.Data.SQLite.Core` - SQLite数据库
- `Newtonsoft.Json` - JSON序列化
- `MaterialDesignThemes` - UI组件（可选）
- `MaterialDesignColors` - 配色方案（可选）

## 开发说明

### 添加新功能

1. 在 `Models/` 中定义数据模型
2. 在 `Services/` 中实现业务逻辑
3. 在 `MainWindow.xaml.cs` 中集成功能
4. 更新UI（`MainWindow.xaml`）

### 调试

使用Visual Studio或VS Code：
```bash
# VS Code
code .

# 启动调试
F5
```

## 常见问题

### Q: AI识别失败？
A: 确保Ollama服务正在运行，并已安装llava模型：
```bash
ollama pull llava
ollama serve
```

### Q: OSM数据获取超时？
A: 尝试减小查询半径或检查网络连接

### Q: 数据库错误？
A: 删除city3d.db文件，应用会自动重新创建

## 与Unity项目的关系

此WPF应用是Unity项目的桌面版本，提供了：
- 更好的Windows集成
- 独立的可执行文件
- 无需Unity编辑器即可运行
- 更快的启动速度

核心业务逻辑（AI识别、OSM获取、数据库）与Unity版本保持一致。

## 许可证

与主项目相同的开源许可证。

## 贡献

欢迎提交Issue和Pull Request！
