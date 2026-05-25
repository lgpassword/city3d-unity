using System;
using System.IO;
using Newtonsoft.Json;
using City3DDesktop.Models;

namespace City3DDesktop.Services;

/// <summary>
/// 场景数据加载服务
/// 负责从JSON文件加载数字孪生场景数据
/// </summary>
public class SceneDataService
{
    /// <summary>
    /// 从指定路径加载数字孪生场景数据
    /// </summary>
    /// <param name="path">JSON文件路径</param>
    /// <returns>反序列化后的场景对象</returns>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    /// <exception cref="JsonException">JSON解析失败时抛出</exception>
    public DigitalTwinScene LoadFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"场景数据文件不存在: {path}", path);
        }

        var json = File.ReadAllText(path);
        var scene = JsonConvert.DeserializeObject<DigitalTwinScene>(json);

        if (scene == null)
        {
            throw new JsonException($"无法解析场景数据文件: {path}");
        }

        return scene;
    }

    /// <summary>
    /// 加载默认场景数据（Data/beijing_center.json）
    /// </summary>
    /// <returns>反序列化后的场景对象</returns>
    public DigitalTwinScene LoadDefault()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var defaultPath = Path.Combine(baseDir, "Data", "beijing_center.json");
        return LoadFromFile(defaultPath);
    }
}
