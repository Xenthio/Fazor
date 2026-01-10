# Sandbox.UI.AI - AI Debugging Renderer

A rendering backend for Sandbox.UI/Fazor designed to help AI agents (like GitHub Copilot, Claude, etc.) understand, debug, and interact with the UI system.

## Features

- **Structured Text Output**: Outputs the UI hierarchy as a readable text representation including:
  - Panel hierarchy and structure
  - Layout information (positions, sizes, margins, padding, borders)
  - Computed styles (colors, fonts, flex properties)
  - Content (text labels, images)
  - Interactive elements (buttons, inputs, checkboxes, etc.)

- **Bitmap Rendering**: Can render the UI to a PNG file for visual inspection by multimodal AI agents

- **Hit Testing**: Find what panel is at any screen coordinate

- **Interactive Element Discovery**: Get a list of all clickable/interactive elements for UI automation

## Quick Start

```csharp
using Sandbox.UI;
using Sandbox.UI.AI;

// Create your UI
var rootPanel = new RootPanel();
rootPanel.PanelBounds = new Rect(0, 0, 800, 600);

var button = new Button("Click Me");
rootPanel.AddChild(button);
rootPanel.Layout();

// Get a text snapshot of the UI state
Console.WriteLine(AIHelper.Snapshot(rootPanel));

// Get interactive elements summary
Console.WriteLine(AIHelper.GetInteractiveElements(rootPanel));

// Find what's at a coordinate
Console.WriteLine(AIHelper.WhatIsAt(rootPanel, 100, 100));

// Take a screenshot
var imagePath = AIHelper.Screenshot(rootPanel, "my-ui.png");
Console.WriteLine($"Screenshot saved to: {imagePath}");
```

## Output Examples

### Panel Tree Output
```
=== UI STATE SNAPSHOT ===
Viewport: 800x600
Scale: 1
Timestamp: 2024-01-15T10:30:00.000Z

=== PANEL TREE ===
<rootpanel>
  Layout: x=0 y=0 w=800 h=600
  <button class="button">
    Layout: x=10 y=10 w=100 h=40
    Styles: bg: rgba(200,200,200,1.00); font-size: 14px
    Interactive: [button]
    <label class="label">
      Layout: x=15 y=15 w=90 h=20
      Text: "Click Me"
```

### Interactive Elements Output
```
=== INTERACTIVE ELEMENTS ===
(Elements that can be clicked, typed into, or otherwise interacted with)

[1] BUTTON: <button.submit-btn>
    Location: (100, 200) Size: 120x40
    Details: Submit

[2] TEXT INPUT: <textentry.username>
    Location: (100, 100) Size: 200x30
    Details: value=""

[3] CHECKBOX: <checkbox.agree>
    Location: (100, 150) Size: 20x20
    Details: unchecked
```

## API Reference

### AIHelper (Static Helper Class)

| Method | Description |
|--------|-------------|
| `Snapshot(RootPanel)` | Get full UI state as structured text |
| `SnapshotStructure(RootPanel)` | Get structure-only snapshot (no styles/layout) |
| `GetInteractiveElements(RootPanel)` | List all interactive elements |
| `WhatIsAt(RootPanel, x, y)` | Describe the panel at coordinates |
| `Screenshot(RootPanel, filename?)` | Save UI to PNG file |
| `DescribeClick(RootPanel, x, y)` | Describe what clicking at coordinates would do |
| `FindByClass(RootPanel, className)` | Find panels with a CSS class |
| `FindByElement(RootPanel, elementName)` | Find panels by element type |
| `FindById(RootPanel, id)` | Find a panel by its ID |
| `QuickSummary(RootPanel)` | Get a compact overview |
| `PrintSnapshot(RootPanel)` | Print snapshot to console |
| `PrintInteractive(RootPanel)` | Print interactive elements to console |

### AIPanelRenderer

The underlying renderer implementing `IPanelRenderer`. Use `AIHelper` for most cases, or use this directly for more control:

```csharp
var renderer = new AIPanelRenderer();

// Configure output
renderer.IncludeStyles = true;     // Include computed styles
renderer.IncludeLayout = true;     // Include layout/box info
renderer.IncludeContent = true;    // Include text/image content
renderer.IncludeInteractive = true; // Include interactive element info
renderer.MaxDepth = 10;            // Limit tree depth

// Render and get output
renderer.Render(rootPanel);
string output = renderer.LastOutput;

// Render to bitmap
renderer.RenderToBitmap(rootPanel, "output.png");

// Hit testing
Panel? panel = renderer.HitTest(rootPanel, 100, 100);
```

## Use Cases

### 1. Debugging Layout Issues
```csharp
// When a panel isn't appearing where expected
Console.WriteLine(AIHelper.Snapshot(rootPanel));
// Check Layout: lines for position/size info
```

### 2. Understanding UI Structure
```csharp
// Get a quick overview of what's in the UI
Console.WriteLine(AIHelper.QuickSummary(rootPanel));
```

### 3. Automated Testing
```csharp
// Find a button and verify it exists
var buttons = AIHelper.FindByClass(rootPanel, "submit-button");
Assert.NotEmpty(buttons);
Assert.Contains("Submit", buttons[0].Text);
```

### 4. AI Agent Interaction
```csharp
// For AI agents to understand what they can interact with
var interactive = AIHelper.GetInteractiveElements(rootPanel);
// AI can parse this to find clickable elements

// For visual verification (multimodal AI)
var imagePath = AIHelper.Screenshot(rootPanel);
// AI can analyze the screenshot to verify visual state
```

## Notes

- Screenshots are saved to a temp directory by default (`/tmp/fazor-ai-debug/` or equivalent)
- PNG files are git-ignored by default (see root .gitignore)
- Text measurement uses estimation - for pixel-perfect layout, use the Skia renderer
- The bitmap renderer uses `SkiaPanelRenderer` internally, so screenshots are high-fidelity and match the main UI renderer
