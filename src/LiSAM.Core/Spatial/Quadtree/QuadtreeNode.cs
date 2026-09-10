using System.Diagnostics;
using OpenTK.Mathematics;

namespace LiSAM.Core.Spatial.Quadtree;

public class QuadtreeNode(List<int> pointIndices, float thetaMin, float thetaMax, float phiMin, float phiMax, QuadtreeNode? parent, SphericalQuadtree tree, int depth)
{
    private readonly List<int> _pointIndices = pointIndices;

    public Vector3 Centroid;
    public float MeanSquareErrorFromPlane;
    public Vector3 Normal;
    private SphericalQuadtree _tree { get; } = tree;

    public int Depth { get; } = depth;

    public float ThetaMin { get; } = thetaMin;
    public float ThetaMax { get; } = thetaMax;

    public float PhiMin { get; } = phiMin;
    public float PhiMax { get; } = phiMax;

    public QuadtreeNode? Parent { get; set; } = parent;
    public QuadtreeNode[] Children { get; set; } = [];
    public bool IsLeaf { get; set; }

    public void SpitIfNecessary()
    {
        if (!IsSplittingNecessary())
        {
            IsLeaf = true;
            return;
        }

        IsLeaf = false;

        List<int>[] pointIndicesInQuadrant = Enumerable
            .Range(0, 4)
            .Select(_ => new List<int>())
            .ToArray();

        foreach (int pointIndex in _pointIndices)
        {
            int top = _tree.PolarPoints[pointIndex].Phi >= (PhiMin + PhiMax) / 2f ? 1 : 0;
            int right = _tree.PolarPoints[pointIndex].Theta >= (ThetaMin + ThetaMax) / 2f ? 1 : 0;

            pointIndicesInQuadrant[2 * top + right].Add(pointIndex);
        }

        // 0 -> bottom left
        // 1 -> bottom right
        // 2 -> top left
        // 3 -> top right

        float thetaMid = (ThetaMin + ThetaMax) / 2f;
        float phiMid = (PhiMin + PhiMax) / 2f;

        int newDepth = Depth + 1;

        Children = new QuadtreeNode[4];
        Children[0] = _tree.AddNewNode(new QuadtreeNode(pointIndicesInQuadrant[0], ThetaMin, thetaMid, PhiMin, phiMid, this, _tree, newDepth));
        Children[1] = _tree.AddNewNode(new QuadtreeNode(pointIndicesInQuadrant[1], thetaMid, ThetaMax, PhiMin, phiMid, this, _tree, newDepth));
        Children[2] = _tree.AddNewNode(new QuadtreeNode(pointIndicesInQuadrant[2], ThetaMin, thetaMid, phiMid, PhiMax, this, _tree, newDepth));
        Children[3] = _tree.AddNewNode(new QuadtreeNode(pointIndicesInQuadrant[3], thetaMid, ThetaMax, phiMid, PhiMax, this, _tree, newDepth));
    }

    private bool IsSplittingNecessary()
    {
        if (_pointIndices.Count == 0) return false;

        bool isSplitting = false;

        float minRadius = float.MaxValue;
        float maxRadius = 0f;

        foreach ((float Radius, float Theta, float Phi) node in _pointIndices.Select(pointIndex => _tree.PolarPoints[pointIndex]))
        {
            minRadius = Math.Min(minRadius, node.Radius);
            maxRadius = Math.Max(maxRadius, node.Radius);
        }

        Stopwatch _timer = new();
        _timer.Start();

        (Centroid, Normal, MeanSquareErrorFromPlane) = Mathematics.FitPlane([.. _pointIndices.Select(i => _tree.CartesianPoints[i])]);

        _timer.Stop();
        Console.WriteLine($"{_timer.ElapsedMilliseconds}ms for {_pointIndices.Count} points");

        if (maxRadius - minRadius > 4 || MeanSquareErrorFromPlane > 0.003f) isSplitting = true;
        bool b = Depth <= 10 && isSplitting;

        return b;
    }

    public List<QuadtreeNode> FlattenIntoLeaves()
    {
        if (IsLeaf) return [this];
        return
        [
            .. Children[0].FlattenIntoLeaves(),
            .. Children[1].FlattenIntoLeaves(),
            .. Children[2].FlattenIntoLeaves(),
            .. Children[3].FlattenIntoLeaves()
        ];
    }
}