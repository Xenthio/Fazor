using Sandbox.UI;
using Fazor.Controls;

namespace Fazor.UI.PanelInspector;

/// <summary>
/// Represents a single style property row in the style inspector.
/// Based on s&box's StyleRow.cs (MIT Licensed)
/// </summary>
public class StyleRow : Panel
{
	private readonly IStyleBlock styleBlock;
	private IStyleBlock.StyleProperty property;
	private Label? nameLabel;
	private Label? valueLabel;
	private TextEntry? valueEditor;
	private Button? saveButton;
	private Button? restoreButton;
	private bool isEditing;

	public StyleRow(IStyleBlock block, IStyleBlock.StyleProperty prop) : base()
	{
		ElementName = "stylerow";
		AddClass("style-row");
		
		styleBlock = block;
		property = prop;
		
		BuildUI();
	}

	private void BuildUI()
	{
		// Mark as invalid if needed
		SetClass("invalid", !property.IsValid);
		
		// Property name
		nameLabel = new Label($"{property.Name}");
		nameLabel.Parent = this;
		nameLabel.AddClass("property-name");
		
		var colonLabel = new Label(":");
		colonLabel.Parent = this;
		colonLabel.AddClass("property-colon");

		// Property value (editable)
		valueLabel = new Label(property.Value ?? "");
		valueLabel.Parent = this;
		valueLabel.AddClass("property-value");
		valueLabel.AddEventListener("onclick", (PanelEvent e) => StartEditing());

		// Action buttons container
		var buttonContainer = new Panel(this);
		buttonContainer.AddClass("button-container");

		saveButton = new Button();
		saveButton.Parent = buttonContainer;
		saveButton.Text = "Save";
		saveButton.Style.Display = (property.Value != property.OriginalValue) ? DisplayMode.Flex : DisplayMode.None;
		saveButton.AddEventListener("onclick", SaveChanges);

		restoreButton = new Button();
		restoreButton.Parent = buttonContainer;
		restoreButton.Text = "Restore";
		restoreButton.Style.Display = (property.Value != property.OriginalValue) ? DisplayMode.Flex : DisplayMode.None;
		restoreButton.AddEventListener("onclick", RestoreValue);
	}

	private void StartEditing()
	{
		if (isEditing || valueLabel == null) return;
		
		isEditing = true;
		valueLabel.Style.Display = DisplayMode.None;

		valueEditor = new TextEntry();
		valueEditor.Parent = this;
		valueEditor.Text = property.Value;
		valueEditor.Style.FlexGrow = 1;
		valueEditor.Focus();
		
		// Setup event handlers - simplified since we don't have all the events yet
		valueEditor.AddEventListener("onblur", (PanelEvent e) => FinishEditing());
	}

	private void FinishEditing()
	{
		if (!isEditing || valueEditor == null || valueLabel == null) return;
		
		UpdateValue(valueEditor.Text);
		
		valueEditor.Delete();
		valueEditor = null;
		valueLabel.Style.Display = DisplayMode.Flex;
		isEditing = false;
	}

	private void UpdateValue(string newValue)
	{
		newValue = newValue.TrimEnd(';', ' ');
		

		property.Value = newValue;
		bool success = styleBlock.SetRawValue(property.Name, property.Value);
		property.IsValid = success;

		if (valueLabel != null)
			valueLabel.Text = property.Value;

		if (nameLabel != null)
			nameLabel.SetClass("invalid", !property.IsValid);

		if (saveButton != null)
			saveButton.Style.Display = (property.Value != property.OriginalValue) ? DisplayMode.Flex : DisplayMode.None;

		if (restoreButton != null)
			restoreButton.Style.Display = (property.Value != property.OriginalValue) ? DisplayMode.Flex : DisplayMode.None;
	}

	private void SaveChanges()
	{
		// Read the file
		try
		{
			var lines = System.IO.File.ReadAllLines(styleBlock.AbsolutePath);
			if (property.Line < 0 || property.Line >= lines.Length)
				return;

			var line = lines[property.Line];

			// Find the property name
			var nameIndex = line.IndexOf(property.Name, System.StringComparison.OrdinalIgnoreCase);
			if (nameIndex == -1) return;

			// Find the colon after the property name
			var colonIndex = line.IndexOf(':', nameIndex);
			if (colonIndex == -1) return;

			// Find the original value
			var valueIndex = line.IndexOf(property.OriginalValue, colonIndex);
			if (valueIndex == -1) return;

			// Replace the value
			line = line.Remove(valueIndex, property.OriginalValue.Length);
			line = line.Insert(valueIndex, property.Value);

			// Write back
			lines[property.Line] = line;
			System.IO.File.WriteAllLines(styleBlock.AbsolutePath, lines);

			// Update original value
			property.OriginalValue = property.Value;
			styleBlock.SetRawValue(property.Name, property.Value, property.Value);

			if (saveButton != null) saveButton.Style.Display = DisplayMode.None;
			if (restoreButton != null) restoreButton.Style.Display = DisplayMode.None;
		}
		catch (System.Exception ex)
		{
			Console.WriteLine($"Error saving style changes: {ex.Message}");
		}
	}

	private void RestoreValue()
	{
		UpdateValue(property.OriginalValue);
		
		if (saveButton != null) saveButton.Style.Display = DisplayMode.None;
		if (restoreButton != null) restoreButton.Style.Display = DisplayMode.None;
	}
}
