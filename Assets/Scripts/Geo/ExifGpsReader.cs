using System;

/// <summary>
/// 自实现 EXIF GPS 解析器，无需任何外部依赖。
/// </summary>
public static class ExifGpsReader
{
    /// <summary>
    /// 从 JPEG 字节中读取 GPS 坐标。
    /// </summary>
    /// <param name="jpeg">JPEG 图片字节数据。</param>
    /// <returns>GPS 坐标；未包含坐标时返回空。</returns>
    public static GpsCoordinate Read(byte[] jpeg)
    {
        // 校验 JPEG 文件头。
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
            return null;

        int off = 2;
        while (off < jpeg.Length - 4)
        {
            if (jpeg[off] != 0xFF) break;
            byte marker = jpeg[off + 1];
            int segLen = (jpeg[off + 2] << 8) | jpeg[off + 3];
            if (marker == 0xE1)
                return ParseApp1(jpeg, off + 4, segLen - 2);
            off += 2 + segLen;
        }

        return null;
    }

    // 解析 APP1 段中的 EXIF 和 TIFF 目录。
    private static GpsCoordinate ParseApp1(byte[] d, int start, int length)
    {
        if (start + 6 >= d.Length) return null;
        if (d[start] != 'E' || d[start + 1] != 'x' || d[start + 2] != 'i' || d[start + 3] != 'f')
            return null;

        int tiff = start + 6;
        bool le = d[tiff] == 0x49;

        ushort US(int p) => le
            ? (ushort)(d[p] | d[p + 1] << 8)
            : (ushort)(d[p] << 8 | d[p + 1]);
        uint UI(int p) => le
            ? (uint)(d[p] | d[p + 1] << 8 | d[p + 2] << 16 | d[p + 3] << 24)
            : (uint)(d[p] << 24 | d[p + 1] << 16 | d[p + 2] << 8 | d[p + 3]);

        int ifd0 = tiff + (int)UI(tiff + 4);
        int n0 = US(ifd0);
        int gpsOff = -1;

        // 在 IFD0 中查找 GPS 子目录偏移。
        for (int i = 0; i < n0; i++)
        {
            int ep = ifd0 + 2 + i * 12;
            if (US(ep) == 0x8825)
            {
                gpsOff = tiff + (int)UI(ep + 8);
                break;
            }
        }

        if (gpsOff < 0) return null;

        int gpsN = US(gpsOff);
        double lat = double.NaN, lon = double.NaN;
        bool latS = false, lonW = false;

        // 读取 GPS 纬度、经度和方向标记。
        for (int i = 0; i < gpsN; i++)
        {
            int ep = gpsOff + 2 + i * 12;
            ushort tag = US(ep);
            int vOff = tiff + (int)UI(ep + 8);
            switch (tag)
            {
                case 1: latS = d[ep + 8] == 'S'; break;
                case 2: lat = DMS(d, vOff, UI); break;
                case 3: lonW = d[ep + 8] == 'W'; break;
                case 4: lon = DMS(d, vOff, UI); break;
            }
        }

        if (double.IsNaN(lat) || double.IsNaN(lon)) return null;
        return new GpsCoordinate(latS ? -lat : lat, lonW ? -lon : lon);
    }

    // 将度分秒有理数转换为十进制度。
    private static double DMS(byte[] d, int off, Func<int, uint> UI)
    {
        double Rat(int o)
        {
            uint den = UI(o + 4);
            return den == 0 ? 0 : (double)UI(o) / den;
        }

        return Rat(off) + Rat(off + 8) / 60.0 + Rat(off + 16) / 3600.0;
    }
}
