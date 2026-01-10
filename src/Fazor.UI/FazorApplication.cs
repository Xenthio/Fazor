using Microsoft.AspNetCore.Components;
using Sandbox.UI;
#if INCLUDE_AI_RENDERER
using Sandbox.UI.AI;
#endif
using Sandbox.UI.Reflection;
using Sandbox.UI.Skia;

namespace Fazor.UI;

/// <summary>
/// Main application entry point for Fazor apps
/// Simplified API similar to s&box's application model
/// </summary>
public static class FazorApplication
{
#if INCLUDE_AI_RENDERER
    /// <summary>
    /// Force AI renderer mode even if a display is available
    /// </summary>
    public static bool ForceAIMode { get; set; } = false;
    
    /// <summary>
    /// Whether to run AI mode in interactive mode with a command prompt.
    /// Default is true. Set to false for non-interactive batch output.
    /// </summary>
    public static bool AIInteractiveMode { get; set; } = true;
    
    /// <summary>
    /// Callback invoked when running in AI mode, allowing custom handling of the root panel.
    /// If not set, default AI output is printed to console.
    /// </summary>
    public static Action<RootPanel>? AIModeCallback { get; set; }
#endif
    
    /// <summary>
    /// The window manager for opening additional native windows.
    /// Shared across the entire application.
    /// </summary>
    private static WindowManager? _windowManager;

    /// <summary>
    /// Static constructor to ensure text measurement is available before any panels are created
    /// </summary>
    static FazorApplication()
    {
        // Touch SkiaPanelRenderer to trigger its static constructor,
        // which registers the text measurement function for Label layout
        _ = SkiaPanelRenderer.EnsureInitialized();
    }
    
    /// <summary>
    /// Check if a display environment is available
    /// </summary>
    public static bool HasDisplayEnvironment()
    {
#if INCLUDE_AI_RENDERER
        if (ForceAIMode)
            return false;
            
        // Check environment variable override
        var aiMode = Environment.GetEnvironmentVariable("FAZOR_AI_MODE");
        if (aiMode == "1" || aiMode?.ToLower() == "true")
            return false;
#endif
            
        // Check for common display environment indicators
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            // Windows typically always has a display
            return true;
        }
        
        // On Linux/macOS, check for DISPLAY or WAYLAND_DISPLAY
        var display = Environment.GetEnvironmentVariable("DISPLAY");
        var waylandDisplay = Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
        
