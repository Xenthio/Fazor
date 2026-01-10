using Sandbox.UI;

using System.Numerics;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Inspector window that shows the panel hierarchy.
/// Based on s&box's PanelList.cs (MIT Licensed)
/// </summary>
public class PanelListWindow : Window
{
	private Panel? contentContainer;
	private TextEntry? filterInput;
	private Button? refreshButton;
	private Button? pickerButton;
	private Panel? treeContainer;
	
	private RootPanel? targetRootPanel;
	private Panel? selectedPanel;
	private Panel? hoveredPanel;
	private PickerOverlay? pickerOverlay;
	private bool pickerActive;

	public event Action<Panel?>? PanelSelected;
	public event Action<Panel?>? PanelHovered;

	public PanelListWindow() : base()
	{
		Title = "Panel Inspector";
		WindowWidth = 400;
		WindowHeight = 600;
		
		// Add inspector window classes
		AddClass("inspector-window");
		AddClass("panel-list-window");
		
		// Load stylesheet - use path relative to application base directory
		var stylesheetPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "themes", "PanelInspector.scss");
		StyleSheet.Load(stylesheetPath, failSilently: false);
		
		BuildUI();
	}

	private void BuildUI()
	{
		// Create toolbar
		var toolbar = new Panel(this);
		toolbar.AddClass("toolbar");

		filterInput = new TextEntry();
		filterInput.Parent = toolbar;
		filterInput.Placeholder = "Filter panels...";
		filterInput.Style.FlexGrow = 1;

		pickerButton = new Button();
		pickerButton.Parent = toolbar;
		pickerButton.Text = "Pick";
		pickerButton.AddClass("picker-button");
		pickerButton.AddEventListener("onclick", TogglePicker);

		refreshButton = new Button();
		refreshButton.Parent = toolbar;
		refreshButton.Text = "Refresh";
		refreshButton.AddEventListener("onclick", Rebuild);

		// Create tree container
		treeContainer = new Panel(this);
		treeContainer.AddClass("tree-container");
		treeContainer.Style.FlexGrow = 1;
		treeContainer.Style.Overflow = OverflowMode.Scroll;
	}

	public void SetTargetRootPanel(RootPanel? root)
	{
		targetRootPanel = root;
		
		// Create picker overlay if needed
		if (root != null && pickerOverlay == null)
		{
			pickerOverlay = new PickerOverlay(root);
			pickerOverlay.Deactivate(); // Start inactive
		}
		
		Rebuild();
	}

	private void Rebuild()
	{
		if (treeContainer == null) return;
		
		treeContainer.DeleteChildren();

		if (targetRootPanel == null || !targetRootPanel.IsValid())
			return;

		BuildPanelTree(targetRootPanel, treeContainer, 0);
	}

	private void BuildPanelTree(Panel panel, Panel container, int depth)
	{
		if (panel == null || !panel.IsValid())
			return;

		var nodeContainer = new Panel(container);
		nodeContainer.AddClass("node-row");
		nodeContainer.Style.PaddingLeft = depth * 20;

		new PanelTreeNode(panel, nodeContainer);
		
		// Make it clickable
		nodeContainer.AddEventListener("onclick", (PanelEvent e) =>
		{
			SelectPanel(panel);
		});

		// Build children - snapshot the collection first to avoid modification during enumeration
		if (panel.ChildrenCount > 0)
		{
			var children = panel.Children.ToList(); // Create a snapshot
			foreach (var child in children)
			{
				BuildPanelTree(child, container, depth + 1);
			}
		}
	}

	private void SelectPanel(Panel? panel)
	{
		selectedPanel = panel;
		PanelSelected?.Invoke(panel);
	}

	private void TogglePicker()
	{
		pickerActive = !pickerActive;
		pickerButton?.SetClass("active", pickerActive);
		
		if (pickerActive && targetRootPanel != null && pickerOverlay != null)
		{
			// Activate picker overlay
			pickerOverlay.Activate(targetRootPanel, OnPickerPanelClicked);
		}
		else if (pickerOverlay != null)
		{
			// Deactivate picker overlay
			pickerOverlay.Deactivate();
		}
	}

	private void OnPickerPanelClicked(Panel? panel)
	{
		// Deactivate picker mode
		pickerActive = false;
		pickerButton?.SetClass("active", false);
		pickerOverlay?.Deactivate();
		
		if (panel == null) return; // Cancelled via Escape key
		
		// Select the panel
		SelectPanel(panel);
		
		// Rebuild tree to show selection
		Rebuild();
		
		// Scroll to the selected panel in the tree
		// Scroll to the selected panel in the tree (not yet implemented)
	}

	public override void Tick()
	{
		base.Tick();
		
	}

	public override void OnDeleted()
	{
		base.OnDeleted();
		pickerActive = false;
		pickerOverlay?.Delete();
		pickerOverlay = null;
	}
}
