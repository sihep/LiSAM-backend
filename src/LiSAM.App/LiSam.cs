using LiSAM.Core.Data;
using LiSAM.Core.Data.DataImporters;
using LiSAM.Core.Inference;
using LiSAM.Core.ML;
using LiSAM.Core.Spatial;
using LiSAM.Visualization;
using OpenTK.Mathematics;

namespace LiSAM.App;

public class LiSam(Visualizer visualizer)
{
    private readonly Visualizer _visualizer = visualizer;

    public async void Run(string[] args)
    {
        HttpClient client = new();
        client.BaseAddress = new Uri("http://103.125.154.215:25565/datasets/");

        PointCloudData data =
            await SemanticKITTIDataImporter.ImportPointCloudDataFromUrl(client,
                "semanticKITTI/sequences/00/velodyne/000002.bin");
        CalibrationData calibrationData =
            await SemanticKITTIDataImporter.ImportCalibrationDataFromUrl(client,
                "semanticKITTI/sequences/00/calib.txt");
        LabelData labelData =
            await SemanticKITTIDataImporter.ImportLabelDataFromUrl(client,
                "semanticKITTI/sequences/00/labels/000002.label");

        SemanticKITTIDataImporter.ApplyCalibrationData(data, calibrationData, Matrix4.Identity);

        for (int i = 0; i < data.Points.Length; i++)
        {
            _visualizer.AddPoint(new CloudPoint(
                data.Points[i],
                data.Intensities[i] * GetColor(labelData.Labels[i])
            ));
        }
    }

    /// <summary>
    /// Test harness for the new Spatial -> ROI -> Inference pipeline. Loads
    /// the same cloud as Run(), but colors points by the pipeline's
    /// *predicted* labels rather than ground truth, and dims anything no
    /// ROI covered so you can see the crop working at a glance.
    /// </summary>
    public async void RunInferencePipelineTest(string[] args)
    {
        HttpClient client = new();
        client.BaseAddress = new Uri("http://103.125.154.215:25565/datasets/");

        PointCloudData data =
            await SemanticKITTIDataImporter.ImportPointCloudDataFromUrl(client,
                "semanticKITTI/sequences/00/velodyne/000002.bin");
        CalibrationData calibrationData =
            await SemanticKITTIDataImporter.ImportCalibrationDataFromUrl(client,
                "semanticKITTI/sequences/00/calib.txt");

        SemanticKITTIDataImporter.ApplyCalibrationData(data, calibrationData, Matrix4.Identity);

        PolarGridConfig gridConfig = new() { MaxRange = 80f, RingCount = 40, SectorCount = 360 };
        HeuristicRoiSelector roiSelector = new() { ScoreThreshold = 0.3f };

        // Swap this for a trained PointNetLite once you have one — same interface.
        IPointCloudModel model = new RandomPointCloudModel(seed: 42);

        InferencePipeline pipeline = new(gridConfig, roiSelector, model);
        InferenceResult result = pipeline.Run(data);

        Console.WriteLine($"ROIs found: {result.RoiCount}");
        Console.WriteLine($"Points processed by network: {result.PointsProcessed} / {result.PointsTotal} " +
                           $"({result.FractionProcessed:P1})");

        LabelData predictedLabels = result.ToLabelData();

        for (int i = 0; i < data.Points.Length; i++)
        {
            Vector3 color = predictedLabels.Labels[i] == LidarSemanticLabel.Unknown
                ? new Vector3(0.15f, 0.15f, 0.15f) // not covered by any ROI
                : GetColor(predictedLabels.Labels[i]);

            _visualizer.AddPoint(new CloudPoint(data.Points[i], data.Intensities[i] * color));
        }
    }

    private static Vector3 GetColor(LidarSemanticLabel label)
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

    public async void RunRoiHighlightTest(string[] args)
    {
        HttpClient client = new();
        client.BaseAddress = new Uri("http://103.125.154.215:25565/datasets/");

        PointCloudData data =
            await SemanticKITTIDataImporter.ImportPointCloudDataFromUrl(client,
                "semanticKITTI/sequences/00/velodyne/000002.bin");
        CalibrationData calibrationData =
            await SemanticKITTIDataImporter.ImportCalibrationDataFromUrl(client,
                "semanticKITTI/sequences/00/calib.txt");

        SemanticKITTIDataImporter.ApplyCalibrationData(data, calibrationData, Matrix4.Identity);

        PolarGridConfig gridConfig = new() { MaxRange = 80f, RingCount = 40, SectorCount = 360 };
        HeuristicRoiSelector roiSelector = new() { ScoreThreshold = 0.3f };

        PolarGrid grid = PolarGrid.Build(data, gridConfig);
        List<RoiRegion> rois = roiSelector.SelectRois(grid);

        Console.WriteLine($"ROIs found: {rois.Count}");
        int coveredCount = 0;
        foreach (RoiRegion roi in rois) coveredCount += roi.PointCount;
        Console.WriteLine($"Points covered: {coveredCount} / {data.Points.Length} " +
                           $"({(float)coveredCount / data.Points.Length:P1})");

        Vector3 uncoveredColor = new(0.1f, 0.1f, 0.1f);
        Vector3[] colors = new Vector3[data.Points.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = uncoveredColor;

        for (int r = 0; r < rois.Count; r++)
        {
            Vector3 color = RoiColor(r);
            foreach (int idx in rois[r].PointIndices())
                colors[idx] = color;
        }

        for (int i = 0; i < data.Points.Length; i++)
            _visualizer.AddPoint(new CloudPoint(data.Points[i], data.Intensities[i] * colors[i]));
    }

    /// <summary>Visually distinct color per ROI index, spaced by the golden ratio so adjacent indices don't look similar.</summary>
    private static Vector3 RoiColor(int index)
    {
        float hue = (index * 0.61803398875f) % 1f;
        return HsvToRgb(hue, 0.85f, 1f);
    }

    private static Vector3 HsvToRgb(float h, float s, float v)
    {
        int i = (int)(h * 6f);
        float f = h * 6f - i;
        float p = v * (1f - s);
        float q = v * (1f - f * s);
        float t = v * (1f - (1f - f) * s);

        return (i % 6) switch
        {
            0 => new Vector3(v, t, p),
            1 => new Vector3(q, v, p),
            2 => new Vector3(p, v, t),
            3 => new Vector3(p, q, v),
            4 => new Vector3(t, p, v),
            _ => new Vector3(v, p, q)
        };
    }



}