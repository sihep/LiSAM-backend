using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public struct PointCloudData(Vector3[] points, float[] intensities)
{
    public readonly Vector3[] Points = points;
    public readonly float[] Intensities = intensities;
}

public struct LabelData(LidarSemanticLabel[] labels, int[] instanceIDs)
{
    public LidarSemanticLabel[] Labels = labels;
    public int[] InstanceIDs = instanceIDs;
}

public struct CalibrationData
{
    public Matrix3x4 P0, P1, P2, P3, Transform, TransformIMUToVelo;
    public Matrix3 TransformCameraToNormal;
}

public struct PosesData(Matrix3x4[] transforms)
{
    public readonly Matrix3x4[] Transforms = transforms;
}

public class LiDARData
{
    public Vector3[] Positions { get; private set; } = null!;
    public LidarSemanticLabel[] Labels { get; private set; } = null!;
    public float[] Intensities { get; private set; } = null!;
    public int[] InstanceIDs { get; private set; } = null!;
    public Matrix3x4 Pose { get; private set; }

    public Matrix3x4 P0 { get; private set; }
    public Matrix3x4 P1 { get; private set; }
    public Matrix3x4 P2 { get; private set; }
    public Matrix3x4 P3 { get; private set; }
    public Matrix3x4 Transform { get; private set; }

    public Vector3 LiDARCenterPosition { get; private set; }

    public async Task ImportData<T>(HttpClient client, int sequence, int scene) where T : IDataImporter
    {
        Task<PointCloudData> pointCloudDataTask = T.ImportPointCloudDataFromUrl(client, T.GetPointCloudDataPath(sequence, scene));
        Task<LabelData> labelDataTask = T.ImportLabelDataFromUrl(client, T.GetLabelDataPath(sequence, scene));
        Task<CalibrationData> calibrationDataTask = T.ImportCalibrationDataFromUrl(client, T.GetCalibrationDataPath(sequence, scene));
        Task<PosesData> posesDataTask = T.ImportPosesDataFromUrl(client, T.GetPosesDataPath(sequence, scene));
        await Task.WhenAll(pointCloudDataTask, labelDataTask, calibrationDataTask, posesDataTask);

        Pose = posesDataTask.Result.Transforms[scene];

        T.ApplyCalibrationData(pointCloudDataTask.Result, calibrationDataTask.Result, Matrix4.Identity);

        P0 = calibrationDataTask.Result.P0;
        P1 = calibrationDataTask.Result.P1;
        P2 = calibrationDataTask.Result.P2;
        P3 = calibrationDataTask.Result.P3;
        Transform = calibrationDataTask.Result.Transform;

        Positions = new Vector3[pointCloudDataTask.Result.Points.Length];
        Intensities = new float[pointCloudDataTask.Result.Points.Length];
        Labels = new LidarSemanticLabel[labelDataTask.Result.Labels.Length];
        InstanceIDs = new int[pointCloudDataTask.Result.Points.Length];

        for (int i = 0; i < pointCloudDataTask.Result.Points.Length; i++)
        {
            Positions[i] = pointCloudDataTask.Result.Points[i];
            Intensities[i] = pointCloudDataTask.Result.Intensities[i];
            Labels[i] = labelDataTask.Result.Labels[i];
            InstanceIDs[i] = labelDataTask.Result.InstanceIDs[i];
        }

        // Transform the sensor origin through the same pipeline as its points.
        PointCloudData sensorOrigin = new([Vector3.Zero], [0f]);
        T.ApplyCalibrationData(sensorOrigin, calibrationDataTask.Result, Matrix4.Identity);
        LiDARCenterPosition = sensorOrigin.Points[0];
    }
}

public enum LidarSemanticLabel : byte
{
    Unknown = 0,

    Car = 1,
    Truck = 2,
    Bus = 3,
    OtherVehicle = 4,
    Motorcycle = 5,
    Bicycle = 6,

    Pedestrian = 7,
    Cyclist = 8,
    Motorcyclist = 9,

    Road = 10,
    Parking = 11,
    Sidewalk = 12,
    Terrain = 13,
    OtherGround = 14,

    Building = 15,
    Fence = 16,
    Barrier = 17,

    Vegetation = 18,
    Trunk = 19,

    Pole = 20,
    TrafficSign = 21,
    TrafficLight = 22,

    OtherObject = 23
}