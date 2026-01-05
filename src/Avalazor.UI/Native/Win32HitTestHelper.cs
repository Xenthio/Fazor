using System;
using System.Runtime.InteropServices;
using Silk.NET.Windowing;

namespace Avalazor.UI.Native;

/// <summary>
/// Win32 helper for implementing custom hit testing (WM_NCHITTEST) on borderless windows.
/// This enables native OS-handled window resizing from outside the window bounds.
/// </summary>
public static partial class Win32HitTestHelper
{
    // Win32 message constants
    private const int WM_NCHITTEST = 0x0084;
    
    // Win32 constants for WM_NCHITTEST return values
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;
    private const int HTCAPTION = 2;
    private const int HTCLIENT = 1;

    // Edge detection distance (matching XGUI-3)
    private const int RESIZE_BORDER_WIDTH = 5;
    
    // Window subclassing
    private const int GWLP_WNDPROC = -4;
    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    private static WndProcDelegate? _wndProcDelegate;
    private static IntPtr _oldWndProc = IntPtr.Zero;
    private static IWindow? _currentWindow;
    private static bool _hasCustomChrome = false;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
    
    /// <summary>
    /// Window procedure that intercepts WM_NCHITTEST for custom hit testing
    /// </summary>
    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_NCHITTEST && _currentWindow != null && _hasCustomChrome)
        {
            var result = PerformHitTest(_currentWindow, _currentWindow.Size.X, _currentWindow.Size.Y, 30, true);
            if (result != HTCLIENT)
            {
                Console.WriteLine($"[Win32HitTest] Hit test returned: {result} at window size {_currentWindow.Size.X}x{_currentWindow.Size.Y}");
                return new IntPtr(result);
            }
        }
        
        // Call original window procedure
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }
    
    /// <summary>
    /// Installs the custom hit test handler for a borderless window
    /// </summary>
    public static void InstallHitTestHandler(IWindow window, bool hasCustomChrome)
    {
        if (!OperatingSystem.IsWindows() || !hasCustomChrome)
        {
            return;
        }
        
        try
        {
            var hwnd = window.Native!.Win32!.Value.Hwnd;
            _currentWindow = window;
            _hasCustomChrome = hasCustomChrome;
            
            // Create delegate and keep it alive
            _wndProcDelegate = WndProc;
            
            // Subclass the window
            _oldWndProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
            
            Console.WriteLine("[Win32HitTest] Installed hit test handler for borderless window");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Win32HitTest] Failed to install handler: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Uninstalls the custom hit test handler
    /// </summary>
    public static void UninstallHitTestHandler(IWindow window)
    {
        if (!OperatingSystem.IsWindows() || _oldWndProc == IntPtr.Zero)
        {
            return;
        }
        
        try
        {
            var hwnd = window.Native!.Win32!.Value.Hwnd;
            SetWindowLongPtr(hwnd, GWLP_WNDPROC, _oldWndProc);
            _oldWndProc = IntPtr.Zero;
            _currentWindow = null;
            _wndProcDelegate = null;
            
            Console.WriteLine("[Win32HitTest] Uninstalled hit test handler");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Win32HitTest] Failed to uninstall handler: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs hit testing for a borderless window to determine which part of the window the cursor is over.
    /// </summary>
    private static int PerformHitTest(IWindow window, int windowWidth, int windowHeight, int titleBarHeight, bool hasCustomChrome)
    {
        if (!hasCustomChrome)
        {
            return HTCLIENT;
        }

        try
        {
            // Get cursor position in screen coordinates
            if (!GetCursorPos(out POINT cursorPos))
            {
                return HTCLIENT;
            }

            // Convert to client coordinates
            var hwnd = window.Native!.Win32!.Value.Hwnd;
            if (!ScreenToClient(hwnd, ref cursorPos))
            {
                return HTCLIENT;
            }

            int x = cursorPos.X;
            int y = cursorPos.Y;
            
            Console.WriteLine($"[Win32HitTest] Mouse at client coords ({x},{y}), window size ({windowWidth},{windowHeight})");

            // Extend detection zone slightly outside window bounds to catch edges/corners
            // This allows grabbing from just outside the visible window
            const int EXTENDED_BORDER = RESIZE_BORDER_WIDTH + 2; // 7px total (5+2)

            // Check corners first (highest priority)
            // Allow detection when slightly outside the window bounds
            if (x < RESIZE_BORDER_WIDTH && y < RESIZE_BORDER_WIDTH)
                return HTTOPLEFT;
            if (x >= windowWidth - RESIZE_BORDER_WIDTH && y < RESIZE_BORDER_WIDTH)
                return HTTOPRIGHT;
            if (x < RESIZE_BORDER_WIDTH && y >= windowHeight - RESIZE_BORDER_WIDTH)
                return HTBOTTOMLEFT;
            if (x >= windowWidth - RESIZE_BORDER_WIDTH && y >= windowHeight - RESIZE_BORDER_WIDTH)
                return HTBOTTOMRIGHT;

            // Check if cursor is just outside the window bounds (within extended zone)
            bool outsideLeft = x < 0 && x >= -EXTENDED_BORDER;
            bool outsideRight = x >= windowWidth && x < windowWidth + EXTENDED_BORDER;
            bool outsideTop = y < 0 && y >= -EXTENDED_BORDER;
            bool outsideBottom = y >= windowHeight && y < windowHeight + EXTENDED_BORDER;

            // Handle corners when outside window bounds
            if ((outsideLeft || x < EXTENDED_BORDER) && (outsideTop || y < EXTENDED_BORDER))
                return HTTOPLEFT;
            if ((outsideRight || x >= windowWidth - EXTENDED_BORDER) && (outsideTop || y < EXTENDED_BORDER))
                return HTTOPRIGHT;
            if ((outsideLeft || x < EXTENDED_BORDER) && (outsideBottom || y >= windowHeight - EXTENDED_BORDER))
                return HTBOTTOMLEFT;
            if ((outsideRight || x >= windowWidth - EXTENDED_BORDER) && (outsideBottom || y >= windowHeight - EXTENDED_BORDER))
                return HTBOTTOMRIGHT;

            // Check edges (including extended zone outside window)
            if (x < RESIZE_BORDER_WIDTH || outsideLeft)
                return HTLEFT;
            if (x >= windowWidth - RESIZE_BORDER_WIDTH || outsideRight)
                return HTRIGHT;
            if (y < RESIZE_BORDER_WIDTH || outsideTop)
                return HTTOP;
            if (y >= windowHeight - RESIZE_BORDER_WIDTH || outsideBottom)
                return HTBOTTOM;

            // Check titlebar for dragging (if provided)
            if (titleBarHeight > 0 && y < titleBarHeight)
            {
                return HTCAPTION;
            }

            // Default: client area
            return HTCLIENT;
        }
        catch
        {
            return HTCLIENT;
        }
    }

    /// <summary>
    /// Checks if hit testing is supported on the current platform
    /// </summary>
    public static bool IsSupported => OperatingSystem.IsWindows();
}
