using System.Reflection;

namespace XGUI;

/// <summary>
/// Theme loader wrapper for XGUI.
/// This class abstracts theme loading to allow future restructuring of theme file locations
/// while maintaining compatibility with existing XGUI code that uses hardcoded paths.
/// </summary>
public static class XGUIThemeLoader
{
    private static readonly List<string> _searchPaths = new();
    private static readonly Dictionary<string, string> _pathRemappings = new();
    
    /// <summary>
    /// Static constructor to set up default search paths.
    /// </summary>
    static XGUIThemeLoader()
    {
        InitializeDefaultSearchPaths();
    }
    
    /// <summary>
    /// Initializes the default search paths for theme files.
    /// </summary>
    private static void InitializeDefaultSearchPaths()
    {
        // Assembly location
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                AddSearchPath(assemblyDir);
                AddSearchPath(Path.Combine(assemblyDir, "Assets"));
            }
        }
        
        // Current working directory
        var currentDir = Directory.GetCurrentDirectory();
        AddSearchPath(currentDir);
        AddSearchPath(Path.Combine(currentDir, "Assets"));
        
        // Application base directory
        var appBase = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appBase))
        {
            AddSearchPath(appBase);
            AddSearchPath(Path.Combine(appBase, "Assets"));
        }
    }
    
    /// <summary>
    /// Adds a search path for theme files.
    /// </summary>
    public static void AddSearchPath(string path)
    {
        if (!string.IsNullOrEmpty(path) && !_searchPaths.Contains(path))
        {
            _searchPaths.Add(path);
        }
    }
    
    /// <summary>
    /// Adds a path remapping.
    /// When XGUI code requests a file at the "from" path, it will be served from the "to" path.
    /// This allows restructuring theme files while maintaining compatibility.
    /// </summary>
    /// <param name="from">The original path that XGUI code uses (e.g., "/XGUI/DefaultStyles/Computer95.scss")</param>
    /// <param name="to">The new path where the file actually lives (e.g., "/themes/xgui/Computer95.scss")</param>
    public static void AddPathRemapping(string from, string to)
    {
        from = NormalizePath(from);
        to = NormalizePath(to);
        _pathRemappings[from] = to;
    }
    
    /// <summary>
    /// Adds a directory remapping.
    /// All files under the "fromDir" will be remapped to "toDir".
    /// </summary>
    public static void AddDirectoryRemapping(string fromDir, string toDir)
    {
        fromDir = NormalizePath(fromDir).TrimEnd('/');
        toDir = NormalizePath(toDir).TrimEnd('/');
        
        // Store with a special marker
        _pathRemappings[$"DIR:{fromDir}"] = toDir;
    }
    
    /// <summary>
    /// Clears all path remappings.
    /// </summary>
    public static void ClearRemappings()
    {
        _pathRemappings.Clear();
    }
    
    /// <summary>
    /// Resolves a theme path to an absolute file path.
    /// </summary>
    /// <param name="path">The path as specified in XGUI code (e.g., "/XGUI/DefaultStyles/Computer95.scss")</param>
    /// <returns>The resolved absolute file path, or null if not found</returns>
    public static string? ResolveThemePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        
        // Normalize the path
        path = NormalizePath(path);
        
        // Check for exact path remapping
        if (_pathRemappings.TryGetValue(path, out var remappedPath))
        {
            path = remappedPath;
        }
        else
        {
            // Check for directory remappings
            foreach (var kvp in _pathRemappings)
            {
                if (kvp.Key.StartsWith("DIR:"))
                {
                    var fromDir = kvp.Key.Substring(4);
                    if (path.StartsWith(fromDir, StringComparison.OrdinalIgnoreCase))
                    {
                        var relativePart = path.Substring(fromDir.Length);
                        path = kvp.Value + relativePart;
                        break;
                    }
                }
            }
        }
        
        // Remove leading slash for relative resolution
        var relativePath = path.TrimStart('/');
        
        // Search in all paths
        foreach (var basePath in _searchPaths)
        {
            if (string.IsNullOrEmpty(basePath))
                continue;
            
            var fullPath = Path.Combine(basePath, relativePath);
            fullPath = Path.GetFullPath(fullPath);
            
            if (File.Exists(fullPath))
                return fullPath;
        }
        
        // Try as absolute path
        if (Path.IsPathRooted(path) && File.Exists(path))
            return path;
        
        return null;
    }
    
    /// <summary>
    /// Normalizes a path for consistent comparison.
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
    
    /// <summary>
    /// Gets all search paths (for debugging).
    /// </summary>
    public static IReadOnlyList<string> GetSearchPaths() => _searchPaths.AsReadOnly();
    
    /// <summary>
    /// Gets all path remappings (for debugging).
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetRemappings() => _pathRemappings;
}
