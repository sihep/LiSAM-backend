using LiSAM.Visualization;
using OpenTK.Mathematics;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

static void Near(Vector3 actual, Vector3 expected)
{
    Check((actual - expected).Length < 0.0001f, $"Expected {expected}, got {actual}.");
}

static void Invalid(CloudArc arc)
{
    try { arc.ToLines(); }
    catch (ArgumentException) { return; }
    throw new Exception("Invalid arc was accepted.");
}

CloudArc arc = new(new Vector3(1, 2, 3), 2, MathF.PI / 2,
    Quaternion.Identity, new Vector4(1, 0, 0, 0.5f), segments: 12);
CloudLine[] lines = arc.ToLines();
Check(lines.Length == 12, "Explicit resolution ignored.");
Near(lines[0].Start, new Vector3(3, 2, 3));
Near(lines[^1].End, new Vector3(1, 4, 3));
for (int i = 0; i < lines.Length; i++)
{
    Check(MathF.Abs((lines[i].End - arc.Center).Length - arc.Radius) < 0.0001f, "Incorrect radius.");
    Check(lines[i].Color == arc.Color, "Color lost.");
    if (i > 0) Check(lines[i - 1].End == lines[i].Start, "Disconnected segments.");
}
Near((arc with { SweepAngle = -MathF.PI / 2 }).ToLines()[^1].End, new Vector3(1, 0, 3));
Near((arc with { StartAngle = MathF.PI / 2 }).ToLines()[0].Start, new Vector3(1, 4, 3));
Quaternion rotation = Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2);
Near((arc with { Orientation = rotation }).ToLines()[^1].End, new Vector3(1, 2, 5));
Quaternion scaled = new(rotation.X * 10, rotation.Y * 10, rotation.Z * 10, rotation.W * 10);
Near((arc with { Orientation = scaled }).ToLines()[^1].End, new Vector3(1, 2, 5));
foreach (float sweep in new[] { MathF.Tau, -MathF.Tau })
{
    CloudLine[] circle = (arc with { SweepAngle = sweep, segments = 0 }).ToLines();
    Check(circle[^1].End == circle[0].Start, "Circle is not closed exactly.");
    Check(Math.Abs(sweep) / circle.Length <= Math.PI / 36, "Automatic segments exceed 5 degrees.");
}
Check((arc with { SweepAngle = 0 }).ToLines().Length == 0, "Zero sweep should be empty.");
Check((arc with { SweepAngle = 0.001f, segments = 0 }).ToLines().Length == 1, "Tiny arc resolution.");
Invalid(arc with { Radius = 0 });
Invalid(arc with { Radius = float.NaN });
Invalid(arc with { SweepAngle = 7 });
Invalid(arc with { SweepAngle = float.PositiveInfinity });
Invalid(arc with { StartAngle = float.NaN });
Invalid(arc with { segments = -1 });
Invalid(arc with { segments = 65537 });
Invalid(arc with { Orientation = default });
Invalid(arc with { Orientation = new Quaternion(float.NaN, 0, 0, 1) });
Invalid(arc with { Center = new Vector3(float.PositiveInfinity, 0, 0) });
Invalid(arc with { Color = new Vector4(float.NaN) });
Console.WriteLine("All arc geometry checks passed.");

// Check meridians and latitudes against the spherical coordinate definition.
foreach (float theta in new[] { -MathF.PI, -1.2f, 0f, 0.7f, MathF.PI })
{
    Quaternion meridianRotation = Quaternion.FromAxisAngle(Vector3.UnitZ, theta)
        * Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2);
    CloudLine[] meridian = new CloudArc(arc.Center, 5f, MathF.PI,
        meridianRotation, Vector4.One, -MathF.PI / 2, segments: 8).ToLines();
    Near(meridian[0].Start, arc.Center - Vector3.UnitZ * 5);
    Near(meridian[^1].End, arc.Center + Vector3.UnitZ * 5);
    Near(meridian[3].End, arc.Center + new Vector3(5 * MathF.Cos(theta), 5 * MathF.Sin(theta), 0));
    foreach (float phi in new[] { -0.8f, 0f, 0.6f })
    {
        Vector3 latitudeCenter = arc.Center + Vector3.UnitZ * (5 * MathF.Sin(phi));
        CloudLine[] latitude = new CloudArc(latitudeCenter, 5 * MathF.Cos(phi), 0.2f,
            Quaternion.Identity, Vector4.One, theta).ToLines();
        Near(latitude[0].Start, arc.Center + new Vector3(
            5 * MathF.Cos(phi) * MathF.Cos(theta),
            5 * MathF.Cos(phi) * MathF.Sin(theta), 5 * MathF.Sin(phi)));
        foreach (CloudLine line in latitude)
            Check(MathF.Abs((line.End - arc.Center).Length - 5) < 0.0001f, "Latitude left sphere.");
    }
}
Console.WriteLine("Spherical boundary geometry checks passed.");
