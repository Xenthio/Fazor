namespace Sandbox.UI;

/// <summary>
/// Base class for popup panels. Popups are panels that can be displayed in their own 
/// native window, allowing them to extend beyond the bounds of their parent window.
/// Based on s&box's BasePopup.
/// </summary>
[Library("popup")]
public class BasePopup : Panel
{
    /// <summary>
    /// The panel that opened this popup (the "opener" or "anchor")
    /// </summary>
    public Panel? Opener { get; set; }

    /// <summary>
    /// Whether this popup is currently open
    /// </summary>
    public bool IsPopupOpen { get; private set; }

    /// <summary>
    /// Whether Close() has been called (prevents double-close)
    /// </summary>
    private bool _closeInitiated = false;

    /// <summary>
    /// The position in screen coordinates where this popup should appear
    /// </summary>
    public Vector2 PopupPosition { get; set; }

    /// <summary>
    /// Whether to close this popup when clicking outside of it
    /// </summary>
    public bool CloseOnClickOutside { get; set; } = true;

    /// <summary>
    /// Whether to close this popup when focus leaves it
    /// </summary>
    public bool CloseOnFocusLoss { get; set; } = true;

    /// <summary>
    /// Event fired when the popup is opened
    /// </summary>
    public event Action? OnPopupOpened;

    /// <summary>
    /// Event fired when the popup is closed
    /// </summary>
    public event Action? OnPopupClosed;

    /// <summary>
    /// The popup service used to manage this popup's native window
    /// </summary>
    protected IPopupService? PopupService => PopupServiceProvider.Current;

    public BasePopup()
    {
        AddClass("popup");
        Style.Position = PositionMode.Absolute;
    }

    /// <summary>
    /// Open this popup at the specified position relative to the opener panel.
    /// The popup will be positioned in screen coordinates.
    /// </summary>
    /// <param name="opener">The panel that opened this popup</param>
    /// <param name="preferBelow">If true, prefer positioning below the opener; if false, prefer above</param>
    public virtual void Open(Panel opener, bool preferBelow = true)
    {
        Opener = opener;
        _closeInitiated = false; // Reset close state when opening

        // Get the opener's bounds - Box.Rect is already in root panel coordinates
        var openerRect = opener.Box?.Rect ?? new Rect(0, 0, 100, 20);
        
        // Position the popup directly below (or above) the opener
        var popupX = openerRect.Left;
        var popupY = preferBelow ? openerRect.Bottom : openerRect.Top;
        
        PopupPosition = new Vector2(popupX, popupY);
        
        // Use popup service if available and supports native popups
        if (PopupService != null && PopupService.SupportsNativePopups)
        {
            // For native popups, convert to actual screen coordinates
            var screenPos = PopupService.ConvertToScreenCoordinates(PopupPosition, opener.FindRootPanel());
            PopupService.OpenPopup(this, screenPos, opener);
        }
        else
        {
            // Fallback: add to root panel with absolute positioning
            OpenInRootPanel(opener);
        }

        IsPopupOpen = true;
        OnPopupOpened?.Invoke();
    }

    /// <summary>
    /// Open the popup at an explicit screen position
    /// </summary>
    public virtual void Open(Vector2 screenPosition, Panel? opener = null)
    {
        Opener = opener;
        PopupPosition = screenPosition;
        _closeInitiated = false; // Reset close state when opening

        if (PopupService != null && PopupService.SupportsNativePopups)
        {
            PopupService.OpenPopup(this, screenPosition, opener);
        }
        else if (opener != null)
        {
            OpenInRootPanel(opener);
        }

        IsPopupOpen = true;
        OnPopupOpened?.Invoke();
    }

