using LiSAM.Core.Data;
using LiSAM.Core.Data.DataImporters;
using LiSAM.Visualization;
using OpenTK.Mathematics;

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

        PointCloudData data =
            await SemanticKITTIDataImporter.ImportPointCloudDataFromUrl(client,
                "semanticKITTI/sequences/00/velodyne/000001.bin");
        CalibrationData calibrationData =
            await SemanticKITTIDataImporter.ImportCalibrationDataFromUrl(client,
                "semanticKITTI/sequences/00/calib.txt");
        LabelData labelData =
            await SemanticKITTIDataImporter.ImportLabelDataFromUrl(client,
                "semanticKITTI/sequences/00/labels/000001.label");

        SemanticKITTIDataImporter.ApplyCalibrationData(data, calibrationData, Matrix4.Identity);

        CloudPoint[] points = new CloudPoint[data.Points.Length];

        Vector3 GetColor(LidarSemanticLabel label)
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

        for (int i = 0; i < data.Points.Length; i++)
        {
            points[i] = new CloudPoint(
                data.Points[i],
                data.Intensities[i] * GetColor(labelData.Labels[i])
            );

            _visualizer.AddPoint(points[i]);
        }
    }
}