using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A container for RadioButton controls that manages mutual exclusion.
/// Based on XGUI-3's RadioButtons.
/// </summary>
public class RadioButtons : Panel
{
    /// <summary>
    /// The currently selected radio button
    /// </summary>
    public RadioButton? SelectedRadioOption { get; private set; }
    /// <summary>
    /// The value of the currently selected radio button
    /// </summary>
    public string? SelectedValue => SelectedRadioOption?.Value;
    public RadioButtons()
    {
        AddClass("radiobuttons");
        ElementName = "radiobuttons";
    }
    /// <summary>
    /// Select a specific radio button option
    /// </summary>
    public void SelectOption(RadioButton option)
    {
        if (SelectedRadioOption == option)
            return;
        // Deselect previous option
        if (SelectedRadioOption != null)
        {
            SelectedRadioOption.Selected = false;
        }
        // Select new option
        SelectedRadioOption = option;
        option.Selected = true;
        OnSelectionChanged?.Invoke(option);
    }
    /// <summary>
    /// Select an option by its value
    /// </summary>
    public void SelectByValue(string? value)
    {
        if (value == null) return;
        foreach (var child in Children)
        {
            if (child is RadioButton radio && radio.Value == value)
            {
                SelectOption(radio);
                return;
            }
        }
    }
    /// <summary>
    /// Called when the selection changes
    /// </summary>
    public event Action<RadioButton>? OnSelectionChanged;
    public override void SetProperty(string name, string value)
    {
        if (name == "value")
        {
            SelectByValue(value);
        }
        base.SetProperty(name, value);
    }
}
