namespace Sandbox.UI;

/// <summary>
/// Child ordering and CSS 'order' property support.
/// Ported from s&box engine/Sandbox.Engine/Systems/UI/Panel/Panel.Order.cs
/// Note: SetChildIndex is in Panel.Children.cs to avoid duplication
/// Note: SortChildrenOrder is in Panel.Layout.cs (called from PreLayout)
/// </summary>
public partial class Panel
{
	int? LastOrder;

	/// <summary>
	/// Check if the CSS 'order' property changed and mark children for re-ordering if needed.
	/// </summary>
	internal void UpdateOrder()
	{
		if (ComputedStyle?.Order == LastOrder) return;

		LastOrder = ComputedStyle?.Order;
		Parent?.DirtyChildrenOrder();
	}

	bool NeedsOrderSort;

	/// <summary>
	/// Mark this panel's children as needing to be re-ordered.
	/// </summary>
	internal void DirtyChildrenOrder()
	{
		NeedsOrderSort = true;
	}

	/// <summary>
	/// Move this panel to be after the given sibling.
	/// </summary>
	public void MoveAfterSibling(Panel previousSibling)
	{
		if (Parent == null)
			throw new ArgumentException("Can't move after sibling if we have no parent");

		if (previousSibling.Parent != this.Parent)
			throw new ArgumentException("previousSibling doesn't share a parent with us");

		if (Parent.IndexesDirty)
			Parent.UpdateChildrenIndexes();

		// Already in the correct position
		if (previousSibling.SiblingIndex == SiblingIndex - 1)
			return;

		Parent.SetChildIndex(this, previousSibling.SiblingIndex);
	}
}
