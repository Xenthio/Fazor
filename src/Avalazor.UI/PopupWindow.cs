using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using SkiaSharp;
using Sandbox.UI;
using Sandbox.UI.Skia;
using UIVector2 = Sandbox.UI.Vector2;
using SilkWindow = Silk.NET.Windowing.Window;

namespace Avalazor.UI;

/// <summary>
/// A native popup window that can extend beyond the main application window.
/// Used for dropdown menus, tooltips, context menus, etc.
/// 
/// Important: This window must be properly initialized before use by calling Initialize().
/// The window lifecycle is managed manually through DoFrame() calls from the main render loop.
/// </summary>
public class PopupWindow : IDisposable
{
    private readonly IWindow _window;
    private IGraphicsBackend? _backend;
    private IInputContext? _input;
    private IMouse? _mouse;
    private bool _disposed = false;
    private bool _initialized = false;
    private bool _closeRequested = false;
    private bool _resourcesDisposed = false;

    /// <summary>
    /// The root panel containing the popup content
    /// </summary>
    public RootPanel RootPanel { get; }

    /// <summary>
    /// The popup control being displayed
    /// </summary>
    public BasePopup? PopupContent { get; private set; }

    /// <summary>
    /// The panel that opened this popup
    /// </summary>
    public Panel? Opener { get; private set; }

    /// <summary>
    /// Screen position of this popup window
    /// </summary>
    public UIVector2 Position { get; private set; }

    /// <summary>
    /// Size of this popup window
    /// </summary>
    public UIVector2 Size { get; private set; }

    /// <summary>
    /// Reference to the parent window (for coordinate conversion)
    /// </summary>
    public NativeWindow? ParentWindow { get; set; }

    /// <summary>
    /// Whether the window has been initialized
    /// </summary>
    public bool IsInitialized => _initialized;

