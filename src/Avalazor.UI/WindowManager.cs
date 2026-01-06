using Sandbox.UI;
using Sandbox.UI.Reflection;
using System.Threading;

namespace Avalazor.UI;

/// <summary>
/// Manages multiple native windows for the desktop application.
/// Each window runs in its own thread with its own event loop, similar to
/// how multi-window applications work in WPF, WinForms, etc.
/// </summary>
public class WindowManager : IDisposable
{
    private readonly List<WindowInfo> _openWindows = new();
    private readonly object _lock = new object();
    private bool _disposed = false;

    private class WindowInfo
    {
        public Thread Thread { get; set; }
        public bool IsAlive => Thread?.IsAlive ?? false;

        public WindowInfo(Thread thread)
        {
            Thread = thread;
        }
    }

    /// <summary>
    /// Open a new native window with the specified panel type as content.
    /// The window will run in its own thread with its own event loop.
    /// Returns immediately after starting the window thread.
    /// </summary>
    public void OpenWindow<T>(int width = 1280, int height = 720, string? title = null) where T : Panel, new()
    {
        if (_disposed) return;

        // Create thread for the window
        var thread = new Thread(() =>
        {
            try
            {
                // Initialize PanelFactory in this thread
                PanelFactory.Initialize();

                // Create the panel
                var panel = new T();

                // Extract window properties if the panel is a Window
                if (panel is Sandbox.UI.Window windowPanel)
                {
                    // Create RootPanel and add the window panel
                    var rootPanel = new RootPanel();
                    rootPanel.PanelBounds = new Rect(0, 0, width, height);
                    rootPanel.AddChild(panel);
                    
                    // Initial layout to process Razor attributes
                    rootPanel.Layout();
                    
                    // Re-read window properties AFTER layout
                    if (windowPanel.IsWindowWidthExplicit)
                    {
                        width = windowPanel.WindowWidth;
                    }
                    if (windowPanel.IsWindowHeightExplicit)
                    {
                        height = windowPanel.WindowHeight;
                    }
                    
                    if (!string.IsNullOrEmpty(windowPanel.Title))
                    {
                        title = windowPanel.Title;
                    }
                    
                    // Determine if window should be borderless (when using custom chrome)
                    bool borderless = windowPanel.HasCustomChrome;
                    
                    // Create and run the native window (this blocks until window closes)
                    var nativeWindow = new NativeWindow(width, height, title ?? "Window", transparentFramebuffer: true, borderless: borderless);
                    nativeWindow.RootPanel = rootPanel;
                    
                    // Give the window panel a reference to the native window
                    windowPanel.SetNativeWindow(nativeWindow);
                    
                    // Run window (blocks until closed)
                    nativeWindow.Run();
                }
                else
                {
                    // For non-Window panels, just wrap in a RootPanel
                    var rootPanel = new RootPanel();
                    rootPanel.PanelBounds = new Rect(0, 0, width, height);
                    rootPanel.AddChild(panel);
                    
                    var nativeWindow = new NativeWindow(width, height, title ?? "Window");
                    nativeWindow.RootPanel = rootPanel;
                    
                    // Run window (blocks until closed)
                    nativeWindow.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WindowManager] Error in window thread: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        });

        // Configure thread as STA (required for Windows)
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = false; // Keep app alive while window is open
        thread.Name = $"Window-{title ?? typeof(T).Name}";
        
        lock (_lock)
        {
            _openWindows.Add(new WindowInfo(thread));
        }
        
        thread.Start();
    }

    /// <summary>
    /// Check if any windows are currently open
    /// </summary>
    public bool HasOpenWindows
    {
        get
        {
            lock (_lock)
            {
                // Clean up dead threads
                _openWindows.RemoveAll(w => !w.IsAlive);
                return _openWindows.Count > 0;
            }
        }
    }

    /// <summary>
    /// Get the count of currently open windows
    /// </summary>
    public int OpenWindowCount
    {
        get
        {
            lock (_lock)
            {
                _openWindows.RemoveAll(w => !w.IsAlive);
                return _openWindows.Count;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Note: We can't force-close windows from other threads cleanly.
        // Windows will close naturally when their threads end or user closes them.
        // Just clean up our tracking.
        lock (_lock)
        {
            _openWindows.Clear();
        }
    }
}

