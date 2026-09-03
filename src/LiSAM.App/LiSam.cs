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
        client.BaseAddress = new Uri("http://localhost:5000/");

        PointCloudData data =
            await SemanticKITTIDataImporter.ImportPointCloudDataFromFile("dataset/kitti/000001.bin");
        CalibrationData calibrationData =
            await SemanticKITTIDataImporter.ImportCalibrationDataFromFile("dataset/kitti/calib.txt");
        LabelData labelData = await SemanticKITTIDataImporter.ImportLabelDataFromFile("dataset/kitti/000001.label");

        PosesData posesData = await HeLiMOSDataImpoorter.ImportPosesDataFromFile("dataset/poses.txt");

        Matrix3x4 transform = posesData.Transforms[11498];
        transform.Column3 = new Vector3(0f, 0f, 0f);

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