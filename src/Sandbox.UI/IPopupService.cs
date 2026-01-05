namespace Sandbox.UI;

/// <summary>
/// Interface for popup window management services.
/// This allows Sandbox.UI to request popup windows without depending on the windowing implementation.
/// </summary>
public interface IPopupService
{
    /// <summary>
    /// Whether this service supports native popup windows that can extend beyond the main window.
    /// If false, popups will be rendered within the root panel instead.
    /// </summary>
    bool SupportsNativePopups { get; }

    /// <summary>
    /// Open a popup at the specified screen position.
    /// </summary>
    /// <param name="popup">The popup panel to display</param>
    /// <param name="screenPosition">Position in screen coordinates</param>
    /// <param name="opener">The panel that opened this popup (for positioning and event handling)</param>
    void OpenPopup(BasePopup popup, Vector2 screenPosition, Panel? opener = null);

    /// <summary>
    /// Close a specific popup
    /// </summary>
    void ClosePopup(BasePopup popup);

    /// <summary>
    /// Close all open popups
    /// </summary>
    /// <param name="except">Optional popup to exclude from closing</param>
    void CloseAllPopups(BasePopup? except = null);

    /// <summary>
    /// Convert root panel coordinates to screen coordinates
    /// </summary>
    Vector2 ConvertToScreenCoordinates(Vector2 panelPos, RootPanel? rootPanel);

    /// <summary>
    /// Convert screen coordinates to root panel coordinates
    /// </summary>
    Vector2 ConvertFromScreenCoordinates(Vector2 screenPos, RootPanel? rootPanel);

    /// <summary>
    /// Get the main window's position in screen coordinates
    /// </summary>
    Vector2 GetWindowPosition();

    /// <summary>
    /// Get the main window's size
    /// </summary>
    Vector2 GetWindowSize();
}

/// <summary>
/// Provider for the current popup service instance.
/// Set by the application framework (e.g., Avalazor.UI) at startup.
/// </summary>
public static class PopupServiceProvider
{
    /// <summary>
    /// The current popup service, or null if none is registered.
    /// </summary>
    public static IPopupService? Current { get; set; }

    /// <summary>
    /// Register a popup service as the current provider.
    /// </summary>
    public static void Register(IPopupService service)
    {
        Current = service;
    }

    /// <summary>
    /// Unregister the current popup service.
    /// </summary>
    public static void Unregister()
    {
        Current = null;
    }
}
