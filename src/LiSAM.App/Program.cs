using LiSAM.Visualization;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace LiSAM.App;

public static class Program
{
    private static Task Main()
    {
        try
        {
            var nativeWindowSettings = new NativeWindowSettings
            {
                ClientSize = new Vector2i(1920, 1080),
                Title = "LiSAM",
                Flags = ContextFlags.ForwardCompatible
            };

            var visualizer = new Visualizer(GameWindowSettings.Default, nativeWindowSettings);
            Task.Run(() =>
            {
                var lisam = new LiSam(visualizer);
                lisam.Run();
            });

            visualizer.Run();
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}