using System.Reflection;

namespace Sandbox.UI;

public partial class Panel
{
    private List<string>? _loadedTemplateStylesheets;

    /// <summary>
    /// Load the stylesheet for this panel based on [StyleSheet] attributes
    /// </summary>
    protected void LoadStyleSheet()
    {
        if (LoadStyleSheetFromAttribute())
            return;

        if (LoadStyleSheetAuto())
            return;
    }

    /// <summary>
    /// Loads stylesheets from [StyleSheet] attributes.
    /// If an attribute has no name specified, it will automatically look for a stylesheet
    /// with the same name as the component (e.g., MainWindow.razor -> MainWindow.scss).
    /// </summary>
    /// <returns>True if any attribute exists and we loaded from it, otherwise false</returns>
    private bool LoadStyleSheetFromAttribute()
    {
        var type = GetType();
        var attrs = type.GetCustomAttributes<StyleSheetAttribute>(false).ToList();

        if (attrs.Count == 0)
        {
            // Clear any previously loaded stylesheets
            ClearLoadedTemplateStylesheets();
            return false;
        }

        // Clear old sheets and load new ones
        ClearLoadedTemplateStylesheets();
        _loadedTemplateStylesheets = new List<string>();

        foreach (var attr in attrs)
        {
            var path = attr.Name;
            
            // If no path specified, use the type name + .scss
            if (string.IsNullOrWhiteSpace(path))
            {
                path = type.Name + ".scss";
            }
            
            var fullPath = ResolveStyleSheetPath(path, type);
            if (fullPath != null)
            {
                if (LoadStyleSheetFromPath(fullPath, false))
                {
                    _loadedTemplateStylesheets.Add(fullPath);
                }
            }
            else
            {
                System.Console.WriteLine($"Error opening stylesheet: {path} (File not found in search paths)");
            }
        }

        return _loadedTemplateStylesheets.Count > 0;
    }

    /// <summary>
    /// Loads a stylesheet from one based on the class name.
    /// </summary>
    /// <returns>True if loaded, otherwise false</returns>
    private bool LoadStyleSheetAuto()
    {
        var type = GetType();
        
        // Try to find a stylesheet with the same name as the type
        var fullPath = ResolveStyleSheetPath(type.Name + ".scss", type);
        if (fullPath != null && LoadStyleSheetFromPath(fullPath, true))
        {
            _loadedTemplateStylesheets ??= new List<string>();
            _loadedTemplateStylesheets.Add(fullPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clear all previously loaded template stylesheets
    /// </summary>
    private void ClearLoadedTemplateStylesheets()
    {
        if (_loadedTemplateStylesheets == null)
            return;

        foreach (var path in _loadedTemplateStylesheets)
        {
            StyleSheet.Remove(path);
        }
        _loadedTemplateStylesheets.Clear();
    }

    /// <summary>
    /// Loads a stylesheet from the specified path.
    /// </summary>
    /// <returns>True if the stylesheet was loaded successfully, otherwise false</returns>
    private bool LoadStyleSheetFromPath(string? path, bool failSilently)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        // Don't normalize to lowercase since we already have a resolved absolute path
        // with correct casing from ResolveStyleSheetPath
        path = path.NormalizeFilename(enforceInitialSlash: false, enforceLowerCase: false);

        StyleSheet.Load(path, true, failSilently);
        return true;
    }

    /// <summary>
    /// Resolves a stylesheet path to an actual file path.
    /// Searches in: Assets/ subdirectory first, then output directory, relative to assembly, relative to executable.
    /// </summary>
    private string? ResolveStyleSheetPath(string path, Type type)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Normalize path separators
        path = path.Replace('\\', '/');
        
        // Remove leading slash for relative resolution
        var relativePath = path.TrimStart('/');

        // Get the base directories to search
        var searchPaths = GetStyleSheetSearchPaths(type);

        foreach (var basePath in searchPaths)
        {
            if (string.IsNullOrEmpty(basePath))
                continue;

            // Try Assets/ subdirectory first (new structure)
            var assetsPath = System.IO.Path.Combine(basePath, "Assets", relativePath);
            assetsPath = System.IO.Path.GetFullPath(assetsPath);
            
            if (System.IO.File.Exists(assetsPath))
                return assetsPath;

            // Then try direct path (for backward compatibility and non-asset files)
            var fullPath = System.IO.Path.Combine(basePath, relativePath);
            fullPath = System.IO.Path.GetFullPath(fullPath);
            
            if (System.IO.File.Exists(fullPath))
                return fullPath;
        }

        // If nothing found, return null to let the caller handle the missing file
        return null;
    }

    /// <summary>
    /// Gets a list of directories to search for stylesheets.
    /// </summary>
    private IEnumerable<string> GetStyleSheetSearchPaths(Type type)
    {
        // 1. Assembly location (where DLLs and output files are)
        var assemblyLocation = type.Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = System.IO.Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
                yield return assemblyDir;
        }

        // 2. Current directory (where the app is running from)
        yield return System.IO.Directory.GetCurrentDirectory();

        // 3. AppContext.BaseDirectory (runtime base directory)
        yield return AppContext.BaseDirectory;

        // 4. Entry assembly location
        var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            var entryLocation = entryAssembly.Location;
            if (!string.IsNullOrEmpty(entryLocation))
            {
                var entryDir = System.IO.Path.GetDirectoryName(entryLocation);
                if (!string.IsNullOrEmpty(entryDir))
                    yield return entryDir;
            }
        }
    }

    /// <summary>
    /// Checks if a stylesheet is one of this panel's template/component stylesheets.
    /// This includes stylesheets loaded via [StyleSheet] attributes and those
    /// auto-loaded by <see cref="LoadStyleSheetAuto"/>.
    /// Used by SetTheme to avoid removing component/template stylesheets.
    /// </summary>
    /// <param name="styleSheet">The stylesheet to check.</param>
    /// <returns>
    /// True if the stylesheet was loaded as a template/component stylesheet for this panel
    /// (either from a [StyleSheet] attribute or via LoadStyleSheetAuto); false otherwise.
    /// </returns>
    protected bool IsStyleSheetFromAttribute(StyleSheet styleSheet)
    {
        if (_loadedTemplateStylesheets == null || styleSheet?.FileName == null)
            return false;

        // Check if the stylesheet's filename is in our tracked list
        // Normalize paths for comparison (replace backslashes, trim)
        var fileName = styleSheet.FileName.Replace('\\', '/').Trim();
        
        return _loadedTemplateStylesheets.Any(path =>
        {
            var trackedPath = path.Replace('\\', '/').Trim();
            
            // Use exact comparison after normalization. Both values are expected to be
            // fully resolved absolute paths (see ResolveStyleSheetPath), so suffix-based
            // matching is too permissive and can cause false positives.
            return string.Equals(trackedPath, fileName, StringComparison.OrdinalIgnoreCase);
        });
    }
}
