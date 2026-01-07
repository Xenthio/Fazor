# FakeOS App - XGUI Compatibility Demo

This example demonstrates the S&box/XGUI compatibility layer for Fazor/Avalazor. It allows running s&box XGUI code with minimal or no modifications.

## Purpose

The compatibility module (`Sandbox.Compatibility`) provides:

1. **S&box API stubs**: `Component`, `GameObjectSystem`, `Scene`, `GameObject`, `FileSystem`, etc.
2. **XGUI classes**: `XGUISystem`, `XGUIPanel`, `XGUIRootPanel`, `Window`, `TitleBar`
3. **Theme loading wrapper**: `XGUIThemeLoader` for future-proof theme restructuring

## Running the Example

```bash
cd examples/FakeOSApp
dotnet run
```

## Adding XGUI-3 Test Code

To run the actual FakeOS from `xgui-3_test`:

1. Copy the `code/FakeOperatingSystem` folder from [xgui-3_test](https://github.com/Xenthio/xgui-3_test) to `Code/FakeOperatingSystem`
2. Update `Program.cs` to load the FakeOSLoader component instead of TestStartupWindow
3. Copy any required assets to the `Assets` folder

## Theme Loading

The `XGUIThemeLoader` supports path remapping for future theme restructuring:

```csharp
// Current paths work as-is
XGUIThemeLoader.ResolveThemePath("/XGUI/DefaultStyles/Computer95.scss");

// For future restructuring, add remappings
XGUIThemeLoader.AddDirectoryRemapping("/XGUI/DefaultStyles", "/themes/xgui");
```

## Structure

- `Program.cs` - Application entry point
- `Code/` - Place XGUI/FakeOS code here
- `Assets/` - Place theme, font, and image assets here
