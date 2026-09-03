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

public struct CalibrationData
{
    public Matrix3x4 P0, P1, P2, P3, TransformVeloToCam, TransformIMUToVelo;
    public Matrix3 TransformCameraToNormal;
}

public struct PosesData(Matrix3x4[] transforms)
{
    public readonly Matrix3x4[] Transforms = transforms;
}