using Sandbox;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A panel with a title and border, useful for grouping related controls.
/// Based on XGUI-3's GroupBox.
/// </summary>
[Library("groupbox")]
public class GroupBox : Panel
{
    /// <summary>
    /// The title displayed at the top of the group box
    /// </summary>
    public string? Title { get; set; }
    private Label? _titleElement;
    public GroupBox()
    {
        AddClass("group-box");  // Fixed: Changed from "groupbox" to "group-box" to match XGUI-3
        ElementName = "groupbox";
    }
    protected override void OnAfterTreeRender(bool firstTime)
    {
        base.OnAfterTreeRender(firstTime);
        // Add or update title element
        if (!string.IsNullOrEmpty(Title))
        {
            // Check if we already have a title element
            if (_titleElement == null)
            {
                _titleElement = AddChild(new Label("", "group-box-title"));  // Fixed: Changed from "groupbox-title" to "group-box-title" to match XGUI-3
                _titleElement.Text = Title;
            }
            else
            {
                _titleElement.Text = Title;
            }
            // Apply parent's background color to the title element
            // This creates the "cut-out" effect for the border
            UpdateTitleBackground();
        }
    }
    // Call this whenever the background might change
    public override void Tick()
    {
        base.Tick();
        if (_titleElement != null)
        {
            UpdateTitleBackground();
        }
    }
    private void UpdateTitleBackground()
    {
        if (_titleElement == null)
            return;
        // Find the first ancestor with a defined background color
        var backgroundColor = FindAncestorBackgroundColor();
        // Apply the found background color or default to transparent
        _titleElement.Style.BackgroundColor = backgroundColor ?? Color.Transparent;
        _titleElement.Style.Dirty();  // Added: Mark style as dirty to ensure update
    }
    private Color? FindAncestorBackgroundColor()
    {
        // Start with the parent (skip the GroupBox itself)
        Panel? current = Parent;
        // Traverse up the hierarchy
        while (current != null)
        {
            if (current.ComputedStyle?.BackgroundColor == null)
            {
                current = current.Parent;
                continue;
            }
            // Check if this panel has a defined background color
            var bgColor = current.ComputedStyle.BackgroundColor.Value;
            // If the alpha is greater than 0, it's a visible color
            if (bgColor.a > 0)
            {
                return bgColor;
            }
            // Move up to the next ancestor
            current = current.Parent;
        }
        // No suitable background color found
        return null;
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "title")
        {
            Title = value;
            return;
        }
        base.SetProperty(name, value);
    }
}
