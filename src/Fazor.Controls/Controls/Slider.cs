using Sandbox;
using Microsoft.AspNetCore.Components;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A horizontal slider control.
/// Based on XGUI-3's Slider implementation.
/// </summary>
[Library("slider")]
public class Slider : Panel
{
    public Panel? Track { get; protected set; }
    public Panel? TrackInner { get; protected set; }
    public Panel? Thumb { get; protected set; }
    private float _minValue = 0;
    private float _maxValue = 100;
    private float _step = 1.0f;
    private float _value = 0;
    /// <summary>
    /// The minimum value of the slider
    /// </summary>
    public float MinValue
    {
        get => _minValue;
        set
        {
            _minValue = value;
            UpdateSliderPositions();
        }
    }
    /// <summary>
    /// The maximum value of the slider
    /// </summary>
    public float MaxValue
    {
        get => _maxValue;
        set
        {
            _maxValue = value;
            UpdateSliderPositions();
        }
    }
    /// <summary>
    /// Step size for value snapping
    /// If set to 1, value will be rounded to 1's
    /// If set to 10, value will be rounded to 10's
    /// If set to 0.1, value will be rounded to 0.1's
    /// </summary>
    public float Step
    {
        get => _step;
        set
        {
            _step = value;
            UpdateSliderPositions();
        }
    }
    /// <summary>
    /// The current value of the slider
    /// </summary>
    public float Value
    {
        get => MathX.Clamp(_value, MinValue, MaxValue);
        set
        {
            var snapped = Step > 0 ? SnapToGrid(value, Step) : value;
            snapped = MathX.Clamp(snapped, MinValue, MaxValue);
            if (_value == snapped) return;
            _value = snapped;
            OnValueChanged();
            UpdateSliderPositions();
        }
    }
    /// <summary>
    /// Called when the value changes
    /// </summary>
    [Parameter]
    public Action<float>? ValueChanged { get; set; }
    public Slider()
    {
        AddClass("slider");
        AddClass("sliderroot");
        ElementName = "slider";
        // TODO: AcceptsFocus = true; (not available yet in base Panel)
        Track = AddChild(new Panel(this, "track"));
        TrackInner = Track.AddChild(new Panel(this, "inner"));
        Thumb = AddChild(new Panel(this, "thumb"));
    }
    public override void SetProperty(string name, string value)
    {
        switch (name)
        {
            case "min":
                if (float.TryParse(value, out float min))
                    MinValue = min;
                return;
            case "max":
                if (float.TryParse(value, out float max))
                    MaxValue = max;
                return;
            case "step":
                if (float.TryParse(value, out float step))
                    Step = step;
                return;
            case "value":
                if (float.TryParse(value, out float val))
                    Value = val;
                return;
        }
        base.SetProperty(name, value);
    }
    protected virtual void OnValueChanged()
    {
        ValueChanged?.Invoke(Value);
    }
    /// <summary>
    /// Snap value to grid based on step size
    /// </summary>
    protected static float SnapToGrid(float value, float step)
    {
        if (step <= 0) return value;
        return MathF.Round(value / step) * step;
    }
    private int _positionHash;
    /// <summary>
    /// Update the visual position of the slider based on current value
    /// </summary>
    protected virtual void UpdateSliderPositions()
    {
        var hash = HashCode.Combine(Value, MinValue, MaxValue);
        if (hash == _positionHash) return;
        _positionHash = hash;
        var pos = MathX.LerpInverse(Value, MinValue, MaxValue, true);
        if (TrackInner != null)
        {
            TrackInner.Style.Width = Length.Fraction(pos);
            TrackInner.Style.Dirty();
        }
        if (Thumb != null)
        {
            Thumb.Style.Left = Length.Fraction(pos);
            Thumb.Style.Dirty();
        }
    }
    public override void Tick()
    {
        base.Tick();
        // Ensure positions are updated
        UpdateSliderPositions();
    }
}
