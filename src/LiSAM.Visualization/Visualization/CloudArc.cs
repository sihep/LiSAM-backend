using OpenTK.Mathematics;

namespace LiSAM.Visualization;

/// <summary>A circular arc in a rotated local XY plane. Angles are in radians.</summary>
/// <param name="Center">Center in world coordinates.</param>
/// <param name="Radius">Positive radius in world units.</param>
/// <param name="SweepAngle">Signed sweep in [-2π, 2π], from local +X toward +Y when positive.</param>
/// <param name="Orientation">Nonzero quaternion rotating the local plane into world space; normalized automatically.</param>
/// <param name="Color">RGBA line color.</param>
/// <param name="StartAngle">Starting angle from local +X.</param>
/// <param name="segments">0 selects at most 5° per segment; otherwise 1–65536 segments.</param>
public readonly record struct CloudArc(
    Vector3 Center,
    float Radius,
    float SweepAngle,
    Quaternion Orientation,
    Vector4 Color,
    float StartAngle = 0f,
    int segments = 0)
{
    /// <summary>Validates and tessellates the arc without requiring an OpenGL context.</summary>
    public CloudLine[] ToLines()
    {
        if (!float.IsFinite(Center.X) || !float.IsFinite(Center.Y) || !float.IsFinite(Center.Z))
            throw new ArgumentException("Center must be finite.", nameof(Center));
        if (!float.IsFinite(Radius) || Radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(Radius), "Radius must be finite and positive.");
        if (!float.IsFinite(SweepAngle) || MathF.Abs(SweepAngle) > MathF.Tau)
            throw new ArgumentOutOfRangeException(nameof(SweepAngle), "Sweep must be between -2π and 2π radians.");
        if (!float.IsFinite(StartAngle))
            throw new ArgumentOutOfRangeException(nameof(StartAngle), "Start angle must be finite.");
        if (segments < 0 || segments > 65536)
            throw new ArgumentOutOfRangeException(nameof(segments), "Segments must be between 0 and 65536.");
        if (!float.IsFinite(Color.X) || !float.IsFinite(Color.Y) ||
            !float.IsFinite(Color.Z) || !float.IsFinite(Color.W))
            throw new ArgumentException("Color must be finite.", nameof(Color));

        // Scale first so even very large or small finite quaternions normalize safely.
        float scale = MathF.Max(MathF.Max(MathF.Abs(Orientation.X), MathF.Abs(Orientation.Y)),
            MathF.Max(MathF.Abs(Orientation.Z), MathF.Abs(Orientation.W)));
        if (!float.IsFinite(scale) || scale == 0f)
            throw new ArgumentException("Orientation must be finite and nonzero.", nameof(Orientation));
        Quaternion rotation = new(Orientation.X / scale, Orientation.Y / scale,
            Orientation.Z / scale, Orientation.W / scale);
        rotation.Normalize();

        if (SweepAngle == 0f) return [];
        int count = segments == 0
            ? Math.Max(1, (int)Math.Ceiling(Math.Abs((double)SweepAngle) / (Math.PI / 36)))
            : segments;
        double start = Math.IEEERemainder(StartAngle, Math.Tau);
        Vector3 center = Center;
        float radius = Radius;

        Vector3 Position(double angle)
        {
            Vector3 local = new(radius * (float)Math.Cos(angle), radius * (float)Math.Sin(angle), 0f);
            Vector3 position = center + Vector3.Transform(local, rotation);
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                throw new ArgumentException("Arc coordinates exceed the finite world-coordinate range.");
            return position;
        }

        CloudLine[] lines = new CloudLine[count];
        Vector3 first = Position(start);
        Vector3 previous = first;
        for (int i = 1; i <= count; i++)
        {
            // Close full circles exactly, avoiding a floating-point seam.
            Vector3 next = i == count && MathF.Abs(SweepAngle) == MathF.Tau
                ? first
                : Position(start + (double)SweepAngle * i / count);
            lines[i - 1] = new CloudLine(previous, next, Color);
            previous = next;
        }

        return lines;
    }
}