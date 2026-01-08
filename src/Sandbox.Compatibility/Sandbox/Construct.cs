namespace Sandbox.UI.Construct;

/// <summary>
/// Static class providing extension methods for easily constructing panels.
/// </summary>
public static class PanelExtensions
{
    /// <summary>
    /// Gets a helper object for adding children with fluent syntax.
    /// </summary>
    public static PanelCreator Add => new PanelCreator();
}

/// <summary>
/// Helper class for constructing panels with fluent syntax.
/// </summary>
public class PanelCreator
{
    private Panel? _parent;
    
    internal PanelCreator() { }
    
    internal PanelCreator(Panel parent)
    {
        _parent = parent;
    }
    
    /// <summary>
    /// Add a panel with optional class name.
    /// </summary>
    public Panel Panel(string? className = null)
    {
        var panel = new Panel();
        if (!string.IsNullOrEmpty(className))
        {
            foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                panel.AddClass(cls);
            }
        }
        _parent?.AddChild(panel);
        return panel;
    }
    
    /// <summary>
    /// Add a label with text.
    /// </summary>
    public Label Label(string text, string? className = null)
    {
        var label = new Label { Text = text };
        if (!string.IsNullOrEmpty(className))
        {
            foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                label.AddClass(cls);
            }
        }
        _parent?.AddChild(label);
        return label;
    }
    
    /// <summary>
    /// Add a button with text.
    /// </summary>
    public Button Button(string text, Action? onClick = null, string? className = null)
    {
        var button = new Button { Text = text };
        if (onClick != null)
        {
            button.AddEventListener("onclick", onClick);
        }
        if (!string.IsNullOrEmpty(className))
        {
            foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                button.AddClass(cls);
            }
        }
        _parent?.AddChild(button);
        return button;
    }
    
    /// <summary>
    /// Add an image.
    /// </summary>
    public Image Image(string? source = null, string? className = null)
    {
        var image = new Image();
        if (!string.IsNullOrEmpty(source))
        {
            image.SetTexture(source);
        }
        if (!string.IsNullOrEmpty(className))
        {
            foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                image.AddClass(cls);
            }
        }
        _parent?.AddChild(image);
        return image;
    }
    
    /// <summary>
    /// Add a text entry.
    /// </summary>
    public TextEntry TextEntry(string? placeholder = null, string? className = null)
    {
        var entry = new TextEntry();
        if (!string.IsNullOrEmpty(placeholder))
        {
            entry.Placeholder = placeholder;
        }
        if (!string.IsNullOrEmpty(className))
        {
            foreach (var cls in className.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                entry.AddClass(cls);
            }
        }
        _parent?.AddChild(entry);
        return entry;
    }
}

/// <summary>
/// Extension methods for Panel class to support fluent construction.
/// </summary>
public static class PanelConstructExtensions
{
    /// <summary>
    /// Gets a helper object for adding children with fluent syntax.
    /// </summary>
    public static PanelCreator Add(this Panel panel) => new PanelCreator(panel);
}
