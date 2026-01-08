namespace Sandbox;

/// <summary>
/// S&box-compatible Game static class.
/// Provides access to the active scene and other global state.
/// </summary>
public static class Game
{
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public static Scene ActiveScene => Scene.ActiveScene;
}
