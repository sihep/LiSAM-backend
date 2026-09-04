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
}