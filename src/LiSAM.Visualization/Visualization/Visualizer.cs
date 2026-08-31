using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using LiSAM.Visualization.Graphics;

namespace LiSAM.Visualization;

/// <summary>An interactive window optimized for large colored point clouds.</summary>
public class Visualizer(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
    : GameWindow(gameWindowSettings, nativeWindowSettings)
{
    private readonly object _pendingSync = new();
    private readonly List<CloudPoint> _pendingPoints = [];
    private readonly List<CloudLine> _pendingLines = [];
    private readonly List<TranslucentCuboid> _pendingCuboids = [];

    private PointCloudRenderer _points = null!;
    private PrimitiveRenderer _primitives = null!;
    private Camera _camera = null!;
    private Matrix4 _projection;
    private bool _loaded;
    private float _yaw;
    private float _pitch;

    public int PointCount => _loaded ? _points.Count : PendingPointCount;

    private int PendingPointCount
    {
        get
        {
            lock (_pendingSync) return _pendingPoints.Count;
        }
    }

    /// <summary>Adds one point. Size is its on-screen diameter in pixels.</summary>
    public void AddPoint(Vector3 position, Vector3 color, float size = 3f) =>
        AddPoint(new CloudPoint(position, color, size));

    public void AddPoint(Vector3 position) => AddPoint(position, Vector3.One);

    public void AddPoint(CloudPoint point)
    {
        if (_loaded)
        {
            _points.AddPoint(point);
            return;
        }

        lock (_pendingSync) _pendingPoints.Add(point);
    }

    /// <summary>
    /// Adds a batch without creating a render object per point. Prefer this overload
    /// when loading large datasets.
    /// </summary>
    public void AddPoints(ReadOnlySpan<CloudPoint> points)
    {
        if (_loaded)
        {
            _points.AddPoints(points);
            return;
        }

        lock (_pendingSync)
        {
            _pendingPoints.EnsureCapacity(_pendingPoints.Count + points.Length);
            foreach (var point in points) _pendingPoints.Add(point);
        }
    }

    public void ClearPoints()
    {
        if (_loaded)
            _points.Clear();
        else
            lock (_pendingSync) _pendingPoints.Clear();
    }

    public void AddLine(Vector3 start, Vector3 end, Vector4 color) =>
        AddLine(new CloudLine(start, end, color));

    public void AddLine(CloudLine line)
    {
        if (_loaded) _primitives.AddLine(line);
        else lock (_pendingSync) _pendingLines.Add(line);
    }

    public void AddLines(ReadOnlySpan<CloudLine> lines)
    {
        if (_loaded) _primitives.AddLines(lines);
        else lock (_pendingSync)
        {
            _pendingLines.EnsureCapacity(_pendingLines.Count + lines.Length);
            foreach (var line in lines) _pendingLines.Add(line);
        }
    }

    public void AddCuboid(Vector3 min, Vector3 max, Vector4 color) =>
        AddCuboid(new TranslucentCuboid(min, max, color));

    public void AddCuboid(TranslucentCuboid cuboid)
    {
        if (_loaded) _primitives.AddCuboid(cuboid);
        else lock (_pendingSync) _pendingCuboids.Add(cuboid);
    }

    public void AddCuboids(ReadOnlySpan<TranslucentCuboid> cuboids)
    {
        if (_loaded) _primitives.AddCuboids(cuboids);
        else lock (_pendingSync)
        {
            _pendingCuboids.EnsureCapacity(_pendingCuboids.Count + cuboids.Length);
            foreach (var cuboid in cuboids) _pendingCuboids.Add(cuboid);
        }
    }

    public void ClearPrimitives()
    {
        if (_loaded) _primitives.Clear();
        else lock (_pendingSync)
        {
            _pendingLines.Clear();
            _pendingCuboids.Clear();
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ProgramPointSize);
        GL.ClearColor(0.1f, 0.1f, 0.1f, 1f);

        _camera = new Camera(new Vector3(0f, 0f, 5f), Vector3.UnitY, -Vector3.UnitZ);
        (_yaw, _pitch) = _camera.GetYawAndPitch();
        (_yaw, _pitch) = (MathHelper.RadiansToDegrees(_yaw), MathHelper.RadiansToDegrees(_pitch));
        UpdateProjection(Size.X, Size.Y);

        var shaderDirectory = Path.Combine(AppContext.BaseDirectory, "Shaders");
        _points = new PointCloudRenderer(
            Path.Combine(shaderDirectory, "pointcloud.vert"),
            Path.Combine(shaderDirectory, "pointcloud.frag"));
        _points.OnLoad();
        _primitives = new PrimitiveRenderer(
            Path.Combine(shaderDirectory, "primitive.vert"),
            Path.Combine(shaderDirectory, "primitive.frag"));
        _primitives.OnLoad();

        lock (_pendingSync)
        {
            _points.AddPoints(_pendingPoints.ToArray());
            _primitives.AddLines(_pendingLines.ToArray());
            _primitives.AddCuboids(_pendingCuboids.ToArray());
            _pendingPoints.Clear();
            _pendingLines.Clear();
            _pendingCuboids.Clear();
        }

        _loaded = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        const float sensitivity = 0.05f;
        const float speed = 1.5f;
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

        if (MouseState.IsButtonDown(MouseButton.Left))
        {
            CursorState = CursorState.Grabbed;
            _yaw -= MouseState.Delta.X * sensitivity;
            _pitch -= MouseState.Delta.Y * sensitivity;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
            _camera.ChangeDirectionTo(_yaw, _pitch);
        }
        else
        {
            CursorState = CursorState.Normal;
        }

        var distance = speed * (float)args.Time;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.StrafeRight(distance);
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.StrafeLeft(distance);
        if (KeyboardState.IsKeyDown(Keys.Space)) _camera.MoveUp(distance);
        if (KeyboardState.IsKeyDown(Keys.LeftShift)) _camera.MoveDown(distance);
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.MoveForward(distance);
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.MoveBackward(distance);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _points.Draw(_camera.View, _projection);
        _primitives.Draw(_camera.View, _projection, _camera.Position);
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        UpdateProjection(e.Width, e.Height);
    }

    private void UpdateProjection(int width, int height)
    {
        if (height == 0) return;
        _projection = Matrix4.CreatePerspectiveFieldOfView(
            MathHelper.DegreesToRadians(45f),
            (float)width / height,
            0.1f,
            10_000f);
    }

    protected override void OnUnload()
    {
        if (_loaded)
        {
            _points.Dispose();
            _primitives.Dispose();
        }
        base.OnUnload();
    }
}
