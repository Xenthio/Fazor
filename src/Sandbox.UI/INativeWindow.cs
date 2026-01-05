namespace Sandbox.UI;

/// <summary>
/// Interface for native window implementations to allow Window controls to interact with them.
/// Avoids reflection and provides type-safe access to native window properties.
/// </summary>
public interface INativeWindow
{
    /// <summary>
    /// Set the native window title
    /// </summary>
    void SetTitle(string title);

    /// <summary>
    /// Set the native window position
    /// </summary>
    void SetPosition(int x, int y);

    /// <summary>
    /// Set the native window size
    /// </summary>
    void SetSize(int width, int height);

    /// <summary>
    /// Set whether the window should use native window decorations (title bar, borders).
    /// When false, the window is borderless and custom chrome can be drawn by the UI.
    /// </summary>
    void SetWindowBorder(bool hasNativeBorder);

    /// <summary>
    /// Get whether the window currently has native window decorations.
    /// </summary>
    bool HasNativeBorder { get; }

    /// <summary>
    /// Set whether the window should have a transparent framebuffer.
    /// This allows transparency in themes that use semi-transparent backgrounds.
    /// Note: Transparent framebuffer is enabled by default. Runtime changes may not
    /// be supported on all platforms as they can require recreating the window.
    /// </summary>
    void SetTransparentFramebuffer(bool transparent);

    /// <summary>
    /// Get whether the window currently has a transparent framebuffer.
    /// Default is true to support themes with transparency (e.g., ThinGrey).
    /// </summary>
    bool HasTransparentFramebuffer { get; }

    /// <summary>
    /// Get whether the native window currently has focus.
    /// </summary>
    bool IsFocused { get; }

    /// <summary>
    /// Request focus for the native window.
    /// </summary>
    void Focus();

    /// <summary>
    /// Close the native window.
    /// </summary>
    void Close();

    /// <summary>
    /// Get the current window position.
    /// </summary>
    (int x, int y) GetPosition();

    /// <summary>
    /// Get the current window size.
    /// </summary>
    (int width, int height) GetSize();

    /// <summary>
    /// Get the current mouse position in screen coordinates.
    /// </summary>
    (int x, int y) GetScreenMousePosition();

    /// <summary>
    /// Convert client coordinates to screen coordinates.
    /// </summary>
    (int x, int y) ClientToScreen(int clientX, int clientY);
}
