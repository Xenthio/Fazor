using Sandbox;
using Sandbox.UI;

namespace XGUI;

/// <summary>
/// Look up icons respective to the current theme, you can look up icons by name to use in buttons and panels, or icons for file types to use in a file browser.
/// </summary>
public static class XGUIIconSystem
{
    private const string DefaultThemeName = "Computer95";
    private static string _currentThemeName = DefaultThemeName;

    // Cache for icon paths
    private static Dictionary<string, string> IconPathCache = new Dictionary<string, string>();

    /// <summary>
    /// Supported icon types
    /// </summary>
    public enum IconType
    {
        /// <summary>
        /// Standard UI icons (menus, buttons, controls)
        /// </summary>
        UI,

        /// <summary>
        /// Icons for file types
        /// </summary>
        FileType,

        /// <summary>
        /// Icons for folders
        /// </summary>
        Folder,

        /// <summary>
        /// Miscellaneous icons (other types)
        /// </summary>
        Misc
    }

    /// <summary>
    /// Get the current theme name
    /// </summary>
    public static string CurrentTheme
    {
        get => _currentThemeName;
        set
        {
            if (_currentThemeName != value)
            {
                _currentThemeName = value;
                IconPathCache.Clear();
            }
        }
    }

    private static string GetThemeIconBaseDirectory(string themeName)
    {
        return $"XGUI/Resources/{themeName}/Icons";
    }

    private static string GetIconTypeDirectory(IconType iconType)
    {
        return iconType switch
        {
            IconType.UI => "UI",
            IconType.FileType => "FileTypes",
            IconType.Folder => "Folders",
            IconType.Misc => "Misc",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Look up an icon by name and size
    /// </summary>
    public static string? GetIcon(string iconName, IconType iconType = IconType.UI, int size = 16, string? variant = null)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        iconName = iconName.ToLowerInvariant();

        if (!string.IsNullOrEmpty(variant))
        {
            variant = variant.ToLowerInvariant();
        }

        string cacheKey = $"{_currentThemeName}/{iconType}/{iconName}/{size}/{variant}";
        if (IconPathCache.TryGetValue(cacheKey, out string? cachedPath))
            return cachedPath;

        string? iconPath = null;
        if (!string.IsNullOrEmpty(variant))
        {
            iconPath = FindIconInTheme(_currentThemeName, iconName, iconType, size, variant);

            if (string.IsNullOrEmpty(iconPath) && _currentThemeName != DefaultThemeName)
            {
                iconPath = FindIconInTheme(DefaultThemeName, iconName, iconType, size, variant);
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                return GetIcon(iconName, iconType, size);
            }
        }
        else
        {
            iconPath = FindIconInTheme(_currentThemeName, iconName, iconType, size);

            if (string.IsNullOrEmpty(iconPath) && _currentThemeName != DefaultThemeName)
            {
                iconPath = FindIconInTheme(DefaultThemeName, iconName, iconType, size);
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                if (iconType == IconType.UI)
                {
                    iconPath = $"material:{iconName}";
                }
                else if (iconType == IconType.FileType)
                {
                    iconPath = FindIconInTheme(_currentThemeName, "file", iconType, size) ??
                               FindIconInTheme(DefaultThemeName, "file", iconType, size);
                }
                else if (iconType == IconType.Folder)
                {
                    iconPath = FindIconInTheme(_currentThemeName, "folder", iconType, size) ??
                               FindIconInTheme(DefaultThemeName, "folder", iconType, size);
                }
            }
        }

        if (!string.IsNullOrEmpty(iconPath))
        {
            IconPathCache[cacheKey] = iconPath;
        }

        return iconPath;
    }

    /// <summary>
    /// Get an icon for a specific file extension
    /// </summary>
    public static string? GetFileIcon(string extension, int size = 16, string? variant = null)
    {
        if (string.IsNullOrEmpty(extension))
            return GetIcon("file", IconType.FileType, size, variant);

        if (extension.StartsWith("."))
            extension = extension.Substring(1);

        extension = extension.ToLowerInvariant();

        return GetIcon(extension, IconType.FileType, size, variant);
    }

    /// <summary>
    /// Get a folder icon
    /// </summary>
    public static string? GetFolderIcon(string folderType = "folder", int size = 16, string? variant = null)
    {
        return GetIcon(folderType, IconType.Folder, size, variant);
    }

    private static string? FindIconInTheme(string themeName, string iconName, IconType iconType, int size, string? variant = null)
    {
        string baseDir = GetThemeIconBaseDirectory(themeName);
        string typeDir = GetIconTypeDirectory(iconType);

        string fileNamePattern = !string.IsNullOrEmpty(variant) ?
            $"{iconName}_{size}_{variant}.png" : $"{iconName}_{size}.png";

        string exactPath = $"{baseDir}/{typeDir}/{fileNamePattern}";
        
        // Resolve through the theme loader
        var resolvedPath = XGUIThemeLoader.ResolveThemePath(exactPath);
        if (!string.IsNullOrEmpty(resolvedPath) && FileSystem.Mounted.FileExists(resolvedPath))
            return resolvedPath;

        // Also try without size suffix
        string simpleFileName = !string.IsNullOrEmpty(variant) ?
            $"{iconName}_{variant}.png" : $"{iconName}.png";
        string simplePath = $"{baseDir}/{typeDir}/{simpleFileName}";
        resolvedPath = XGUIThemeLoader.ResolveThemePath(simplePath);
        if (!string.IsNullOrEmpty(resolvedPath) && FileSystem.Mounted.FileExists(resolvedPath))
            return resolvedPath;

        return null;
    }

    /// <summary>
    /// Clear the icon cache
    /// </summary>
    public static void ClearCache()
    {
        IconPathCache.Clear();
    }
}

/// <summary>
/// Icon panel that uses the XGUIIconSystem to look up icons based on the current theme
/// </summary>
public class XGUIIconPanel : Panel
{
    private string? _iconName;
    private XGUIIconSystem.IconType _iconType = XGUIIconSystem.IconType.UI;
    private int _iconSize = 16;
    private string? _variant;
    private Image? _iconImage;
    private Label? _materialIconLabel;

