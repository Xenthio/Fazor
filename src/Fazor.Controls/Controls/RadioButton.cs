using Sandbox;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A radio button control that works as part of a RadioButtons group.
/// Based on XGUI-3's RadioButton.
/// </summary>
[Library("radiobutton"), Alias("radio")]
public class RadioButton : Panel
{
    /// <summary>
    /// The radio button icon panel.
    /// </summary>
    public Panel? CheckMark { get; protected set; }
    /// <summary>
    /// Optional radio segments for themes that use characters to make up the radio button.
    /// </summary>
    internal Label? OptionalRadioSegment1 { get; set; }
    internal Label? OptionalRadioSegment2 { get; set; }
    internal Label? OptionalRadioSegment3 { get; set; }
    protected bool _selected = false;
    /// <summary>
    /// Returns true if this radio button is selected
    /// </summary>
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;
            _selected = value;
            OnValueChanged();
        }
    }
    /// <summary>
    /// The value associated with this radio button
    /// </summary>
    public string? Value { get; set; }
    /// <summary>
    /// The label associated with the radio button
    /// </summary>
    public Label? Label { get; protected set; }
    /// <summary>
    /// The text displayed on the label
    /// </summary>
    public string? LabelText
    {
        get => Label?.Text;
        set
        {
            if (Label == null)
            {
                Label = AddChild(new Label());
            }
            Label.Text = value ?? "";
        }
    }
    public RadioButton()
    {
        AddClass("radiobutton");
        ElementName = "radiobutton";
        CheckMark = AddChild(new Panel(this, "checkpanel"));
        var checkLabel = CheckMark.AddChild(new Label("a", "checklabel"));
        // Add radio segments directly to CheckMark, not to RadioButton
        OptionalRadioSegment1 = CheckMark.AddChild(new Label("", "radio-seg1"));
        OptionalRadioSegment2 = CheckMark.AddChild(new Label("", "radio-seg2"));
        OptionalRadioSegment3 = CheckMark.AddChild(new Label("", "radio-seg3"));
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "selected" || name == "checked")
        {
            Selected = value == "true" || value == "1";
            return;
        }
        if (name == "value")
        {
            Value = value;
            return;
        }
        if (name == "text")
        {
            LabelText = value;
            return;
        }
        base.SetProperty(name, value);
    }
    public override void SetContent(string? value)
    {
        LabelText = value?.Trim() ?? "";
    }
    /// <summary>
    /// Called when the selection state changes
    /// </summary>
    public event Action<bool>? ValueChanged;
    public virtual void OnValueChanged()
    {
        UpdateState();
        CreateEvent("onchange", Selected);
        CreateValueEvent("checked", Selected);
        CreateValueEvent("value", Selected);
        ValueChanged?.Invoke(Selected);
        if (Selected)
        {
            CreateEvent("onchecked");
            OnSelected?.Invoke();
        }
        else
        {
            CreateEvent("onunchecked");
            OnDeselected?.Invoke();
        }
    }
    /// <summary>
    /// Called when radio button is selected
    /// </summary>
    public event Action? OnSelected;
    /// <summary>
    /// Called when radio button is deselected
    /// </summary>
    public event Action? OnDeselected;
    protected virtual void UpdateState()
    {
        SetClass("checked", Selected);
    }
    /// <summary>
    /// Handle mouse click to select this radio button
    /// </summary>
    protected override void OnClick(MousePanelEvent e)
    {
        base.OnClick(e);
        if (Parent is RadioButtons radioButtons)
        {
            if (radioButtons.SelectedRadioOption != this)
            {
                radioButtons.SelectOption(this);
            }
        }
        else
        {
            Selected = true;
        }
        e.StopPropagation();
    }
    /// <summary>
    /// Select this radio button (and deselect others in the group if parent is RadioButtons)
    /// </summary>
    public void Select()
    {
        if (Parent is RadioButtons radioButtons)
        {
            radioButtons.SelectOption(this);
        }
        else
        {
            Selected = true;
        }
    }
}
