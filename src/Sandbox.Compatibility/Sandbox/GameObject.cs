namespace Sandbox;

/// <summary>
/// S&box-compatible GameObject class.
/// In Fazor, this represents a container for components.
/// </summary>
public class GameObject
{
    private readonly List<Component> _components = new();
    
    /// <summary>
    /// The scene this game object belongs to.
    /// </summary>
    public Scene? Scene { get; internal set; }
    
    /// <summary>
    /// Container for accessing components attached to this GameObject.
    /// </summary>
    public ComponentCollection Components { get; }
    
    public GameObject()
    {
        Components = new ComponentCollection(this);
    }
    
    /// <summary>
    /// Adds a component of the specified type to this game object.
    /// </summary>
    public T AddComponent<T>() where T : Component, new()
    {
        var component = new T();
        component.GameObject = this;
        component.Scene = Scene;
        _components.Add(component);
        component.InternalStart();
        return component;
    }
    
    /// <summary>
    /// Gets a component of the specified type.
    /// </summary>
    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }
    
    /// <summary>
    /// Collection helper for accessing components.
    /// </summary>
    public class ComponentCollection
    {
        private readonly GameObject _gameObject;
        
        internal ComponentCollection(GameObject gameObject)
        {
            _gameObject = gameObject;
        }
        
        /// <summary>
        /// Tries to get a component of the specified type.
        /// </summary>
        public bool TryGet<T>(out T? component) where T : Component
        {
            component = _gameObject._components.OfType<T>().FirstOrDefault();
            return component != null;
        }
        
        /// <summary>
        /// Gets a component of the specified type, or null if not found.
        /// </summary>
        public T? Get<T>() where T : Component
        {
            return _gameObject._components.OfType<T>().FirstOrDefault();
        }
    }
}
