using System.IO;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;

namespace City3DDesktop.Services;

/// <summary>
/// 3D模型导出服务
/// 支持OBJ、STL格式（HelixToolkit原生支持）
/// </summary>
public class ModelExportService
{
    public enum ExportFormat
    {
        OBJ,
        STL,
        GLTF,
        FBX
    }

    /// <summary>
    /// 复制源OBJ文件到指定路径，并生成对应格式
    /// </summary>
    public async Task<bool> ExportAsync(string sourceObjPath, string targetPath, ExportFormat format)
    {
        try
        {
            switch (format)
            {
                case ExportFormat.OBJ:
                    await CopyAsObjAsync(sourceObjPath, targetPath);
                    break;
                case ExportFormat.STL:
                    await ConvertToStlAsync(sourceObjPath, targetPath);
                    break;
                case ExportFormat.GLTF:
                    await ConvertToGltfAsync(sourceObjPath, targetPath);
                    break;
                case ExportFormat.FBX:
                    await ConvertToFbxAsync(sourceObjPath, targetPath);
                    break;
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导出失败: {ex.Message}");
            return false;
        }
    }

    private async Task CopyAsObjAsync(string source, string target)
    {
        // 直接复制OBJ文件
        await Task.Run(() => File.Copy(source, target, true));

        // 如果有材质文件(.mtl)，也复制
        var sourceMtl = Path.ChangeExtension(source, ".mtl");
        if (File.Exists(sourceMtl))
        {
            var targetMtl = Path.ChangeExtension(target, ".mtl");
            await Task.Run(() => File.Copy(sourceMtl, targetMtl, true));
        }
    }

    private async Task ConvertToStlAsync(string sourceObj, string targetStl)
    {
        await Task.Run(() =>
        {
            var reader = new ObjReader();
            var model = reader.Read(sourceObj);
            if (model == null) throw new Exception("无法读取OBJ文件");

            var stlExporter = new StlExporter();
            using var stream = File.Create(targetStl);
            stlExporter.Export(model, stream);
        });
    }

    private async Task ConvertToGltfAsync(string sourceObj, string targetGltf)
    {
        // GLTF格式简化导出（基础实现）
        await Task.Run(() =>
        {
            var reader = new ObjReader();
            var model = reader.Read(sourceObj);
            if (model == null) throw new Exception("无法读取OBJ文件");

            // 提取顶点和三角面数据
            var vertices = new System.Collections.Generic.List<Point3D>();
            var indices = new System.Collections.Generic.List<int>();

            foreach (var visual in model.Children)
            {
                if (visual is GeometryModel3D geom && geom.Geometry is MeshGeometry3D mesh)
                {
                    var offset = vertices.Count;
                    foreach (var v in mesh.Positions) vertices.Add(v);
                    foreach (var i in mesh.TriangleIndices) indices.Add(i + offset);
                }
            }

            // 生成简化的GLTF JSON（指向外部bin文件）
            var binPath = Path.ChangeExtension(targetGltf, ".bin");
            WriteGltfFiles(targetGltf, binPath, vertices, indices);
        });
    }

    private void WriteGltfFiles(string gltfPath, string binPath,
        System.Collections.Generic.List<Point3D> vertices,
        System.Collections.Generic.List<int> indices)
    {
        // 写入二进制数据
        using (var fs = File.Create(binPath))
        using (var bw = new BinaryWriter(fs))
        {
            foreach (var v in vertices)
            {
                bw.Write((float)v.X);
                bw.Write((float)v.Y);
                bw.Write((float)v.Z);
            }
            foreach (var i in indices)
            {
                bw.Write((uint)i);
            }
        }

        var vertexBytes = vertices.Count * 12;
        var indexBytes = indices.Count * 4;
        var binFileName = Path.GetFileName(binPath);

        var gltfJson = $$"""
{
  "asset": { "version": "2.0", "generator": "City3DDesktop" },
  "scene": 0,
  "scenes": [{ "nodes": [0] }],
  "nodes": [{ "mesh": 0 }],
  "meshes": [{
    "primitives": [{
      "attributes": { "POSITION": 0 },
      "indices": 1
    }]
  }],
  "buffers": [{ "uri": "{{binFileName}}", "byteLength": {{vertexBytes + indexBytes}} }],
  "bufferViews": [
    { "buffer": 0, "byteOffset": 0, "byteLength": {{vertexBytes}}, "target": 34962 },
    { "buffer": 0, "byteOffset": {{vertexBytes}}, "byteLength": {{indexBytes}}, "target": 34963 }
  ],
  "accessors": [
    { "bufferView": 0, "componentType": 5126, "count": {{vertices.Count}}, "type": "VEC3" },
    { "bufferView": 1, "componentType": 5125, "count": {{indices.Count}}, "type": "SCALAR" }
  ]
}
""";
        File.WriteAllText(gltfPath, gltfJson);
    }

    private async Task ConvertToFbxAsync(string sourceObj, string targetFbx)
    {
        // FBX是Autodesk专有格式，没有简单的开源C#库
        // 这里采用变通方案：复制OBJ并提示用户使用Blender转换
        await Task.Run(() =>
        {
            var objCopy = Path.ChangeExtension(targetFbx, ".obj");
            File.Copy(sourceObj, objCopy, true);

            // 写一个README提示
            var readmePath = Path.Combine(Path.GetDirectoryName(targetFbx)!, "FBX转换说明.txt");
            var readme = "FBX格式需要使用Blender或3ds Max进行转换：\n\n" +
                         "1. 打开Blender\n" +
                         "2. File > Import > Wavefront (.obj)\n" +
                         "3. 选择本目录下的.obj文件\n" +
                         "4. File > Export > FBX (.fbx)\n";
            File.WriteAllText(readmePath, readme);
        });
    }

    /// <summary>
    /// 获取格式对应的文件扩展名
    /// </summary>
    public static string GetExtension(ExportFormat format) => format switch
    {
        ExportFormat.OBJ => ".obj",
        ExportFormat.STL => ".stl",
        ExportFormat.GLTF => ".gltf",
        ExportFormat.FBX => ".fbx",
        _ => ".obj"
    };

    /// <summary>
    /// 获取格式说明
    /// </summary>
    public static string GetDescription(ExportFormat format) => format switch
    {
        ExportFormat.OBJ => "Wavefront OBJ - 通用3D模型格式 (推荐)",
        ExportFormat.STL => "STL - 3D打印标准格式",
        ExportFormat.GLTF => "glTF - 现代Web 3D标准",
        ExportFormat.FBX => "FBX - Autodesk行业标准 (需Blender转换)",
        _ => ""
    };
}
