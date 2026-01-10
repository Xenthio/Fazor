# Cursor CSS Support

This document describes the cursor CSS support added to Fazor, which is based on S&box's cursor implementation.

## Overview

The cursor CSS property allows you to change the mouse cursor when hovering over UI elements. This is implemented identically to S&box's approach, using:

1. A `StandardCursor` enum that matches S&box's `InputStandardCursor_t`
2. CSS cursor string to enum mapping
3. Automatic cursor updates based on the hovered panel's styles
4. Integration with Silk.NET's cursor API

## Usage

Simply add the `cursor` CSS property to any element's style:

```scss
.my-button {
    cursor: pointer; // Shows hand cursor on hover
}

.text-input {
    cursor: text; // Shows I-beam cursor for text editing
}

.resize-handle {
    cursor: ew-resize; // Shows horizontal resize cursor
}
```

## Supported Cursor Values

The following CSS cursor values are supported:

### Basic Cursors
- `default` - Standard arrow pointer
- `pointer` / `hand` - Pointing hand (for clickable items)
- `text` - I-beam for text selection
- `wait` - Hourglass/waiting cursor
- `crosshair` - Crosshair for precise selection

### Resize Cursors
- `ns-resize` - North-south (vertical) resize
- `ew-resize` - East-west (horizontal) resize
- `nesw-resize` - Northeast-southwest diagonal resize
- `nwse-resize` - Northwest-southeast diagonal resize
- `move` / `all-scroll` - Move in all directions

### Special Cursors
- `not-allowed` / `no-drop` - Prohibited/not allowed
- `grab` - Open hand (grabbable)
- `grabbing` - Closed hand (grabbing)
- `progress` - Arrow with hourglass

### Auto/Inherit
- `auto` - No explicit cursor (uses parent or default)

## Implementation Details

### StandardCursor Enum

Defined in `src/Sandbox.UI/Types/StandardCursor.cs`, matching S&box's enum exactly:

```csharp
public enum StandardCursor
{
    Arrow = 0,
    IBeam,
    HourGlass,
    Crosshair,
    WaitArrow,
    Up,
    SizeNWSE,
    SizeNESW,
    SizeWE,
    SizeNS,
    SizeALL,
    No,
    Hand,
    HandClosed,
}
```

### CursorHelper

Located in `src/Sandbox.UI/Utility/CursorHelper.cs`, provides mapping from CSS strings to enum values:

```csharp
StandardCursor? cursor = CursorHelper.FromCssString("pointer");
// Returns StandardCursor.Hand
```

### RootPanel.GetCurrentCursor()

New method in `RootPanel` that:
1. Gets the currently hovered panel
2. Walks up the panel hierarchy
3. Returns the first non-null cursor value found
4. Returns null if no cursor is set (uses default)

### NativeWindow Integration

The `NativeWindow` class automatically updates the system cursor every frame based on the hovered panel:

```csharp
private void UpdateCursor()
{
    var panelCursor = RootPanel.GetCurrentCursor();
    if (panelCursor.HasValue)
    {
        _mouse.Cursor.StandardCursor = panelCursor.Value.ToSilkCursor();
    }
}
```

## Example

The `CursorTest.razor` component in `examples/SimpleDesktopApp` demonstrates all supported cursor types. Run the app and click "🖱️ Cursor CSS Test" to see them in action.

## Testing

Comprehensive unit tests are available in `tests/Sandbox.UI.Tests/CursorTests.cs`, covering:
- Enum value correctness
- CSS string mapping
- Case-insensitive parsing
- Null/invalid value handling
- BaseStyles cursor property functionality

All 22 tests pass successfully.

## S&box Compatibility

This implementation follows S&box's cursor system exactly:
- Same enum names and values
- Same CSS property name
- Same inheritance behavior
- Compatible cursor value strings

Code ported from: `engine/Sandbox.System/UI/InputStandardCursor_t.cs`
