using Sandbox;
using Sandbox.UI;

namespace XGUI;

/// <summary>
/// XGUI System for managing UI panels and themes.
/// This is the main entry point for XGUI compatibility in Fazor.
/// </summary>
public class XGUISystem : GameObjectSystem
{
    private string _globalTheme = "/XGUI/DefaultStyles/Computer95.scss";
    
    /// <summary>
    /// The singleton instance of the XGUI system.
    /// </summary>
    public static XGUISystem Instance => Game.ActiveScene.GetSystem<XGUISystem>();
    
    /// <summary>
    /// The default theme that windows will use if not manually set.
    /// </summary>
    public string GlobalTheme
    {
        get => _globalTheme;
        set
        {
            if (_globalTheme != value)
            {
                _globalTheme = value;
                
                // Get the name of theme without the path and extension
                var themeName = value.Split('/').Last().Replace(".scss", "");
                XGUIIconSystem.CurrentTheme = themeName;
            }
        }
    }
    
    /// <summary>
    /// The XGUI root component (compatibility with s&box).
    /// </summary>
    public XGUIRootComponent? Component { get; set; }
    
    /// <summary>
    /// The XGUI root panel.
    /// </summary>
    public XGUIRootPanel? Panel { get; set; }
    
    public XGUISystem(Scene scene) : base(scene)
    {
    }
    
    /// <summary>
    /// Sets the global theme and updates all XGUI panels.
    /// </summary>
    public void SetGlobalTheme(string theme)
    {
        GlobalTheme = theme;
        
        // Find all XGUIPanel type panels in the hierarchy
        if (Panel != null)
        {
            foreach (var xguiPanel in Panel.ChildrenOfType<XGUIPanel>())
            {
                xguiPanel.SetTheme(GlobalTheme);
            }
        }
    }
    
    public override void Dispose()
    {
        base.Dispose();
        Panel?.Delete();
    }
}
