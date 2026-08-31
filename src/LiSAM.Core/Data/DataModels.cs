using OpenTK.Mathematics;

namespace LiSAM.Core.Data;

public struct Data(Vector4[] points)
{
    public readonly Vector4[] Points = points;
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