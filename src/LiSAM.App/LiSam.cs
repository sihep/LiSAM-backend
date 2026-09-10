using System.Diagnostics;
using LiSAM.Core.Data;
using LiSAM.Core.Data.DataImporters;
using LiSAM.Core.Spatial.Quadtree;
using LiSAM.Visualization;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LiSAM.App;

public class LiSam(Visualizer visualizer)
{
    private readonly Visualizer _visualizer = visualizer;

    public async void Run(string[] args)
    {
        /*if (args.Length < 2)
        {
            Console.WriteLine("Usage: LiSAM.App <point-cloud> <calib-file>");
            return;
        }*/


        HttpClient client = new();
        client.BaseAddress = new Uri("http://103.125.154.215:25565/datasets/");

        LiDARData liDarData = new();
        PointHandle[] pointHandles = [];
        CloudPoint[] points;

        async Task SetScene(int seq, int scene, bool colorByLeaf = false)
        {
            Stopwatch _timer = new();

            _visualizer.RemovePoints(pointHandles);

            liDarData = new LiDARData();
            Console.WriteLine($"Downloading scene {scene} from sequence {seq}...");
            try
            {
                await liDarData.ImportData<SemanticKITTIDataImporter>(client, seq, scene);
                Console.WriteLine("Download Completed");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to fetch.");
                return;
            }

            _timer.Start();
            SphericalQuadtree tree = new(liDarData);
            _timer.Stop();
            Console.WriteLine($"Quadtree Building took {_timer.ElapsedMilliseconds} ms");

            points = new CloudPoint[liDarData.Positions.Length];
            pointHandles = new PointHandle[liDarData.Positions.Length];

            _visualizer.SetCameraPosition(liDarData.LiDARCenterPosition);

            for (int i = 0; i < liDarData.Positions.Length; i++)
            {
                points[i] = new CloudPoint(
                    liDarData.Positions[i],
                    colorByLeaf
                        ? GetLeafColor(tree, i)
                        : liDarData.Intensities[i] * GetColor(liDarData.Labels[i])
                );

                pointHandles[i] = _visualizer.AddPoint(points[i]);
            }

            _visualizer.ClearPrimitives();
            VisualizeQuadtree(_visualizer, tree, liDarData.LiDARCenterPosition);
        }

        /*
        _visualizer.AddCuboid(new TranslucentCuboid(liDarData.LiDARCenterPosition - Vector3.One * 0.1f, liDarData.LiDARCenterPosition + Vector3.One * 0.1f, new Vector4(1f, 1f, 0.5f, 0.25f)));
        */
        _visualizer.SetCameraPosition(liDarData.LiDARCenterPosition);

        /*
        _visualizer.AddLine(new CloudLine(Vector3.Zero, Vector3.UnitX, new Vector4(1f, 0f, 0f, 1f)));
        _visualizer.AddLine(new CloudLine(Vector3.Zero, Vector3.UnitY, new Vector4(0f, 1f, 0f, 1f)));
        _visualizer.AddLine(new CloudLine(Vector3.Zero, Vector3.UnitZ, new Vector4(0f, 0f, 1f, 1f)));*/


        int scene = 0;
        while (true)
        {
            if (_visualizer.InputState.IsKeyDown(Keys.Escape)) return;
            if (_visualizer.InputState.IsKeyDown(Keys.E)) await SetScene(0, ++scene, true);
            if (_visualizer.InputState.IsKeyDown(Keys.Q)) await SetScene(0, --scene, true);
        }
    }

    private static Vector3 GetLeafColor(SphericalQuadtree tree, int pointIndex)
    {
        (float Radius, float Theta, float Phi) point = tree.PolarPoints[pointIndex];
        QuadtreeNode node = tree.RootNode;
        uint hash = 2166136261u;
        while (node.Children.Length > 0)
        {
            // Match the split comparisons, including points exactly on a divider.
            int top = point.Phi >= (node.PhiMin + node.PhiMax) / 2f ? 1 : 0;
            int right = point.Theta >= (node.ThetaMin + node.ThetaMax) / 2f ? 1 : 0;
            int quadrant = 2 * top + right;
            hash = unchecked((hash ^ (uint)(quadrant + 1)) * 16777619u);
            node = node.Children[quadrant];
        }

        // Mix the path so neighboring leaves get visibly different, repeatable colors.
        hash = unchecked((hash ^ (hash >> 16)) * 0x7feb352du);
        hash = unchecked((hash ^ (hash >> 15)) * 0x846ca68bu);
        hash ^= hash >> 16;
        return new Vector3(
            0.25f + 0.75f * (hash & 255u) / 255f,
            0.25f + 0.75f * ((hash >> 8) & 255u) / 255f,
            0.25f + 0.75f * ((hash >> 16) & 255u) / 255f);
    }

