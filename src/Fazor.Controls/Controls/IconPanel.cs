using Sandbox;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A panel containing an icon, typically a material icon.
/// Based on s&box's IconPanel.
/// </summary>
[Library("iconpanel"), Alias("icon")]
public class IconPanel : Label
{
    public IconPanel()
    {
        AddClass("iconpanel");
        ElementName = "iconpanel";
    }
    public IconPanel(string icon, string? classes = null) : this()
    {
        Text = icon;
        if (classes != null)
            AddClass(classes);
    }
}
