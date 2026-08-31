using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using LiSAM.Visualization.Graphics;

namespace LiSAM.Visualization;

/// <summary>Batches colored lines and translucent cuboids into two draw calls.</summary>
public sealed class PrimitiveRenderer : IDisposable
{
    private const int FloatsPerVertex = 7;
    private static readonly int[] CubeTriangleCorners =
    [
        0, 1, 3, 0, 3, 2, // -Z
        4, 6, 7, 4, 7, 5, // +Z
        0, 2, 6, 0, 6, 4, // -X
        1, 5, 7, 1, 7, 3, // +X
        2, 3, 7, 2, 7, 6, // +Y
        0, 4, 5, 0, 5, 1  // -Y
    ];

    private readonly object _sync = new();
    private readonly List<CloudLine> _lines = [];
    private readonly List<TranslucentCuboid> _cuboids = [];
    private readonly ShaderProgram _shader;
    private int _lineVao;
    private int _lineVbo;
    private int _cuboidVao;
    private int _cuboidVbo;
    private int _lineVersion;
    private int _uploadedLineVersion = -1;
    private int _lineVertexCount;
    private int _cuboidVersion;
    private int _uploadedCuboidVersion = -1;
    private int _cuboidVertexCount;
    private Vector3 _lastCameraPosition = new(float.NaN);
    private bool _disposed;

    public PrimitiveRenderer(string vertexShaderPath, string fragmentShaderPath) =>
        _shader = new ShaderProgram(vertexShaderPath, fragmentShaderPath);

    public void OnLoad()
    {
        _shader.OnLoad();
        (_lineVao, _lineVbo) = CreateBuffer();
        (_cuboidVao, _cuboidVbo) = CreateBuffer();
    }

    private (int Vao, int Vbo) CreateBuffer()
    {
        var vao = GL.GenVertexArray();
        var vbo = GL.GenBuffer();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        var stride = FloatsPerVertex * sizeof(float);

        var positionLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray((uint)positionLocation);
        GL.VertexAttribPointer((uint)positionLocation, 3, VertexAttribPointerType.Float, false, stride, 0);
        var colorLocation = _shader.GetAttribLocation("aColor");
        GL.EnableVertexAttribArray((uint)colorLocation);
        GL.VertexAttribPointer((uint)colorLocation, 4, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.BindVertexArray(0);
        return (vao, vbo);
    }

    public void AddLine(CloudLine line)
    {
        lock (_sync)
        {
            _lines.Add(line);
            _lineVersion++;
        }
    }

    public void AddLines(ReadOnlySpan<CloudLine> lines)
    {
        lock (_sync)
        {
            _lines.EnsureCapacity(_lines.Count + lines.Length);
            foreach (var line in lines) _lines.Add(line);
            if (lines.Length > 0) _lineVersion++;
        }
    }

    public void AddCuboid(TranslucentCuboid cuboid)
    {
        if (cuboid.Min.X > cuboid.Max.X || cuboid.Min.Y > cuboid.Max.Y || cuboid.Min.Z > cuboid.Max.Z)
            throw new ArgumentException("Cuboid Min must not exceed Max.", nameof(cuboid));
        lock (_sync)
        {
            _cuboids.Add(cuboid);
            _cuboidVersion++;
        }
    }

    public void AddCuboids(ReadOnlySpan<TranslucentCuboid> cuboids)
    {
        foreach (var cuboid in cuboids) AddCuboid(cuboid);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _lines.Clear();
            _cuboids.Clear();
            _lineVersion++;
            _cuboidVersion++;
        }
    }

    public void Draw(Matrix4 view, Matrix4 projection, Vector3 cameraPosition)
    {
        UploadLinesIfChanged();
        UploadCuboidsIfChanged(cameraPosition);
        _shader.Use();
        GL.UniformMatrix4f(_shader.GetUniformLocation("viewMatrix"), 1, true, ref view);
        GL.UniformMatrix4f(_shader.GetUniformLocation("projectionMatrix"), 1, true, ref projection);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        if (_lineVertexCount > 0)
        {
            GL.BindVertexArray(_lineVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _lineVertexCount);
        }

        if (_cuboidVertexCount > 0)
        {
            GL.DepthMask(false);
            GL.BindVertexArray(_cuboidVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _cuboidVertexCount);
            GL.DepthMask(true);
        }

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.Blend);
    }

    private void UploadLinesIfChanged()
    {
        float[] data;
        lock (_sync)
        {
            if (_uploadedLineVersion == _lineVersion) return;
            data = new float[_lines.Count * 2 * FloatsPerVertex];
            var offset = 0;
            foreach (var line in _lines)
            {
                WriteVertex(data, ref offset, line.Start, line.Color);
                WriteVertex(data, ref offset, line.End, line.Color);
            }
            _lineVertexCount = _lines.Count * 2;
            _uploadedLineVersion = _lineVersion;
        }
        Upload(_lineVbo, data);
    }

    private void UploadCuboidsIfChanged(Vector3 cameraPosition)
    {
        float[] data;
        lock (_sync)
        {
            if (_uploadedCuboidVersion == _cuboidVersion && cameraPosition == _lastCameraPosition) return;
            var sorted = _cuboids.OrderByDescending(c => (c.Center - cameraPosition).LengthSquared).ToArray();
            data = new float[sorted.Length * CubeTriangleCorners.Length * FloatsPerVertex];
            var offset = 0;
            foreach (var cuboid in sorted)
            {
                var corners = GetCorners(cuboid.Min, cuboid.Max);
                foreach (var corner in CubeTriangleCorners)
                    WriteVertex(data, ref offset, corners[corner], cuboid.Color);
            }
            _cuboidVertexCount = sorted.Length * CubeTriangleCorners.Length;
            _uploadedCuboidVersion = _cuboidVersion;
            _lastCameraPosition = cameraPosition;
        }
        Upload(_cuboidVbo, data);
    }

    private static Vector3[] GetCorners(Vector3 min, Vector3 max) =>
    [
        new(min.X, min.Y, min.Z), new(max.X, min.Y, min.Z),
        new(min.X, max.Y, min.Z), new(max.X, max.Y, min.Z),
        new(min.X, min.Y, max.Z), new(max.X, min.Y, max.Z),
        new(min.X, max.Y, max.Z), new(max.X, max.Y, max.Z)
    ];

    private static void WriteVertex(float[] data, ref int offset, Vector3 position, Vector4 color)
    {
        data[offset++] = position.X; data[offset++] = position.Y; data[offset++] = position.Z;
        data[offset++] = color.X; data[offset++] = color.Y; data[offset++] = color.Z; data[offset++] = color.W;
    }

    private static void Upload(int vbo, float[] data)
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsage.DynamicDraw);
    }

    public void Dispose()
    {
        if (_disposed) return;
        GL.DeleteBuffer(_lineVbo); GL.DeleteVertexArray(_lineVao);
        GL.DeleteBuffer(_cuboidVbo); GL.DeleteVertexArray(_cuboidVao);
        _shader.Dispose();
        _disposed = true;
    }
}
