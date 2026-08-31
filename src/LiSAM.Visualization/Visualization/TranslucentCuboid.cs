using OpenTK.Mathematics;

namespace LiSAM.Visualization;

public readonly record struct TranslucentCuboid(Vector3 Min, Vector3 Max, Vector4 Color)
{
    public Vector3 Center => (Min + Max) * 0.5f;
}
