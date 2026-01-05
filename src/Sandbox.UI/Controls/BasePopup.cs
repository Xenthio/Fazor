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
    /// Fallback method to open popup in root panel when native popups aren't available
    /// </summary>
    protected virtual void OpenInRootPanel(Panel opener)
    {
        var root = opener.FindRootPanel();
        if (root == null) return;

        root.AddChild(this);
        
        Style.Position = PositionMode.Absolute;
        Style.Left = PopupPosition.x;
        Style.Top = PopupPosition.y;
        Style.ZIndex = 10000;
    }

    /// <summary>
    /// Close this popup
    /// </summary>
    public virtual void Close()
    {
        if (!IsPopupOpen) return;

        IsPopupOpen = false;

        if (PopupService != null && PopupService.SupportsNativePopups)
        {
            PopupService.ClosePopup(this);
        }
        
        OnPopupClosed?.Invoke();
        Delete();
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

    protected override void OnMouseDown(MousePanelEvent e)
    {
        base.OnMouseDown(e);
        // Don't propagate clicks to prevent closing from our own content
        e.StopPropagation();
    }
}
