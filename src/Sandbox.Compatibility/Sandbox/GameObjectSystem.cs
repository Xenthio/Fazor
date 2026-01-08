namespace Sandbox;

/// <summary>
/// S&box-compatible base class for scene systems.
/// In Fazor, these are singleton services that can be retrieved from the scene.
/// </summary>
public class GameObjectSystem : IDisposable
{
    private readonly Scene _scene;
    private bool _disposed = false;
    
    /// <summary>
    /// The scene this system is associated with.
    /// </summary>
    public Scene Scene => _scene;
    
    protected GameObjectSystem(Scene scene)
    {
        _scene = scene;
    }
    
    /// <summary>
    /// Called to dispose of the system.
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
