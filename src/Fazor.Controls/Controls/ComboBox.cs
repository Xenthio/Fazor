using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A UI control which provides multiple options via a dropdown box.
/// Based on XGUI-3's ComboBox.
/// Now uses BasePopup for proper popup window support.
/// </summary>
public class ComboBox : Button
{
    /// <summary>
    /// The dropdown popup that shows options
    /// </summary>
    protected ComboBoxDropdown? DropdownPopup;
    /// <summary>
    /// Legacy: The dropdown panel (for backward compatibility with root panel fallback)
    /// </summary>
    [Obsolete("Use DropdownPopup instead")]
    protected Panel? DropdownPane => DropdownPopup;
    /// <summary>
    /// The icon of an arrow pointing down on the right of the element.
    /// </summary>
    protected IconPanel? DropdownIndicator;
    /// <summary>
    /// Called when the value has been changed.
    /// </summary>
    public Action<string>? ValueChanged { get; set; }
    /// <summary>
    /// Called just before opening, allows options to be dynamic.
    /// </summary>
    public Func<List<Option>>? BuildOptions { get; set; }
    /// <summary>
    /// The options to show on click. You can edit these directly via this property.
    /// </summary>
    public List<Option> Options { get; set; } = new();
    private Option? _selected;
    private object? _value;
    private int _valueHash;
    /// <summary>
    /// The current string value. This is useful to have if Selected is null.
    /// </summary>
    public new object? Value
    {
        get => _value;
        set
        {
            if (_valueHash == HashCode.Combine(value))
                return;
            if ($"{_value}" == $"{value}")
                return;
            _valueHash = HashCode.Combine(value);
            _value = value;
            if (BuildOptions != null)
            {
                Options = BuildOptions.Invoke();
            }
            if (_value != null && Options.Count == 0)
            {
                PopulateOptionsFromType(_value.GetType());
            }
            Select(_value?.ToString(), false);
        }
    }
    /// <summary>
    /// The currently selected option.
    /// </summary>
    public Option? Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            if (_selected != null)
            {
                _value = _selected.Value;
                _valueHash = HashCode.Combine(_value);
                if (_selected.Icon != null) Icon = _selected.Icon;
                if (_selected.Title != null) Text = _selected.Title;
                ValueChanged?.Invoke(_value?.ToString() ?? "");
                CreateEvent("onchange");
                CreateValueEvent("value", _selected.Value);
            }
        }
    }
    public ComboBox()
    {
        AddClass("selector");
        DropdownIndicator = AddChild(new IconPanel("u", "selector_indicator"));
    }
    public ComboBox(Panel parent) : this()
    {
        Parent = parent;
    }
    /// <summary>
    /// Given the type, populate options. This is useful if you're an enum type.
    /// </summary>
    private void PopulateOptionsFromType(Type type)
    {
        if (type == typeof(bool))
        {
            Options.Add(new Option("True", true));
            Options.Add(new Option("False", false));
            return;
        }
        if (type.IsEnum)
        {
            var names = type.GetEnumNames();
            var values = type.GetEnumValues();
            for (int i = 0; i < names.Length; i++)
            {
                Options.Add(new Option(names[i], values.GetValue(i)));
            }
            return;
        }
    }
    public bool IsOpen { get; private set; } = false;
    protected override void OnMouseDown(MousePanelEvent e)
    {
        base.OnMouseDown(e);
        if (!IsOpen)
        {
            Open();
        }
        else
        {
            Close();
        }
    }
    /// <summary>
    /// Open the dropdown.
    /// </summary>
    public void Open()
    {
        IsOpen = true;
        // Create dropdown popup
        DropdownPopup = new ComboBoxDropdown();
        DropdownPopup.OwnerComboBox = this;
        DropdownPopup.SelectedOption = Selected;
        DropdownPopup.OnOptionSelected += OnDropdownOptionSelected;
        DropdownPopup.OnPopupClosed += OnDropdownClosed;
        if (BuildOptions != null)
        {
            Options = BuildOptions.Invoke();
        }
        DropdownPopup.Options = Options;
        // Copy stylesheets for consistent styling
        foreach (var stylesheet in AllStyleSheets)
        {
            DropdownPopup.StyleSheet.Add(stylesheet);
        }
        // Open the popup below this element
        DropdownPopup.Open(this, preferBelow: true);
    }
    private void OnDropdownOptionSelected(Option option)
    {
        Select(option);
    }
    private void OnDropdownClosed()
    {
        if (DropdownPopup != null)
        {
            DropdownPopup.OnOptionSelected -= OnDropdownOptionSelected;
            DropdownPopup.OnPopupClosed -= OnDropdownClosed;
            DropdownPopup = null;
        }
        IsOpen = false;
    }
    /// <summary>
    /// Close the dropdown.
    /// </summary>
    public void Close()
    {
        DropdownPopup?.Close();
        // OnDropdownClosed will handle the rest
    }
    /// <summary>
    /// Select an option.
    /// </summary>
    protected virtual void Select(Option? option, bool triggerChange = true)
    {
        if (!triggerChange)
        {
            _selected = option;
            if (option != null)
            {
                _value = option.Value;
                _valueHash = HashCode.Combine(_value);
                if (option.Icon != null) Icon = option.Icon;
                if (option.Title != null) Text = option.Title;
            }
        }
        else
        {
            Selected = option;
        }
        Close();
    }
    /// <summary>
    /// Select an option by value string.
    /// </summary>
    protected virtual void Select(string? value, bool triggerChange = true)
    {
        if (value == null) return;
        Select(Options.FirstOrDefault(x => string.Equals(x.Value?.ToString(), value, StringComparison.OrdinalIgnoreCase)), triggerChange);
    }
    private string? _override = null;
    public override void Tick()
    {
        base.Tick();
        SetClass("open", DropdownPopup != null && DropdownPopup.IsPopupOpen);
        SetClass("active", DropdownPopup != null && DropdownPopup.IsPopupOpen);
        // Note: Position updates are now handled by the popup system
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "default")
        {
            _override = value;
            return;
        }
        base.SetProperty(name, value);
    }
    protected override void OnParametersSet()
    {
        // Only clear if we have some options to populate
        if (Children.Any(x => x.ElementName.Equals("option", StringComparison.OrdinalIgnoreCase))) 
            Options.Clear();
        foreach (var child in Children.ToList())
        {
            if (child.ElementName.Equals("option", StringComparison.OrdinalIgnoreCase))
            {
                var o = new Option();
                o.Title = string.Join("", child.Descendants.OfType<Label>().Select(x => x.Text));
                o.Value = child.GetAttribute("value") ?? o.Title;
                o.Icon = child.GetAttribute("icon");
                Options.Add(o);
                var optionValue = child.GetAttribute("value") ?? o.Title;
                if (_override != null && _override == optionValue)
                {
                    Select(o, true);
                }
                // Don't delete options - CSS handles hiding them with > option { display: none; }
            }
        }
    }
}