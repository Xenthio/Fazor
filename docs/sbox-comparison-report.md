# S&box vs Fazor UI Deep Comparison Report

## Executive Summary

This report documents the differences between Fazor's Sandbox.UI and S&box's UI system (from `engine/Sandbox.Engine/Systems/UI`). The analysis covers:
- Missing files/features
- Implementation differences in existing files
- Extra files in Fazor (not in S&box)

## 1. Missing Files in Fazor (Originally 29 files, now several implemented)

### Critical Missing Features

| File | Description | Priority | Status |
|------|-------------|----------|--------|
| `Panel/Panel.Drag.cs` | Drag scrolling support | HIGH | ✅ IMPLEMENTED |
| `Panel/Panel.Data.cs` | StringValue property, CreateValueEvent | HIGH | ✅ Already exists in Panel.Property.cs |
| `Panel/Panel.Tooltip.cs` | Tooltip system | MEDIUM | ✅ IMPLEMENTED |
| `Panel/Panel.Layer.cs` | Layer rendering for filters/masks | MEDIUM | ⏳ Not yet |
| `Engine/TextBlock.cs` | Rich text rendering with RichTextKit | HIGH | ⏳ Not yet |
| `Panel/Event/DragEvent.cs` | Drag event class | HIGH | ✅ IMPLEMENTED |
| `Panel/Event/CutCopyPasteEvent.cs` | Copy/Cut/Paste events | MEDIUM | ✅ IMPLEMENTED |
| `Panel/Event/PanelEventAttribute.cs` | Attribute-based event listeners | MEDIUM | ✅ IMPLEMENTED |

### VirtualLayouts (Performance Optimization)

| File | Description | Priority | Status |
|------|-------------|----------|--------|
| `VirtualLayouts/BaseVirtualPanel.cs` | Base virtualized scrolling panel | LOW | ⏳ Not yet |
| `VirtualLayouts/VirtualList.cs` | Virtualized list | LOW | ⏳ Not yet |
| `VirtualLayouts/VirtualGrid.cs` | Virtualized grid | LOW | ⏳ Not yet |
| `VirtualLayouts/LayoutUtility/VerticalLayout.cs` | Vertical layout helper | LOW | ⏳ Not yet |
| `VirtualLayouts/LayoutUtility/GridLayout.cs` | Grid layout helper | LOW | ⏳ Not yet |

### Utility/Support Files

| File | Description | Priority | Status |
|------|-------------|----------|--------|
| `Utility/PanelCreator.cs` | Panel.Add helper | LOW | ✅ IMPLEMENTED |
| `Utility/Clipboard.cs` | Clipboard access | MEDIUM | ⏳ Not yet |
| `Utility/Emoji.cs` | Emoji support | LOW | ⏳ Not yet |
| `Engine/SkiaCompat.cs` | Skia compatibility helpers | LOW | ⏳ Not yet |
| `Razor/RouteAttribute.cs` | Route attribute for pages | LOW | ⏳ Not yet |

## 2. Key Implementation Differences (Many Fixed)

### Panel.cs
- **S&box**: Has `Task` property for async, `Invoke/InvokeOnce/CancelInvoke` methods - ✅ IMPLEMENTED
- **S&box**: Has `Scene` and `GameObject` properties for game integration - N/A for desktop app
- **S&box**: Has `PlaySound` method - N/A for desktop app
- **S&box**: Has `IsValid` property (Fazor has `IsValid()` method)
- **S&box**: `InitializeEvents()` call in constructor - ✅ IMPLEMENTED
- **S&box**: `AddToLists()`/`RemoveFromLists()` for event registration - N/A for desktop app

### Panel.Children.cs
- **S&box**: Has `PanelCreator Add` property for quick child creation - ✅ IMPLEMENTED
- **S&box**: Throws exception in RemoveChild if child not found
- **S&box**: Has UnsignedMod for looping in GetChild

### Panel.Input.cs
- **S&box**: `MousePosition` uses GlobalMatrix transform - ✅ IMPLEMENTED
- **S&box**: `IsInside` includes border-radius hit testing - ✅ IMPLEMENTED
- **S&box**: Has `RayToLocalPosition` for world panel input - ✅ IMPLEMENTED
- **S&box**: `GetClipboardValue` supports `AllowChildSelection`
- **Fazor**: Has extra `GetPanelAt` method (useful but not in S&box)

### Panel.Event.cs
- **S&box**: Full reflection-based `InitializeEvents()` with `PanelEventAttribute` - ✅ IMPLEMENTED
- **S&box**: Handles `CopyEvent`, `CutEvent`, `PasteEvent` events - ✅ IMPLEMENTED
- **S&box**: Handles `DragEvent` via `InternalDragEvent` - ✅ IMPLEMENTED

### Panel.Layout.cs
- **S&box**: Has `OnVisibilityChanged()` virtual callback - ✅ IMPLEMENTED
- **S&box**: Has `backgroundRenderDirty` tracking - N/A (Fazor uses different renderer)
- **S&box**: Has detailed `HasBackdropFilter` and `HasFilter` calculations - N/A
- **S&box**: Uses `CalcVisible()` from ComputedStyle - ✅ IMPLEMENTED (HasActiveTransitions check)

### Label.cs
- **S&box**: Uses internal `TextBlock` class for rich text rendering - ⏳ TextBlock not yet ported
- **S&box**: Has `SelectionStart`, `SelectionEnd`, `ShouldDrawSelection` properties - ⏳ Not yet
- **S&box**: Has `GetSelectedText()`, `HasSelection()` methods - ⏳ Not yet
- **S&box**: Has `SelectionColor` property - ⏳ Not yet
- **S&box**: Supports HTML parsing with style lookup
- **S&box**: Has rich hover interaction for links
- **Fazor**: Uses delegate-based text measurement (works well)
- **Fazor**: Has reflection-based wrapper access

