using Sandbox.UI;
using Fazor.Controls;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Main panel inspector that combines the panel list and style inspector.
/// Provides a complete debugging interface for UI panels.
/// Based on s&box's PanelInspector (MIT Licensed)
/// </summary>
public class PanelInspector
{
	private PanelListWindow? panelListWindow;
	private StyleInspectorWindow? styleInspectorWindow;
    private RootPanel? targetRootPanel;

    /// <summary>
    /// Whether to open inspector windows in separate OS windows (true) or as overlays (false).
    /// When true, requires WindowCreator to be set.
    /// </summary>
    public bool UseSeparateWindows { get; set; } = false;

	/// <summary>
	/// Callback to create a separate OS window for an inspector panel.
	/// Required when UseSeparateWindows is true.
	/// The callback should create a native window, add the panel to a RootPanel, and show it.
	/// </summary>
	public Action<Window, string>? WindowCreator { get; set; }

	public PanelInspector()
	{
	}

	/// <summary>
	/// Open the panel inspector windows.
	/// </summary>
	/// <param name="rootPanel">The root panel to inspect</param>
	public void Open(RootPanel rootPanel)
	{
		targetRootPanel = rootPanel;
		
		Console.WriteLine($"[PanelInspector] Opening inspector windows (UseSeparateWindows={UseSeparateWindows})...");

		if (UseSeparateWindows)
		{
			if (WindowCreator == null)
			{
				Console.WriteLine("[PanelInspector] ERROR: UseSeparateWindows is true but WindowCreator is not set!");
				Console.WriteLine("[PanelInspector] Falling back to overlay mode.");
				OpenAsOverlays(rootPanel);
				return;
			}
			OpenInSeparateWindows(rootPanel);
		}
		else
		{
			OpenAsOverlays(rootPanel);
		}
	}

	private void OpenInSeparateWindows(RootPanel targetRoot)
	{
		Console.WriteLine("[PanelInspector] Creating inspector windows as separate OS windows...");
		
		// Create Panel List window
		if (panelListWindow == null || !panelListWindow.IsValid())
		{
			Console.WriteLine("[PanelInspector] Creating PanelListWindow...");
			panelListWindow = new PanelListWindow();
			panelListWindow.WindowWidth = 400;
			panelListWindow.WindowHeight = 600;
			panelListWindow.SetTargetRootPanel(targetRoot);
			panelListWindow.PanelSelected += OnPanelSelected;
			panelListWindow.PanelHovered += OnPanelHovered;
			
			// Use the callback to create the OS window
			WindowCreator?.Invoke(panelListWindow, "Panel Inspector - Hierarchy");
			Console.WriteLine("[PanelInspector] PanelListWindow created in separate window");
		}

		// Create Style Inspector window
		if (styleInspectorWindow == null || !styleInspectorWindow.IsValid())
		{
			Console.WriteLine("[PanelInspector] Creating StyleInspectorWindow...");
			styleInspectorWindow = new StyleInspectorWindow();
			styleInspectorWindow.WindowWidth = 500;
			styleInspectorWindow.WindowHeight = 700;
			
			// Use the callback to create the OS window
			WindowCreator?.Invoke(styleInspectorWindow, "Panel Inspector - Styles");
			Console.WriteLine("[PanelInspector] StyleInspectorWindow created in separate window");
		}
		
		Console.WriteLine("[PanelInspector] Inspector windows opened in separate OS windows");
	}

	private void OpenAsOverlays(RootPanel rootPanel)
	{
		// Original implementation - windows as overlays
		if (panelListWindow == null || !panelListWindow.IsValid())
		{
			Console.WriteLine("[PanelInspector] Creating PanelListWindow as overlay...");
			panelListWindow = new PanelListWindow();
			panelListWindow.Parent = rootPanel;
			panelListWindow.SetTargetRootPanel(rootPanel);
			panelListWindow.PanelSelected += OnPanelSelected;
			panelListWindow.PanelHovered += OnPanelHovered;
			
			// Position on left side
			panelListWindow.Style.Left = 20;
			panelListWindow.Style.Top = 20;
			Console.WriteLine($"[PanelInspector] PanelListWindow created, IsValid={panelListWindow.IsValid()}");
		}

		if (styleInspectorWindow == null || !styleInspectorWindow.IsValid())
		{
			Console.WriteLine("[PanelInspector] Creating StyleInspectorWindow as overlay...");
			styleInspectorWindow = new StyleInspectorWindow();
			styleInspectorWindow.Parent = rootPanel;
			
			// Position on right side, next to panel list
			styleInspectorWindow.Style.Left = 440;
			styleInspectorWindow.Style.Top = 20;
			Console.WriteLine($"[PanelInspector] StyleInspectorWindow created, IsValid={styleInspectorWindow.IsValid()}");
		}
		
		Console.WriteLine("[PanelInspector] Inspector windows opened as overlays");
	}

	/// <summary>
	/// Close the panel inspector windows.
	/// </summary>
	public void Close()
	{
		panelListWindow?.Delete();
		panelListWindow = null;

		styleInspectorWindow?.Delete();
		styleInspectorWindow = null;
	}

	/// <summary>
	/// Toggle the panel inspector windows.
	/// </summary>
	public void Toggle(RootPanel rootPanel)
	{
		if (IsOpen())
			Close();
		else
			Open(rootPanel);
	}

	/// <summary>
	/// Check if the inspector is currently open.
	/// </summary>
	public bool IsOpen()
	{
		return panelListWindow != null && panelListWindow.IsValid();
	}

	private void OnPanelSelected(Panel? panel)
	{
		styleInspectorWindow?.SetSelectedPanel(panel);
	}

	private void OnPanelHovered(Panel? panel)
	{
		// Could highlight the panel in the UI here
	}

	/// <summary>
	/// Update the inspector (call this every frame if the inspector is open).
	/// </summary>
	public void Update()
	{
		// Windows handle their own updates in Tick()
	}
}
