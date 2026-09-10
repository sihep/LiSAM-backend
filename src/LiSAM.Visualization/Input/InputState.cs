using OpenTK.Windowing.GraphicsLibraryFramework;

namespace LiSAM.Visualization.Input;

public sealed class InputState
{
    private readonly HashSet<Keys> _heldKeys = new();
    private readonly object _lock = new();

    public void SetHeldKeys(IEnumerable<Keys> keys)
    {
        lock (_lock)
        {
            _heldKeys.Clear();

            foreach (Keys key in keys) _heldKeys.Add(key);
        }
    }

    public void SetKeyHeld(Keys key)
    {
        lock (_lock)
        {
            _heldKeys.Add(key);
        }
    }

    public void SetKeyUp(Keys key)
    {
        lock (_lock)
        {
            _heldKeys.Remove(key);
        }
    }

    public bool IsKeyDown(Keys key)
    {
        lock (_lock)
        {
            return _heldKeys.Contains(key);
        }
    }
}