namespace Sandbox.UI.Construct;

/// <summary>
/// Used for <see cref="Panel.Add"/> for quick panel creation with certain settings. Other panels types are added via extension methods.
/// Ported from s&box's Utility/PanelCreator.cs
/// </summary>
public ref struct PanelCreator
{
	/// <summary>
	/// The panel to add children to.
	/// </summary>
	public Panel panel;

	internal PanelCreator(Panel panel)
	{
		this.panel = panel;
	}

	/// <summary>
	/// Add a new blank panel as a child.
	/// </summary>
	/// <returns>The created panel.</returns>
	public Panel Panel()
	{
		return panel.AddChild<Panel>();
	}

	/// <summary>
	/// Add a new blank panel with given CSS classes as a child.
	/// </summary>
	/// <returns>The created panel.</returns>
	public Panel Panel(string? classname)
	{
		var control = panel.AddChild<Panel>();
		if (classname != null)
			control.AddClass(classname);
		return control;
	}

	/// <summary>
	/// Create a simple text label with given text and CSS classname.
	/// </summary>
	public Label Label(string? text = null, string? classname = null)
	{
		var control = panel.AddChild<Label>();

		if (text != null)
			control.Text = text;

		if (classname != null)
			control.AddClass(classname);

		return control;
	}

	/// <summary>
	/// Create an image panel with given texture path and CSS classname.
	/// </summary>
	public Image Image(string? texturePath = null, string? classname = null)
	{
		var control = panel.AddChild<Image>();

		if (texturePath != null)
			control.SetTexture(texturePath);

		if (classname != null)
			control.AddClass(classname);

		return control;
	}
}
