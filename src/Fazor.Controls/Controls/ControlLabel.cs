using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A label control that highlights when its children have focus.
/// Based on XGUI-3's ControlLabel.
/// </summary>
public class ControlLabel : Panel
{
    /// <summary>
    /// The label element
    /// </summary>
    public Label? Label { get; protected set; }
    public ControlLabel()
    {
        AddClass("controllabel");
        ElementName = "controllabel";
        Label = AddChild(new Label());
    }
    public override void Tick()
    {
        base.Tick();
        var shouldFocus = PanelHasFocus(this) || AnyChildHasFocus(this);
        SetClass("focus", shouldFocus);
    }
    public bool AnyChildHasFocus(Panel panel)
    {
        foreach (var child in panel.Children)
        {
            if (child != null && PanelHasFocus(child))
                return true;
        }
        return false;
    }
    public bool PanelHasFocus(Panel panel)
    {
        return panel.HasFocus;
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "label" || name == "text")
        {
            if (Label != null)
                Label.Text = value;
            return;
        }
        base.SetProperty(name, value);
    }
}
