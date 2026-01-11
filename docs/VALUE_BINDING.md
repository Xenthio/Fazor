# Value Binding and Event Handling in Fazor

This document explains how to properly use value binding and event handling with Fazor controls, following S&box's UI framework patterns.

## Overview

Fazor controls support multiple patterns for handling value changes:
1. **Event binding** using `@onchange`
2. **Parameter callback** using `ValueChanged`, `OnTextEdited`, etc.
3. **Manual two-way binding** using lambda expressions

## Pattern 1: Event Binding (@onchange)

Use the `@onchange` event for simple scenarios where you don't need the new value as a parameter.

```razor
<check value="@myBool" @onchange="OnMyBoolChanged">Enable Feature</check>
<combobox value="@mySelection" @onchange="OnSelectionChanged">
    <option value="opt1">Option 1</option>
    <option value="opt2">Option 2</option>
</combobox>
<textentry value="@myText" @onchange="OnTextChanged" />

@code {
    private bool myBool = false;
    private string mySelection = "opt1";
    private string myText = "";
    
    private void OnMyBoolChanged()
    {
        // Note: myBool is NOT automatically updated!
        // You need to manually read the control's value or use Pattern 2
        StateHasChanged();
    }
    
    private void OnSelectionChanged()
    {
        // Same here - mySelection is NOT automatically updated
        StateHasChanged();
    }
    
    private void OnTextChanged()
    {
        // myText is NOT automatically updated
        StateHasChanged();
    }
}
```

**Important**: With `@onchange`, the bound variable is **NOT** automatically updated. The event is just a notification that something changed. You need to either:
- Read the control's current value manually
- Use Pattern 2 (Parameter callback) instead

## Pattern 2: Parameter Callback (Recommended)

Use parameter callbacks to receive the new value directly. This is the **S&box standard** pattern.

### CheckBox / RadioButton

```razor
<check value="@isEnabled" ValueChanged="@OnEnabledChanged">Enable Feature</check>

@code {
    private bool isEnabled = false;
    
    private void OnEnabledChanged(bool newValue)
    {
        isEnabled = newValue;  // Update the variable
        Console.WriteLine($"Enabled: {newValue}");
        StateHasChanged();  // Trigger re-render
    }
}
```

### ComboBox

```razor
<combobox value="@selectedOption" ValueChanged="@OnOptionChanged">
    <option value="option1">Option 1</option>
    <option value="option2">Option 2</option>
</combobox>

@code {
    private string selectedOption = "option1";
    
    private void OnOptionChanged(string newValue)
    {
        selectedOption = newValue;
        Console.WriteLine($"Selected: {newValue}");
        StateHasChanged();
    }
}
```

### TextEntry

```razor
<textentry value="@userName" OnTextEdited="@OnUserNameChanged" placeholder="Enter name" />

@code {
    private string userName = "";
    
    private void OnUserNameChanged(string newValue)
    {
        userName = newValue;
        Console.WriteLine($"Name: {newValue}");
        StateHasChanged();
    }
}
```

### Slider

```razor
<slider value="@volume" ValueChanged="@OnVolumeChanged" min="0" max="100" />

@code {
    private float volume = 50f;
    
    private void OnVolumeChanged(float newValue)
    {
        volume = newValue;
        Console.WriteLine($"Volume: {newValue}");
        StateHasChanged();
    }
}
```

## Pattern 3: Manual Two-Way Binding

For simple cases, you can use inline lambda expressions for immediate two-way binding:

```razor
<check value="@isChecked" ValueChanged="@((val) => { isChecked = val; StateHasChanged(); })">
    Enable
</check>

<textentry value="@text" OnTextEdited="@((val) => { text = val; StateHasChanged(); })" />

<slider value="@sliderVal" ValueChanged="@((val) => { sliderVal = val; StateHasChanged(); })" />

@code {
    private bool isChecked = false;
    private string text = "";
    private float sliderVal = 0f;
}
```

## Control-Specific Callbacks

Different controls use different callback names to match S&box conventions:

| Control | Callback Property | Parameter Type |
|---------|------------------|----------------|
| CheckBox | `ValueChanged` | `Action<bool>` |
| CheckBox | `OnChecked` | `Action` |
| CheckBox | `OnUnchecked` | `Action` |
| RadioButton | `ValueChanged` | `Action<bool>` |
| RadioButton | `OnSelected` | `Action` |
| ComboBox | `ValueChanged` | `Action<string>` |
| TextEntry | `OnTextEdited` | `Action<string>` |
| Slider | `ValueChanged` | `Action<float>` |
| SliderScale | `ValueChanged` | `Action<float>` |
| SliderScaleEntry | `ValueChanged` | `Action<float>` |

## @bind-value (Future)

True two-way binding with `@bind-value` syntax is not yet fully implemented in Fazor's Razor transpiler. Use Pattern 2 or Pattern 3 instead.

```razor
<!-- NOT YET SUPPORTED -->
<textentry @bind-value="@myText" />
```

## Best Practices

1. **Use Pattern 2** (Parameter callback) for most scenarios - it's the S&box standard
2. **Always call `StateHasChanged()`** after updating state to trigger re-rendering
3. **Use Pattern 3** for quick prototypes or very simple bindings
4. **Avoid Pattern 1** unless you specifically need event notifications without value updates
5. **Don't mix patterns** on the same control - choose one approach and stick with it

## Examples

See `/examples/TestApp/ValueBindingTest.razor` for a complete working example demonstrating all three patterns.
