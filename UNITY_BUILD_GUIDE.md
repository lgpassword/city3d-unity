# Unity渲染引擎构建指南

本文档说明如何将Unity项目构建为独立可执行文件，供WPF桌面应用调用。

## 构建步骤

### 1. 打开Unity项目

在Unity Hub中打开主项目（D:\github\3d）

### 2. 配置构建设置

1. 打开 **File > Build Settings**
2. 选择平台：**Windows, Mac, Linux**
3. 选择架构：**Windows (x86_64)**
4. 确保场景列表中包含主场景

### 3. 配置Player Settings

点击 **Player Settings** 按钮，配置以下选项：

#### Company Name
- 设置为：`City3D`

#### Product Name
- 设置为：`City3D`

#### Resolution and Presentation
- **Fullscreen Mode**: Windowed
- **Default Screen Width**: 1024
- **Default Screen Height**: 768
- **Resizable Window**: ✓ 启用
- **Run In Background**: ✓ 启用（重要！）

#### Other Settings
- **Scripting Backend**: Mono 或 IL2CPP
- **API Compatibility Level**: .NET Framework 或 .NET Standard 2.1

### 4. 构建项目

1. 点击 **Build** 按钮
2. 选择输出目录：
   ```
   D:\github\3d\City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer\
   ```
3. 文件名设置为：`City3D.exe`
4. 点击 **保存** 开始构建

### 5. 验证构建

构建完成后，目录结构应该如下：

```
City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\
└── UnityRenderer\
    ├── City3D.exe                    # Unity可执行文件
    ├── UnityPlayer.dll               # Unity运行时
    ├── City3D_Data\                  # 游戏数据
    │   ├── Managed\                  # 托管程序集
    │   ├── Resources\                # 资源文件
    │   └── ...
    └── MonoBleedingEdge\             # Mono运行时（如果使用Mono）
```

## 命令行参数

Unity构建支持以下命令行参数：

- `-pipeName <name>`: 命名管道名称，用于与WPF通信
- `-popupwindow`: 无边框窗口模式

示例：
```bash
City3D.exe -pipeName City3D_Unity_Pipe_12345 -popupwindow
```

## 测试构建

### 独立测试

直接运行Unity可执行文件：
```bash
cd City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer
City3D.exe
```

应该能看到Unity场景正常运行。

### WPF集成测试

1. 运行WPF应用：
   ```bash
   cd City3DDesktop\City3DDesktop
   dotnet run
   ```

2. WPF应用会自动启动Unity渲染引擎并嵌入窗口

## 常见问题

### Q: 构建后Unity窗口无法嵌入？
A: 确保在Player Settings中启用了 **Resizable Window** 和 **Run In Background**

### Q: Unity进程启动失败？
A: 检查以下几点：
- Unity可执行文件路径是否正确
- 是否有足够的磁盘空间
- 是否有防火墙或杀毒软件阻止

### Q: 命名管道连接失败？
A: 确保：
- WPF先启动，创建管道服务器
- Unity后启动，连接到管道
- 管道名称参数正确传递

### Q: 场景数据无法传输？
A: 检查：
- Unity中是否添加了 `WpfBridgeReceiver` 组件
- 是否添加了 `UnityMainThreadDispatcher` 组件
- 查看Unity的日志输出

## 开发模式

在开发阶段，可以在Unity编辑器中测试：

1. 在Unity编辑器中运行场景
2. WPF应用会检测到Unity未构建，显示提示信息
3. 可以独立测试Unity场景功能

## 发布版本

发布WPF应用时，需要包含整个 `UnityRenderer` 目录：

```
City3DDesktop-Release\
├── City3DDesktop.exe
├── City3DDesktop.dll
├── *.dll (依赖库)
└── UnityRenderer\
    ├── City3D.exe
    ├── UnityPlayer.dll
    └── City3D_Data\
```

## 性能优化

### 减小构建体积

1. **Player Settings > Other Settings**
   - Strip Engine Code: ✓ 启用
   - Managed Stripping Level: Medium 或 High

2. **删除未使用的资源**
   - 移除未使用的材质、纹理、音频

3. **压缩设置**
   - Texture Compression: 启用
   - Audio Compression: 启用

### 提升启动速度

1. **减少启动场景复杂度**
   - 延迟加载非必要资源
   - 使用异步加载

2. **优化脚本**
   - 减少Awake/Start中的重操作
   - 使用对象池

## 更新Unity构建

当Unity项目代码更新后：

1. 重新构建Unity项目
2. 覆盖 `UnityRenderer` 目录
3. 重启WPF应用测试

## 自动化构建

可以使用Unity命令行构建：

```bash
"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" ^
  -quit ^
  -batchmode ^
  -projectPath "D:\github\3d" ^
  -buildWindows64Player "D:\github\3d\City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer\City3D.exe"
```

将此命令保存为 `build-unity.bat` 脚本，方便快速构建。
