using System.Globalization;
using System.Text;
using OpenTK.Mathematics;

namespace LiSAM.Core.Data.DataImporters;

public abstract class HeLiMOSDataImpoorter : IDataImporter
{
    public static async Task<PointCloudData> ImportPointCloudData(Stream stream)
    {
        await using MemoryStream memory = new();
        await stream.CopyToAsync(memory);

        byte[] byteBuffer = memory.ToArray();

        if (byteBuffer.Length % 16 != 0)
            throw new InvalidDataException(
                $"Invalid stream: {stream.Length} bytes is not divisible by 16."
            );

        Vector3[] points = new Vector3[byteBuffer.Length / 16];
        float[] intensities = new float[byteBuffer.Length / 16];
        for (int i = 0; i < points.Length; i++)
        {
            float x = BitConverter.ToSingle(byteBuffer, i * 16);
            float z = BitConverter.ToSingle(byteBuffer, i * 16 + 4);
            float y = BitConverter.ToSingle(byteBuffer, i * 16 + 8);
            float intensity = BitConverter.ToSingle(byteBuffer, i * 16 + 12) / 255f;
            points[i] = new Vector3(x, y, z);
            intensities[i] = intensity;
        }

        return new PointCloudData(points, intensities);
    }

    public static async Task<PointCloudData> ImportPointCloudDataFromFile(string path)
    {
        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        return await ImportPointCloudData(fileStream);
    }

    public static async Task<PointCloudData> ImportPointCloudDataFromUrl(HttpClient client, string url)
    {
        Stream stream = await client.GetStreamAsync(url);
        return await ImportPointCloudData(stream);
    }

    public static async Task<CalibrationData> ImportCalibrationData(Stream stream)
    {
        CalibrationData calib = new();

        using StreamReader reader = new(stream, Encoding.UTF8);
        string content = await reader.ReadToEndAsync();

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] parts = line.Split(':', 2);

            if (parts.Length != 2)
                continue;

            string key = parts[0].Trim();

            float[] values = parts[1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                .ToArray();

            switch (key)
            {
                case "P0":
                    calib.P0 = IDataImporter.ToMatrix3x4(values);
                    break;

                case "P1":
                    calib.P1 = IDataImporter.ToMatrix3x4(values);
                    break;

                case "P2":
                    calib.P2 = IDataImporter.ToMatrix3x4(values);
                    break;

                case "P3":
                    calib.P3 = IDataImporter.ToMatrix3x4(values);
                    break;

                case "Tr":
                    calib.TransformVeloToCam = IDataImporter.ToMatrix3x4(values);
                    break;
            }
        }

        return calib;
    }

    public static async Task<CalibrationData> ImportCalibrationDataFromFile(string path)
    {
        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        return await ImportCalibrationData(fileStream);
    }

    public static async Task<CalibrationData> ImportCalibrationDataFromUrl(HttpClient client, string url)
    {
        Stream stream = await client.GetStreamAsync(url);
        return await ImportCalibrationData(stream);
    }

    public static async Task<PosesData> ImportPosesData(Stream stream)
    {
        using StreamReader reader = new(stream, Encoding.UTF8);
        string[] lines = (await reader.ReadToEndAsync()).Split("\n");
        PosesData poses = new(new Matrix3x4[lines.Length]);

        for (int i = 0; i < lines.Length; i++)
        {
            float[] values = lines[i].Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
                .ToArray();

            if (values.Length != 12) continue;

            poses.Transforms[i] = IDataImporter.ToMatrix3x4(values);
        }

        return poses;
    }

    public static async Task<PosesData> ImportPosesDataFromFile(string path)
    {
        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        return await ImportPosesData(fileStream);
    }

    public static async Task<PosesData> ImportPosesDataFromUrl(HttpClient client, string url)
    {
        Stream stream = await client.GetStreamAsync(url);
        return await ImportPosesData(stream);
    }

    public static Task<LabelData> ImportLabelData(Stream stream)
    {
        throw new NotImplementedException();
    }

    public static Task<LabelData> ImportLabelDataFromFile(string path)
    {
        throw new NotImplementedException();
    }

    public static Task<LabelData> ImportLabelDataFromUrl(HttpClient client, string url)
    {
        throw new NotImplementedException();
    }

    public static void ApplyCalibrationData(
        PointCloudData pointCloudData,
        CalibrationData calibrationData,
        Matrix4 transform)
    {
        for (int i = 0; i < pointCloudData.Points.Length; i++)
        {
            Vector4 p = new(
                pointCloudData.Points[i].X,
                pointCloudData.Points[i].Y,
                pointCloudData.Points[i].Z,
                1f);

            Vector3 calibrated = calibrationData.TransformVeloToCam * p;

            Vector4 world = transform * new Vector4(
                calibrated.X,
                calibrated.Y,
                calibrated.Z,
                1f);

            pointCloudData.Points[i] = world.Xyz;
        }
    }
}