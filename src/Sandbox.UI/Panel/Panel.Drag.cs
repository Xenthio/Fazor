using Sandbox.UI.Utility;

namespace Sandbox.UI;

/// <summary>
/// Panel partial class: Drag scrolling support
/// Ported from s&box's Panel.Drag.cs
/// </summary>
public partial class Panel
{
	/// <summary>
	/// Return true if this panel wants to be dragged
	/// </summary>
	public virtual bool WantsDrag => !ScrollSize.IsNearZeroLength && WantsDragScrolling;

	/// <summary>
	/// Set this to false if you want to opt out of drag scrolling
	/// </summary>
	public bool CanDragScroll { get; set; } = true;

	protected virtual bool WantsDragScrolling
	{
		get
		{
			if (!CanDragScroll)
				return false;

			if (ComputedStyle?.OverflowX == OverflowMode.Scroll)
				return true;

			if (ComputedStyle?.OverflowY == OverflowMode.Scroll)
				return true;

			return false;
		}
	}

	/// <summary>
	/// Find a panel in our hierarchy that wants to be dragged
	/// </summary>
	internal Panel? FindDragTarget()
	{
		if (WantsDrag) return this;
		return Parent?.FindDragTarget();
	}

	/// <summary>
	/// Distribute the drag events to specific virtual functions
	/// </summary>
	void InternalDragEvent(DragEvent e)
	{
		if (e.Is("ondragstart")) OnDragStart(e);
		if (e.Is("ondragend")) OnDragEnd(e);
		if (e.Is("ondrag")) OnDrag(e);
	}

	protected virtual void OnDragStart(DragEvent e)
	{
		if (e.Target != this) return;
		if (ScrollSize.IsNearZeroLength) return;
		if (!WantsDragScrolling) return;

		ScrollVelocity = Vector2.Zero;
		e.StopPropagation();

		IsDragScrolling = true;
	}

	protected virtual void OnDragEnd(DragEvent e)
	{
		IsDragScrolling = false;

		if (e.Target != this) return;
		if (ScrollSize.IsNearZeroLength) return;
		if (!WantsDragScrolling) return;

		// Note: In S&box this uses Mouse.Velocity, which we don't have
		// For now, we'll use a simplified version
		var delta = Vector2.Zero; // Would be: Mouse.Velocity * -6.0f

		if (!HasScrollX) delta.x = 0.0f;
		if (!HasScrollY) delta.y = 0.0f;

		ScrollVelocity += delta;
		e.StopPropagation();
	}

	protected virtual void OnDrag(DragEvent e)
	{
		if (e.Target != this) return;

		if (ScrollSize.IsNearZeroLength) return;
		if (!WantsDragScrolling) return;

		e.StopPropagation();

		var delta = e.LocalGrabPosition - e.LocalPosition;

		// don't drag in directions we don't overflow in
		if (!HasScrollX) delta.x = 0.0f;
		if (!HasScrollY) delta.y = 0.0f;

		ScrollOffset += delta;

		//
		// If we overshot, let us drag out of bounds a little bit, but make it feel
		// tough and resistant to being pulled any more than that.
		//
		{
			Vector2 overShoot = Vector2.Zero;

			if (ScrollOffset.y < 0) overShoot.y = ScrollOffset.y;
			if (ScrollOffset.x < 0) overShoot.x = ScrollOffset.x;
			if (ScrollOffset.y > ScrollSize.y) overShoot.y = ScrollOffset.y - ScrollSize.y;
			if (ScrollOffset.x > ScrollSize.x) overShoot.x = ScrollOffset.x - ScrollSize.x;

			if (!overShoot.IsNearZeroLength)
			{
				float overDrag = 16.0f;
				float overSize = overShoot.Length / (overDrag * 12.0f);
				overSize = Easing.EaseOut(Math.Clamp(overSize, 0.0f, 1.0f));

				ScrollOffset -= overShoot;
				ScrollOffset += overShoot.Normal * overSize * overDrag;
			}
		}
	}
}
