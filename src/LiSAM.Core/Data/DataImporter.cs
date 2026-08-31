using System.Globalization;
using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public static class DataImporter
{
    public static Data ImportData(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        if (fs.Length % 16 != 0)
            throw new InvalidDataException(
                $"Invalid LiDAR .bin file: {fs.Length} bytes is not divisible by 16."
            );

        var byteBuffer = new byte[fs.Length];
        fs.ReadExactly(byteBuffer);

        var points = new Vector4[byteBuffer.Length / 16];
        for (var i = 0; i < points.Length; i++)
        {
            var x = BitConverter.ToSingle(byteBuffer, i * 16);
            var y = BitConverter.ToSingle(byteBuffer, i * 16 + 4);
            var z = BitConverter.ToSingle(byteBuffer, i * 16 + 8);
            var w = BitConverter.ToSingle(byteBuffer, i * 16 + 12);
            points[i] = new Vector4(x, y, z, w);
        }

        return new Data(points);
    }

    public static Data ImportData(string filePath, string calibFilePath)
    {
        var calibData = ImportCalibration(calibFilePath);

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        var byteBuffer = new byte[fs.Length];
        fs.ReadExactly(byteBuffer);

        var points = new Vector4[byteBuffer.Length / 16];
        Parallel.For(0, points.Length, i =>
        {
            var x = BitConverter.ToSingle(byteBuffer, i * 16);
            var y = BitConverter.ToSingle(byteBuffer, i * 16 + 4);
            var z = BitConverter.ToSingle(byteBuffer, i * 16 + 8);

            var pointInVelo = new Vector4(x, -y, -z, 1);
            var rectifiedPoint = calibData.TransformCameraToNormal * (calibData.TransformVeloToCam * pointInVelo);

            var w = BitConverter.ToSingle(byteBuffer, i * 16 + 12);
            points[i] = new Vector4(rectifiedPoint.X, rectifiedPoint.Y, rectifiedPoint.Z, w);
        });

        return new Data(points);
    }

    public static CalibrationData ImportCalibration(string calibFilePath)
    {
        var calib = new CalibrationData();

        foreach (var rawLine in File.ReadLines(calibFilePath))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(':', 2);

            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();

            var values = parts[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                .ToArray();

            switch (key)
            {
                case "P0":
                    calib.P0 = ToMatrix3x4(values);
                    break;

                case "P1":
                    calib.P1 = ToMatrix3x4(values);
                    break;

                case "P2":
                    calib.P2 = ToMatrix3x4(values);
                    break;

                case "P3":
                    calib.P3 = ToMatrix3x4(values);
                    break;

                case "R0_rect":
                    calib.TransformCameraToNormal = ToMatrix3(values);
                    break;

                case "Tr_velo_to_cam":
                    calib.TransformVeloToCam = ToMatrix3x4(values);
                    break;

                case "Tr_imu_to_velo":
                    calib.TransformIMUToVelo = ToMatrix3x4(values);
                    break;
            }
        }

        return calib;
    }

    private static Matrix3x4 ToMatrix3x4(float[] v)
    {
        if (v.Length != 12)
            throw new InvalidDataException(
                $"Expected 12 values for Matrix3x4, got {v.Length}.");

        return new Matrix3x4(
            v[0], v[1], v[2], v[3],
            v[4], v[5], v[6], v[7],
            v[8], v[9], v[10], v[11]
        );
    }

    private static Matrix3 ToMatrix3(float[] v)
    {
        if (v.Length != 9)
            throw new InvalidDataException(
                $"Expected 9 values for Matrix3, got {v.Length}.");

        return new Matrix3(
            v[0], v[1], v[2],
            v[3], v[4], v[5],
            v[6], v[7], v[8]
        );
    }
}