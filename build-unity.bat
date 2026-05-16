@echo off
REM Unity自动构建脚本
REM 将Unity项目构建为独立可执行文件供WPF调用

echo ========================================
echo Unity渲染引擎自动构建脚本
echo ========================================
echo.

REM Unity编辑器路径（根据实际安装路径修改）
set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe

REM 项目路径
set PROJECT_PATH=%~dp0

REM 输出路径
set OUTPUT_PATH=%PROJECT_PATH%City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer\City3D.exe

echo Unity编辑器: %UNITY_PATH%
echo 项目路径: %PROJECT_PATH%
echo 输出路径: %OUTPUT_PATH%
echo.

REM 检查Unity是否存在
if not exist "%UNITY_PATH%" (
    echo [错误] 未找到Unity编辑器！
    echo 请修改脚本中的UNITY_PATH变量为正确的Unity安装路径
    echo.
    pause
    exit /b 1
)

echo [1/3] 准备构建目录...
if not exist "%PROJECT_PATH%City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer" (
    mkdir "%PROJECT_PATH%City3DDesktop\City3DDesktop\bin\Debug\net8.0-windows\UnityRenderer"
)

echo [2/3] 开始构建Unity项目...
echo 这可能需要几分钟时间，请耐心等待...
echo.

"%UNITY_PATH%" ^
  -quit ^
  -batchmode ^
  -nographics ^
  -projectPath "%PROJECT_PATH%" ^
  -buildWindows64Player "%OUTPUT_PATH%" ^
  -logFile "%PROJECT_PATH%unity-build.log"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [错误] Unity构建失败！
    echo 请查看日志文件: %PROJECT_PATH%unity-build.log
    echo.
    pause
    exit /b 1
)

echo.
echo [3/3] 验证构建结果...

if exist "%OUTPUT_PATH%" (
    echo.
    echo ========================================
    echo 构建成功！
    echo ========================================
    echo.
    echo Unity可执行文件: %OUTPUT_PATH%
    echo.
    echo 现在可以运行WPF应用测试Unity集成：
    echo   cd City3DDesktop\City3DDesktop
    echo   dotnet run
    echo.
) else (
    echo.
    echo [错误] 构建完成但未找到输出文件！
    echo 请查看日志文件: %PROJECT_PATH%unity-build.log
    echo.
)

pause
