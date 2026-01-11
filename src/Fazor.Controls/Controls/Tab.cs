using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A single tab content panel within a TabContainer.
/// The tab markup should include slot="tab" and attributes for tabname, tabtext, and optionally tabicon.
/// Based on XGUI-3 pattern.
/// </summary>
public class Tab : Panel
{
    public Tab()
    {
        AddClass("tab");
        ElementName = "tab";
    }
    public override void SetProperty(string name, string value)
    {
        // Tab properties are handled by TabContainer.OnTemplateSlot
        // We just pass through to base for standard properties
        if (name == "slot")
        {
            // Ignore slot attribute - it's used for positioning in markup
            return;
        }
        base.SetProperty(name, value);
    }
}
