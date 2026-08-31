using LiSAM.Core.Data;
using LiSAM.Visualization;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace LiSAM.App;

public static class Program
{
    private static void Main()
    {
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1920, 1080),
            Title = "LiSAM",
            Flags = ContextFlags.ForwardCompatible
        };

        using var visualizer = new Visualizer(GameWindowSettings.Default, nativeWindowSettings);
        var data = DataImporter.ImportData("/home/falingunit/Projects/1.bin", "/home/falingunit/Projects/1.txt");
        foreach (var point in data.Points)
        {
            var cloudPoint = new CloudPoint(new Vector3(point.X, point.Y, point.Z),
                new Vector3(point.W, point.W, point.W));
            visualizer.AddPoint(cloudPoint);
        }

        visualizer.Run();
    }
}