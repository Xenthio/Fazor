namespace Sandbox.UI;

/// <summary>
/// Drag event for drag scrolling and dragging panels.
/// Ported from s&box's Panel/Event/DragEvent.cs
/// </summary>
public class DragEvent : PanelEvent
{
	/// <summary>
	/// For ondrag event - the delta of the mouse movement
	/// </summary>
	public Vector2 MouseDelta;

	/// <summary>
	/// The position on the Target panel where the drag started
	/// </summary>
	public Vector2 LocalGrabPosition;

	/// <summary>
	/// The position relative to the screen where the drag started
	/// </summary>
	public Vector2 ScreenGrabPosition;

	/// <summary>
	/// The current mouse position relative to target
	/// </summary>
	public Vector2 LocalPosition;

	/// <summary>
	/// The current position relative to the screen
	/// </summary>
	public Vector2 ScreenPosition;

	internal DragEvent(string event_name, Panel? active, Vector2 localDragStart, Vector2 globalDragStart) : base(event_name, active)
	{
		Name = event_name;
		Target = active;

		LocalGrabPosition = localDragStart;
		ScreenGrabPosition = globalDragStart;

		LocalPosition = (Target?.MousePosition ?? Vector2.Zero) + (Target?.ScrollOffset ?? Vector2.Zero);
		
		// In S&box this would be Mouse.Position, but we get it from root panel
		if (Target?.FindRootPanel() is RootPanel root)
		{
			ScreenPosition = root.MousePos;
		}
	}
}
