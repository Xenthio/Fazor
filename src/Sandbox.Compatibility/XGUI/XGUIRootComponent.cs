using Sandbox;
using Sandbox.UI;

namespace XGUI;

/// <summary>
/// XGUI Root Component that manages the XGUI system initialization.
/// In Fazor, this is used to set up the XGUI environment.
/// </summary>
[Title("XGUI Root Component")]
public class XGUIRootComponent : PanelComponent
{
    /// <summary>
    /// Whether to use desktop scaling.
    /// </summary>
    [Property]
    public bool UseDesktopScale { get; set; } = true;
    
    /// <summary>
    /// Whether the mouse is unlocked (UI receives input).
    /// </summary>
    [Property]
    public bool MouseUnlocked { get; set; } = true;
    
    /// <summary>
    /// The XGUI root panel.
    /// </summary>
    public XGUIRootPanel? XGUIPanel { get; private set; }
    
    /// <summary>
    /// The screen panel (compatibility stub).
    /// </summary>
    public ScreenPanel? ScreenPanel { get; private set; }
    
    protected override void OnStart()
    {
        base.OnStart();
        
        // Create the XGUI root panel
        XGUIPanel = new XGUIRootPanel();
        Panel?.AddChild(XGUIPanel);
        
        // Register with the XGUI system
        if (Scene != null)
        {
            var system = Scene.GetSystem<XGUISystem>();
            system.Component = this;
            system.Panel = XGUIPanel;
        }
    }
    
    protected override void OnFixedUpdate()
    {
        base.OnFixedUpdate();
        
        if (XGUIPanel != null)
        {
            XGUIPanel.Style.PointerEvents = MouseUnlocked 
                ? PointerEvents.All 
                : PointerEvents.None;
        }
    }
}

/// <summary>
/// Screen panel stub for s&box compatibility.
/// </summary>
public class ScreenPanel : Component
{
    /// <summary>
    /// The root panel.
    /// </summary>
    public Panel? Panel { get; set; }
    
    /// <summary>
    /// Whether to automatically scale the screen.
    /// </summary>
    public bool AutoScreenScale { get; set; } = true;
    
    /// <summary>
    /// The scale factor.
    /// </summary>
    public float Scale { get; set; } = 1.0f;
}
