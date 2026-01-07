using Sandbox.UI;

namespace XGUI;

/// <summary>
/// A themeable panel designed to be at the root of the XGUI hierarchy.
/// For example, windows, popups, etc.
/// </summary>
public class XGUIPanel : Panel
{
    /// <summary>
    /// The current theme applied to this panel.
    /// </summary>
    public string CurrentTheme { get; protected set; } = "";
    
    public XGUIPanel()
    {
        AddClass("xgui-panel");
    }
    
    protected override void OnAfterTreeRender(bool firstTime)
    {
        base.OnAfterTreeRender(firstTime);
        if (firstTime)
        {
            // Set the initial theme from the XGUI system
            if (string.IsNullOrEmpty(CurrentTheme))
            {
                SetTheme(XGUISystem.Instance.GlobalTheme);
            }
        }
    }
    
    /// <summary>
    /// Sets the theme for this panel and all its children.
    /// </summary>
    public void SetTheme(string theme)
    {
        var parent = this.Parent;
        
        // Remove existing style sheets (except .razor.scss ones)
        foreach (var style in AllStyleSheets.ToList())
        {
            if (!style.FileName.EndsWith(".razor.scss") && !style.FileName.EndsWith(".cs.scss"))
            {
                StyleSheet.Remove(style.FileName);
            }
        }
        
        CurrentTheme = theme;
        
        // Resolve the theme path
        var resolvedPath = XGUIThemeLoader.ResolveThemePath(theme);
        if (resolvedPath == null)
        {
            Console.WriteLine($"[XGUI] Warning: Theme stylesheet not found: {theme}");
            return;
        }
        
        var styleToApply = Sandbox.UI.StyleSheet.FromFile(resolvedPath);
        
        // Apply the new style
        StyleSheet.Add(styleToApply);
        
        // Force immediate style update
        Style.Dirty();
        
        // Force a complete rebuild by temporarily removing from parent and re-adding
        Parent = null;
        Parent = parent;
        
        // Force layout recalculation - traverse child hierarchy
        ForceStyleUpdateRecursive(this);
    }
    
    private void ForceStyleUpdateRecursive(Panel panel)
    {
        // Mark this panel's style as dirty to force recalculation
        panel.Style.Dirty();
        
        // Update all immediate children
        foreach (var child in panel.Children)
        {
            if (child == null || !child.IsValid()) continue;
            
            // Mark the child's style as dirty
            child.Style.Dirty();
            
            // Recursively update this child's children
            ForceStyleUpdateRecursive(child);
        }
    }
}
