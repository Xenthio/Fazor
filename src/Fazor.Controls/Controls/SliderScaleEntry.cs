using Sandbox;
using Microsoft.AspNetCore.Components;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A horizontal slider with scale markers and a text entry for numeric input.
/// Based on XGUI-3's SliderScaleEntry implementation.
/// </summary>
[Library("sliderscaleentry")]
public class SliderScaleEntry : Panel
{
    public SliderScale? Slider { get; protected set; }
    public TextEntry? TextEntry { get; protected set; }
    /// <summary>
    /// The minimum value
    /// </summary>
    public float MinValue
    {
        get => Slider?.MinValue ?? 0;
        set
        {
            if (Slider != null)
                Slider.MinValue = value;
            if (TextEntry != null)
                TextEntry.MinValue = value;
        }
    }
    /// <summary>
    /// The maximum value
    /// </summary>
    public float MaxValue
    {
        get => Slider?.MaxValue ?? 100;
        set
        {
            if (Slider != null)
                Slider.MaxValue = value;
            if (TextEntry != null)
                TextEntry.MaxValue = value;
        }
    }
    /// <summary>
    /// Step size for value snapping
    /// </summary>
    public float Step
    {
        get => Slider?.Step ?? 1;
        set
        {
            if (Slider != null)
                Slider.Step = value;
        }
    }
    /// <summary>
    /// Number format for the text entry
    /// </summary>
    public string Format
    {
        get => TextEntry?.NumberFormat ?? "0.###";
        set
        {
            if (TextEntry != null)
                TextEntry.NumberFormat = value;
        }
    }
    /// <summary>
    /// The current value
    /// </summary>
    public float Value
    {
        get => Slider?.Value ?? 0;
        set
        {
            if (Slider != null)
                Slider.Value = value;
        }
    }
    /// <summary>
    /// Called when the value changes
    /// </summary>
    [Parameter]
    public Action<float>? ValueChanged { get; set; }
    private bool _isUpdating = false;
    public SliderScaleEntry()
    {
        AddClass("sliderentry");
        ElementName = "sliderscaleentry";
        Slider = AddChild(new SliderScale());
        if (Slider.SliderArea != null)
        {
            TextEntry = Slider.SliderArea.AddChild(new TextEntry());
            TextEntry.Numeric = true;
            TextEntry.NumberFormat = "0.###";
        }
        // Wire up events
        if (Slider != null)
        {
            Slider.ValueChanged += OnSliderChanged;
            Slider.AddEventListener("value.changed", (e) => OnSliderChanged(Slider.Value));
        }
        if (TextEntry != null)
        {
            TextEntry.OnTextEdited += OnEntryChanged;
            TextEntry.AddEventListener("value.changed", (e) => OnEntryChanged(TextEntry.Value));
        }
    }
    private void OnSliderChanged(float value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            if (TextEntry != null)
            {
                TextEntry.Value = value.ToString(TextEntry.NumberFormat);
            }
            OnValueChanged(value);
        }
        finally
        {
            _isUpdating = false;
        }
    }
    private void OnEntryChanged(string value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            if (float.TryParse(value, out float floatValue))
            {
                if (Slider != null)
                {
                    Slider.Value = floatValue;
                }
                OnValueChanged(floatValue);
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }
    protected virtual void OnValueChanged(float value)
    {
        CreateValueEvent("value", value);
        ValueChanged?.Invoke(value);
    }
    public override void SetProperty(string name, string value)
    {
        switch (name)
        {
            case "min":
            case "max":
            case "value":
            case "step":
            case "mintext":
            case "maxtext":
            case "label":
                // Forward these to the slider
                Slider?.SetProperty(name, value);
                return;
            case "format":
                if (TextEntry != null)
                    TextEntry.NumberFormat = value;
                return;
        }
        base.SetProperty(name, value);
    }
}
