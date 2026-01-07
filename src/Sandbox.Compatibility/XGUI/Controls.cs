using Sandbox.UI;

namespace XGUI;

/// <summary>
/// XGUI ListView control - stub implementation.
/// </summary>
public class ListView : Panel
{
    public ListView()
    {
        AddClass("listview");
    }
    
    /// <summary>
    /// The currently selected item.
    /// </summary>
    public object? SelectedItem { get; set; }
    
    /// <summary>
    /// The items in the list.
    /// </summary>
    public List<object> Items { get; } = new();
    
    /// <summary>
    /// Event fired when selection changes.
    /// </summary>
    public event Action<object?>? OnSelectionChanged;
    
    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    public void AddItem(object item)
    {
        Items.Add(item);
        StateHasChanged();
    }
    
    /// <summary>
    /// Removes an item from the list.
    /// </summary>
    public void RemoveItem(object item)
    {
        Items.Remove(item);
        StateHasChanged();
    }
    
    /// <summary>
    /// Clears all items.
    /// </summary>
    public void Clear()
    {
        Items.Clear();
        SelectedItem = null;
        StateHasChanged();
    }
}

/// <summary>
/// XGUI ContextMenu control - stub implementation.
/// </summary>
public class ContextMenu : Panel
{
    public ContextMenu()
    {
        AddClass("contextmenu");
        AddClass("popup");
    }
    
    /// <summary>
    /// Shows the context menu at the specified position.
    /// </summary>
    public void Show(float x, float y)
    {
        Style.Position = PositionMode.Absolute;
        Style.Left = x;
        Style.Top = y;
        Style.Display = DisplayMode.Flex;
    }
    
    /// <summary>
    /// Hides the context menu.
    /// </summary>
    public void Hide()
    {
        Style.Display = DisplayMode.None;
    }
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    public void AddItem(string text, Action? onClick = null)
    {
        var item = new Button { Text = text };
        if (onClick != null)
        {
            item.AddEventListener("onclick", onClick);
        }
        AddChild(item);
    }
    
    /// <summary>
    /// Adds a separator.
    /// </summary>
    public void AddSeparator()
    {
        var sep = new Panel();
        sep.AddClass("separator");
        AddChild(sep);
    }
}

/// <summary>
/// XGUI Toolbar control - stub implementation.
/// </summary>
public class Toolbar : Panel
{
    public Toolbar()
    {
        AddClass("toolbar");
        Style.FlexDirection = FlexDirection.Row;
    }
}

/// <summary>
/// XGUI ToolbarButton control - stub implementation.
/// </summary>
public class ToolbarButton : Button
{
    public ToolbarButton()
    {
        AddClass("toolbarbutton");
    }
}

/// <summary>
/// XGUI FileBrowserView control - stub implementation.
/// </summary>
public class FileBrowserView : Panel
{
    public FileBrowserView()
    {
        AddClass("filebrowserview");
    }
    
    public FileBrowserViewMode ViewMode { get; set; } = FileBrowserViewMode.Icons;
    public string CurrentPath { get; set; } = "";
    public event Action<string>? OnPathChanged;
    public event Action<string>? OnFileSelected;
    public event Action<string>? OnFileActivated;
}

/// <summary>
/// File browser view modes.
/// </summary>
public enum FileBrowserViewMode
{
    Icons,
    List,
    Details,
    SmallIcons,
    Thumbnails
}

/// <summary>
/// XGUI FileBrowserTree control - stub implementation.
/// </summary>
public class FileBrowserTree : Panel
{
    public FileBrowserTree()
    {
        AddClass("filebrowsertree");
    }
    
    public string CurrentPath { get; set; } = "";
    public event Action<string>? OnPathChanged;
}

/// <summary>
/// XGUI FileItem control - represents a file item in browser views.
/// </summary>
public class FileItem : Panel
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// XGUI WebPanel control - stub implementation.
/// </summary>
public class WebPanel : Panel
{
    public WebPanel()
    {
        AddClass("webpanel");
    }
    
    public string Url { get; set; } = "";
    
    public void Navigate(string url)
    {
        Url = url;
        StateHasChanged();
    }
}