    /// <summary>Draws spherical cell boundaries through maxDepth (root depth is 1).</summary>
    private void VisualizeQuadtree(Visualizer visualizer, SphericalQuadtree quadtree, Vector3 center,
        float radius = 5f, int maxDepth = 16)
    {
        ArgumentNullException.ThrowIfNull(visualizer);
        ArgumentNullException.ThrowIfNull(quadtree);
        if (!float.IsFinite(radius) || radius <= 0f)
            throw new ArgumentOutOfRangeException(nameof(radius));
        if (!float.IsFinite(center.X) || !float.IsFinite(center.Y) || !float.IsFinite(center.Z))
            throw new ArgumentException("Center must be finite.", nameof(center));
        if (maxDepth < 1) throw new ArgumentOutOfRangeException(nameof(maxDepth));

        // Theta is azimuth about +Z; phi is elevation above XY.
        void AddMeridian(float theta, float phiMin, float phiMax)
        {
            Quaternion orientation = Quaternion.FromAxisAngle(Vector3.UnitZ, theta)
                                     * Quaternion.FromAxisAngle(Vector3.UnitX, MathF.PI / 2f);
            visualizer.AddArc(new CloudArc(center, radius, phiMax - phiMin,
                orientation, Vector4.One, phiMin));
        }

        void AddLatitude(float phi, float thetaMin, float thetaMax)
        {
            float latitudeRadius = radius * MathF.Cos(phi);
            // At either pole a latitude collapses to a point.
            if (latitudeRadius <= radius * 1e-6f) return;
            Vector3 latitudeCenter = center + Vector3.UnitZ * (radius * MathF.Sin(phi));
            visualizer.AddArc(new CloudArc(latitudeCenter, latitudeRadius, thetaMax - thetaMin,
                Quaternion.Identity, Vector4.One, thetaMin));
        }

        QuadtreeNode root = quadtree.RootNode;
        AddMeridian(root.ThetaMin, root.PhiMin, root.PhiMax);
        if (root.ThetaMax - root.ThetaMin < MathF.Tau)
            AddMeridian(root.ThetaMax, root.PhiMin, root.PhiMax);
        AddLatitude(root.PhiMin, root.ThetaMin, root.ThetaMax);
        AddLatitude(root.PhiMax, root.ThetaMin, root.ThetaMax);

        void DrawSplits(QuadtreeNode node)
        {
            if (node.Children.Length == 0 || node.Depth >= maxDepth) return;

            // Child outer edges are already supplied by ancestors. Draw only the two new dividers.
            AddMeridian((node.ThetaMin + node.ThetaMax) * 0.5f, node.PhiMin, node.PhiMax);
            AddLatitude((node.PhiMin + node.PhiMax) * 0.5f, node.ThetaMin, node.ThetaMax);
            foreach (QuadtreeNode child in node.Children) DrawSplits(child);
        }

        foreach (QuadtreeNode leaf in quadtree.GetFlattenedNodes())
        {
            CloudLine line = new(leaf.Centroid, leaf.Centroid + leaf.Normal * 0.25f, Vector4.One);
            _visualizer.AddLine(line);
        }

        DrawSplits(root);
    }

    private Vector3 GetColor(LidarSemanticLabel label)
    {
        return label switch
        {
            LidarSemanticLabel.Car => new Vector3(1f, 0f, 0f),
            LidarSemanticLabel.Road => new Vector3(0f, 1f, 0f),
            LidarSemanticLabel.Fence => new Vector3(0f, 0f, 1f),
            LidarSemanticLabel.Sidewalk => new Vector3(0f, 1f, 1f),
            LidarSemanticLabel.TrafficSign => new Vector3(1f, 1f, 0f),
            LidarSemanticLabel.Unknown => new Vector3(0f, 0f, 0f),
            _ => Vector3.One
        };
    }
}