    public PopupWindow(int width, int height, int x, int y, NativeWindow? parent = null)
    {
        Size = new UIVector2(width, height);
        Position = new UIVector2(x, y);
        ParentWindow = parent;

        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Position = new Vector2D<int>(x, y);
        options.Title = ""; // Popups typically have no title
        options.WindowBorder = WindowBorder.Hidden; // Borderless popup
        options.TopMost = true; // Always on top
        options.IsVisible = false; // Start hidden until initialized
        options.ShouldSwapAutomatically = true;
        options.VSync = true;
        options.IsEventDriven = false;
        options.TransparentFramebuffer = false;

        // Use same backend type as parent
        if (OperatingSystem.IsWindows())
        {
#if INCLUDE_D3D11_BACKEND
            options.API = GraphicsAPI.None;
            _backend = new D3D11Backend();
#else
            // Fallback to OpenGL if D3D11 not included
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));
            _backend = new OpenGLBackend();
#endif
        }
        else
        {
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));
            _backend = new OpenGLBackend();
        }

        _window = SilkWindow.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FocusChanged += OnFocusChanged;

        RootPanel = new RootPanel();
        RootPanel.PanelBounds = new Rect(0, 0, width, height);
    }

    /// <summary>
    /// Initialize the popup window. Must be called before DoFrame() or accessing IsClosing.
    /// This triggers the window's Load event and sets up graphics resources.
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;
        
        // Initialize the window - this triggers the Load event
        _window.Initialize();
        _initialized = true;
    }

    private void OnLoad()
    {
        _backend.Initialize(_window);

        _input = _window.CreateInput();
        if (_input.Mice.Count > 0)
        {
            _mouse = _input.Mice[0];
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
        }
    }

    private void OnRender(double delta)
    {
        if (!_initialized) return;
        
        var size = _window.FramebufferSize;
        if (size.X <= 0 || size.Y <= 0) return;
        
        RootPanel.PanelBounds = new Rect(0, 0, size.X, size.Y);

        var mousePos = _mouse != null ? new UIVector2(_mouse.Position.X, _mouse.Position.Y) : UIVector2.Zero;
        RootPanel.UpdateInput(mousePos, _mouse != null);
        RootPanel.Layout();

        _backend.Render(RootPanel);
    }

    private void OnClosing()
    {
        // Only dispose resources once - prevent double disposal
        if (_resourcesDisposed) return;
        _resourcesDisposed = true;
        
        // Unsubscribe mouse events first to prevent callbacks during disposal
        if (_mouse != null)
        {
            try
            {
                _mouse.MouseDown -= OnMouseDown;
                _mouse.MouseUp -= OnMouseUp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to unsubscribe mouse events: {ex.Message}");
            }
            _mouse = null;
        }
        
        // Dispose input context
        if (_input != null)
        {
            try
            {
                _input.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to dispose input context: {ex.Message}");
            }
            _input = null;
        }
        
        // Dispose backend
        if (_backend != null)
        {
            try
            {
                _backend.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to dispose backend: {ex.Message}");
            }
            _backend = null;
        }
    }

    private void OnFocusChanged(bool focused)
    {
        // When focus is lost, mark for closing - but don't invoke the callback
        // directly from within this event handler to avoid disposing resources
        // while GLFW is still processing events
        if (!focused && PopupContent?.CloseOnFocusLoss == true)
        {
            _closeRequested = true;
            // The OnCloseRequested event will be fired during the next ProcessPopups() call
            // when IsClosing is checked - this avoids disposing during callback
        }
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        // Check if click is inside the popup
        var mousePos = new UIVector2(mouse.Position.X, mouse.Position.Y);
        var isInside = mousePos.x >= 0 && mousePos.x < Size.x && 
                       mousePos.y >= 0 && mousePos.y < Size.y;

        if (isInside)
        {
            RootPanel.ProcessButtonEvent(MouseButtonToString(button), true, KeyboardModifiers.None);
        }
        else if (PopupContent?.CloseOnClickOutside == true)
        {
            // Mark for closing - don't invoke callback during event processing
            _closeRequested = true;
        }
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        RootPanel.ProcessButtonEvent(MouseButtonToString(button), false, KeyboardModifiers.None);
    }

    private static string MouseButtonToString(MouseButton button) => button switch
    {
        MouseButton.Left => "mouseleft",
        MouseButton.Right => "mouseright",
        MouseButton.Middle => "mousemiddle",
        _ => $"mouse{(int)button}"
    };

    /// <summary>
    /// Set the content of this popup window
    /// </summary>
    public void SetContent(BasePopup popup, Panel? opener = null)
    {
        PopupContent = popup;
        Opener = opener;

        // Clear existing children and add the popup
        RootPanel.DeleteChildren(true);
        RootPanel.AddChild(popup);

        // Popup fills the window
        popup.Style.Position = PositionMode.Absolute;
        popup.Style.Left = 0;
        popup.Style.Top = 0;
        popup.Style.Width = Length.Percent(100);
        popup.Style.Height = Length.Percent(100);

        // Copy stylesheets from opener for consistent styling
        if (opener != null)
        {
            foreach (var stylesheet in opener.AllStyleSheets)
            {
                popup.StyleSheet.Add(stylesheet);
            }
        }

        // Initial layout
        RootPanel.Layout();
    }

    /// <summary>
    /// Update the window position and size
    /// </summary>
    public void UpdateBounds(int x, int y, int width, int height)
    {
        Position = new UIVector2(x, y);
        Size = new UIVector2(width, height);

        _window.Position = new Vector2D<int>(x, y);
        _window.Size = new Vector2D<int>(width, height);
        RootPanel.PanelBounds = new Rect(0, 0, width, height);
    }

    /// <summary>
    /// Process one frame of the popup window.
    /// Must call Initialize() first.
    /// </summary>
    public void DoFrame()
    {
        if (!_initialized || _disposed) return;
        
        _window.DoEvents();
        
        // Check if window is closing (either requested or native close)
        if (!IsClosing)
        {
            _window.DoUpdate();
            _window.DoRender();
        }
    }

    /// <summary>
    /// Check if this window should be closed.
    /// Returns true if close was requested or window is being disposed.
    /// </summary>
    public bool IsClosing
    {
        get
        {
            if (_closeRequested || _disposed) return true;
            if (!_initialized) return false;
            
            try
            {
                return _window.IsClosing;
            }
            catch
            {
                // If we can't access window state, treat as closing
                return true;
            }
        }
    }

    /// <summary>
    /// Show the popup window
    /// </summary>
    public void Show()
    {
        if (!_initialized)
        {
            Initialize();
        }
        _window.IsVisible = true;
    }

    /// <summary>
    /// Hide the popup window
    /// </summary>
    public void Hide()
    {
        if (_initialized)
        {
            _window.IsVisible = false;
        }
    }

    /// <summary>
    /// Close the popup window
    /// </summary>
    public void Close()
    {
        _closeRequested = true;
        if (_initialized)
        {
            try
            {
                _window.Close();
            }
            catch
            {
                // Ignore close errors
            }
        }
    }

    /// <summary>
    /// Reset and clean up window resources (call when removing from list)
    /// </summary>
    public void Reset()
    {
        if (_initialized)
        {
            try
            {
                _window.Reset();
            }
            catch
            {
                // Ignore reset errors
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _closeRequested = true;

        // Dispose our resources first (input, backend)
        OnClosing();
        
        // Then dispose the window itself
        if (_initialized)
        {
            try
            {
                // Unsubscribe from window events to prevent callbacks during disposal
                _window.Load -= OnLoad;
                _window.Render -= OnRender;
                _window.Closing -= OnClosing;
                _window.FocusChanged -= OnFocusChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to unsubscribe window events during Dispose: {ex.Message}");
            }
            
            try
            {
                _window.Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to reset window during Dispose: {ex.Message}");
            }
            
            try
            {
                _window.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupWindow] Failed to dispose window during Dispose: {ex.Message}");
            }
        }
    }
}
