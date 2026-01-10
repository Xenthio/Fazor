using Sandbox.UI;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Window that shows style properties for the selected panel.
/// Based on s&box's StyleInspector.cs and StyleEditor.cs (MIT Licensed)
/// </summary>
public class StyleInspectorWindow : Window
{
	private Panel? selectedPanel;
	private Panel? contentContainer;
	private Panel? pseudoClassToolbar;
	private Panel? stylesContainer;
	private PanelHighlight? selectedHighlight;
	
	private Button? hoverButton;
	private Button? activeButton;
	private Button? focusButton;

	public StyleInspectorWindow() : base()
	{
		Title = "Style Inspector";
		WindowWidth = 500;
		WindowHeight = 700;
		
		// Add inspector window classes
		AddClass("inspector-window");
		AddClass("style-inspector-window");
		
		// Load stylesheet - use path relative to application base directory
		var stylesheetPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "themes", "PanelInspector.scss");
		StyleSheet.Load(stylesheetPath, failSilently: false);
		
		BuildUI();
	}

	private void BuildUI()
	{
		// Pseudo-class toolbar
		pseudoClassToolbar = new Panel(this);
		pseudoClassToolbar.AddClass("toolbar");
		pseudoClassToolbar.AddClass("pseudo-class-toolbar");

		hoverButton = CreatePseudoClassButton(":hover", PseudoClass.Hover);
		activeButton = CreatePseudoClassButton(":active", PseudoClass.Active);
		focusButton = CreatePseudoClassButton(":focus", PseudoClass.Focus);

		// Styles container (scrollable)
		stylesContainer = new Panel(this);
		stylesContainer.AddClass("styles-container");
		stylesContainer.Style.PaddingRight = 8;
		stylesContainer.Style.PaddingTop = 8;
		stylesContainer.Style.PaddingBottom = 8;
	}

	private Button CreatePseudoClassButton(string label, PseudoClass pseudoClass)
	{
		var button = new Button();
		button.Parent = pseudoClassToolbar;
		button.Text = label;
		button.AddClass("pseudo-class-button");
		button.AddEventListener("onclick", () => TogglePseudoClass(pseudoClass, button));
		return button;
	}

	private void TogglePseudoClass(PseudoClass pseudoClass, Button button)
	{
		if (selectedPanel == null) return;

		bool hasClass = (selectedPanel.PseudoClass & pseudoClass) != 0;
		
		if (hasClass)
			selectedPanel.PseudoClass &= ~pseudoClass;
		else
			selectedPanel.PseudoClass |= pseudoClass;

		button.SetClass("active", !hasClass);
		
		// Rebuild to show updated styles
		Rebuild();
	}

	public void SetSelectedPanel(Panel? panel)
	{
		selectedPanel = panel;
		
		// Update the selected highlight
		if (selectedHighlight == null && panel != null)
		{
			var root = panel.FindRootPanel();
			if (root != null)
			{
				selectedHighlight = new PanelHighlight(root);
			}
		}
		
		if (selectedHighlight != null)
		{
			// Yellow/orange color for selected
			var selectedColor = new Color(1.0f, 0.6f, 0.0f, 0.9f);
			selectedHighlight.SetTarget(panel, selectedColor);
		}
		
		Rebuild();
	}

	private void Rebuild()
	{
		if (stylesContainer == null) return;
		
		stylesContainer.DeleteChildren();

		if (selectedPanel == null || !selectedPanel.IsValid())
		{
			var emptyLabel = new Label("No panel selected");
			emptyLabel.Parent = stylesContainer;
			return;
		}

		// Update pseudo-class button states
		UpdatePseudoClassButtons();

		// Show panel info
		var infoPanel = new Panel(stylesContainer);
		infoPanel.AddClass("panel-info");
		
		var titleLabel = new Label($"<{selectedPanel.ElementName}>");
		titleLabel.Parent = infoPanel;
		titleLabel.AddClass("panel-title");

		if (!string.IsNullOrEmpty(selectedPanel.Id))
		{
			var idLabel = new Label($"ID: {selectedPanel.Id}");
			idLabel.Parent = infoPanel;
		}

		var classes = selectedPanel.Classes;
		if (!string.IsNullOrEmpty(classes))
		{
			var classLabel = new Label($"Classes: {classes}");
			classLabel.Parent = infoPanel;
		}

		// Show active style blocks
		var styleBlocks = selectedPanel.ActiveStyleBlocks.Reverse().ToList();
		
		if (styleBlocks.Count == 0)
		{
			var noStylesLabel = new Label("No styles applied");
			noStylesLabel.Parent = stylesContainer;
			return;
		}

		foreach (var block in styleBlocks)
		{
			BuildStyleBlock(block);
		}
	}

	private void UpdatePseudoClassButtons()
	{
		if (selectedPanel == null) return;

		hoverButton?.SetClass("active", (selectedPanel.PseudoClass & PseudoClass.Hover) != 0);
		activeButton?.SetClass("active", (selectedPanel.PseudoClass & PseudoClass.Active) != 0);
		focusButton?.SetClass("active", (selectedPanel.PseudoClass & PseudoClass.Focus) != 0);
	}

	private void BuildStyleBlock(IStyleBlock block)
	{
		if (stylesContainer == null || block == null) return;

		// Block container
		var blockContainer = new Panel(stylesContainer);
		blockContainer.AddClass("style-block");
		blockContainer.Style.PaddingLeft = 8;
		blockContainer.Style.PaddingRight = 8;
		blockContainer.Style.PaddingTop = 8;
		blockContainer.Style.PaddingBottom = 8;

		// Selectors
		var selectorContainer = new Panel(blockContainer);
		selectorContainer.Style.FlexDirection = FlexDirection.Row;
		selectorContainer.Style.JustifyContent = Justify.SpaceBetween;
		selectorContainer.Style.AlignItems = Align.Center;

		var selectorLabel = new Label(string.Join(", ", block.SelectorStrings));
		selectorLabel.Parent = selectorContainer;
		selectorLabel.AddClass("selector-label");

		// File location
		if (!string.IsNullOrEmpty(block.FileName))
		{
			var fileName = System.IO.Path.GetFileName(block.FileName);
			var fileLabel = new Label($"{fileName}:{block.FileLine}");
			fileLabel.Parent = selectorContainer;
			fileLabel.AddClass("file-label");
		}

		// Opening brace
		var openBrace = new Label("{");
		openBrace.Parent = blockContainer;
		openBrace.AddClass("brace");

		// Properties
		var properties = block.GetRawValues();
		foreach (var prop in properties)
		{
			var row = new StyleRow(block, prop);
			row.Parent = blockContainer;
		}

		// Closing brace
		var closeBrace = new Label("}");
		closeBrace.Parent = blockContainer;
		closeBrace.AddClass("brace");

		// Separator
		var separator = new Panel(stylesContainer);
		separator.AddClass("separator");
		separator.Style.Height = 1;
		separator.Style.BackgroundColor = new Color(0.3f, 0.3f, 0.3f, 1.0f);
	}

	public override void Tick()
	{
		base.Tick();

		// Check if panel is still valid
		if (selectedPanel != null && !selectedPanel.IsValid())
		{
			SetSelectedPanel(null);
		}
	}
}
