namespace Sandbox;

/// <summary>
/// S&box-compatible input system.
/// </summary>
public static class Input
{
    private static readonly HashSet<string> _pressedActions = new();
    private static readonly HashSet<string> _releasedActions = new();
    private static readonly HashSet<string> _downActions = new();
    
    /// <summary>
    /// Checks if an action was just pressed this frame.
    /// </summary>
    public static bool Pressed(string action)
    {
        return _pressedActions.Contains(action);
    }
    
    /// <summary>
    /// Checks if an action was just released this frame.
    /// </summary>
    public static bool Released(string action)
    {
        return _releasedActions.Contains(action);
    }
    
    /// <summary>
    /// Checks if an action is currently held down.
    /// </summary>
    public static bool Down(string action)
    {
        return _downActions.Contains(action);
    }
    
    /// <summary>
    /// Simulates pressing an action (for testing or input injection).
    /// </summary>
    internal static void SimulatePress(string action)
    {
        _pressedActions.Add(action);
        _downActions.Add(action);
    }
    
    /// <summary>
    /// Simulates releasing an action (for testing or input injection).
    /// </summary>
    internal static void SimulateRelease(string action)
    {
        _releasedActions.Add(action);
        _downActions.Remove(action);
    }
    
    /// <summary>
    /// Clears the pressed/released state (called at end of frame).
    /// </summary>
    internal static void EndFrame()
    {
        _pressedActions.Clear();
        _releasedActions.Clear();
    }
}

/// <summary>
/// S&box-compatible mouse input.
/// </summary>
public static class Mouse
{
    /// <summary>
    /// The current mouse position in screen coordinates.
    /// </summary>
    public static Sandbox.UI.Vector2 Position { get; set; }
    
    /// <summary>
    /// The mouse delta since last frame.
    /// </summary>
    public static Sandbox.UI.Vector2 Delta { get; set; }
    
    /// <summary>
    /// The scroll wheel delta.
    /// </summary>
    public static float Wheel { get; set; }
    
    /// <summary>
    /// Whether the left mouse button is down.
    /// </summary>
    public static bool LeftDown { get; set; }
    
    /// <summary>
    /// Whether the right mouse button is down.
    /// </summary>
    public static bool RightDown { get; set; }
    
    /// <summary>
    /// Whether the middle mouse button is down.
    /// </summary>
    public static bool MiddleDown { get; set; }
}

/// <summary>
/// S&box-compatible screen information.
/// </summary>
public static class Screen
{
    private static float _desktopScale = 1.0f;
    
    /// <summary>
    /// The desktop scale factor (DPI scaling).
    /// </summary>
    public static float DesktopScale
    {
        get => _desktopScale;
        set => _desktopScale = value;
    }
    
    /// <summary>
    /// The screen width.
    /// </summary>
    public static int Width { get; set; } = 1920;
    
    /// <summary>
    /// The screen height.
    /// </summary>
    public static int Height { get; set; } = 1080;
    
    /// <summary>
    /// The screen size as a vector.
    /// </summary>
    public static System.Numerics.Vector2 Size => new(Width, Height);
}
