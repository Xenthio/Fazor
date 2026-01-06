using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Input;
using Silk.NET.Core.Loader;
using SkiaSharp;
using Sandbox.UI;
using Avalazor.UI.Native;
using UIVector2 = Sandbox.UI.Vector2;
using System.Runtime.InteropServices;

namespace Avalazor.UI;

public enum GraphicsBackendType
{
    OpenGL,
    Vulkan,
    DirectX11,
    /// <summary>
    /// DirectX 11 with DirectComposition for full per-pixel transparency support.
    /// This backend properly supports transparent windows on Windows.
    /// </summary>
    DirectX11Composition
}

public class NativeWindow : INativeWindow, IDisposable
{
    private readonly IWindow _window;
    private IGraphicsBackend _backend;

    private IInputContext? _input;
    private IMouse? _mouse;
    private IKeyboard? _keyboard;
    private bool _disposed = false;
    private PopupManager? _popupManager;
    private bool _hasNativeBorder = true;
    private bool _hasTransparentFramebuffer = false;

    // Win32 interop for forcing window frame redraw
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_FRAMECHANGED = 0x0020;

    public RootPanel? RootPanel { get; set; }

    /// <summary>
    /// Get the current window position in screen coordinates
    /// </summary>
    public Vector2D<int> WindowPosition => _window.Position;

    /// <summary>
    /// Get the current window size
    /// </summary>
    public Vector2D<int> WindowSize => _window.Size;

    /// <summary>
    /// The popup manager for this window
    /// </summary>
    public PopupManager? PopupManager => _popupManager;

    public NativeWindow(int width = 1280, int height = 720, string title = "Avalazor App", GraphicsBackendType? backendType = null, bool transparentFramebuffer = true, bool borderless = false)
    {
        // Always enable transparent framebuffer by default to support themes with transparency (e.g., ThinGrey)
        _hasTransparentFramebuffer = transparentFramebuffer;
        _hasNativeBorder = !borderless;
        
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(width, height);
        options.Title = title;
        options.VSync = true;
        options.IsEventDriven = false;
        options.TransparentFramebuffer = transparentFramebuffer; // Enable by default for theme transparency support
        options.WindowBorder = borderless ? WindowBorder.Hidden : WindowBorder.Resizable;

        // Auto-select best backend for platform if not specified
        if (backendType == null)
        {
            if (OperatingSystem.IsWindows())
            {
#if INCLUDE_D3D11_BACKEND
                backendType = GraphicsBackendType.DirectX11Composition; // Best for Windows
                Console.WriteLine("Auto-selected DirectX11Composition backend for Windows");
#else
                backendType = GraphicsBackendType.OpenGL; // Fallback to OpenGL if D3D11 not included
                Console.WriteLine("Auto-selected OpenGL backend (D3D11 not included in build)");
#endif
            }
            else
            {
                backendType = GraphicsBackendType.OpenGL; // Works well on Linux/macOS
                Console.WriteLine("Auto-selected OpenGL backend");
            }
        }

        // Select backend and configure window options
        switch (backendType)
        {
            case GraphicsBackendType.OpenGL:
                Console.WriteLine("Starting OpenGL backend...");
                options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.ForwardCompatible, new APIVersion(3, 3));
                _backend = new OpenGLBackend();
                break;

#if INCLUDE_VULKAN_BACKEND
            case GraphicsBackendType.Vulkan:
                Console.WriteLine("Starting Vulkan backend...");
                options.API = GraphicsAPI.DefaultVulkan; // Request Vulkan API
                options.ShouldSwapAutomatically = false; // We handle swapchain ourselves
                _backend = new VulkanBackend();
                break;
#else
            case GraphicsBackendType.Vulkan:
                throw new NotSupportedException("Vulkan backend was not included in this build. Add INCLUDE_VULKAN_BACKEND to DefineConstants and rebuild.");
#endif

#if INCLUDE_D3D11_BACKEND
            case GraphicsBackendType.DirectX11:
                Console.WriteLine("Starting DirectX11 backend...");
                if (!OperatingSystem.IsWindows())
                {
                    throw new PlatformNotSupportedException("DirectX11 backend is only available on Windows");
                }
                options.API = GraphicsAPI.None; // D3D11 handles its own context
                _backend = new D3D11Backend();
                break;

            case GraphicsBackendType.DirectX11Composition:
                Console.WriteLine("Starting DirectX11 Composition backend (with transparency support)...");
                if (!OperatingSystem.IsWindows())
                {
                    throw new PlatformNotSupportedException("DirectX11 Composition backend is only available on Windows");
                }
                options.API = GraphicsAPI.None; // D3D11 handles its own context
                options.TransparentFramebuffer = true; // Required for composition transparency
                _backend = new D3D11CompositionBackend();
                break;
#else
            case GraphicsBackendType.DirectX11:
            case GraphicsBackendType.DirectX11Composition:
                throw new NotSupportedException("Direct3D11 backends were not included in this build. Add INCLUDE_D3D11_BACKEND to DefineConstants and rebuild.");
#endif

            default:
                throw new ArgumentException($"Unsupported backend type: {backendType}");
        }

