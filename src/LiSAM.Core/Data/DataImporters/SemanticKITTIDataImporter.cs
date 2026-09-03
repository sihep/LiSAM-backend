using System.Globalization;
using System.Text;
using OpenTK.Mathematics;

namespace LiSAM.Core.Data.DataImporters;

public abstract class SemanticKITTIDataImporter : IDataImporter
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
            float y = BitConverter.ToSingle(byteBuffer, i * 16 + 4);
            float z = BitConverter.ToSingle(byteBuffer, i * 16 + 8);
            float intensity = BitConverter.ToSingle(byteBuffer, i * 16 + 12);
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

    public static async Task<LabelData> ImportLabelData(Stream stream)
    {
        await using MemoryStream memory = new();
        await stream.CopyToAsync(memory);

        byte[] byteBuffer = memory.ToArray();

        if (byteBuffer.Length % 4 != 0)
            throw new InvalidDataException(
                $"Invalid label stream: {byteBuffer.Length} bytes is not divisible by 4."
            );

        int count = byteBuffer.Length / 4;

        LidarSemanticLabel[] semanticLabels = new LidarSemanticLabel[count];
        int[] instanceIDs = new int[count];

        for (int i = 0; i < count; i++)
        {
            ushort semanticId = BitConverter.ToUInt16(byteBuffer, i * 4);
            ushort instanceId = BitConverter.ToUInt16(byteBuffer, i * 4 + 2);

            semanticLabels[i] = MapSemanticKitti(semanticId);
            instanceIDs[i] = instanceId;
        }

        return new LabelData(semanticLabels, instanceIDs);
    }

    public static Task<LabelData> ImportLabelDataFromFile(string path)
    {
        FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
        return ImportLabelData(fileStream);
    }

    public static async Task<LabelData> ImportLabelDataFromUrl(HttpClient client, string url)
    {
        Stream stream = await client.GetStreamAsync(url);
        return await ImportLabelData(stream);
    }

    public static void ApplyCalibrationData(PointCloudData pointCloudData, CalibrationData calibrationData,
        Matrix4 transform)
    {
        for (int i = 0; i < pointCloudData.Points.Length; i++)
            pointCloudData.Points[i] = calibrationData.TransformVeloToCam *
                                       new Vector4(pointCloudData.Points[i].X, -pointCloudData.Points[i].Y,
                                           -pointCloudData.Points[i].Z, 1f);
    }

    private static LidarSemanticLabel MapSemanticKitti(int label)
    {
        return label switch
        {
            10 => LidarSemanticLabel.Car,
            11 => LidarSemanticLabel.Bicycle,
            13 => LidarSemanticLabel.Bus,
            15 => LidarSemanticLabel.Motorcycle,
            18 => LidarSemanticLabel.Truck,

            20 => LidarSemanticLabel.OtherVehicle,

            30 => LidarSemanticLabel.Pedestrian,
            31 => LidarSemanticLabel.Cyclist,
            32 => LidarSemanticLabel.Motorcyclist,

            40 => LidarSemanticLabel.Road,
            44 => LidarSemanticLabel.Parking,
            48 => LidarSemanticLabel.Sidewalk,
            49 => LidarSemanticLabel.OtherGround,

            50 => LidarSemanticLabel.Building,
            51 => LidarSemanticLabel.Fence,

            70 => LidarSemanticLabel.Vegetation,
            71 => LidarSemanticLabel.Trunk,
            72 => LidarSemanticLabel.Terrain,

            80 => LidarSemanticLabel.Pole,
            81 => LidarSemanticLabel.TrafficSign,

            252 => LidarSemanticLabel.Car,
            253 => LidarSemanticLabel.Cyclist,
            254 => LidarSemanticLabel.Pedestrian,
            255 => LidarSemanticLabel.Motorcyclist,
            257 => LidarSemanticLabel.OtherVehicle,
            258 => LidarSemanticLabel.Truck,
            259 => LidarSemanticLabel.OtherVehicle,

            _ => LidarSemanticLabel.Unknown
        };
    }
}