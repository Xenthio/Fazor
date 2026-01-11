# S&box vs Fazor UI Deep Comparison Report

## Executive Summary

This report documents the differences between Fazor's Sandbox.UI and S&box's UI system (from `engine/Sandbox.Engine/Systems/UI`). The analysis covers:
- Missing files/features
- Implementation differences in existing files
- Extra files in Fazor (not in S&box)

## 1. Missing Files in Fazor (29 files)

### Critical Missing Features

| File | Description | Priority |
|------|-------------|----------|
| `Panel/Panel.Drag.cs` | Drag scrolling support | HIGH |
| `Panel/Panel.Data.cs` | StringValue property, CreateValueEvent | HIGH |
| `Panel/Panel.Tooltip.cs` | Tooltip system | MEDIUM |
| `Panel/Panel.Layer.cs` | Layer rendering for filters/masks | MEDIUM |
| `Engine/TextBlock.cs` | Rich text rendering with RichTextKit | HIGH |
| `Panel/Event/DragEvent.cs` | Drag event class | HIGH |
| `Panel/Event/CutCopyPasteEvent.cs` | Copy/Cut/Paste events | MEDIUM |
| `Panel/Event/PanelEventAttribute.cs` | Attribute-based event listeners | MEDIUM |

### VirtualLayouts (Performance Optimization)

| File | Description | Priority |
|------|-------------|----------|
| `VirtualLayouts/BaseVirtualPanel.cs` | Base virtualized scrolling panel | LOW |
| `VirtualLayouts/VirtualList.cs` | Virtualized list | LOW |
| `VirtualLayouts/VirtualGrid.cs` | Virtualized grid | LOW |
| `VirtualLayouts/LayoutUtility/VerticalLayout.cs` | Vertical layout helper | LOW |
| `VirtualLayouts/LayoutUtility/GridLayout.cs` | Grid layout helper | LOW |

### Utility/Support Files

| File | Description | Priority |
|------|-------------|----------|
| `Utility/PanelCreator.cs` | Panel.Add helper | LOW |
| `Utility/Clipboard.cs` | Clipboard access | MEDIUM |
| `Utility/Emoji.cs` | Emoji support | LOW |
| `Engine/SkiaCompat.cs` | Skia compatibility helpers | LOW |
| `Razor/RouteAttribute.cs` | Route attribute for pages | LOW |

## 2. Key Implementation Differences

### Panel.cs
- **S&box**: Has `Task` property for async, `Invoke/InvokeOnce/CancelInvoke` methods
- **S&box**: Has `Scene` and `GameObject` properties for game integration
- **S&box**: Has `PlaySound` method
- **S&box**: Has `IsValid` property (Fazor has `IsValid()` method)
- **Fazor**: Missing `InitializeEvents()` call in constructor
- **Fazor**: Missing `AddToLists()`/`RemoveFromLists()` for event registration

### Panel.Children.cs
- **S&box**: Has `PanelCreator Add` property for quick child creation
- **S&box**: Throws exception in RemoveChild if child not found
- **S&box**: Has UnsignedMod for looping in GetChild

### Panel.Input.cs
- **S&box**: `MousePosition` uses GlobalMatrix transform
- **S&box**: `IsInside` includes border-radius hit testing
- **S&box**: Has `RayToLocalPosition` for world panel input
- **S&box**: `GetClipboardValue` supports `AllowChildSelection`
- **Fazor**: Has extra `GetPanelAt` method (useful but not in S&box)

### Panel.Event.cs
- **S&box**: Full reflection-based `InitializeEvents()` with `PanelEventAttribute`
- **S&box**: Handles `CopyEvent`, `CutEvent`, `PasteEvent` events
- **S&box**: Handles `DragEvent` via `InternalDragEvent`
- **Fazor**: Has placeholder `InitializeEvents()` - TODO comment

### Panel.Layout.cs
- **S&box**: Has `OnVisibilityChanged()` virtual callback
- **S&box**: Has `backgroundRenderDirty` tracking
- **S&box**: Has detailed `HasBackdropFilter` and `HasFilter` calculations
- **S&box**: Uses `CalcVisible()` from ComputedStyle
- **Fazor**: Different visibility calculation

### Label.cs
- **S&box**: Uses internal `TextBlock` class for rich text rendering
- **S&box**: Has `SelectionStart`, `SelectionEnd`, `ShouldDrawSelection` properties
- **S&box**: Has `GetSelectedText()`, `HasSelection()` methods
- **S&box**: Has `SelectionColor` property
- **S&box**: Supports HTML parsing with style lookup
- **S&box**: Has rich hover interaction for links
- **Fazor**: Uses delegate-based text measurement
- **Fazor**: Has reflection-based wrapper access (less type-safe)

### RootPanel.cs
- **S&box**: Has VR support (`IsVR`, `IsHighQualityVR`)
- **S&box**: Has `Render()` method using renderer
- **S&box**: Has parallel style rule building
- **S&box**: Has event attributes for transitions/language
- **Fazor**: Has DPI scale support (SystemDpiScale)
- **Fazor**: Has button event interceptor for inspector
- **Fazor**: Has cursor retrieval method

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
