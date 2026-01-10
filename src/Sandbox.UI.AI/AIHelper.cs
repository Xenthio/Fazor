using System.Text;
using SkiaSharp;

namespace Sandbox.UI.AI;

/// <summary>
/// Simplified helper class for AI agents to interact with and debug UI.
/// Provides high-level methods for common operations.
/// </summary>
public static class AIHelper
{
    private static AIPanelRenderer? _renderer;
    private static string _lastBitmapPath = "";

    /// <summary>
    /// Default output directory for bitmap snapshots
    /// </summary>
    public static string OutputDirectory { get; set; } = Path.Combine(Path.GetTempPath(), "fazor-ai-debug");

    /// <summary>
    /// Get or create the shared renderer instance
    /// </summary>
    public static AIPanelRenderer Renderer
    {
        get
        {
            _renderer ??= new AIPanelRenderer();
            return _renderer;
        }
    }

    /// <summary>
    /// Capture a full snapshot of the UI state as text.
    /// This is the primary method for AI agents to understand the current UI state.
    /// </summary>
    public static string Snapshot(RootPanel panel)
    {
        Renderer.Render(panel);
        return Renderer.LastOutput;
    }

    /// <summary>
    /// Capture a simplified snapshot focusing on structure only (no styles or layout details)
    /// </summary>
    public static string SnapshotStructure(RootPanel panel)
    {
        var renderer = Renderer;
        var prevStyles = renderer.IncludeStyles;
        var prevLayout = renderer.IncludeLayout;

        renderer.IncludeStyles = false;
        renderer.IncludeLayout = false;

        renderer.Render(panel);
        var result = renderer.LastOutput;

        renderer.IncludeStyles = prevStyles;
        renderer.IncludeLayout = prevLayout;

        return result;
    }

    /// <summary>
    /// Get a list of all interactive elements that can be clicked or interacted with
    /// </summary>
    public static string GetInteractiveElements(RootPanel panel)
    {
        return Renderer.GetInteractiveElementsSummary(panel);
    }

    /// <summary>
    /// Find what panel is at a specific screen coordinate
    /// </summary>
    public static string WhatIsAt(RootPanel panel, float x, float y)
    {
        return Renderer.GetPanelInfoAt(panel, x, y);
    }

    /// <summary>
    /// Take a visual screenshot and save it to the output directory.
    /// Returns the path to the saved image.
    /// </summary>
    public static string Screenshot(RootPanel panel, string? filename = null)
    {
        EnsureOutputDirectory();

        filename ??= $"ui-snapshot-{DateTime.UtcNow:yyyyMMdd-HHmmss}.png";
        var path = Path.Combine(OutputDirectory, filename);

        Renderer.RenderToBitmap(panel, path);
        _lastBitmapPath = path;

        return path;
    }

    /// <summary>
    /// Get the path to the last screenshot taken
    /// </summary>
    public static string LastScreenshotPath => _lastBitmapPath;