### RootPanel.cs
- **S&box**: Has VR support (`IsVR`, `IsHighQualityVR`) - N/A for desktop app
- **S&box**: Has `Render()` method using renderer - Fazor has different architecture
- **S&box**: Has parallel style rule building - Fazor has single-threaded version
- **S&box**: Has event attributes for transitions/language
- **Fazor**: Has DPI scale support (SystemDpiScale) - Fazor addition
- **Fazor**: Has button event interceptor for inspector - Fazor addition
- **Fazor**: Has cursor retrieval method - Fazor addition

### Styles.cs
- **S&box**: Uses `StyleParser.GetPropertyFromAlias` for property aliases
- **S&box**: Logs errors for invalid properties
- **Fazor**: Silently ignores invalid properties
- **Fazor**: Has `BackgroundGradient` property
- **Fazor**: Has `GetCustomProperty` method

## 3. Extra Files in Fazor (49 files)

These are files that exist in Fazor but not in S&box's UI system. Many are because:
- S&box has these in other locations (e.g., System namespace)
- Fazor is standalone and needs types S&box gets from game engine
- Different architectural choices

### Necessary Additions
- `Types/*.cs` - Vector2, Color, Rect, etc. (S&box has these in engine)
- `Utility/*.cs` - Various helper utilities
- `Html/*.cs` - HTML parsing (may be elsewhere in S&box)
- `INativeWindow.cs`, `IPopupService.cs` - Platform abstractions
- `Panel/Box.cs` - Box class (in S&box this is in Panel.Layout.cs)

### Fazor-Specific
- `Attributes/*.cs` - Compatibility attributes
- `Reflection/PanelFactory.cs` - Panel reflection factory
- `Razor/PanelRenderTreeBuilder.cs` - Custom render tree builder

## 4. Recommendations (Priority Order)

### HIGH Priority
1. **Add Panel.Drag.cs** - Drag scrolling is core functionality
2. **Add Panel.Data.cs** - StringValue needed for form controls
3. **Add DragEvent.cs** - Required for drag functionality
4. **Implement InitializeEvents() with PanelEventAttribute** - Reflection-based events
5. **Fix Panel.Input.cs IsInside()** - Add border-radius hit testing

### MEDIUM Priority
6. **Add Panel.Tooltip.cs** - Common UI feature
7. **Add CutCopyPasteEvent.cs** - Clipboard operations
8. **Add Clipboard.cs** - Clipboard access
9. **Fix Label selection** - Add ShouldDrawSelection, SelectionStart, SelectionEnd
10. **Fix MousePosition** - Add GlobalMatrix transform

### LOW Priority
11. **Add VirtualLayouts** - Performance optimization for long lists
12. **Add PanelCreator** - Convenience method
13. **Add Panel.Layer.cs** - Advanced filter/mask rendering

## 5. Method Comparison Details

### Missing Methods in Panel.cs
- `Invoke(float seconds, Action action)` - Delayed invoke
- `InvokeOnce(string name, float seconds, Action action)` - Named delayed invoke
- `CancelInvoke(string name)` - Cancel named invoke
- `DirtyStylesWithStyle(Styles, bool)` - Style dirty propagation
- `PlaySound(string sound)` - Sound playback

### Missing Properties in Panel.cs
- `Task` - TaskSource for async/await
- `Scene` - Scene reference
- `GameObject` - GameObject reference
- `IsValid` (property vs method)

### Missing in Label.cs
- `ShouldDrawSelection` - Draw selection highlight
- `SelectionStart/SelectionEnd` - Selection range
- `SelectionColor` - Selection highlight color
- `GetSelectedText()` - Get selected text
- `HasSelection()` - Check if selection exists
- `GetLetterAtScreenPosition()` - Get letter at screen pos
- `GetCaretRect()` - Get caret rectangle
- `hoveredNode` - Hovered HTML node for rich text

## 6. Implementation Differences Fixed in This PR

### StyleParser.cs
- **S&box**: Uses `GetPropertyFromAlias()` for CSS property aliasing - ✅ IMPLEMENTED
- **Aliases**: `color -> font-color`, `background-image-tint -> background-tint`

### Styles.Set.cs  
- **S&box**: Uses `StyleParser.GetPropertyFromAlias(property)` at start - ✅ IMPLEMENTED
- **Fazor**: Was using hardcoded `if (property == "color")` check - ✅ FIXED

### Panel.Layout.cs UpdateVisibility
- **S&box**: Uses `ComputedStyle?.CalcVisible() ?? false` - ✅ FIXED
- **Fazor**: Was manually checking `Display` and `Opacity`

### TextBlockWrapper (Fazor) vs TextBlock (S&box)

Fazor's TextBlockWrapper has:
- ✅ Basic text measurement
- ✅ Caret positioning (GetCaretRect)
- ✅ Hit testing (HitTest)
- ✅ Selection rendering
- ✅ Font smoothing options

Missing features from S&box's TextBlock:
- ⏳ HTML text parsing (`SetHtml`)
- ⏳ Text gradients (linear/radial)
- ⏳ Text effects/shadows
- ⏳ Text decoration (underline, strikethrough)
- ⏳ Letter spacing and word spacing
- ⏳ Line height
- ⏳ Text transform (uppercase/lowercase)
