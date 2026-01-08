# FakeOS App - XGUI Compatibility Demo

This example demonstrates the S&box/XGUI compatibility layer for Fazor/Avalazor. The goal is to run s&box XGUI code (like FakeOS from xgui-3_test) with minimal modifications.

## Current Progress

The `Sandbox.Compatibility` module provides substantial infrastructure:

### S&box API Stubs
- `Component`, `PanelComponent`, `GameObjectSystem`, `Scene`, `GameObject` - ECS lifecycle
- `BaseFileSystem`, `FileSystem` - File system abstraction with Data/Mounted mounts
- `Log`, `TypeLibrary`, `Input`, `Mouse`, `Screen` - Utility classes
- `Sound`, `SoundFile` - Audio stubs
- `Game`, `TimeSince`, `RealTimeSince` - Global state and time
- `Color32` - 32-bit color struct
- `Package`, `PackageResult` - Package system stubs
- `Services.Stats`, `Services.Achievements` - Services stubs
- `Steamworks` - Basic Steam API stubs

### XGUI Controls
- `Window`, `TitleBar` - Full XGUI window system
- `ListView` with `ListViewItem` - Multi-view list control with drag support
- `FileBrowserView` - File browser view with virtual methods
- `TreeView`, `TreeViewNode` - Tree structure control
- `ContextMenu`, `ComboBox`, `CheckBox`, `RadioButton` - Form controls
- `ScrollPanel`, `GroupBox`, `Backdrop` - Container panels
- `SelectList`, `ListOption` - Selection list
- `SliderScale`, `ColorPickerControl` - Value controls
- `XGUIIconPanel`, `XGUIIconSystem` - Theme-aware icons
- `XGUIThemeLoader` - Path remapping for themes

### Build Infrastructure
- Namespace shims for `Sandbox.UI.Construct`
- `LayoutBoxInset` panel type

## Remaining Work for Full xgui-3_test Compatibility

### 1. Window Class Conflict
The generated Razor code has ambiguous references between `Sandbox.UI.Window` (native desktop windows) and `XGUI.Window` (in-panel windows). Options:
- Rename one of the Window classes
- Use explicit namespace qualification in all razor files
- Modify the Razor code generator to use global using aliases

### 2. Nested Types Need Moving
Some XGUI types are nested classes that need to be accessed properly:
- `TreeView.TreeViewNode` - Move TreeViewNode to top-level in XGUI namespace
- `ListView.ListViewItem` - Already implemented

### 3. Missing Virtual Methods
Some overrides in VirtualFileBrowserView reference methods that don't exist in base:
- Make `FileBrowserView` methods virtual (partially done)

### 4. Namespace Self-References
The XGUI-3 code uses `using FakeOperatingSystem.X` from within the FakeOperatingSystem namespace, which should work but requires all sub-namespaces to be properly defined.

### 5. Additional Missing Types
- `RenderFragment` - Need `@using Microsoft.AspNetCore.Components` in all razor files
- `Parameter` attribute - Same issue

## Running

```bash
cd examples/FakeOSApp
dotnet build  # Will show remaining errors
```

## Theme Loading

```csharp
// Current XGUI paths work as-is
XGUIThemeLoader.ResolveThemePath("/XGUI/DefaultStyles/Computer95.scss");

// For future restructuring
XGUIThemeLoader.AddDirectoryRemapping("/XGUI/DefaultStyles", "/themes/xgui");
```
