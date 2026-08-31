using System;
using LiSAM.Core.Data;
using LiSAM.Visualization;
using OpenTK.Mathematics;

namespace LiSAM.App;

public class LiSam(Visualizer visualizer)
{
    private readonly Visualizer _visualizer = visualizer;

    public void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: LiSAM.App <point-cloud.bin> <calib.txt>");
            return;
        }

        var data = DataImporter.ImportData(args[0], args[1]);

        var points = new CloudPoint[data.Points.Length];

        for (var i = 0; i < data.Points.Length; i++)
        {
            points[i] = new CloudPoint(
                data.Points[i].Xyz,
                new Vector3(data.Points[i].W)
            );

            _visualizer.AddPoint(points[i]);
        }
    }
}