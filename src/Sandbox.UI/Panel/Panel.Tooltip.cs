using Microsoft.AspNetCore.Components;

namespace Sandbox.UI;

/// <summary>
/// Panel partial class: Tooltip support
/// Ported from s&box's Panel.Tooltip.cs
/// </summary>
public partial class Panel
{
	/// <summary>
	/// A string to show when hovering over this panel.
	/// </summary>
	[Parameter]
	public string? Tooltip { get; set; }

	/// <summary>
	/// The created tooltip element will have this class, if set.
	/// </summary>
	[Parameter]
	public string? TooltipClass { get; set; }

	/// <summary>
	/// You should override and return true if you're overriding <see cref="CreateTooltipPanel"/>.
	/// Otherwise this will return true if <see cref="Tooltip"/> is not empty.
	/// </summary>
	public virtual bool HasTooltip => !string.IsNullOrWhiteSpace(Tooltip);

	/// <summary>
	/// Create a tooltip panel. You can override this to create a custom tooltip panel.
	/// If you're overriding this and not setting <see cref="Tooltip"/>, then you must override and return true in <see cref="HasTooltip"/>.
	/// </summary>
	protected virtual Panel? CreateTooltipPanel()
	{
		if (string.IsNullOrWhiteSpace(Tooltip))
			return null;

		var p = new Panel(null);
		p.AddClass("tooltip");
		p.AddClass(TooltipClass);
		p.SetProperty("style", "position: absolute; pointer-events: none; z-index: 10000;");

		var textContents = new Label
		{
			Parent = p,
			Text = Tooltip
		};

		p.Parent = FindRootPanel();

		return p;
	}
}
