namespace LiSAM.Visualization;

/// <summary>A stable identifier used to update or remove a point.</summary>
public readonly record struct PointHandle
{
    internal long Value { get; }
    internal PointHandle(long value) => Value = value;
    public bool IsValid => Value != 0;
}