    /// <summary>
    /// Fallback method to open popup in root panel when native popups aren't available.
    /// Adjusts position to keep popup within window bounds when possible.
    /// </summary>
    protected virtual void OpenInRootPanel(Panel opener)
    {
        var root = opener.FindRootPanel();
        if (root == null) return;

        root.AddChild(this);
        
        Style.Position = PositionMode.Absolute;
        Style.ZIndex = 10000;

        // Get the window bounds
        var windowBounds = root.PanelBounds;
        var openerRect = opener.Box?.Rect ?? new Rect(0, 0, 100, 20);
        
        // Estimate popup size (use a reasonable default, will be refined after layout)
        var estimatedWidth = openerRect.Width;
        var estimatedHeight = 200f; // Default estimated height for dropdowns
        
        // Calculate initial position
        var popupX = PopupPosition.x;
        var popupY = PopupPosition.y;
        
        // Adjust horizontal position if popup would extend beyond right edge
        if (popupX + estimatedWidth > windowBounds.Width)
        {
            popupX = Math.Max(0, windowBounds.Width - estimatedWidth);
        }
        
        // Adjust vertical position if popup would extend beyond bottom edge
        // Try to flip above the opener if there's more room there
        if (popupY + estimatedHeight > windowBounds.Height)
        {
            var spaceBelow = windowBounds.Height - openerRect.Bottom;
            var spaceAbove = openerRect.Top;
            
            if (spaceAbove > spaceBelow)
            {
                // Position above the opener
                popupY = openerRect.Top - estimatedHeight;
                if (popupY < 0) popupY = 0;
            }
            else
            {
                // Keep below but constrain to window
                popupY = Math.Max(0, windowBounds.Height - estimatedHeight);
            }
        }
        
        Style.Left = popupX;
        Style.Top = popupY;
    }

    /// <summary>
    /// Close this popup
    /// </summary>
    public virtual void Close()
    {
        // Prevent double-close - only proceed if we haven't started closing yet
        if (_closeInitiated) return;
        _closeInitiated = true;
        
        // Mark as not open
        var wasOpen = IsPopupOpen;
        IsPopupOpen = false;

        // Mark popup for closing (deferred) - the PopupManager will handle actual cleanup
        // on the next frame, which is safe since we won't be in the middle of event processing
        if (PopupService != null && PopupService.SupportsNativePopups)
        {
            PopupService.MarkPopupForClose(this);
        }
        
        // Fire the closed event so owners (like ComboBox) can update their state
        if (wasOpen)
        {
            OnPopupClosed?.Invoke();
        }
        
        // Delete this panel (safe to do immediately since it just removes from panel tree)
        if (!IsDeleting)
        {
            Delete();
        }
    }

    /// <summary>
    /// Get the screen position of a point relative to a panel.
    /// Returns coordinates in root panel space (not actual screen coordinates).
    /// </summary>
    protected Vector2 GetRootPanelPosition(Panel panel, Vector2 localPos)
    {
        // Start with the local position relative to the panel
        var pos = localPos;

        // Walk up the panel tree to convert to root panel coordinates
        // We add the box position of each parent since localPos is already in the panel's coordinate space
        var current = panel.Parent;
        while (current != null && current is not RootPanel)
        {
            if (current.Box != null)
            {
                pos = new Vector2(
                    pos.x + current.Box.Rect.Left,
                    pos.y + current.Box.Rect.Top
                );
            }
            current = current.Parent;
        }

        return pos;
    }

    /// <summary>
    /// Get the screen position of a point relative to a panel
    /// </summary>
    protected Vector2 GetScreenPosition(Panel panel, Vector2 localPos)
    {
        // First get position in root panel coordinates
        var pos = GetRootPanelPosition(panel, localPos);

        // If we have a popup service and native popups, convert to actual screen coordinates
        if (PopupService != null && PopupService.SupportsNativePopups)
        {
            pos = PopupService.ConvertToScreenCoordinates(pos, panel.FindRootPanel());
        }

        return pos;
    }

    /// <summary>
    /// Close all open popups
    /// </summary>
    public static void CloseAll(Panel? exceptThisOne = null)
    {
        PopupServiceProvider.Current?.CloseAllPopups(exceptThisOne as BasePopup);
    }

    /// <summary>
    /// Get the preferred size for this popup. Override in derived classes to 
    /// calculate actual content size.
    /// </summary>
    /// <param name="maxWidth">Maximum width constraint</param>
    /// <param name="maxHeight">Maximum height constraint</param>
    /// <returns>Preferred width and height for the popup</returns>
    public virtual Vector2 GetPreferredSize(float maxWidth = 400, float maxHeight = 600)
    {
        // Default implementation returns a reasonable default size
        // Derived classes should override to calculate actual content size
        return new Vector2(Math.Min(200, maxWidth), Math.Min(200, maxHeight));
    }

    protected override void OnMouseDown(MousePanelEvent e)
    {
        base.OnMouseDown(e);
        // Don't propagate clicks to prevent closing from our own content
        e.StopPropagation();
    }
}
