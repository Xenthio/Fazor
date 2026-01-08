namespace Sandbox;

/// <summary>
/// S&box-compatible Scene class.
/// In Fazor, this represents the application environment and provides access to systems.
/// </summary>
public class Scene
{
    private static Scene? _activeScene;
    private readonly Dictionary<Type, GameObjectSystem> _systems = new();
    private readonly List<Component> _components = new();
    private readonly List<GameObject> _gameObjects = new();
    
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public static Scene ActiveScene => _activeScene ??= new Scene();
    
    /// <summary>
    /// The camera for the scene (stub for s&box compatibility).
    /// </summary>
    public SceneCamera? Camera { get; set; }
    
    /// <summary>
    /// Whether the scene is valid.
    /// </summary>
    public bool IsValid() => true;
    
    /// <summary>
    /// Gets a system by type, creating it if it doesn't exist.
    /// </summary>
    public T GetSystem<T>() where T : GameObjectSystem
    {
        if (_systems.TryGetValue(typeof(T), out var system))
            return (T)system;
            
        // Create the system using reflection
        var newSystem = (T)Activator.CreateInstance(typeof(T), this)!;
        _systems[typeof(T)] = newSystem;
        return newSystem;
    }
    
    /// <summary>
    /// Registers a system instance.
    /// </summary>
    public void RegisterSystem<T>(T system) where T : GameObjectSystem
    {
        _systems[typeof(T)] = system;
    }
    
    /// <summary>
    /// Adds a component to the scene.
    /// </summary>
    internal void AddComponent(Component component)
    {
        component.Scene = this;
        _components.Add(component);
        component.InternalStart();
    }
    
    /// <summary>
    /// Adds a game object to the scene.
    /// </summary>
    internal void AddGameObject(GameObject gameObject)
    {
        gameObject.Scene = this;
        _gameObjects.Add(gameObject);
    }
    
    /// <summary>
    /// Performs a fixed update tick on all components.
    /// </summary>
    public void FixedUpdate()
    {
        foreach (var component in _components)
        {
            component.InternalFixedUpdate();
        }
    }
    
    /// <summary>
    /// Performs a frame update on all components.
    /// </summary>
    public void Update()
    {
        foreach (var component in _components)
        {
            component.InternalUpdate();
        }
    }
}

/// <summary>
/// Stub for s&box's SceneCamera.
/// </summary>
public class SceneCamera
{
    /// <summary>
    /// Adds a render hook (stub).
    /// </summary>
    public void AddHookBeforeOverlay(string name, int order, Action<SceneCamera> hook)
    {
        // In Fazor, we don't use render hooks - rendering is handled by the SkiaPanelRenderer
    }
    
    /// <summary>
    /// Converts screen pixel position to a ray (stub).
    /// </summary>
    public Ray ScreenPixelToRay(System.Numerics.Vector2 screenPos)
    {
        return new Ray();
    }
}

/// <summary>
/// Stub for s&box's Ray.
/// </summary>
public struct Ray
{
    public System.Numerics.Vector3 Origin { get; set; }
    public System.Numerics.Vector3 Direction { get; set; }
}
