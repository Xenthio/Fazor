using Sandbox.UI;

namespace XGUI;

/// <summary>
/// The root panel for XGUI applications.
/// Acts as the container for all XGUI windows and controls.
/// </summary>
public class XGUIRootPanel : Panel
{
    public XGUIRootPanel()
    {
        AddClass("xgui-root");
        
        // Fill the entire parent
        Style.Position = PositionMode.Absolute;
        Style.Left = 0;
        Style.Top = 0;
        Style.Width = Length.Percent(100);
        Style.Height = Length.Percent(100);
        
        // Enable pointer events
        Style.PointerEvents = PointerEvents.All;
    }
    
    /// <summary>
    /// Whether the panel is rendered manually (compatibility with s&box).
    /// In Fazor, this is ignored as rendering is handled by the SkiaPanelRenderer.
    /// </summary>
    public bool RenderedManually { get; set; }
    
    /// <summary>
    /// Scene reference for compatibility with s&box.
    /// </summary>
    public Sandbox.Scene? Scene { get; set; }
    
    /// <summary>
    /// Manual render method (stub for s&box compatibility).
    /// In Fazor, rendering is handled automatically.
    /// </summary>
    public void RenderManual()
    {
        // No-op - Fazor handles rendering automatically
    }
}