    /// <summary>
    /// The name of the icon
    /// </summary>
    public string? IconName
    {
        get => _iconName;
        set
        {
            if (_iconName != value)
            {
                _iconName = value;
                UpdateIcon();
            }
        }
    }

    /// <summary>
    /// The type of icon
    /// </summary>
    public XGUIIconSystem.IconType IconType
    {
        get => _iconType;
        set
        {
            if (_iconType != value)
            {
                _iconType = value;
                UpdateIcon();
            }
        }
    }

    /// <summary>
    /// The desired size of the icon
    /// </summary>
    public int IconSize
    {
        get => _iconSize;
        set
        {
            if (_iconSize != value)
            {
                _iconSize = value;
                UpdateIcon();
            }
        }
    }

    /// <summary>
    /// The variant of the icon (e.g., "hover", "active", "disabled")
    /// </summary>
    public string? Variant
    {
        get => _variant;
        set
        {
            if (_variant != value)
            {
                _variant = value;
                UpdateIcon();
            }
        }
    }

    public XGUIIconPanel()
    {
        AddClass("xgui-icon-panel");

        _iconImage = AddChild<Image>();
        _iconImage.AddClass("icon-image");

        _materialIconLabel = AddChild<Label>();
        _materialIconLabel.AddClass("material-icon");

        _iconImage.Style.Display = DisplayMode.None;
        _materialIconLabel.Style.Display = DisplayMode.None;
    }

    public XGUIIconPanel(string iconName, XGUIIconSystem.IconType iconType = XGUIIconSystem.IconType.UI, int iconSize = 16, string? variant = null)
        : this()
    {
        _iconName = iconName;
        _iconType = iconType;
        _iconSize = iconSize;
        _variant = variant;
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (_iconImage == null || _materialIconLabel == null)
            return;
            
        if (string.IsNullOrEmpty(_iconName))
        {
            _iconImage.Style.Display = DisplayMode.None;
            _materialIconLabel.Style.Display = DisplayMode.None;
            return;
        }

        if (_iconName.StartsWith("url:"))
        {
            var imagePath = _iconName.Substring(4);
            _iconImage.Style.Display = DisplayMode.Flex;
            _materialIconLabel.Style.Display = DisplayMode.None;
            _iconImage.SetTexture(imagePath);
            _iconImage.Style.Width = Length.Pixels(_iconSize);
            _iconImage.Style.Height = Length.Pixels(_iconSize);
            return;
        }

        string? iconPath = XGUIIconSystem.GetIcon(_iconName, _iconType, _iconSize, _variant);

        if (string.IsNullOrEmpty(iconPath))
        {
            _iconImage.Style.Display = DisplayMode.None;
            _materialIconLabel.Style.Display = DisplayMode.None;
        }
        else if (iconPath.StartsWith("material:"))
        {
            _iconImage.Style.Display = DisplayMode.None;
            _materialIconLabel.Style.Display = DisplayMode.Flex;
            _materialIconLabel.Text = iconPath.Substring(9);
            _materialIconLabel.Style.FontSize = Length.Pixels(_iconSize);
        }
        else
        {
            _iconImage.Style.Display = DisplayMode.Flex;
            _materialIconLabel.Style.Display = DisplayMode.None;
            _iconImage.SetTexture(iconPath);
            _iconImage.Style.Width = Length.Pixels(_iconSize);
            _iconImage.Style.Height = Length.Pixels(_iconSize);
        }
    }

    /// <summary>
    /// Set the icon by name
    /// </summary>
    public void SetIcon(string iconName, XGUIIconSystem.IconType iconType = XGUIIconSystem.IconType.UI, int iconSize = 16, string? variant = null)
    {
        _iconName = iconName;
        _iconType = iconType;
        _iconSize = iconSize;
        _variant = variant;
        UpdateIcon();
    }

    /// <summary>
    /// Set the icon for a file extension
    /// </summary>
    public void SetFileIcon(string extension, int iconSize = 16, string? variant = null)
    {
        _iconType = XGUIIconSystem.IconType.FileType;
        _iconSize = iconSize;
        _variant = variant;

        if (string.IsNullOrEmpty(extension))
        {
            _iconName = "file";
        }
        else
        {
            if (extension.StartsWith("."))
                extension = extension.Substring(1);

            _iconName = extension.ToLowerInvariant();
        }

        UpdateIcon();
    }

    /// <summary>
    /// Set the icon for a folder
    /// </summary>
    public void SetFolderIcon(string folderType = "folder", int iconSize = 16, string? variant = null)
    {
        _iconType = XGUIIconSystem.IconType.Folder;
        _iconSize = iconSize;
        _iconName = folderType;
        _variant = variant;
        UpdateIcon();
    }
}
