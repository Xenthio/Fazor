using Sandbox;
using Sandbox.UI;

// Use Sandbox.UI types to avoid ambiguity
using Vector2 = Sandbox.UI.Vector2;
using Log = Sandbox.UI.Log;

namespace XGUI;

/// <summary>
/// XGUI ListOption - an option in a SelectList.
/// </summary>
public class ListOption : Panel
{
    private bool _selected;

    public bool Selected
    {
        get => _selected;
        set
        {
            SetClass("selected", value);
            _selected = value;
        }
    }
    
    public SelectList? ParentList { get; set; }
    
    public ListOption()
    {
        SetClass("listoption", true);
    }
    
    protected override void OnClick(MousePanelEvent e)
    {
        base.OnClick(e);
        if (ParentList == null)
        {
            Log.Error("ListOption used outside of SelectList");
            return;
        }

        ParentList.OptionSelected(this);
    }
}

/// <summary>
/// XGUI SelectList - a list of selectable options.
/// </summary>
public class SelectList : Panel
{
    public ListOption? SelectedOption { get; private set; }
    public Action<ListOption>? OnSelected { get; set; }

    public SelectList()
    {
        AddClass("selectlist");
    }

    protected override void OnChildAdded(Panel child)
    {
        base.OnChildAdded(child);
        if (child is ListOption opt)
        {
            opt.ParentList = this;
        }
    }

    public void OptionSelected(ListOption option)
    {
        // Deselect previous
        if (SelectedOption != null)
        {
            SelectedOption.Selected = false;
        }

        // Select new
        SelectedOption = option;
        option.Selected = true;
        OnSelected?.Invoke(option);
    }
}

/// <summary>
/// XGUI SliderScale control - a slider with scale markers.
/// </summary>
public class SliderScale : Panel
{
    private float _value;
    private float _min;
    private float _max = 100;
    
    public float Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _min, _max);
            OnValueChanged?.Invoke(_value);
            UpdateSlider();
        }
    }
    
    public float Min
    {
        get => _min;
        set
        {
            _min = value;
            Value = Math.Clamp(_value, _min, _max);
        }
    }
    
    public float Max
    {
        get => _max;
        set
        {
            _max = value;
            Value = Math.Clamp(_value, _min, _max);
        }
    }
    
    public Action<float>? OnValueChanged { get; set; }
    
    private Panel? _track;
    private Panel? _thumb;

    public SliderScale()
    {
        AddClass("sliderscale");
        
        _track = AddChild<Panel>("sliderscale-track");
        _thumb = _track.AddChild<Panel>("sliderscale-thumb");
    }
    
    private void UpdateSlider()
    {
        if (_thumb == null || _max <= _min) return;
        
        float percent = (_value - _min) / (_max - _min);
        _thumb.Style.Left = Length.Percent(percent * 100);
    }
}

/// <summary>
/// XGUI ColorPickerControl - a color picker.
/// </summary>
public class ColorPickerControl : Panel
{
    private Color _value = Color.White;
    
    public Color Value
    {
        get => _value;
        set
        {
            _value = value;
            OnValueChanged?.Invoke(value);
        }
    }
    
    public Action<Color>? OnValueChanged { get; set; }

    public ColorPickerControl()
    {
        AddClass("colorpicker");
    }
}

/// <summary>
/// XGUI ControlLabel - a label for form controls.
/// </summary>
public class ControlLabel : Label
{
    public ControlLabel()
    {
        AddClass("controllabel");
    }
    
    public ControlLabel(string text) : this()
    {
        Text = text;
    }
}