        return !string.IsNullOrEmpty(display) || !string.IsNullOrEmpty(waylandDisplay);
    }

    public static void Run(RootPanel rootPanel, int width = 1280, int height = 720, string title = "Fazor Application")
    {
#if INCLUDE_AI_RENDERER
        if (!HasDisplayEnvironment())
        {
            RunWithAIRenderer(rootPanel, width, height, title);
            return;
        }
#endif
        
        using var window = new NativeWindow(width, height, title);
        window.RootPanel = rootPanel;
        window.Run();
    }

    /// <summary>
    /// Run a Panel-derived Razor component as the root of the application.
    /// This properly processes the Razor render tree to create child panels.
    /// If the panel is a Window, extracts window properties for native window creation.
    /// Automatically detects if a display is available and falls back to AI renderer if not.
    /// </summary>
    public static void RunPanel<T>(int width = 1280, int height = 720, string title = "Fazor Application") where T : Panel, new()
    {
        try
        {
            // Initialize PanelFactory to register all [Library] and [Alias] types
            PanelFactory.Initialize();

            // Create the panel
            var panel = new T();
            
            // Wrap in RootPanel
            var rootPanel = new RootPanel();
            rootPanel.PanelBounds = new Rect(0, 0, width, height);
            rootPanel.AddChild(panel);
            
            // Perform initial layout which will trigger Razor tree processing
            rootPanel.Layout();

            // Re-read window properties AFTER layout (Razor attributes are now processed)
            if (panel is Sandbox.UI.Window windowPanel)
            {
                // Check if width/height were explicitly set
                bool needsAutoWidth = !windowPanel.IsWindowWidthExplicit;
                bool needsAutoHeight = !windowPanel.IsWindowHeightExplicit;
                
                if (needsAutoWidth || needsAutoHeight)
                {
                    // For automatic sizing, we need to temporarily remove the 100% constraint
                    // and let the content determine the size
                    var originalWidthStyle = windowPanel.Style.Width;
                    var originalHeightStyle = windowPanel.Style.Height;
                    
                    if (needsAutoWidth)
                    {
                        windowPanel.Style.Width = null; // Auto width
                    }
                    if (needsAutoHeight)
                    {
                        windowPanel.Style.Height = null; // Auto height
                    }
                    
                    // Re-layout with auto sizing
                    rootPanel.Layout();
                    
                    // Now grab the computed size from the layout
                    int? computedWidth = null;
                    int? computedHeight = null;
                    
                    // Cache MinSize to avoid multiple property accesses
                    var minWidth = (int)windowPanel.MinSize.x;
                    var minHeight = (int)windowPanel.MinSize.y;
                    
                    if (needsAutoWidth && windowPanel.Box != null)
                    {
                        // Use the outer rect width (includes margins and borders)
                        int w = (int)Math.Ceiling(windowPanel.Box.Rect.Width);
                        // Enforce minimum size
                        w = Math.Max(w, minWidth);
                        if (w > 0)
                        {
                            computedWidth = w;
                            width = w;
                        }
                    }
                    if (needsAutoHeight && windowPanel.Box != null)
                    {
                        // Use the outer rect height (includes margins and borders)
                        int h = (int)Math.Ceiling(windowPanel.Box.Rect.Height);
                        // Enforce minimum size
                        h = Math.Max(h, minHeight);
                        if (h > 0)
                        {
                            computedHeight = h;
                            height = h;
                        }
                    }
                    
                    // Set the computed size without marking as explicitly set
                    if (computedWidth.HasValue || computedHeight.HasValue)
                    {
                        windowPanel.SetAutoComputedSize(computedWidth, computedHeight);
                    }
                    
                    // Restore the 100% sizing for native window rendering
                    // (the native window will fill the RootPanel, which is sized to the computed dimensions)
                    windowPanel.Style.Width = originalWidthStyle;
                    windowPanel.Style.Height = originalHeightStyle;
                }
                else
                {
                    // Explicitly set dimensions - use them
                    width = windowPanel.WindowWidth > 0 ? windowPanel.WindowWidth : width;
                    height = windowPanel.WindowHeight > 0 ? windowPanel.WindowHeight : height;
                }
                
                title = !string.IsNullOrEmpty(windowPanel.Title) ? windowPanel.Title : title;
            }
            
            // Check if we have a display environment
#if INCLUDE_AI_RENDERER
            if (!HasDisplayEnvironment())
            {
                Console.WriteLine($"[Fazor] No display environment detected - using AI renderer");
                
                // Mark any top-level Window as a root window so it fills the viewport
                // instead of using its Position/Size as embedded window coordinates
                if (panel is Sandbox.UI.Window win)
                {
                    win.IsRootWindow = true;
                    
                    // Reset the Window styles to fill the viewport (undo any position/size that was applied)
                    win.Style.Position = PositionMode.Absolute;
                    win.Style.Left = 0;
                    win.Style.Top = 0;
                    win.Style.Width = Length.Percent(100);
                    win.Style.Height = Length.Percent(100);
                }
                
                // Re-layout with updated bounds and root window flag
                rootPanel.PanelBounds = new Rect(0, 0, width, height);
                rootPanel.Layout();
                
                RunWithAIRenderer(rootPanel, width, height, title);
                return;
            }
#endif

            Console.WriteLine($"Creating native window from Window properties (after layout): {width}x{height}, Title: '{title}'");
            Console.WriteLine($"Root panel created: {rootPanel != null}");
            Console.WriteLine($"Component type: {panel.GetType().Name}");
            Console.WriteLine($"Root panel children: {rootPanel.ChildrenCount}");
            
            PrintPanelTree(rootPanel, 0);

            // Create and configure native window
            var nativeWindow = new NativeWindow(width, height, title);
            
            // If panel is a Window, give it a reference to the native window
            if (panel is Sandbox.UI.Window winPanel)
            {
                winPanel.SetNativeWindow(nativeWindow);
            }
            
            nativeWindow.RootPanel = rootPanel;
            
            // Initialize window manager for multi-window support (create once globally)
            if (_windowManager == null)
            {
                _windowManager = new WindowManager();
            }
            
            nativeWindow.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RunPanel: {ex}");
            throw;
        }
    }
    
