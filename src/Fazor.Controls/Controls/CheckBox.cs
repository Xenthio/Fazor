using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A checkbox control.
/// Based on XGUI-3's CheckBox.
/// </summary>
public class CheckBox : Panel
{
    /// <summary>
    /// The checkmark icon panel.
    /// </summary>
    public Panel? CheckMark { get; protected set; }
    protected bool _checked = false;
    /// <summary>
    /// Returns true if this checkbox is checked
    /// </summary>
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
                return;
            _checked = value;
            OnValueChanged();
        }
    }
    /// <summary>
    /// Returns true if this checkbox is checked (alias for Checked)
    /// </summary>
    public bool Value
    {
        get => Checked;
        set => Checked = value;
    }
    /// <summary>
    /// The label associated with the checkbox
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
    public CheckBox()
    {
        AddClass("checkbox");
        ElementName = "checkbox";
        CheckMark = AddChild(new Panel(this, "checkpanel"));
        var checkLabel = CheckMark.AddChild(new Label("a", "checklabel"));
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "checked" || name == "value")
        {
            Checked = value == "true" || value == "1";
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
    /// Called when the value changes
    /// </summary>
    public event Action<bool>? ValueChanged;
    public virtual void OnValueChanged()
    {
        UpdateState();
        CreateEvent("onchange", Checked);
        CreateValueEvent("checked", Checked);
        CreateValueEvent("value", Checked);
        ValueChanged?.Invoke(Checked);
        if (Checked)
        {
            CreateEvent("onchecked");
            OnChecked?.Invoke();
        }
        else
        {
            CreateEvent("onunchecked");
            OnUnchecked?.Invoke();
        }
    }
    /// <summary>
    /// Called when checkbox is checked
    /// </summary>
    public event Action? OnChecked;
    /// <summary>
    /// Called when checkbox is unchecked
    /// </summary>
    public event Action? OnUnchecked;
    protected virtual void UpdateState()
    {
        SetClass("checked", Checked);
    }
    /// <summary>
    /// Handle mouse click to toggle checkbox
    /// </summary>
    protected override void OnClick(MousePanelEvent e)
    {
        base.OnClick(e);
        Checked = !Checked;
        e.StopPropagation();
    }
    /// <summary>
    /// Toggle the checkbox state
    /// </summary>
    public void Toggle()
    {
        Checked = !Checked;
    }
}
