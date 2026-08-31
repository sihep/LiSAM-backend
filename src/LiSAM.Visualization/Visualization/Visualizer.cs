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
    private readonly Dictionary<PointHandle, CloudPoint> _pendingPoints = [];
    private readonly List<CloudLine> _pendingLines = [];
    private readonly List<TranslucentCuboid> _pendingCuboids = [];

    private PointCloudRenderer _points = null!;
    private PrimitiveRenderer _primitives = null!;
    private Camera _camera = null!;
    private Matrix4 _projection;
    private bool _loaded;
    private float _yaw;
    private float _pitch;
    private long _nextPointId;

    public int PointCount
    {
        get
        {
            lock (_pendingSync) return _loaded ? _points.Count : _pendingPoints.Count;
        }
    }

    /// <summary>Adds one point. Size is its on-screen diameter in pixels.</summary>
    public PointHandle AddPoint(Vector3 position, Vector3 color, float size = 3f) =>
        AddPoint(new CloudPoint(position, color, size));

    public PointHandle AddPoint(Vector3 position) => AddPoint(position, Vector3.One);

    public PointHandle AddPoint(CloudPoint point)
    {
        var handle = new PointHandle(Interlocked.Increment(ref _nextPointId));
        lock (_pendingSync)
        {
            if (_loaded) _points.AddPoint(handle, point);
            else _pendingPoints.Add(handle, point);
        }
        return handle;
    }

    /// <summary>
    /// Adds a batch without creating a render object per point. Prefer this overload
    /// when loading large datasets.
    /// </summary>
    public PointHandle[] AddPoints(ReadOnlySpan<CloudPoint> points)
    {
        var handles = new PointHandle[points.Length];
        for (var i = 0; i < handles.Length; i++)
            handles[i] = new PointHandle(Interlocked.Increment(ref _nextPointId));

        lock (_pendingSync)
        {
            if (_loaded)
            {
                _points.AddPoints(handles, points);
            }
            else
            {
                for (var i = 0; i < points.Length; i++)
                    _pendingPoints.Add(handles[i], points[i]);
            }
        }
        return handles;
    }

    /// <summary>Removes a point before startup or while the window is running.</summary>
    public bool RemovePoint(PointHandle handle)
    {
        lock (_pendingSync)
        {
            return _loaded ? _points.RemovePoint(handle) : _pendingPoints.Remove(handle);
        }
    }

    public int RemovePoints(ReadOnlySpan<PointHandle> handles)
    {
        lock (_pendingSync)
        {
            if (_loaded) return _points.RemovePoints(handles);
            var removed = 0;
            foreach (var handle in handles)
                if (_pendingPoints.Remove(handle)) removed++;
            return removed;
        }
    }

    public void ClearPoints()
    {
        lock (_pendingSync)
        {
            if (_loaded) _points.Clear();
            else _pendingPoints.Clear();
        }
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
            var pendingHandles = _pendingPoints.Keys.ToArray();
            var pendingPoints = _pendingPoints.Values.ToArray();
            _points.AddPoints(pendingHandles, pendingPoints);
            _primitives.AddLines(_pendingLines.ToArray());
            _primitives.AddCuboids(_pendingCuboids.ToArray());
            _pendingPoints.Clear();
            _pendingLines.Clear();
            _pendingCuboids.Clear();
            _loaded = true;
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        const float sensitivity = 0.05f;
        const float arrowSensitivity = 0.05f;
        const float speed = 1.5f;
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();

        if (MouseState.IsButtonDown(MouseButton.Left))
        {
            CursorState = CursorState.Grabbed;
            _yaw -= MouseState.Delta.X * sensitivity;
            _pitch -= MouseState.Delta.Y * sensitivity;
            _pitch = Math.Clamp(_pitch, -89f, 89f);
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

        if (KeyboardState.IsKeyDown(Keys.Right)) _yaw -= arrowSensitivity;
        if (KeyboardState.IsKeyDown(Keys.Left)) _yaw += arrowSensitivity;
        if (KeyboardState.IsKeyDown(Keys.Up)) _pitch += arrowSensitivity;
        if (KeyboardState.IsKeyDown(Keys.Down)) _pitch -= arrowSensitivity;

        _camera.ChangeDirectionTo(_yaw, _pitch);
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
        lock (_pendingSync)
        {
            if (_loaded)
            {
                _loaded = false;
                _points.Dispose();
                _primitives.Dispose();
            }
        }
        base.OnUnload();
    }
}
