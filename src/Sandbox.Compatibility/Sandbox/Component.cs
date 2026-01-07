namespace Sandbox;

/// <summary>
/// S&box-compatible base class for game components.
/// In Fazor, we use this as a simple lifecycle object since we don't have a full ECS.
/// </summary>
public class Component : IDisposable
{
    private bool _isEnabled = true;
    private bool _disposed = false;
    
    /// <summary>
    /// Reference to the scene containing this component.
    /// </summary>
    public Scene? Scene { get; internal set; }
    
    /// <summary>
    /// Reference to the GameObject this component is attached to.
    /// </summary>
    public GameObject? GameObject { get; internal set; }
    
    /// <summary>
    /// Whether this component is enabled.
    /// </summary>
    public bool Enabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            if (_isEnabled)
                OnEnabled();
            else
                OnDisabled();
        }
    }
    
    /// <summary>
    /// Called when the component is started.
    /// </summary>
    protected virtual void OnStart() { }
    
    /// <summary>
    /// Called when the component is enabled.
    /// </summary>
    protected virtual void OnEnabled() { }
    
    /// <summary>
    /// Called when the component is disabled.
    /// </summary>
    protected virtual void OnDisabled() { }
    
    /// <summary>
    /// Called every fixed update tick.
    /// </summary>
    protected virtual void OnFixedUpdate() { }
    
    /// <summary>
    /// Called every frame update.
    /// </summary>
    protected virtual void OnUpdate() { }
    
    /// <summary>
    /// Manually trigger the start lifecycle method.
    /// </summary>
    internal void InternalStart()
    {
        OnStart();
    }
    
    /// <summary>
    /// Manually trigger the fixed update lifecycle method.
    /// </summary>
    internal void InternalFixedUpdate()
    {
        if (_isEnabled)
            OnFixedUpdate();
    }
    
    /// <summary>
    /// Manually trigger the frame update lifecycle method.
    /// </summary>
    internal void InternalUpdate()
    {
        if (_isEnabled)
            OnUpdate();
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        OnDisabled();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// S&box-compatible base class for UI panel components.
/// </summary>
public class PanelComponent : Component
{
    /// <summary>
    /// The root panel for this component.
    /// </summary>
    public UI.Panel? Panel { get; set; }
}
