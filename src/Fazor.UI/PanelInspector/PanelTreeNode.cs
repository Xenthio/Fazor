using Sandbox.UI;
using Fazor.Controls;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Represents a panel node in the inspector tree view.
/// Based on s&box's PanelNode.cs (MIT Licensed)
/// </summary>
public class PanelTreeNode : Panel
{
	public Panel? TargetPanel { get; private set; }
	private Label? labelElement;
	private bool isExpanded;

	public PanelTreeNode(Panel? parent = null) : base(parent)
	{
		ElementName = "paneltreenode";
		AddClass("tree-node");
	}

	public PanelTreeNode(Panel targetPanel, Panel? parent = null) : this(parent)
	{
		TargetPanel = targetPanel;
		BuildUI();
	}

	private void BuildUI()
	{
		if (TargetPanel == null) return;

		labelElement = new Label(GetPanelDescription())
		{
			Parent = this
		};
	}

	private string GetPanelDescription()
	{
		if (TargetPanel == null) return "";

		var name = TargetPanel.ElementName;
		var desc = $"<{name}";

		if (!string.IsNullOrEmpty(TargetPanel.Id))
			desc += $" id=\"{TargetPanel.Id}\"";

		// Classes is now a string property (space-separated)
		var classes = TargetPanel.Classes;
		if (!string.IsNullOrEmpty(classes))
			desc += $" class=\"{classes}\"";

		desc += ">";

		if (!string.IsNullOrEmpty(TargetPanel.SourceFile))
		{
			var fileName = System.IO.Path.GetFileName(TargetPanel.SourceFile);
			desc += $" {fileName}:{TargetPanel.SourceLine}";
		}

		return desc;
	}

	public override void Tick()
	{
		base.Tick();

		// Update visual state based on target panel
		if (TargetPanel != null)
		{
			SetClass("visible", TargetPanel.IsVisible);
			SetClass("hovered", TargetPanel.HasHovered);

			// Update text if properties changed
			if (labelElement != null)
			{
				var newDesc = GetPanelDescription();
				if (labelElement.Text != newDesc)
					labelElement.Text = newDesc;
			}
		}
	}
}
