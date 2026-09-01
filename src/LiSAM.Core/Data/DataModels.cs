using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public struct PointCloudData(Vector3[] points, float[] intensities)
{
    public readonly Vector3[] Points = points;
    public readonly float[] Intensities = intensities;
}

public struct LabelData(int[] points)
{
    public int[] Points = points;
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