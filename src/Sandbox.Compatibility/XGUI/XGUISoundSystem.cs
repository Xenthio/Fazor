namespace XGUI;

/// <summary>
/// XGUI sound system for theme-aware sound loading.
/// </summary>
public static class XGUISoundSystem
{
    /// <summary>
    /// The current theme name.
    /// </summary>
    public static string CurrentTheme { get; set; } = "Computer95";
    
    private static readonly Dictionary<string, string> _soundCache = new();
    
    /// <summary>
    /// Gets a sound path for the given sound event name.
    /// </summary>
    public static string GetSound(string soundEvent)
    {
        var key = $"{CurrentTheme}/{soundEvent}";
        if (_soundCache.TryGetValue(key, out var cachedPath))
        {
            return cachedPath;
        }
        
        // Try theme-specific path first
        var themeSoundPath = $"/XGUI/Resources/{CurrentTheme}/Sounds/{soundEvent}.wav";
        var resolvedPath = XGUIThemeLoader.ResolveThemePath(themeSoundPath);
        
        if (resolvedPath != null)
        {
            _soundCache[key] = resolvedPath;
            return resolvedPath;
        }
        
        // Fall back to default sounds
        var defaultSoundPath = $"/XGUI/Resources/Sounds/{soundEvent}.wav";
        resolvedPath = XGUIThemeLoader.ResolveThemePath(defaultSoundPath);
        
        if (resolvedPath != null)
        {
            _soundCache[key] = resolvedPath;
            return resolvedPath;
        }
        
        // Return original path if nothing found
        return soundEvent;
    }
    
    /// <summary>
    /// Clears the sound cache (call when theme changes).
    /// </summary>
    public static void ClearCache()
    {
        _soundCache.Clear();
    }
}
