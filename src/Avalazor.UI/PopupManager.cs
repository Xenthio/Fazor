using Sandbox.UI;
using UIVector2 = Sandbox.UI.Vector2;

namespace Avalazor.UI;

/// <summary>
/// Manages popup windows for the desktop application.
/// Creates actual native OS windows for popups, allowing them to extend beyond
/// the main application window bounds - proper behavior for desktop applications.
/// </summary>
public class PopupManager : IPopupService, IDisposable
{
    private readonly List<PopupWindow> _openPopups = new();
    private readonly NativeWindow _mainWindow;
    private bool _disposed = false;

    /// <summary>
    /// Whether native popup windows are supported.
    /// When true, popups will be rendered in separate native OS windows that can
    /// extend beyond the main application window bounds.
    /// </summary>
    public bool SupportsNativePopups { get; set; } = true;

    /// <summary>
    /// If true, use overlay-based fallback instead of native popup windows.
    /// Overlay popups appear as high z-index overlays within the main window.
    /// Set this to true if native popups cause issues on specific platforms.
    /// </summary>
    public bool UseOverlayFallback { get; set; } = false;

    public PopupManager(NativeWindow mainWindow)
    {
        _mainWindow = mainWindow;

        // Register as the popup service provider
        PopupServiceProvider.Register(this);
    }

