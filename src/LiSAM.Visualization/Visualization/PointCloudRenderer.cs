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
    private readonly List<PointEntry> _points = [];
    private readonly Dictionary<PointHandle, int> _indices = [];
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
            lock (_sync) return _points.Count;
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

    public void AddPoint(PointHandle handle, CloudPoint point)
    {
        Validate(point);
        lock (_sync)
        {
            if (!handle.IsValid || _indices.ContainsKey(handle))
                throw new ArgumentException("Point handle must be valid and unique.", nameof(handle));
            _indices.Add(handle, _points.Count);
            _points.Add(new PointEntry(handle, point));
            _version++;
        }
    }

    public void AddPoints(ReadOnlySpan<PointHandle> handles, ReadOnlySpan<CloudPoint> points)
    {
        if (handles.Length != points.Length)
            throw new ArgumentException("Handle and point counts must match.");
        lock (_sync)
        {
            _points.EnsureCapacity(_points.Count + points.Length);
            for (var i = 0; i < points.Length; i++)
            {
                Validate(points[i]);
                if (!handles[i].IsValid || _indices.ContainsKey(handles[i]))
                    throw new ArgumentException("Every point handle must be valid and unique.", nameof(handles));
                _indices.Add(handles[i], _points.Count);
                _points.Add(new PointEntry(handles[i], points[i]));
            }

            if (points.Length > 0) _version++;
        }
    }

    public bool RemovePoint(PointHandle handle)
    {
        lock (_sync)
        {
            if (!_indices.Remove(handle, out var index)) return false;

            var lastIndex = _points.Count - 1;
            if (index != lastIndex)
            {
                var moved = _points[lastIndex];
                _points[index] = moved;
                _indices[moved.Handle] = index;
            }
            _points.RemoveAt(lastIndex);
            _version++;
            return true;
        }
    }

    public int RemovePoints(ReadOnlySpan<PointHandle> handles)
    {
        var removed = 0;
        foreach (var handle in handles)
            if (RemovePoint(handle)) removed++;
        return removed;
    }

    public void Clear()
    {
        lock (_sync)
        {
            _points.Clear();
            _indices.Clear();
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
            snapshot = new float[_points.Count * FloatsPerPoint];
            var offset = 0;
            foreach (var entry in _points) Append(snapshot, ref offset, entry.Point);
            _pointCount = _points.Count;
            _uploadedVersion = _version;
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            snapshot.Length * sizeof(float),
            snapshot,
            BufferUsage.DynamicDraw);
    }

    private static void Append(float[] vertices, ref int offset, CloudPoint point)
    {
        vertices[offset++] = point.Position.X;
        vertices[offset++] = point.Position.Y;
        vertices[offset++] = point.Position.Z;
        vertices[offset++] = point.Color.X;
        vertices[offset++] = point.Color.Y;
        vertices[offset++] = point.Color.Z;
        vertices[offset++] = point.Size;
    }

    private readonly record struct PointEntry(PointHandle Handle, CloudPoint Point);

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
