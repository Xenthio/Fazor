# FakeOS App - XGUI Compatibility Demo

This example demonstrates the S&box/XGUI compatibility layer for Fazor/Avalazor. It provides infrastructure for running s&box XGUI code with the compatibility module.

## Current Status

The compatibility module (`Sandbox.Compatibility`) provides foundational infrastructure:

1. **S&box API stubs**: `Component`, `GameObjectSystem`, `Scene`, `GameObject`, `FileSystem`, etc.
2. **XGUI classes**: `XGUISystem`, `XGUIPanel`, `XGUIRootPanel`, `Window`, `TitleBar`
3. **Theme loading wrapper**: `XGUIThemeLoader` for future-proof theme restructuring
4. **Basic controls**: `ListView`, `ContextMenu`, `Toolbar`, `FileBrowserView` (stubs)

## Running the Example

```bash
cd examples/FakeOSApp
dotnet run
```

This runs a test window that demonstrates the basic XGUI Window class is functional.

## Additional Work Required for Full XGUI-3 Support

Running the complete FakeOS from `xgui-3_test` requires additional work:

### Missing Types in Compatibility Layer
- `XGUI.ListView.ListViewItem` - nested class for list items
- `XGUI.FileBrowserView` with full implementation
- `XGUI.FileBrowserTree` with full implementation
- `XGUI.WebPanel` - web rendering panel
- `Sandbox.Color32` - color type
- `Sandbox.Package` - s&box package system
- `Sandbox.Services.Steamworks` - Steam integration

### Namespace Changes Required in XGUI-3 Code
The original code uses s&box's namespace conventions that need adjustment:
- `Sandbox.FakeOperatingSystem.*` → `FakeOperatingSystem.*`
- `Sandbox.FakeSteam` → `FakeSteam`
- `Sandbox.UI.Construct` attribute (not implemented)

### Window Class Conflict
Both `Sandbox.UI.Window` (for native desktop windows) and `XGUI.Window` (for in-panel windows) exist. The XGUI-3 code expects `XGUI.Window`. The generated Razor code needs proper namespace resolution.

## Theme Loading

The `XGUIThemeLoader` supports path remapping for future theme restructuring:

```csharp
// Current paths work as-is
XGUIThemeLoader.ResolveThemePath("/XGUI/DefaultStyles/Computer95.scss");

// For future restructuring, add remappings
XGUIThemeLoader.AddDirectoryRemapping("/XGUI/DefaultStyles", "/themes/xgui");
```

## Structure

- `Program.cs` - Application entry point with XGUI setup demonstration
- `Code/` - Place XGUI/FakeOS code here (requires adaptation for compatibility)
- `Assets/` - Place theme, font, and image assets here
