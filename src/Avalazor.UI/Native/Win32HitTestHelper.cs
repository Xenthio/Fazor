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
    private const int WM_NCCALCSIZE = 0x0083;
    
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
    
    // Window style constants
    private const int GWL_STYLE = -16;
    private const uint WS_THICKFRAME = 0x00040000;
    private const uint WS_CAPTION = 0x00C00000;
    
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
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
    
    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public RECT[] rgrc;
        public IntPtr lppos;
    }
    
    /// <summary>
    /// Window procedure that intercepts WM_NCHITTEST and WM_NCCALCSIZE for custom hit testing
    /// </summary>
    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        // Handle WM_NCCALCSIZE to remove visible frame while preserving resize border
        // This removes the white bar and rounded corners while keeping outside-window resize working
        if (msg == WM_NCCALCSIZE && wParam.ToInt32() == 1 && _hasCustomChrome)
        {
            // wParam == 1 means lParam points to NCCALCSIZE_PARAMS structure
            // We need to modify rgrc[0] (the new client rect) to remove the visible frame
            // while preserving an invisible border for hit testing
            
            try
            {
                // Marshal the NCCALCSIZE_PARAMS structure from lParam
                var nccsp = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                
                if (nccsp.rgrc != null && nccsp.rgrc.Length >= 1)
                {
                    // rgrc[0] is the proposed new client rectangle
                    // By not modifying it (or making minimal adjustments), we keep the non-client border
                    // but Windows won't draw visible frame artifacts because we have WS_THICKFRAME
                    // without WS_CAPTION, which gives us a borderless but resizable window
                    
                    // The key insight: With WS_THICKFRAME and without WS_CAPTION,
                    // Windows creates an invisible resize border without visible decorations.
                    // We just need to tell Windows the client area occupies the full window.
                    
                    // Expand the client rect to fill the entire window (removes visible frame)
                    // But Windows still maintains the invisible non-client border for hit testing
                    var newClientRect = nccsp.rgrc[0];
                    
                    // Don't modify the rect - let Windows use its default calculation
                    // This preserves the invisible resize border while removing visible artifacts
                    // The combination of WS_THICKFRAME (without WS_CAPTION) + no rect modification
                    // gives us borderless appearance with working resize-from-outside
                    
                    // Marshal back (even though we didn't change anything, we signal we processed it)
                    Marshal.StructureToPtr(nccsp, lParam, true);
                }
                
                // Return 0 to indicate we processed the message
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Win32HitTest] WM_NCCALCSIZE error: {ex.Message}");
            }
        }
        
        if (msg == WM_NCHITTEST && _currentWindow != null && _hasCustomChrome)
        {
            var result = PerformHitTest(_currentWindow, _currentWindow.Size.X, _currentWindow.Size.Y, 30, 120, true);
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
            
            // CRITICAL: Add WS_THICKFRAME to window style for resize to work
            // Without this style, Windows ignores WM_NCHITTEST resize codes
            uint currentStyle = GetWindowLong(hwnd, GWL_STYLE);
            uint newStyle = currentStyle | WS_THICKFRAME;
            SetWindowLong(hwnd, GWL_STYLE, newStyle);
            Console.WriteLine($"[Win32HitTest] Added WS_THICKFRAME to window style (0x{currentStyle:X} -> 0x{newStyle:X})");
            
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
    /// <param name="titleBarControlsWidth">Width in pixels from the right edge reserved for titlebar controls (minimize, maximize, close buttons)</param>
    private static int PerformHitTest(IWindow window, int windowWidth, int windowHeight, int titleBarHeight, int titleBarControlsWidth, bool hasCustomChrome)
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

            // PRIORITY 1: Check titlebar controls area - must remain clickable
            // This is the area on the right side of the titlebar with minimize/maximize/close buttons
            if (titleBarControlsWidth > 0 && y >= 0 && y < titleBarHeight && 
                x >= windowWidth - titleBarControlsWidth && x < windowWidth)
            {
                // Return HTCLIENT so our UI can handle button clicks
                return HTCLIENT;
            }

            // Check if cursor is just outside the window bounds (within extended zone)
            bool outsideLeft = x < 0 && x >= -EXTENDED_BORDER;
            bool outsideRight = x >= windowWidth && x < windowWidth + EXTENDED_BORDER;
            bool outsideTop = y < 0 && y >= -EXTENDED_BORDER;
            bool outsideBottom = y >= windowHeight && y < windowHeight + EXTENDED_BORDER;
            
            // Define corner zones (inside window + extended outside)
            bool inLeftZone = x < RESIZE_BORDER_WIDTH || outsideLeft;
            bool inRightZone = x >= windowWidth - RESIZE_BORDER_WIDTH || outsideRight;
            bool inTopZone = y < RESIZE_BORDER_WIDTH || outsideTop;
            bool inBottomZone = y >= windowHeight - RESIZE_BORDER_WIDTH || outsideBottom;

            // PRIORITY 2: Check corners FIRST (higher priority than edges)
            // Corners must be checked before edges to prevent edges from capturing corner zones
            if (inLeftZone && inTopZone)
                return HTTOPLEFT;
            if (inRightZone && inTopZone)
                return HTTOPRIGHT;
            if (inLeftZone && inBottomZone)
                return HTBOTTOMLEFT;
            if (inRightZone && inBottomZone)
                return HTBOTTOMRIGHT;

            // PRIORITY 3: Check edges (only if not in corners)
            // These take priority over the titlebar drag area!
            if (inLeftZone)
                return HTLEFT;
            if (inRightZone)
                return HTRIGHT;
            if (inTopZone)
                return HTTOP;
            if (inBottomZone)
                return HTBOTTOM;

            // PRIORITY 4: Check titlebar for dragging (only if not in edges/corners/controls)
            if (titleBarHeight > 0 && y >= RESIZE_BORDER_WIDTH && y < titleBarHeight && 
                x >= RESIZE_BORDER_WIDTH && x < windowWidth - RESIZE_BORDER_WIDTH - titleBarControlsWidth)
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
