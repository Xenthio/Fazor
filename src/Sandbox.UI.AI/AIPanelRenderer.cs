using System.Text;
using SkiaSharp;
using Sandbox.UI;
using Fazor.Controls;
using Sandbox.UI.Skia;

namespace Sandbox.UI.AI;

/// <summary>
/// AI-focused panel renderer that outputs structured text representations of UI state.
/// Designed to help AI agents understand, debug, and interact with the UI system.
/// 
/// Features:
/// - Structured text output showing panel hierarchy, layout, styles, and content
/// - Optional bitmap rendering for visual inspection (multimodal AI support)
/// - Accessibility-tree-like output for understanding interactive elements
/// - Hit-testing support for simulating user interaction
/// </summary>
public class AIPanelRenderer : IPanelRenderer
{
    private readonly StringBuilder _output = new();
    private int _indentLevel = 0;
    private bool _includeStyles = true;
    private bool _includeLayout = true;
    private bool _includeContent = true;
    private bool _includeInteractive = true;
    private int _maxDepth = int.MaxValue;
    private SKBitmap? _bitmap;
    private SKCanvas? _canvas;

    /// <summary>
    /// Whether to include computed style information in the output
    /// </summary>
    public bool IncludeStyles
    {
        get => _includeStyles;
        set => _includeStyles = value;
    }

    /// <summary>
    /// Whether to include layout/box information in the output
    /// </summary>
    public bool IncludeLayout
    {
        get => _includeLayout;
        set => _includeLayout = value;
    }

    /// <summary>
    /// Whether to include content (text, images) in the output
    /// </summary>
    public bool IncludeContent
    {
        get => _includeContent;
        set => _includeContent = value;
    }

    /// <summary>
    /// Whether to include interactive element information (hover, click targets)
    /// </summary>
    public bool IncludeInteractive
    {
        get => _includeInteractive;
        set => _includeInteractive = value;
    }

    /// <summary>
    /// Maximum depth of the panel tree to render (default: unlimited)
    /// </summary>
    public int MaxDepth
    {
        get => _maxDepth;
        set => _maxDepth = value;
    }

    /// <summary>
    /// Current screen/viewport rect
    /// </summary>
    public Rect Screen { get; private set; }

    /// <summary>
    /// The last rendered output as text
    /// </summary>
    public string LastOutput => _output.ToString();

    /// <summary>
    /// The last rendered bitmap (if bitmap rendering was enabled)
    /// </summary>
    public SKBitmap? LastBitmap => _bitmap;

    /// <summary>
    /// Static constructor to ensure text measurement is available
    /// </summary>
    static AIPanelRenderer()
    {
        // Register text measurement function for Label layout calculations
        // Uses a simple estimation since we're focused on structural output
        if (Label.TextMeasureFunc == null)
        {
            Label.TextMeasureFunc = (text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping) =>
            {
                if (string.IsNullOrEmpty(text))
                    return new Vector2(0, fontSize * 1.2f);

                // Simple estimation: average character width is ~0.5 * fontSize
                var width = text.Length * fontSize * 0.5f;
                var height = fontSize * 1.2f;

                if (allowWrapping && !float.IsNaN(maxWidth) && maxWidth > 0 && width > maxWidth)
                {
                    var lines = (int)Math.Ceiling(width / maxWidth);
                    width = maxWidth;
                    height = lines * fontSize * 1.2f;
                }

                return new Vector2(width, height);
            };
        }
    }

    /// <summary>
    /// Ensures the static constructor has run and text measurement is available.
    /// </summary>
    public static bool EnsureInitialized()
    {
        return Label.TextMeasureFunc != null;
    }

    public void RegisterAsActiveRenderer()
    {
        // Text measurement is set up in static constructor
    }

    public Vector2 MeasureText(string text, string? fontFamily, float fontSize, int fontWeight)
    {
        if (string.IsNullOrEmpty(text))
            return new Vector2(0, fontSize * 1.2f);

        var width = text.Length * fontSize * 0.5f;
        var height = fontSize * 1.2f;

        return new Vector2(width, height);
    }

    /// <summary>
    /// Render a root panel and return structured text output
    /// </summary>
    public void Render(RootPanel panel, float opacity = 1.0f)
    {
        _output.Clear();
        _indentLevel = 0;
        Screen = panel.PanelBounds;

        AppendLine("=== UI STATE SNAPSHOT ===");
        AppendLine($"Viewport: {Screen.Width}x{Screen.Height}");
        AppendLine($"Scale: {panel.Scale}");
        AppendLine($"Timestamp: {DateTime.UtcNow:O}");
        AppendLine("");
        AppendLine("=== PANEL TREE ===");

        var state = new RenderState
        {
            X = Screen.Left,
            Y = Screen.Top,
            Width = Screen.Width,
            Height = Screen.Height,
            RenderOpacity = opacity
        };

        Render(panel, state);
    }

