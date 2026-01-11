using Sandbox.UI;
using Fazor.Controls;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Global panel inspector that can be activated via keyboard shortcut (F12).
/// Automatically attaches to any active RootPanel.
/// </summary>
public static class GlobalPanelInspector
{
	private static PanelInspector? _inspector;
	private static bool _isEnabled = true;
    private static RootPanel? _currentRootPanel;

    static GlobalPanelInspector()
	{
		// Register our interceptor with RootPanel
		RootPanel.ButtonEventInterceptor = ProcessButtonEvent;
	}

	/// <summary>
	/// Enable or disable the global inspector hotkey
	/// </summary>
	public static bool IsEnabled
	{
		get => _isEnabled;
		set => _isEnabled = value;
	}

	/// <summary>
	/// The keyboard button that toggles the inspector (default: "f12")
	/// </summary>
	public static string InspectorHotkey { get; set; } = "f12";

	/// <summary>
	/// Whether to open inspector windows in separate OS windows (default: false - uses overlays)
	/// </summary>
	public static bool UseSeparateWindows { get; set; } = false;

	/// <summary>
	/// Callback to create separate OS windows for inspector panels.
	/// Required when UseSeparateWindows is true.
	/// </summary>
	public static Action<Window, string>? WindowCreator { get; set; }

	/// <summary>
	/// Process button events to check for inspector hotkey.
	/// Should be called from RootPanel.ProcessButtonEvent
	/// </summary>
	public static bool ProcessButtonEvent(RootPanel rootPanel, string button, bool pressed, KeyboardModifiers modifiers)
	{

		if (!_isEnabled) return false;
		if (!pressed) return false; // Only on key down
		if (button != InspectorHotkey) return false;

		// F12 pressed - toggle inspector
		Console.WriteLine($"[GlobalPanelInspector] Inspector hotkey pressed! Toggling inspector...");
		_currentRootPanel = rootPanel;
		if (_inspector == null)
		{
			_inspector = new PanelInspector
			{
				UseSeparateWindows = UseSeparateWindows,
				WindowCreator = WindowCreator
			};
		}

		_inspector.Toggle(rootPanel);
		
		// Return true to indicate we handled this event
		return true;
	}

	/// <summary>
	/// Close the inspector if it's open
	/// </summary>
	public static void Close()
	{
		_inspector?.Close();
		_inspector = null;
	}

	/// <summary>
	/// Check if the inspector is currently open
	/// </summary>
	public static bool IsOpen()
	{
		return _inspector?.IsOpen() ?? false;
	}
}
