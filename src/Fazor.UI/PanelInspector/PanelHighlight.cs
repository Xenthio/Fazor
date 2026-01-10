using Sandbox.UI;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Visual highlight that shows a border around the currently inspected panel.
/// </summary>
public class PanelHighlight : Panel
{
	private Panel? targetPanel;
	private Color highlightColor;

	public PanelHighlight(Panel? parent = null) : base(parent)
	{
		ElementName = "panelhighlight";
		AddClass("panel-highlight");
		
		// Positioning and styling
		Style.Position = PositionMode.Absolute;
		Style.PointerEvents = PointerEvents.None; // Don't intercept clicks
		Style.ZIndex = 10000; // Above everything else
		Style.BorderWidth = 2;
		Style.BorderColor = new Color(0.0f, 0.8f, 1.0f, 0.9f); // Cyan
		Style.BackgroundColor = new Color(0.0f, 0.8f, 1.0f, 0.1f); // Light cyan overlay
		
		highlightColor = new Color(0.0f, 0.8f, 1.0f, 0.9f);
		Style.Display = DisplayMode.None;
	}

	public void SetTarget(Panel? panel, Color? color = null)
	{
		targetPanel = panel;
		
		if (color.HasValue)
		{
			highlightColor = color.Value;
			Style.BorderColor = highlightColor;
			Style.BackgroundColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0.1f);
		}
		
		Style.Display = (panel != null && panel.IsValid()) ? DisplayMode.Flex : DisplayMode.None;
		UpdatePosition();
	}

	public override void Tick()
	{
		base.Tick();
		
		if (targetPanel == null || !targetPanel.IsValid())
		{
			Style.Display = DisplayMode.None;
			return;
		}
		
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (targetPanel == null || !targetPanel.IsValid())
			return;

		// Position the highlight over the target panel
		var rect = targetPanel.Box.Rect;
		
		Style.Left = rect.Left;
		Style.Top = rect.Top;
		Style.Width = rect.Width;
		Style.Height = rect.Height;
	}
}