#if INCLUDE_AI_RENDERER
    /// <summary>
    /// Run the application with the AI renderer (for headless/CI environments or AI agent debugging)
    /// </summary>
    private static void RunWithAIRenderer(RootPanel rootPanel, int width, int height, string title)
    {
        // Update bounds
        rootPanel.PanelBounds = new Rect(0, 0, width, height);
        rootPanel.Layout();
        
        // If custom handler is set, use it
        if (AIModeCallback != null)
        {
            AIModeCallback(rootPanel);
            return;
        }
        
        // Default AI mode output
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║             FAZOR AI RENDERER MODE                           ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"Application: {title}");
        Console.WriteLine($"Viewport: {width}x{height}");
        Console.WriteLine($"No display detected - outputting UI state for AI analysis.\n");
        
        // Output the UI state
        Console.WriteLine(AIHelper.Snapshot(rootPanel));
        
        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("INTERACTIVE ELEMENTS");
        Console.WriteLine(new string('─', 60) + "\n");
        Console.WriteLine(AIHelper.GetInteractiveElements(rootPanel));
        
        Console.WriteLine("\n" + new string('─', 60));
        Console.WriteLine("QUICK SUMMARY");
        Console.WriteLine(new string('─', 60) + "\n");
        Console.WriteLine(AIHelper.QuickSummary(rootPanel));
        
        // Try to save a screenshot if SkiaSharp is available
        try
        {
            var screenshotPath = AIHelper.Screenshot(rootPanel);
            Console.WriteLine($"\n[AI] Screenshot saved to: {screenshotPath}");
            Console.WriteLine("[AI] You can view this image to see the visual state of the UI.");
        }
        catch (DllNotFoundException ex)
        {
            // SkiaSharp native library not available in headless environments
            Console.WriteLine($"\n[AI] Note: Screenshot not available (native library not found: {ex.Message})");
        }
        catch (Exception ex)
        {
            // Log other errors for debugging
            Console.WriteLine($"\n[AI] Note: Screenshot failed: {ex.Message}");
        }
        
        // Run interactive prompt if enabled
        if (AIInteractiveMode)
        {
            RunInteractivePrompt(rootPanel, width, height, title);
        }
        else
        {
            Console.WriteLine("\n" + new string('═', 60));
            Console.WriteLine("AI MODE TIPS:");
            Console.WriteLine("- Use AIHelper.Snapshot(rootPanel) to get current UI state");
            Console.WriteLine("- Use AIHelper.WhatIsAt(rootPanel, x, y) to inspect coordinates");
            Console.WriteLine("- Use AIHelper.FindByClass(rootPanel, \"classname\") to find elements");
            Console.WriteLine("- Set FAZOR_AI_MODE=0 or provide a DISPLAY to use GUI mode");
            Console.WriteLine(new string('═', 60));
        }
    }
    
    /// <summary>
    /// Run the interactive AI command prompt
    /// </summary>
    private static void RunInteractivePrompt(RootPanel rootPanel, int width, int height, string title)
    {
        Console.WriteLine("\n" + new string('═', 60));
        Console.WriteLine("INTERACTIVE AI MODE");
        Console.WriteLine(new string('═', 60));
        Console.WriteLine("Commands:");
        Console.WriteLine("  snapshot          - Show full UI state");
        Console.WriteLine("  interactive       - List interactive elements");
        Console.WriteLine("  summary           - Show quick summary");
        Console.WriteLine("  screenshot [file] - Save screenshot to file");
        Console.WriteLine("  click <x> <y>     - Simulate click at coordinates");
        Console.WriteLine("  what <x> <y>      - Describe element at coordinates");
        Console.WriteLine("  find <class>      - Find elements by class name");
        Console.WriteLine("  findid <id>       - Find element by ID");
        Console.WriteLine("  hover <x> <y>     - Simulate hover at coordinates");
        Console.WriteLine("  help              - Show this help");
        Console.WriteLine("  quit / exit       - Exit the application");
        Console.WriteLine(new string('═', 60) + "\n");
        
        while (true)
        {
            Console.Write("ai> ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input))
                continue;
                
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();
            
            try
            {
                switch (command)
                {
                    case "quit":
                    case "exit":
                    case "q":
                        Console.WriteLine("Exiting AI mode.");
                        return;
                        
                    case "help":
                    case "?":
                        Console.WriteLine("Commands:");
                        Console.WriteLine("  snapshot          - Show full UI state");
                        Console.WriteLine("  interactive       - List interactive elements");
                        Console.WriteLine("  summary           - Show quick summary");
                        Console.WriteLine("  screenshot [file] - Save screenshot to file");
                        Console.WriteLine("  click <x> <y>     - Simulate click at coordinates");
                        Console.WriteLine("  what <x> <y>      - Describe element at coordinates");
                        Console.WriteLine("  find <class>      - Find elements by class name");
                        Console.WriteLine("  findid <id>       - Find element by ID");
                        Console.WriteLine("  hover <x> <y>     - Simulate hover at coordinates");
                        Console.WriteLine("  tick              - Run a tick cycle");
                        Console.WriteLine("  layout            - Re-run layout");
                        Console.WriteLine("  quit / exit       - Exit the application");
                        break;
                        
                    case "snapshot":
                    case "snap":
                        Console.WriteLine(AIHelper.Snapshot(rootPanel));
                        break;
                        
                    case "interactive":
                    case "elements":
                    case "int":
                        Console.WriteLine(AIHelper.GetInteractiveElements(rootPanel));
                        break;
                        
                    case "summary":
                    case "sum":
                        Console.WriteLine(AIHelper.QuickSummary(rootPanel));
                        break;
                        
                    case "screenshot":
                    case "ss":
                        string? filename = parts.Length > 1 ? parts[1] : null;
                        var path = AIHelper.Screenshot(rootPanel, filename);
                        Console.WriteLine($"Screenshot saved to: {path}");
                        break;
                        
                    case "click":
                        if (parts.Length >= 3 && float.TryParse(parts[1], out float clickX) && float.TryParse(parts[2], out float clickY))
                        {
                            var clickTarget = AIHelper.Renderer.HitTest(rootPanel, clickX, clickY);
                            if (clickTarget != null)
                            {
                                Console.WriteLine($"Clicking on: <{clickTarget.ElementName}> {(clickTarget.Id != null ? $"id=\"{clickTarget.Id}\"" : "")} classes=[{clickTarget.Classes}]");
                                
                                // Simulate click event
                                var clickEvent = new MousePanelEvent("onclick", clickTarget, "click");
                                clickEvent.LocalPosition = new Vector2(clickX - (clickTarget.Box?.Rect.Left ?? 0), clickY - (clickTarget.Box?.Rect.Top ?? 0));
                                clickTarget.CreateEvent(clickEvent);
                                
                                // Run tick and layout to process any state changes
                                rootPanel.Tick();
                                rootPanel.Layout();
                                
                                Console.WriteLine("Click processed. Use 'snapshot' or 'screenshot' to see updated state.");
                            }
                            else
                            {
                                Console.WriteLine($"No panel found at ({clickX}, {clickY})");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: click <x> <y>");
                        }
                        break;
                        
                    case "what":
                    case "at":
                        if (parts.Length >= 3 && float.TryParse(parts[1], out float whatX) && float.TryParse(parts[2], out float whatY))
                        {
                            Console.WriteLine(AIHelper.WhatIsAt(rootPanel, whatX, whatY));
                        }
                        else
                        {
                            Console.WriteLine("Usage: what <x> <y>");
                        }
                        break;
                        
                    case "find":
                        if (parts.Length >= 2)
                        {
                            var className = parts[1];
                            var found = AIHelper.FindByClass(rootPanel, className);
                            Console.WriteLine($"Found {found.Count} element(s) with class '{className}':");
                            foreach (var info in found)
                            {
                                Console.WriteLine($"  {info}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: find <classname>");
                        }
                        break;
                        
                    case "findid":
                        if (parts.Length >= 2)
                        {
                            var id = parts[1];
                            var found = AIHelper.FindById(rootPanel, id);
                            if (found != null)
                            {
                                Console.WriteLine($"Found: {found}");
                            }
                            else
                            {
                                Console.WriteLine($"No element found with id '{id}'");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: findid <id>");
                        }
                        break;
                        
                    case "hover":
                        if (parts.Length >= 3 && float.TryParse(parts[1], out float hoverX) && float.TryParse(parts[2], out float hoverY))
                        {
                            var hoverTarget = AIHelper.Renderer.HitTest(rootPanel, hoverX, hoverY);
                            if (hoverTarget != null)
                            {
                                Console.WriteLine($"Hovering over: <{hoverTarget.ElementName}> classes=[{hoverTarget.Classes}]");
                                
                                // Apply hover pseudo-class state (propagates up ancestor chain)
                                hoverTarget.Switch(PseudoClass.Hover, true);
                                
                                // Run tick and layout
                                rootPanel.Tick();
                                rootPanel.Layout();
                                
                                Console.WriteLine("Hover applied. Use 'screenshot' to see visual state.");
                            }
                            else
                            {
                                Console.WriteLine($"No panel found at ({hoverX}, {hoverY})");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: hover <x> <y>");
                        }
                        break;
                        
                    case "tick":
                        rootPanel.Tick();
                        Console.WriteLine("Tick cycle completed.");
                        break;
                        
                    case "layout":
                        rootPanel.Layout();
                        Console.WriteLine("Layout recalculated.");
                        break;
                        
                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Open a new native window with the specified panel type.
    /// The window will run in its own thread with its own event loop.
    /// Can be called from any thread after the application has started.
    /// </summary>
    public static void OpenWindow<T>(int width = 1280, int height = 720, string? title = null) where T : Panel, new()
    {
        // Create WindowManager on first use if needed
        if (_windowManager == null)
        {
            _windowManager = new WindowManager();
        }
        
        _windowManager.OpenWindow<T>(width, height, title);
    }

    /// <summary>
    /// [DEPRECATED] Run a Blazor IComponent as the root of the application.
    /// This method is no longer supported. Use RunPanel with Panel-derived Razor components instead.
    /// </summary>
    [Obsolete("Use RunPanel<T> instead with Panel-derived components")]
    public static void RunComponent<T>(int width = 1280, int height = 720, string title = "Fazor Application") where T : IComponent
    {
        throw new NotSupportedException("RunComponent is no longer supported. Use RunPanel<T> with Panel-derived components instead.");
    }
#endif
    
    private static void PrintPanelTree(Panel panel, int depth)
    {
        var indent = new string(' ', depth * 2);
        var text = panel is Label label ? $" Text='{label.Text}'" : "";
        Console.WriteLine($"{indent}{panel.GetType().Name} (ElementName={panel.ElementName}, Children={panel.ChildrenCount}){text}, classes=[{string.Join(", ", panel.Classes)}]");
        foreach (var child in panel.Children)
        {
            PrintPanelTree(child, depth + 1);
        }
    }
}
