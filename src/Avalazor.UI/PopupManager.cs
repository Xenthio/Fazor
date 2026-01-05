using Sandbox.UI;
using UIVector2 = Sandbox.UI.Vector2;

namespace Avalazor.UI;

/// <summary>
/// Manages popup panels within the application.
/// Uses an overlay-based approach where popups are rendered at the root level with high z-index.
/// This provides consistent behavior across platforms without the complexity of multiple windows.
/// </summary>
public class PopupManager : IPopupService, IDisposable
{
    private readonly List<BasePopup> _openPopups = new();
    private readonly NativeWindow _mainWindow;
    private bool _disposed = false;

    /// <summary>
    /// Whether native popup windows are supported.
    /// Currently returns false to use the overlay approach which is more reliable.
    /// </summary>
    public bool SupportsNativePopups => false;

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
        // Track this popup
        _openPopups.Add(popup);
        popup.OnPopupClosed += () => OnPopupClosed(popup);

        // The popup will handle its own placement via OpenInRootPanel fallback
    }

    /// <summary>
    /// Close a specific popup
    /// </summary>
    public void ClosePopup(BasePopup popup)
    {
        if (_openPopups.Contains(popup))
        {
            _openPopups.Remove(popup);
            if (!popup.IsDeleting)
            {
                popup.Delete();
            }
        }
    }

    /// <summary>
    /// Close all open popups
    /// </summary>
    public void CloseAllPopups(BasePopup? except = null)
    {
        var toClose = _openPopups.Where(p => p != except).ToList();
        foreach (var popup in toClose)
        {
            _openPopups.Remove(popup);
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

    private void OnPopupClosed(BasePopup popup)
    {
        _openPopups.Remove(popup);
    }

    /// <summary>
    /// Check if a click at the given position should close open popups
    /// </summary>
    public void HandleGlobalClick(UIVector2 position, RootPanel rootPanel)
    {
        // Check each open popup to see if the click is outside
        var popupsToClose = new List<BasePopup>();
        
        foreach (var popup in _openPopups.ToList())
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
            popup.Close();
        }
    }

    /// <summary>
    /// Check if any popups are currently open
    /// </summary>
    public bool HasOpenPopups => _openPopups.Count > 0;

    /// <summary>
    /// Get all currently open popups
    /// </summary>
    public IReadOnlyList<BasePopup> OpenPopups => _openPopups;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Close all popups
        foreach (var popup in _openPopups.ToList())
        {
            if (!popup.IsDeleting)
            {
                popup.Delete();
            }
        }
        _openPopups.Clear();

        // Unregister as popup service
        if (PopupServiceProvider.Current == this)
        {
            PopupServiceProvider.Unregister();
        }
    }
}
