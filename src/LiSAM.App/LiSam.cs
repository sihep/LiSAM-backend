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
            await HeLiMOSDataImpoorter.ImportPointCloudDataFromFile("dataset/chunk_003/velodyne/011499.bin");
        CalibrationData calibrationData =
            await HeLiMOSDataImpoorter.ImportCalibrationDataFromFile("dataset/calibH.txt");
        PosesData posesData = await HeLiMOSDataImpoorter.ImportPosesDataFromFile("dataset/poses.txt");

        Matrix3x4 transform = posesData.Transforms[11498];
        transform.Column3 = new Vector3(0f, 0f, 0f);

        HeLiMOSDataImpoorter.ApplyCalibrationData(data, calibrationData,
            IDataImporter.Matrix3x4ToMatrix4(transform));

        CloudPoint[] points = new CloudPoint[data.Points.Length];

        for (int i = 0; i < data.Points.Length; i++)
        {
            points[i] = new CloudPoint(
                data.Points[i],
                new Vector3(data.Intensities[i] / 255f)
            );

            _visualizer.AddPoint(points[i]);
        }
    }
}