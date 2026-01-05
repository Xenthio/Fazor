using System;
using System.Collections.Generic;

namespace Sandbox.UI;

/// <summary>
/// The dropdown popup for a ComboBox control.
/// Extends BasePopup to support native popup windows.
/// </summary>
public class ComboBoxDropdown : BasePopup
{
    /// <summary>
    /// The ComboBox that owns this dropdown
    /// </summary>
    public ComboBox? OwnerComboBox { get; set; }

    /// <summary>
    /// The options to display
    /// </summary>
    public List<Option> Options { get; set; } = new();

    /// <summary>
    /// The currently selected option
    /// </summary>
    public Option? SelectedOption { get; set; }

    /// <summary>
    /// Event fired when an option is selected
    /// </summary>
    public event Action<Option>? OnOptionSelected;

    public ComboBoxDropdown()
    {
        AddClass("dropdown-panel");
        AddClass("flat-top");
        Style.FlexDirection = FlexDirection.Column;
    }

    /// <summary>
    /// Populate the dropdown with options
    /// </summary>
    public void PopulateOptions()
    {
        DeleteChildren(true);

        foreach (var option in Options)
        {
            var optionButton = AddChild(new Button(option.Title ?? "", () => SelectOption(option)));
            if (option.Icon != null) optionButton.Icon = option.Icon;

            if (SelectedOption != null && option.Value?.Equals(SelectedOption.Value) == true)
            {
                optionButton.AddClass("active");
            }
        }
    }

    private void SelectOption(Option option)
    {
        OnOptionSelected?.Invoke(option);
        Close();
    }

    public override void Open(Panel opener, bool preferBelow = true)
    {
        // Match opener width
        if (opener.Box != null)
        {
            Style.Width = opener.Box.Rect.Width;
        }

        // Set max height to allow scrolling for long lists
        Style.MaxHeight = 300;
        Style.OverflowY = OverflowMode.Scroll;

        base.Open(opener, preferBelow);
        PopulateOptions();
    }

    protected override void OpenInRootPanel(Panel opener)
    {
        base.OpenInRootPanel(opener);
        PopulateOptions();
    }
}
