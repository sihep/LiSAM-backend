using LiSAM.Core.Data;
using OpenTK.Mathematics;

namespace LiSAM.Core.Spatial.Quadtree;

public sealed class SphericalQuadtree
{
    private readonly LiDARData _liDarData;

    private readonly List<QuadtreeNode> _newNodes = [];
    public (float Radius, float Theta, float Phi)[] PolarPoints { get; }

    public Vector3[] CartesianPoints => _liDarData.Positions;

    public QuadtreeNode RootNode { get; set; }

    public SphericalQuadtree(LiDARData liDarData)
    {
        _liDarData = liDarData;
        PolarPoints = new (float, float, float)[liDarData.Positions.Length];

        Parallel.For(0, liDarData.Positions.Length, i =>
        {
            Vector3 relative = liDarData.Positions[i] - liDarData.LiDARCenterPosition;
            float radius = relative.Length;
            float theta = MathF.Atan2(relative.Y, relative.X);
            float phi = MathF.Atan2(relative.Z, MathF.Sqrt(relative.X * relative.X + relative.Y * relative.Y));

            PolarPoints[i] = (radius, theta, phi);
        });

        RootNode = AddNewNode(new QuadtreeNode([.. Enumerable.Range(0, liDarData.Positions.Length)], -MathF.PI, MathF.PI, -MathF.PI / 2f, MathF.PI / 2f, null, this, 1));
        while (_newNodes.Count > 0)
        {
            List<QuadtreeNode> nodes = [.. _newNodes];
            _newNodes.Clear();
            foreach (QuadtreeNode node in nodes) node.SpitIfNecessary();
        }
    }

    public QuadtreeNode AddNewNode(QuadtreeNode node)
    {
        _newNodes.Add(node);
        return node;
    }

    public List<QuadtreeNode> GetFlattenedNodes()
    {
        return RootNode.FlattenIntoLeaves();
    }
}