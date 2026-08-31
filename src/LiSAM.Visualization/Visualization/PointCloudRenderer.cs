using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using LiSAM.Visualization.Graphics;

namespace LiSAM.Visualization;

/// <summary>
/// Renders an arbitrary number of colored points from one interleaved vertex buffer
/// using a single draw call.
/// </summary>
public sealed class PointCloudRenderer : IDisposable
{
    private const int FloatsPerPoint = 7;
    private readonly object _sync = new();
    private readonly List<float> _vertices = [];
    private readonly ShaderProgram _shader;

    private int _vao;
    private int _vbo;
    private int _uploadedVersion = -1;
    private int _version;
    private int _pointCount;
    private bool _disposed;

    public int Count
    {
        get
        {
            lock (_sync) return _vertices.Count / FloatsPerPoint;
        }
    }

    public PointCloudRenderer(string vertexShaderPath, string fragmentShaderPath)
    {
        _shader = new ShaderProgram(vertexShaderPath, fragmentShaderPath);
    }

    public void OnLoad()
    {
        _shader.OnLoad();

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

        var stride = FloatsPerPoint * sizeof(float);
        var positionLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray((uint)positionLocation);
        GL.VertexAttribPointer((uint)positionLocation, 3, VertexAttribPointerType.Float, false, stride, 0);

        var colorLocation = _shader.GetAttribLocation("aColor");
        GL.EnableVertexAttribArray((uint)colorLocation);
        GL.VertexAttribPointer((uint)colorLocation, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

        var sizeLocation = _shader.GetAttribLocation("aSize");
        GL.EnableVertexAttribArray((uint)sizeLocation);
        GL.VertexAttribPointer((uint)sizeLocation, 1, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));

        GL.BindVertexArray(0);
    }

    public void AddPoint(CloudPoint point)
    {
        Validate(point);
        lock (_sync)
        {
            Append(point);
            _version++;
        }
    }

    public void AddPoints(ReadOnlySpan<CloudPoint> points)
    {
        lock (_sync)
        {
            _vertices.EnsureCapacity(_vertices.Count + points.Length * FloatsPerPoint);
            foreach (var point in points)
            {
                Validate(point);
                Append(point);
            }

            if (points.Length > 0) _version++;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _vertices.Clear();
            _version++;
        }
    }

    public void Draw(Matrix4 view, Matrix4 projection)
    {
        UploadIfChanged();
        if (_pointCount == 0) return;

        _shader.Use();
        GL.UniformMatrix4f(_shader.GetUniformLocation("viewMatrix"), 1, true, ref view);
        GL.UniformMatrix4f(_shader.GetUniformLocation("projectionMatrix"), 1, true, ref projection);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Points, 0, _pointCount);
        GL.BindVertexArray(0);
    }

    private void UploadIfChanged()
    {
        float[] snapshot;
        lock (_sync)
        {
            if (_uploadedVersion == _version) return;
            snapshot = _vertices.ToArray();
            _pointCount = snapshot.Length / FloatsPerPoint;
            _uploadedVersion = _version;
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            snapshot.Length * sizeof(float),
            snapshot,
            BufferUsage.DynamicDraw);
    }

    private void Append(CloudPoint point)
    {
        _vertices.Add(point.Position.X);
        _vertices.Add(point.Position.Y);
        _vertices.Add(point.Position.Z);
        _vertices.Add(point.Color.X);
        _vertices.Add(point.Color.Y);
        _vertices.Add(point.Color.Z);
        _vertices.Add(point.Size);
    }

    private static void Validate(CloudPoint point)
    {
        if (point.Size <= 0f)
            throw new ArgumentOutOfRangeException(nameof(point), "Point size must be greater than zero.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        GL.DeleteBuffer(_vbo);
        GL.DeleteVertexArray(_vao);
        _shader.Dispose();
        _disposed = true;
    }
}
