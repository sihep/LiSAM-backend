using LiSAM.Visualization;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace LiSAM.App;

public static class Program
{
    private static async Task Main(string[] args)
    {
        NativeWindowSettings nativeWindowSettings = new()
        {
            ClientSize = new Vector2i(1920, 1080),
            Title = "LiSAM",
            Flags = ContextFlags.ForwardCompatible
        };

        Visualizer visualizer = new(GameWindowSettings.Default, nativeWindowSettings);

        Task lisamTask = Task.Run(() =>
        {
            LiSam lisam = new(visualizer);
            lisam.Run(args);
        });

        visualizer.Run();

        await lisamTask;
    }
}