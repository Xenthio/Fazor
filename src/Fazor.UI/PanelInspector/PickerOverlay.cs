using Sandbox.UI;

using System.Numerics;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Overlay panel that shows visual feedback for the element picker.
/// Highlights the currently hovered panel and handles click selection.
/// </summary>
public class PickerOverlay : Panel
{
	private Panel? highlightedPanel;
	private Panel? targetRootPanel;
	private Action<Panel?>? onPanelClicked;
	private readonly PanelHighlight? highlight;
	private bool isActive;

	public PickerOverlay(Panel? parent = null) : base(parent)
	{
		ElementName = "pickeroverlay";
		AddClass("picker-overlay");
		
		// Make this overlay cover the entire window
		Style.Position = PositionMode.Absolute;
		Style.Left = 0;
		Style.Top = 0;
		Style.Width = Length.Percent(100);
		Style.Height = Length.Percent(100);
		Style.PointerEvents = PointerEvents.All;
		Style.ZIndex = 9999; // Very high z-index to be on top
		Style.BackgroundColor = new Color(0, 0, 0, 0); // Transparent
		
		// Create highlight
		highlight = new PanelHighlight(this);
	}

	public void Activate(Panel rootPanel, Action<Panel?> onPanelClicked)
	{
		this.targetRootPanel = rootPanel;
		this.onPanelClicked = onPanelClicked;
		isActive = true;
		Style.Display = DisplayMode.Flex;
	}

	public void Deactivate()
	{
		isActive = false;
		Style.Display = DisplayMode.None;
		highlightedPanel = null;
		highlight?.SetTarget(null);
	}

	public override void Tick()
	{
		base.Tick();

		if (!isActive || targetRootPanel == null)
			return;

		// Get the mouse position relative to the root panel
		var mousePos = MousePosition;
		
		// Find the panel at the mouse position (exclude the inspector windows and this overlay)
		var panelAtMouse = FindPanelAtPosition(targetRootPanel, mousePos);
		
		if (panelAtMouse != highlightedPanel)
		{
			highlightedPanel = panelAtMouse;
			
			// Update highlight visual
			if (highlight != null)
			{
				var highlightColor = new Color(0.0f, 0.8f, 1.0f, 0.9f); // Cyan for hover
				highlight.SetTarget(highlightedPanel, highlightColor);
			}
		}
	}

	private Panel? FindPanelAtPosition(Panel root, Sandbox.UI.Vector2 position)
	{
		// Don't pick inspector windows or the overlay itself
		var panel = root.GetPanelAt(position, true, false);
		
		// Filter out inspector-related panels
		while (panel != null)
		{
			if (panel == this)
			{
				panel = panel.Parent;
				continue;
			}
			
			// Check if this panel or any ancestor is an inspector window
			var current = panel;
			bool isInspectorPanel = false;
			while (current != null)
			{
				if (current.ElementName == "panelinspectorwindow" || 
				    current.ElementName == "panellistwindow" || 
				    current.ElementName == "styleinspectorwindow" ||
				    current.ElementName == "pickeroverlay")
				{
					isInspectorPanel = true;
					break;
				}
				current = current.Parent;
			}
			
			if (isInspectorPanel)
			{
				panel = panel.Parent;
			}
			else
			{
				break;
			}
		}
		
		return panel;
	}

	public override void OnButtonEvent(ButtonEvent e)
	{
		if (!isActive)
		{
			base.OnButtonEvent(e);
			return;
		}

		// Handle left click to select panel
		if (e.Button == "mouseleft" && e.Pressed && highlightedPanel != null)
		{
			onPanelClicked?.Invoke(highlightedPanel);
			e.StopPropagation = true;
			return;
		}
		
		// Handle Escape key to cancel picker mode
		if (e.Button == "escape" && e.Pressed)
		{
			onPanelClicked?.Invoke(null);
			e.StopPropagation = true;
			return;
		}

		// Stop all events from propagating when picker is active
		e.StopPropagation = true;
	}
}