    /// <summary>
    /// Render a panel to the structured text output
    /// </summary>
    public void Render(Panel panel, RenderState state)
    {
        if (panel.ComputedStyle == null) return;
        if (!panel.IsVisible)
        {
            AppendLine($"[HIDDEN] <{panel.ElementName}>");
            return;
        }
        if (_indentLevel > _maxDepth) return;

        // Panel header with basic info
        var header = BuildPanelHeader(panel);
        AppendLine(header);

        _indentLevel++;

        // Layout information
        if (_includeLayout)
        {
            RenderLayoutInfo(panel);
        }

        // Style information
        if (_includeStyles)
        {
            RenderStyleInfo(panel);
        }

        // Content information
        if (_includeContent)
        {
            RenderContentInfo(panel, ref state);
        }

        // Interactive information
        if (_includeInteractive)
        {
            RenderInteractiveInfo(panel);
        }

        // Render children
        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                Render(child, state);
            }
        }

        _indentLevel--;
    }

    private string BuildPanelHeader(Panel panel)
    {
        var sb = new StringBuilder();
        sb.Append($"<{panel.ElementName}");

        if (!string.IsNullOrEmpty(panel.Id))
            sb.Append($" id=\"{panel.Id}\"");

        var classes = panel.Class.ToArray();
        if (classes.Length > 0)
            sb.Append($" class=\"{string.Join(" ", classes)}\"");

        sb.Append('>');

        // Add type info if it's a known control
        var typeName = panel.GetType().Name;
        if (typeName != "Panel" && typeName != panel.ElementName)
        {
            sb.Append($" [{typeName}]");
        }

        return sb.ToString();
    }

    private void RenderLayoutInfo(Panel panel)
    {
        var box = panel.Box;
        if (box == null) return;

        AppendLine($"Layout: x={box.Rect.Left:F0} y={box.Rect.Top:F0} w={box.Rect.Width:F0} h={box.Rect.Height:F0}");

        if (box.Margin.Left > 0 || box.Margin.Top > 0 || box.Margin.Right > 0 || box.Margin.Bottom > 0)
        {
            AppendLine($"Margin: {box.Margin.Left:F0} {box.Margin.Top:F0} {box.Margin.Right:F0} {box.Margin.Bottom:F0}");
        }

        if (box.Padding.Left > 0 || box.Padding.Top > 0 || box.Padding.Right > 0 || box.Padding.Bottom > 0)
        {
            AppendLine($"Padding: {box.Padding.Left:F0} {box.Padding.Top:F0} {box.Padding.Right:F0} {box.Padding.Bottom:F0}");
        }

        if (box.Border.Left > 0 || box.Border.Top > 0 || box.Border.Right > 0 || box.Border.Bottom > 0)
        {
            AppendLine($"Border: {box.Border.Left:F0} {box.Border.Top:F0} {box.Border.Right:F0} {box.Border.Bottom:F0}");
        }
    }

    private void RenderStyleInfo(Panel panel)
    {
        var style = panel.ComputedStyle;
        if (style == null) return;

        var styleProps = new List<string>();

        // Background
        if (style.BackgroundColor.HasValue && style.BackgroundColor.Value.a > 0)
        {
            var c = style.BackgroundColor.Value;
            styleProps.Add($"bg: rgba({c.r * 255:F0},{c.g * 255:F0},{c.b * 255:F0},{c.a:F2})");
        }

        // Font
        if (style.FontSize != null)
        {
            styleProps.Add($"font-size: {style.FontSize.Value.Value}{style.FontSize.Value.Unit}");
        }
        if (!string.IsNullOrEmpty(style.FontFamily))
        {
            styleProps.Add($"font-family: {style.FontFamily}");
        }
        if (style.FontColor.HasValue)
        {
            var c = style.FontColor.Value;
            styleProps.Add($"color: rgba({c.r * 255:F0},{c.g * 255:F0},{c.b * 255:F0},{c.a:F2})");
        }

        // Display/Flex
        if (style.Display != null)
        {
            styleProps.Add($"display: {style.Display}");
        }
        if (style.FlexDirection != null)
        {
            styleProps.Add($"flex-direction: {style.FlexDirection}");
        }
        if (style.JustifyContent != null)
        {
            styleProps.Add($"justify-content: {style.JustifyContent}");
        }
        if (style.AlignItems != null)
        {
            styleProps.Add($"align-items: {style.AlignItems}");
        }

        // Opacity
        if (style.Opacity.HasValue && style.Opacity.Value < 1)
        {
            styleProps.Add($"opacity: {style.Opacity.Value:F2}");
        }

        if (styleProps.Count > 0)
        {
            AppendLine($"Styles: {string.Join("; ", styleProps)}");
        }

        // Pseudo classes
        var pseudoClasses = new List<string>();
        if (panel.HasHovered) pseudoClasses.Add(":hover");
        if (panel.HasActive) pseudoClasses.Add(":active");
        if (panel.HasFocus) pseudoClasses.Add(":focus");
        if (panel.HasIntro) pseudoClasses.Add(":intro");
        if (panel.HasOutro) pseudoClasses.Add(":outro");

        if (pseudoClasses.Count > 0)
        {
            AppendLine($"PseudoClasses: {string.Join(" ", pseudoClasses)}");
        }
    }

    private void RenderContentInfo(Panel panel, ref RenderState state)
    {
        if (panel is Label label && !string.IsNullOrEmpty(label.Text))
        {
            var truncatedText = label.Text.Length > 100 ? label.Text.Substring(0, 97) + "..." : label.Text;
            truncatedText = truncatedText.Replace("\n", "\\n").Replace("\r", "");
            AppendLine($"Text: \"{truncatedText}\"");
        }
        else if (panel is Image image && !string.IsNullOrEmpty(image.TexturePath))
        {
            AppendLine($"Image: \"{image.TexturePath}\"");
        }
    }

    private void RenderInteractiveInfo(Panel panel)
    {
        var interactive = new List<string>();

        // Check for pointer events
        var style = panel.ComputedStyle;
        if (style?.PointerEvents == PointerEvents.All)
        {
            interactive.Add("clickable");
        }

        // Check for known interactive controls
        if (panel is Button)
            interactive.Add("button");
        else if (panel is TextEntry)
            interactive.Add("text-input");
        else if (panel is CheckBox)
            interactive.Add("checkbox");
        else if (panel is Slider)
            interactive.Add("slider");
        else if (panel is ComboBox)
            interactive.Add("combobox");

        if (interactive.Count > 0)
        {
            AppendLine($"Interactive: [{string.Join(", ", interactive)}]");
        }
    }

    private void AppendLine(string line)
    {
        var indent = new string(' ', _indentLevel * 2);
        _output.AppendLine($"{indent}{line}");
    }

    /// <summary>
    /// Render the UI to a bitmap for visual inspection using the full Skia renderer.
    /// Returns the path to the saved PNG file.
    /// </summary>
    public string RenderToBitmap(RootPanel panel, string outputPath, int width = 0, int height = 0)
    {
        // Use panel bounds if no size specified
        if (width == 0) width = (int)panel.PanelBounds.Width;
        if (height == 0) height = (int)panel.PanelBounds.Height;

        // Ensure minimum size
        if (width < 1) width = 800;
        if (height < 1) height = 600;

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create bitmap and canvas
        _bitmap?.Dispose();
        _bitmap = new SKBitmap(width, height);
        _canvas = new SKCanvas(_bitmap);

        // Clear with light gray background
        _canvas.Clear(new SKColor(240, 240, 240));

        // Use the full SkiaPanelRenderer for high-quality rendering
        var skiaRenderer = new SkiaPanelRenderer();
        skiaRenderer.Render(_canvas, panel);

        // Save to file
        using var image = SKImage.FromBitmap(_bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return outputPath;
    }

    /// <summary>
    /// Get a simplified accessibility-tree-like representation of interactive elements
    /// </summary>
    public string GetInteractiveElementsSummary(RootPanel panel)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== INTERACTIVE ELEMENTS ===");
        sb.AppendLine("(Elements that can be clicked, typed into, or otherwise interacted with)");
        sb.AppendLine("");

        int index = 1;
        CollectInteractiveElements(panel, sb, ref index);

        return sb.ToString();
    }

    private void CollectInteractiveElements(Panel panel, StringBuilder sb, ref int index)
    {
        if (!panel.IsVisible) return;

        var isInteractive = false;
        var elementType = "";
        var details = new List<string>();

        if (panel is Button button)
        {
            isInteractive = true;
            elementType = "BUTTON";
            details.Add(GetLabelText(button));
        }
        else if (panel is TextEntry textEntry)
        {
            isInteractive = true;
            elementType = "TEXT INPUT";
            details.Add($"value=\"{textEntry.Text ?? ""}\"");
        }
        else if (panel is CheckBox checkBox)
        {
            isInteractive = true;
            elementType = "CHECKBOX";
            details.Add(checkBox.Checked ? "checked" : "unchecked");
        }
        else if (panel is Slider slider)
        {
            isInteractive = true;
            elementType = "SLIDER";
            details.Add($"value={slider.Value:F2}");
        }
        else if (panel is ComboBox comboBox)
        {
            isInteractive = true;
            elementType = "DROPDOWN";
            details.Add($"selected=\"{comboBox.Selected?.Title ?? ""}\"");
        }
        else if (panel.ComputedStyle?.PointerEvents == PointerEvents.All && panel is not RootPanel)
        {
            // Generic clickable element
            isInteractive = true;
            elementType = "CLICKABLE";
        }

        if (isInteractive)
        {
            var rect = panel.Box?.Rect ?? Rect.Zero;
            var classes = panel.Class.ToArray();
            var classStr = classes.Length > 0 ? $".{string.Join(".", classes)}" : "";

            sb.AppendLine($"[{index}] {elementType}: <{panel.ElementName}{classStr}>");
            sb.AppendLine($"    Location: ({rect.Left:F0}, {rect.Top:F0}) Size: {rect.Width:F0}x{rect.Height:F0}");
            if (details.Count > 0)
            {
                sb.AppendLine($"    Details: {string.Join(", ", details)}");
            }
            sb.AppendLine("");
            index++;
        }

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                CollectInteractiveElements(child, sb, ref index);
            }
        }
    }

    private string GetLabelText(Panel panel)
    {
        // Find a label child or text content
        if (panel is Label label) return label.Text ?? "";

        if (panel.HasChildren)
        {
            foreach (var child in panel.Children)
            {
                if (child is Label childLabel && !string.IsNullOrEmpty(childLabel.Text))
                    return childLabel.Text;

                var text = GetLabelText(child);
                if (!string.IsNullOrEmpty(text))
                    return text;
            }
        }

        return "";
    }

    /// <summary>
    /// Find a panel at the given screen coordinates
    /// </summary>
    public Panel? HitTest(RootPanel panel, float x, float y)
    {
        return HitTestRecursive(panel, x, y);
    }

    private Panel? HitTestRecursive(Panel panel, float x, float y)
    {
        if (!panel.IsVisible) return null;

        var rect = panel.Box?.Rect ?? Rect.Zero;
        if (!rect.Contains(x, y)) return null;

        // Check children in reverse order (top-most first)
        if (panel.HasChildren)
        {
            var children = panel.Children.ToArray();
            for (int i = children.Length - 1; i >= 0; i--)
            {
                var hit = HitTestRecursive(children[i], x, y);
                if (hit != null) return hit;
            }
        }

        return panel;
    }

    /// <summary>
    /// Get panel info at specific coordinates (useful for understanding what's at a location)
    /// </summary>
    public string GetPanelInfoAt(RootPanel rootPanel, float x, float y)
    {
        var panel = HitTest(rootPanel, x, y);
        if (panel == null)
            return $"No panel at ({x}, {y})";

        var sb = new StringBuilder();
        sb.AppendLine($"Panel at ({x}, {y}):");
        sb.AppendLine(BuildPanelHeader(panel));

        var rect = panel.Box?.Rect ?? Rect.Zero;
        sb.AppendLine($"  Bounds: ({rect.Left:F0}, {rect.Top:F0}) to ({rect.Right:F0}, {rect.Bottom:F0})");

        if (panel is Label label && !string.IsNullOrEmpty(label.Text))
        {
            sb.AppendLine($"  Text: \"{label.Text}\"");
        }

        // Show path to root
        sb.AppendLine("  Path: " + GetPanelPath(panel));

        return sb.ToString();
    }

    private string GetPanelPath(Panel panel)
    {
        var path = new List<string>();
        var current = panel;
        while (current != null)
        {
            var name = current.ElementName;
            if (!string.IsNullOrEmpty(current.Id))
                name = $"{name}#{current.Id}";
            else if (current.Classes.Any())
                name = $"{name}.{current.Classes.First()}";

            path.Insert(0, name);
            current = current.Parent;
        }
        return string.Join(" > ", path);
    }

    /// <summary>
    /// Print the structured output to console
    /// </summary>
    public void PrintToConsole()
    {
        Console.WriteLine(LastOutput);
    }

    /// <summary>
    /// Dispose of any resources
    /// </summary>
    public void Dispose()
    {
        _bitmap?.Dispose();
        _bitmap = null;
        _canvas = null;
    }
}
