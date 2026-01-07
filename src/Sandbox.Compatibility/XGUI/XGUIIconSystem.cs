namespace XGUI;

/// <summary>
/// XGUI icon system for theme-aware icon loading.
/// </summary>
public static class XGUIIconSystem
{
    /// <summary>
    /// The current theme name.
    /// </summary>
    public static string CurrentTheme { get; set; } = "Computer95";
    
    private static readonly Dictionary<string, string> _iconCache = new();
    
    /// <summary>
    /// Gets an icon path for the given icon name.
    /// </summary>
    public static string GetIcon(string iconName)
    {
        var key = $"{CurrentTheme}/{iconName}";
        if (_iconCache.TryGetValue(key, out var cachedPath))
        {
            return cachedPath;
        }
        
        // Try theme-specific path first
        var themeIconPath = $"/XGUI/Resources/{CurrentTheme}/Icons/{iconName}";
        var resolvedPath = XGUIThemeLoader.ResolveThemePath(themeIconPath);
        
        if (resolvedPath != null)
        {
            _iconCache[key] = resolvedPath;
            return resolvedPath;
        }
        
        // Fall back to default icons
        var defaultIconPath = $"/XGUI/Resources/Icons/{iconName}";
        resolvedPath = XGUIThemeLoader.ResolveThemePath(defaultIconPath);
        
        if (resolvedPath != null)
        {
            _iconCache[key] = resolvedPath;
            return resolvedPath;
        }
        
        // Return original path if nothing found
        return iconName;
    }
    
    /// <summary>
    /// Clears the icon cache (call when theme changes).
    /// </summary>
    public static void ClearCache()
    {
        _iconCache.Clear();
    }
}