        // Configure path resolver to find native DLLs extracted from single-file bundle
        // This enables IncludeNativeLibrariesForSelfExtract=true to work with Silk.NET
        // See: https://github.com/dotnet/Silk.NET/issues/2157
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string nativeDllSearchDirectories)
        {
            ((DefaultPathResolver)PathResolver.Default).Resolvers.Add(file =>
                nativeDllSearchDirectories.Split(';').Select(directory => Path.Combine(directory, file))
            );
        }

        _window = Silk.NET.Windowing.Window.Create(options);

        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClosing;
        _window.FramebufferResize += OnFramebufferResize;
        _window.FocusChanged += OnFocusChanged;
    }

    public void Run() => _window.Run();

    private void OnLoad()
    {
        _backend.Initialize(_window);

        _input = _window.CreateInput();
        if (_input.Mice.Count > 0)
        {
            _mouse = _input.Mice[0];
            _mouse.MouseDown += OnMouseDown;
            _mouse.MouseUp += OnMouseUp;
            _mouse.Scroll += OnMouseScroll;
        }
        if (_input.Keyboards.Count > 0)
        {
            _keyboard = _input.Keyboards[0];
            _keyboard.KeyDown += OnKeyDown;
            _keyboard.KeyUp += OnKeyUp;
            _keyboard.KeyChar += OnKeyChar;
        }

        // Set system DPI scale for UI rendering
        UpdateDpiScale();

        // Initialize popup manager
        _popupManager = new PopupManager(this);
        
        // Install Win32 hit test handler for borderless windows with custom chrome
        if (!_hasNativeBorder && Win32HitTestHelper.IsSupported)
        {
            Win32HitTestHelper.InstallHitTestHandler(_window, true);
        }
    }

    private void OnFocusChanged(bool focused)
    {
        _isFocused = focused;
    }

    private void UpdateDpiScale()
    {
        // Try to get DPI from the window's monitor
        // Silk.NET uses FramebufferSize / Size to calculate content scale
        var size = _window.Size;
        var fbSize = _window.FramebufferSize;

        if (size.X > 0 && fbSize.X > 0)
        {
            var dpiScaleX = (float)fbSize.X / size.X;
            var dpiScaleY = (float)fbSize.Y / size.Y;

            // Use the larger scale (usually they're the same)
            RootPanel.SystemDpiScale = Math.Max(dpiScaleX, dpiScaleY);
        }
    }

    private void OnFramebufferResize(Vector2D<int> size)
    {
        if (size.X <= 0 || size.Y <= 0) return;

        Console.WriteLine($"[NativeWindow] OnFramebufferResize: {size.X}x{size.Y}");

        // Use framebuffer size directly - this is the actual render buffer size
        _backend.Resize(size);

        if (RootPanel != null)
        {
            RootPanel.PanelBounds = new Rect(0, 0, size.X, size.Y);
            RootPanel.InvalidateLayout();
            RootPanel.Layout();
        }
    }

    private void OnRender(double delta)
    {
        if (RootPanel == null) return;

        // Update panel time for transitions and animations
        PanelRealTime.Update(delta);
        RealTime.Update(delta);

        var size = _window.FramebufferSize;
        RootPanel.PanelBounds = new Rect(0, 0, size.X, size.Y);

        var mousePos = _mouse != null ? new UIVector2(_mouse.Position.X, _mouse.Position.Y) : UIVector2.Zero;
        RootPanel.UpdateInput(mousePos, _mouse != null);
        RootPanel.Layout();

        // Update cursor based on hovered panel's CSS cursor property
        UpdateCursor();

        _backend.Render(RootPanel);

        // Process native popup windows when enabled
        if (_popupManager != null && _popupManager.SupportsNativePopups && !_popupManager.UseOverlayFallback)
        {
            _popupManager.ProcessPopups();
        }
    }

    /// <summary>
    /// Update the window cursor based on the currently hovered panel's CSS cursor property
    /// </summary>
    private void UpdateCursor()
    {
        if (_mouse?.Cursor == null || RootPanel == null) return;

        var panelCursor = RootPanel.GetCurrentCursor();
        if (panelCursor.HasValue)
        {
            var silkCursor = panelCursor.Value.ToSilkCursor();
            _mouse.Cursor.StandardCursor = silkCursor;
        }
        else
        {
            // Reset to default arrow cursor when no cursor is specified
            _mouse.Cursor.StandardCursor = Silk.NET.Input.StandardCursor.Arrow;
        }
    }

    private void OnClosing()
    {
        _popupManager?.Dispose();
        _popupManager = null;
        _backend.Dispose();
        _input?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        // Uninstall hit test handler
        if (!_hasNativeBorder)
        {
            Win32HitTestHelper.UninstallHitTestHandler(_window);
        }
        
        OnClosing();
        _window?.Dispose();
        _disposed = true;
    }

    // --- Public API for Window control ---

    /// <summary>
    /// Set the native window title
    /// </summary>
    public void SetTitle(string title)
    {
        _window.Title = title;
    }

    /// <summary>
    /// Set the native window position
    /// </summary>
    public void SetPosition(int x, int y)
    {
        _window.Position = new Vector2D<int>(x, y);
    }

    /// <summary>
    /// Set the native window size
    /// </summary>
    public void SetSize(int width, int height)
    {
        _window.Size = new Vector2D<int>(width, height);
    }

    /// <summary>
    /// Set whether the window should use native window decorations (title bar, borders).
    /// When false, the window is borderless and custom chrome can be drawn by the UI.
    /// </summary>
    public void SetWindowBorder(bool hasNativeBorder)
    {
        if (_hasNativeBorder == hasNativeBorder) return;
        
        _hasNativeBorder = hasNativeBorder;

        // Silk.NET uses WindowBorder enum: Fixed, Hidden, Resizable
        _window.WindowBorder = hasNativeBorder ? WindowBorder.Resizable : WindowBorder.Hidden;
        
        // Install or uninstall hit test handler
        if (!hasNativeBorder && Win32HitTestHelper.IsSupported)
        {
            Win32HitTestHelper.InstallHitTestHandler(_window, true);
        }
        else if (hasNativeBorder)
        {
            Win32HitTestHelper.UninstallHitTestHandler(_window);
        }
        
        // Force window frame redraw on Windows
        // This ensures the native window frame appears immediately when switching from borderless to bordered
        if (OperatingSystem.IsWindows() && hasNativeBorder)
        {
            try
            {
                var hwnd = _window.Native?.Win32?.Hwnd ?? IntPtr.Zero;
                if (hwnd != IntPtr.Zero)
                {
                    // SWP_FRAMECHANGED forces Windows to redraw the window frame
                    // SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER means don't change position, size, or Z-order
                    SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NativeWindow] Failed to force frame redraw: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get whether the window currently has native window decorations.
    /// </summary>
    public bool HasNativeBorder => _hasNativeBorder;

    /// <summary>
    /// Set whether the window should have a transparent framebuffer.
    /// This allows transparency in themes that use semi-transparent backgrounds.
    /// Note: Transparent framebuffer is enabled by default to support all themes.
    /// Changing this at runtime is not supported on most platforms as it requires
    /// recreating the window context.
    /// </summary>
    public void SetTransparentFramebuffer(bool transparent)
    {
        // Note: Changing transparent framebuffer at runtime is not supported by most platforms.
        // This would require recreating the window. Since transparent framebuffer is now 
        // enabled by default, this should rarely need to be called.
        // We update the tracking field but the actual window property cannot be changed.
        _hasTransparentFramebuffer = transparent;
    }

    /// <summary>
    /// Get whether the window currently has a transparent framebuffer.
    /// Default is true to support themes with transparency (e.g., ThinGrey).
    /// </summary>
    public bool HasTransparentFramebuffer => _hasTransparentFramebuffer;

    /// <summary>
    /// Get whether the native window currently has focus.
    /// </summary>
    public bool IsFocused => _isFocused;
    private bool _isFocused = true; // Assume focused on start

    /// <summary>
    /// Request focus for the native window.
    /// </summary>
    public void Focus()
    {
        _window.Focus();
    }

    /// <summary>
    /// Close the native window.
    /// </summary>
    public void Close()
    {
        _window.Close();
    }

    /// <summary>
    /// Get the current window position.
    /// </summary>
    public (int x, int y) GetPosition()
    {
        return (_window.Position.X, _window.Position.Y);
    }

    /// <summary>
    /// Get the current window size.
    /// </summary>
    public (int width, int height) GetSize()
    {
        return (_window.Size.X, _window.Size.Y);
    }

    /// <summary>
    /// Get the current mouse position in screen coordinates.
    /// </summary>
    public (int x, int y) GetScreenMousePosition()
    {
        if (_mouse == null) return (0, 0);
        
        // Mouse position is in client coordinates, convert to screen
        var clientX = (int)_mouse.Position.X;
        var clientY = (int)_mouse.Position.Y;
        return ClientToScreen(clientX, clientY);
    }

    /// <summary>
    /// Convert client coordinates to screen coordinates.
    /// </summary>
    public (int x, int y) ClientToScreen(int clientX, int clientY)
    {
        var (winX, winY) = GetPosition();
        return (winX + clientX, winY + clientY);
    }

    // --- Input Helpers ---
    private void OnMouseDown(IMouse mouse, MouseButton button) 
    {
        // Handle popup close on click outside
        if (button == MouseButton.Left && _popupManager != null && RootPanel != null)
        {
            var mousePos = new UIVector2(mouse.Position.X, mouse.Position.Y);
            _popupManager.HandleGlobalClick(mousePos, RootPanel);
        }
        
        RootPanel?.ProcessButtonEvent(MouseButtonToString(button), true, GetKeyboardModifiers());
    }
    private void OnMouseUp(IMouse mouse, MouseButton button) => RootPanel?.ProcessButtonEvent(MouseButtonToString(button), false, GetKeyboardModifiers());
    private void OnMouseScroll(IMouse mouse, ScrollWheel scroll) => RootPanel?.ProcessMouseWheel(new UIVector2(scroll.X, -scroll.Y), GetKeyboardModifiers());
    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode) => RootPanel?.ProcessButtonEvent(key.ToString().ToLower(), true, GetKeyboardModifiers());
    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode) => RootPanel?.ProcessButtonEvent(key.ToString().ToLower(), false, GetKeyboardModifiers());
    private void OnKeyChar(IKeyboard keyboard, char character) => RootPanel?.ProcessCharTyped(character);
    private string MouseButtonToString(MouseButton button) => button switch { MouseButton.Left => "mouseleft", MouseButton.Right => "mouseright", MouseButton.Middle => "mousemiddle", _ => $"mouse{(int)button}" };
    private KeyboardModifiers GetKeyboardModifiers() { if (_keyboard == null) return KeyboardModifiers.None; var m = KeyboardModifiers.None; if (_keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight)) m |= KeyboardModifiers.Shift; if (_keyboard.IsKeyPressed(Key.ControlLeft) || _keyboard.IsKeyPressed(Key.ControlRight)) m |= KeyboardModifiers.Ctrl; if (_keyboard.IsKeyPressed(Key.AltLeft) || _keyboard.IsKeyPressed(Key.AltRight)) m |= KeyboardModifiers.Alt; return m; }
}