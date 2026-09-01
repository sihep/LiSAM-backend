using OpenTK.Graphics.OpenGL;

namespace LiSAM.Visualization.Graphics;

internal sealed class ShaderProgram : IDisposable
{
    private readonly string _fragmentPath;
    private readonly string _vertexPath;
    private int _handle;

    public ShaderProgram(string vertexPath, string fragmentPath)
    {
        _vertexPath = vertexPath;
        _fragmentPath = fragmentPath;
    }

    public void Dispose()
    {
        if (_handle == 0) return;
        GL.DeleteProgram(_handle);
        _handle = 0;
    }

    public void OnLoad()
    {
        int vertexShader = Compile(ShaderType.VertexShader, File.ReadAllText(_vertexPath), _vertexPath);
        int fragmentShader = Compile(ShaderType.FragmentShader, File.ReadAllText(_fragmentPath), _fragmentPath);

        try
        {
            _handle = GL.CreateProgram();
            GL.AttachShader(_handle, vertexShader);
            GL.AttachShader(_handle, fragmentShader);
            GL.LinkProgram(_handle);
            GL.GetProgrami(_handle, ProgramProperty.LinkStatus, out int success);
            if (success == 0)
                throw new InvalidOperationException($"Shader link failed: {GL.GetProgramInfoLog(_handle)}");
        }
        finally
        {
            GL.DetachShader(_handle, vertexShader);
            GL.DetachShader(_handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }
    }

    private static int Compile(ShaderType type, string source, string path)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out int success);
        if (success != 0) return shader;

        string message = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"Shader compilation failed for '{path}': {message}");
    }

    public void Use()
    {
        GL.UseProgram(_handle);
    }

    public int GetAttribLocation(string name)
    {
        return GL.GetAttribLocation(_handle, name);
    }

    public int GetUniformLocation(string name)
    {
        return GL.GetUniformLocation(_handle, name);
    }
}