    /// <summary>
    /// Simulate a click at the given coordinates and return info about what was clicked
    /// </summary>
    public static string DescribeClick(RootPanel panel, float x, float y)
    {
        var targetPanel = Renderer.HitTest(panel, x, y);
        if (targetPanel == null)
        {
            return $"Click at ({x}, {y}) - no panel found";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Click at ({x}, {y}):");
        sb.AppendLine($"  Target: <{targetPanel.ElementName}>");

        if (!string.IsNullOrEmpty(targetPanel.Id))
            sb.AppendLine($"  ID: {targetPanel.Id}");

        var classes = targetPanel.Class.ToArray();
        if (classes.Length > 0)
            sb.AppendLine($"  Classes: {string.Join(", ", classes)}");

        // Describe the type
        var typeName = targetPanel.GetType().Name;
        sb.AppendLine($"  Type: {typeName}");

        // Special handling for known controls
        if (targetPanel is Button)
        {
            sb.AppendLine($"  Action: This is a button - clicking will trigger its action");
            sb.AppendLine($"  Text: \"{GetButtonText(targetPanel)}\"");
        }
        else if (targetPanel is TextEntry textEntry)
        {
            sb.AppendLine($"  Action: This is a text input - clicking will focus it for text entry");
            sb.AppendLine($"  Current value: \"{textEntry.Text ?? ""}\"");
        }
        else if (targetPanel is CheckBox checkBox)
        {
            sb.AppendLine($"  Action: This is a checkbox - clicking will toggle it");
            sb.AppendLine($"  Current state: {(checkBox.Checked ? "checked" : "unchecked")}");
        }
        else if (targetPanel is Slider slider)
        {
            sb.AppendLine($"  Action: This is a slider - clicking/dragging will change its value");
            sb.AppendLine($"  Current value: {slider.Value:F2}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Find panels by their class name
    /// </summary>
    public static List<PanelInfo> FindByClass(RootPanel panel, string className)
    {
        var results = new List<PanelInfo>();
        FindByClassRecursive(panel, className, results);
        return results;
    }

    /// <summary>
    /// Find panels by their element name (tag)
    /// </summary>
    public static List<PanelInfo> FindByElement(RootPanel panel, string elementName)
    {
        var results = new List<PanelInfo>();
        FindByElementRecursive(panel, elementName, results);
        return results;
    }

    /// <summary>
    /// Find panels by their ID
    /// </summary>
    public static PanelInfo? FindById(RootPanel panel, string id)
    {
        var found = FindByIdRecursive(panel, id);
        if (found == null) return null;
        return new PanelInfo(found);
    }

    /// <summary>
    /// Get a compact summary of the UI suitable for quick overview
    /// </summary>
    public static string QuickSummary(RootPanel panel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== QUICK UI SUMMARY ===");
        sb.AppendLine($"Viewport: {panel.PanelBounds.Width}x{panel.PanelBounds.Height}");

        // Count panels by type
        var counts = new Dictionary<string, int>();
        CountPanelTypes(panel, counts);

        sb.AppendLine($"Total panels: {counts.Values.Sum()}");
        sb.AppendLine("Panel types:");
        foreach (var kvp in counts.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }

        // Count interactive elements
        int interactive = 0;
        CountInteractive(panel, ref interactive);
        sb.AppendLine($"Interactive elements: {interactive}");

        return sb.ToString();
    }

    /// <summary>
    /// Print the UI state to console (convenience method)
    /// </summary>
    public static void PrintSnapshot(RootPanel panel)
    {
        Console.WriteLine(Snapshot(panel));
    }

    /// <summary>
    /// Print interactive elements to console
    /// </summary>
    public static void PrintInteractive(RootPanel panel)
    {
        Console.WriteLine(GetInteractiveElements(panel));
    }

    // --- Private helpers ---

    private static void EnsureOutputDirectory()
    {
        if (!Directory.Exists(OutputDirectory))
        {
            Directory.CreateDirectory(OutputDirectory);
        }
    }

    private static string GetButtonText(Panel button)
    {
        if (button is Label label) return label.Text ?? "";

        // First, look for labels with common button text classes
        var buttonLabel = FindLabelWithClass(button, "button-text") 
                       ?? FindLabelWithClass(button, "button-label")
                       ?? FindLabelWithClass(button, "text");
        
        if (buttonLabel != null && !string.IsNullOrEmpty(buttonLabel.Text))
            return buttonLabel.Text;

        // Fall back to first visible label with non-single-character text
        // (to avoid icon characters like "a" for checkmarks)
        foreach (var child in button.Children)
        {
            if (child is Label childLabel && !string.IsNullOrEmpty(childLabel.Text) && childLabel.Text.Length > 1)
                return childLabel.Text;

            var text = GetButtonText(child);
            if (!string.IsNullOrEmpty(text) && text.Length > 1)
                return text;
        }

        return "";
    }
    
    private static Label? FindLabelWithClass(Panel panel, string className)
    {
        if (panel is Label label && panel.HasClass(className))
            return label;
            
        foreach (var child in panel.Children)
        {
            var found = FindLabelWithClass(child, className);
            if (found != null)
                return found;
        }
        
        return null;
    }

    private static void FindByClassRecursive(Panel panel, string className, List<PanelInfo> results)
    {
        if (panel.HasClass(className))
        {
            results.Add(new PanelInfo(panel));
        }

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                FindByClassRecursive(child, className, results);
            }
        }
    }

    private static void FindByElementRecursive(Panel panel, string elementName, List<PanelInfo> results)
    {
        if (panel.ElementName.Equals(elementName, StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new PanelInfo(panel));
        }

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                FindByElementRecursive(child, elementName, results);
            }
        }
    }

    private static Panel? FindByIdRecursive(Panel panel, string id)
    {
        if (panel.Id == id) return panel;

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                var found = FindByIdRecursive(child, id);
                if (found != null) return found;
            }
        }

        return null;
    }

    private static void CountPanelTypes(Panel panel, Dictionary<string, int> counts)
    {
        var typeName = panel.GetType().Name;
        counts.TryGetValue(typeName, out var count);
        counts[typeName] = count + 1;

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                CountPanelTypes(child, counts);
            }
        }
    }

    private static void CountInteractive(Panel panel, ref int count)
    {
        if (panel is Button or TextEntry or CheckBox or Slider or ComboBox)
        {
            count++;
        }
        else if (panel.ComputedStyle?.PointerEvents == PointerEvents.All && panel is not RootPanel)
        {
            count++;
        }

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                CountInteractive(child, ref count);
            }
        }
    }
}

/// <summary>
/// Simplified panel information for AI consumption
/// </summary>
public class PanelInfo
{
    public string ElementName { get; }
    public string? Id { get; }
    public string[] Classes { get; }
    public string TypeName { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public string? Text { get; }
    public bool IsVisible { get; }

    /// <summary>
    /// Reference to the underlying Panel object
    /// </summary>
    public Panel Panel { get; }

    public PanelInfo(Panel panel)
    {
        Panel = panel;
        ElementName = panel.ElementName;
        Id = panel.Id;
        Classes = panel.Class.ToArray();
        TypeName = panel.GetType().Name;
        IsVisible = panel.IsVisible;

        var rect = panel.Box?.Rect ?? Rect.Zero;
        X = rect.Left;
        Y = rect.Top;
        Width = rect.Width;
        Height = rect.Height;

        if (panel is Label label)
        {
            Text = label.Text;
        }
    }

    public override string ToString()
    {
        var classStr = Classes.Length > 0 ? $".{string.Join(".", Classes)}" : "";
        var idStr = !string.IsNullOrEmpty(Id) ? $"#{Id}" : "";
        return $"<{ElementName}{idStr}{classStr}> at ({X:F0},{Y:F0}) size {Width:F0}x{Height:F0}";
    }
}
