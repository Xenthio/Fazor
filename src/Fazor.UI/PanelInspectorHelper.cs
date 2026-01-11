using Sandbox.UI;
using Fazor.Controls;
using Fazor.UI.PanelInspector;

namespace Fazor.UI;

/// <summary>
/// Helper class to enable Panel Inspector in separate OS windows for Fazor applications.
/// </summary>
public static class PanelInspectorHelper
{
	/// <summary>
	/// Enable the Panel Inspector with separate OS windows.
	/// Call this before running your application to enable F12 inspector with separate windows.
	/// </summary>
	public static void EnableSeparateWindows()
	{
		GlobalPanelInspector.UseSeparateWindows = true;
		GlobalPanelInspector.WindowCreator = CreateSeparateWindow;
		
		Console.WriteLine("[PanelInspectorHelper] Panel Inspector configured to use separate OS windows.");
		Console.WriteLine("[PanelInspectorHelper] Press F12 to open the inspector.");
	}

	/// <summary>
	/// Enable the Panel Inspector with overlay mode (default).
	/// Inspector windows appear as overlays in the same window.
	/// </summary>
	public static void EnableOverlayMode()
	{
		GlobalPanelInspector.UseSeparateWindows = false;
		GlobalPanelInspector.WindowCreator = null;
		
		Console.WriteLine("[PanelInspectorHelper] Panel Inspector configured to use overlay mode.");
		Console.WriteLine("[PanelInspectorHelper] Press F12 to open the inspector.");
	}

	private static void CreateSeparateWindow(Window inspectorWindow, string title)
	{
		// Create a separate window in a background thread
		var thread = new Thread(() =>
		{
			try
			{
				Console.WriteLine($"[PanelInspectorHelper] Creating separate window: {title}");
				
				// Create a root panel for the inspector window
				var rootPanel = new RootPanel();
				rootPanel.AddChild(inspectorWindow);
				rootPanel.Layout();
				
				// Create native window
				var nativeWindow = new NativeWindow(
					inspectorWindow.WindowWidth > 0 ? inspectorWindow.WindowWidth : 400,
					inspectorWindow.WindowHeight > 0 ? inspectorWindow.WindowHeight : 600,
					title
				);
				
				inspectorWindow.SetNativeWindow(nativeWindow);
				nativeWindow.RootPanel = rootPanel;
				
				Console.WriteLine($"[PanelInspectorHelper] Separate window created: {title}");
				
				// Run the window (blocking call)
				nativeWindow.Run();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[PanelInspectorHelper] Error creating separate window '{title}': {ex.Message}");
			}
		});
		
		// Run inspector window on a background thread so it does not block application shutdown
	thread.IsBackground = true;
		thread.Start();
		
		// Give the window a moment to initialize
		Thread.Sleep(50);
	}
}
