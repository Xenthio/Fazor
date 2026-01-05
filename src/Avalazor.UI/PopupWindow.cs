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
/// </summary>
public class PopupWindow : IDisposable
{
    private readonly IWindow _window;
    private IGraphicsBackend _backend;
    private IInputContext? _input;
    private IMouse? _mouse;
    private bool _disposed = false;

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
    /// Event fired when this popup should be closed (e.g., clicked outside)
    /// </summary>
    public event Action<PopupWindow>? OnCloseRequested;

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
        options.IsVisible = true;
        options.ShouldSwapAutomatically = true;
        options.VSync = true;
        options.IsEventDriven = false;
        options.TransparentFramebuffer = false; // Could enable for drop shadows

        // Use same backend type as parent
        if (OperatingSystem.IsWindows())
        {
            options.API = GraphicsAPI.None;
            _backend = new D3D11Backend();
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
        PanelRealTime.Update(delta);
        RealTime.Update(delta);

        var size = _window.FramebufferSize;
        RootPanel.PanelBounds = new Rect(0, 0, size.X, size.Y);

        var mousePos = _mouse != null ? new UIVector2(_mouse.Position.X, _mouse.Position.Y) : UIVector2.Zero;
        RootPanel.UpdateInput(mousePos, _mouse != null);
        RootPanel.Layout();

        _backend.Render(RootPanel);
    }

    private void OnClosing()
    {
        _backend.Dispose();
        _input?.Dispose();
    }

    private void OnFocusChanged(bool focused)
    {
        if (!focused && PopupContent?.CloseOnFocusLoss == true)
        {
            // Delay the close request slightly to allow click events to process
            OnCloseRequested?.Invoke(this);
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
            OnCloseRequested?.Invoke(this);
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
    /// Process one frame of the popup window
    /// </summary>
    public void DoFrame()
    {
        _window.DoEvents();
        if (!_window.IsClosing)
        {
            _window.DoUpdate();
            _window.DoRender();
        }
    }

    /// <summary>
    /// Check if this window should be closed
    /// </summary>
    public bool IsClosing => _window.IsClosing;

    /// <summary>
    /// Show the popup window
    /// </summary>
    public void Show()
    {
        _window.IsVisible = true;
    }

    /// <summary>
    /// Hide the popup window
    /// </summary>
    public void Hide()
    {
        _window.IsVisible = false;
    }

    /// <summary>
    /// Close the popup window
    /// </summary>
    public void Close()
    {
        _window.Close();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        OnClosing();
        _window.Dispose();
    }
}
