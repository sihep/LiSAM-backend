using OpenTK.Mathematics;

namespace LiSAM.Visualization.Graphics;

internal sealed class Camera
{
    private Vector3 _position;
    private Vector3 _up;
    private Vector3 _direction;
    private Matrix4 _view;
    private bool _dirty = true;

    public Vector3 Position
    {
        get => _position;
        private set { _position = value; _dirty = true; }
    }

    public Vector3 Direction
    {
        get => _direction;
        private set
        {
            _direction = value.Normalized();
            _dirty = true;
        }
    }

    public Matrix4 View
    {
        get
        {
            if (!_dirty) return _view;
            _view = Matrix4.LookAt(Position, Position + Direction, _up);
            _dirty = false;
            return _view;
        }
    }

    public Camera(Vector3 position, Vector3 up, Vector3 direction)
    {
        _position = position;
        _up = up.Normalized();
        Direction = direction;
    }

    public void MoveForward(float distance) => Position += distance * Direction;
    public void MoveBackward(float distance) => MoveForward(-distance);
    public void MoveUp(float distance) => Position += distance * _up;
    public void MoveDown(float distance) => MoveUp(-distance);
    public void StrafeRight(float distance) => Position += distance * Vector3.Cross(Direction, _up).Normalized();
    public void StrafeLeft(float distance) => StrafeRight(-distance);

    public void ChangeDirectionTo(float yaw, float pitch)
    {
        var pitchRadians = MathHelper.DegreesToRadians(Math.Clamp(pitch, -89f, 89f));
        var yawRadians = MathHelper.DegreesToRadians(yaw);
        Direction = new Vector3(
            MathF.Cos(pitchRadians) * MathF.Sin(yawRadians),
            MathF.Sin(pitchRadians),
            MathF.Cos(pitchRadians) * MathF.Cos(yawRadians));
    }

    public (float Yaw, float Pitch) GetYawAndPitch() =>
        (MathF.Atan2(Direction.X, Direction.Z), MathF.Asin(Direction.Y));
}
