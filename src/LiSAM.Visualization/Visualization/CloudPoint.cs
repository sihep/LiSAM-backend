using OpenTK.Mathematics;

namespace LiSAM.Visualization;

/// <param name="Position">Position in world space.</param>
/// <param name="Color">RGB color, with each component normally between 0 and 1.</param>
/// <param name="Size">Diameter on screen in pixels.</param>
public readonly record struct CloudPoint(Vector3 Position, Vector3 Color, float Size = 3f);