    /// <summary>
    /// Open a popup at the specified screen position
    /// </summary>
    public void OpenPopup(BasePopup popup, UIVector2 screenPosition, Panel? opener = null)
    {
        if (UseOverlayFallback || !SupportsNativePopups)
        {
            // Track popup for click-outside handling even in overlay mode
            TrackPopupForOverlay(popup);
            return;
        }

        // Calculate popup size - start with a reasonable default
        var width = 200;
        var height = 300; // Reasonable height for dropdown lists

        // If opener has a width, match it for dropdowns
        if (opener?.Box != null)
        {
            width = Math.Max(width, (int)opener.Box.Rect.Width);
        }

        try
        {
            // Create native popup window
            var popupWindow = new PopupWindow(
                width, height,
                (int)screenPosition.x, (int)screenPosition.y,
                _mainWindow
            );

            popupWindow.SetContent(popup, opener);
            // Note: We don't subscribe to OnCloseRequested anymore - we check IsClosing in ProcessPopups()

            _openPopups.Add(popupWindow);
            
            // Initialize and show the popup window
            popupWindow.Show();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PopupManager] Failed to create native popup: {ex.Message}");
            Console.WriteLine($"[PopupManager] Falling back to overlay mode");
            
            // Fall back to overlay mode if native popup fails
            TrackPopupForOverlay(popup);
        }
    }

    private readonly List<BasePopup> _overlayPopups = new();

    private void TrackPopupForOverlay(BasePopup popup)
    {
        _overlayPopups.Add(popup);
        popup.OnPopupClosed += () => _overlayPopups.Remove(popup);
    }

    /// <summary>
    /// Close a specific popup
    /// </summary>
    public void ClosePopup(BasePopup popup)
    {
        // Find and close native popup window
        var window = _openPopups.FirstOrDefault(w => w.PopupContent == popup);
        if (window != null)
        {
            ClosePopupWindow(window);
        }

        // Also remove from overlay tracking
        _overlayPopups.Remove(popup);
    }

    /// <summary>
    /// Close all open popups
    /// </summary>
    public void CloseAllPopups(BasePopup? except = null)
    {
        // Close native popup windows
        var toClose = _openPopups.Where(w => w.PopupContent != except).ToList();
        foreach (var window in toClose)
        {
            ClosePopupWindow(window);
        }

        // Close overlay popups
        var overlayToClose = _overlayPopups.Where(p => p != except).ToList();
        foreach (var popup in overlayToClose)
        {
            _overlayPopups.Remove(popup);
            if (!popup.IsDeleting)
            {
                popup.Delete();
            }
        }
    }

    /// <summary>
    /// Convert root panel coordinates to screen coordinates
    /// </summary>
    public UIVector2 ConvertToScreenCoordinates(UIVector2 panelPos, RootPanel? rootPanel)
    {
        var windowPos = GetWindowPosition();
        return new UIVector2(
            windowPos.x + panelPos.x,
            windowPos.y + panelPos.y
        );
    }

    /// <summary>
    /// Convert screen coordinates to root panel coordinates
    /// </summary>
    public UIVector2 ConvertFromScreenCoordinates(UIVector2 screenPos, RootPanel? rootPanel)
    {
        var windowPos = GetWindowPosition();
        return new UIVector2(
            screenPos.x - windowPos.x,
            screenPos.y - windowPos.y
        );
    }

    /// <summary>
    /// Get the main window's position in screen coordinates
    /// </summary>
    public UIVector2 GetWindowPosition()
    {
        var pos = _mainWindow.WindowPosition;
        return new UIVector2(pos.X, pos.Y);
    }

    /// <summary>
    /// Get the main window's size
    /// </summary>
    public UIVector2 GetWindowSize()
    {
        var size = _mainWindow.WindowSize;
        return new UIVector2(size.X, size.Y);
    }

    private void ClosePopupWindow(PopupWindow window)
    {
        // Remove from list first to prevent re-processing
        _openPopups.Remove(window);
        
        // Notify the popup content that it's being closed
        if (window.PopupContent != null && !window.PopupContent.IsDeleting)
        {
            try
            {
                // Don't call popup.Close() as that would recurse
                window.PopupContent.Delete();
            }
            catch { }
        }
        
        // Clean up and dispose the window - wrap in try/catch for safety
        try
        {
            window.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PopupManager] Error disposing popup window: {ex.Message}");
        }
    }

    /// <summary>
    /// Process all popup windows. Call this from the main render loop.
    /// </summary>
    public void ProcessPopups()
    {
        if (_openPopups.Count == 0) return;
        
        // Process each native popup window
        var closedPopups = new List<PopupWindow>();
        
        foreach (var popup in _openPopups.ToList())
        {
            try
            {
                if (popup.IsClosing)
                {
                    closedPopups.Add(popup);
                }
                else
                {
                    popup.DoFrame();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PopupManager] Error processing popup: {ex.Message}");
                closedPopups.Add(popup);
            }
        }

        // Clean up closed popups
        foreach (var popup in closedPopups)
        {
            ClosePopupWindow(popup);
        }
    }

    /// <summary>
    /// Handle global click for overlay popups (click outside to close)
    /// </summary>
    public void HandleGlobalClick(UIVector2 position, RootPanel rootPanel)
    {
        // For overlay-based popups, check if click is outside
        var popupsToClose = new List<BasePopup>();
        
        foreach (var popup in _overlayPopups.ToList())
        {
            if (!popup.IsValid() || popup.IsDeleting)
            {
                popupsToClose.Add(popup);
                continue;
            }

            if (popup.CloseOnClickOutside)
            {
                var box = popup.Box?.Rect;
                if (box.HasValue)
                {
                    // Check if click is inside popup
                    var isInside = position.x >= box.Value.Left && position.x <= box.Value.Right &&
                                   position.y >= box.Value.Top && position.y <= box.Value.Bottom;
                    
                    // Check if click is on popup's opener
                    var isOnOpener = false;
                    if (popup.Opener?.Box != null)
                    {
                        var openerBox = popup.Opener.Box.Rect;
                        isOnOpener = position.x >= openerBox.Left && position.x <= openerBox.Right &&
                                     position.y >= openerBox.Top && position.y <= openerBox.Bottom;
                    }

                    if (!isInside && !isOnOpener)
                    {
                        popupsToClose.Add(popup);
                    }
                }
            }
        }

        foreach (var popup in popupsToClose)
        {
            _overlayPopups.Remove(popup);
            popup.Close();
        }
    }

    /// <summary>
    /// Check if any popups are currently open
    /// </summary>
    public bool HasOpenPopups => _openPopups.Count > 0 || _overlayPopups.Count > 0;

    /// <summary>
    /// Get all currently open native popup windows
    /// </summary>
    public IReadOnlyList<PopupWindow> OpenPopupWindows => _openPopups;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Close all native popup windows
        foreach (var popup in _openPopups.ToList())
        {
            try
            {
                popup.Reset();
                popup.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
        _openPopups.Clear();

        // Close overlay popups
        foreach (var popup in _overlayPopups.ToList())
        {
            if (!popup.IsDeleting)
            {
                popup.Delete();
            }
        }
        _overlayPopups.Clear();

        // Unregister as popup service
        if (PopupServiceProvider.Current == this)
        {
            PopupServiceProvider.Unregister();
        }
    }
}
