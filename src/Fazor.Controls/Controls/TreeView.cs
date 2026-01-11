using Sandbox;
using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A Win32-inspired TreeView control for hierarchical data display.
/// Port from XGUI-3.
/// </summary>
public partial class TreeView : Panel
{
	public class TreeViewNode : Panel
	{
		public TreeView ParentTreeView { get; }
		public TreeViewNode? ParentNode { get; }
		public object? Data { get; set; }
		public string Text { get; set; }
		public string? IconName { get; set; }

		public List<TreeViewNode> ChildNodes { get; } = new();
		public bool IsExpanded { get; private set; }
		public bool IsSelected { get; private set; }

		public Panel? HeaderRow { get; private set; }
		public FazorIconPanel? IconPanel { get; private set; }
		public Label? NodeLabel { get; private set; }
		public Panel? Expander { get; private set; }
		public Panel? ChildrenContainer { get; private set; }

		private const float IndentSize = 16f; // Indentation per level

		public TreeViewNode( TreeView parentTreeView, TreeViewNode? parentNode, string text, string? iconName = null, object? data = null )
		{
			ParentTreeView = parentTreeView;
			ParentNode = parentNode;
			Text = text;
			IconName = iconName;
			Data = data;

			AddClass( "treeview-node" );
			Render();
		}

		private void Render()
		{
			HeaderRow = AddChild<Panel>( "treeview-node-header" );
			HeaderRow.Style.PaddingLeft = (GetDepth() * IndentSize);

			Expander = HeaderRow.AddChild<Panel>( "treeview-expander" );
			Expander.AddEventListener( "onclick", ToggleExpand );

			IconPanel = HeaderRow.AddChild<FazorIconPanel>();
			IconPanel.AddClass( "treeview-icon" );
			if ( !string.IsNullOrEmpty( IconName ) )
			{
				IconPanel.SetIcon( IconName, 16 );
			}
			else
			{
				IconPanel.Style.Display = DisplayMode.None; // Hide if no icon
			}

			NodeLabel = HeaderRow.AddChild<Label>( "treeview-label" );
			NodeLabel.Text = Text;

			ChildrenContainer = AddChild<Panel>( "treeview-children-container" );
			ChildrenContainer.Style.Display = DisplayMode.None; // Initially hidden

			UpdateExpanderIcon();
		}

		public void SetText( string text )
		{
			Text = text;
			if ( NodeLabel != null )
				NodeLabel.Text = text;
		}

		public void SetIcon( string iconName )
		{
			IconName = iconName;
			if ( IconPanel != null )
			{
				if ( !string.IsNullOrEmpty( IconName ) )
				{
					IconPanel.SetIcon( IconName, 16 );
					IconPanel.Style.Display = DisplayMode.Flex;
				}
				else
				{
					IconPanel.Style.Display = DisplayMode.None;
				}
			}
		}

		public int GetDepth()
		{
			int depth = 0;
			var current = ParentNode;
			while ( current != null )
			{
				depth++;
				current = current.ParentNode;
			}
			return depth;
		}

		public TreeViewNode AddChild( string text, string? iconName = null, object? data = null )
		{
			var childNode = new TreeViewNode( ParentTreeView, this, text, iconName, data );
			ChildNodes.Add( childNode );
			ChildrenContainer?.AddChild( childNode );
			UpdateExpanderIcon();
			return childNode;
		}

		public void RemoveChild( TreeViewNode childNode )
		{
			if ( ChildNodes.Remove( childNode ) )
			{
				childNode.Delete();
				UpdateExpanderIcon();
			}
		}

		public void ClearChildren()
		{
			foreach ( var child in ChildNodes.ToList() )
			{
				child.Delete();
			}
			ChildNodes.Clear();
			UpdateExpanderIcon();
		}

		public void ToggleExpand( PanelEvent e )
		{
			if ( !ChildNodes.Any() ) return;

			IsExpanded = !IsExpanded;
			if ( ChildrenContainer != null )
				ChildrenContainer.Style.Display = IsExpanded ? DisplayMode.Flex : DisplayMode.None;
			UpdateExpanderIcon();

			if ( IsExpanded )
				ParentTreeView.OnNodeExpanded?.Invoke( this );
			else
				ParentTreeView.OnNodeCollapsed?.Invoke( this );

			e.StopPropagation();
		}

		public void Expand()
		{
			if ( IsExpanded || !ChildNodes.Any() ) return;
			IsExpanded = true;
			if ( ChildrenContainer != null )
				ChildrenContainer.Style.Display = DisplayMode.Flex;
			UpdateExpanderIcon();
			ParentTreeView.OnNodeExpanded?.Invoke( this );
		}

		public void Collapse()
		{
			if ( !IsExpanded || !ChildNodes.Any() ) return;
			IsExpanded = false;
			if ( ChildrenContainer != null )
				ChildrenContainer.Style.Display = DisplayMode.None;
			UpdateExpanderIcon();
			ParentTreeView.OnNodeCollapsed?.Invoke( this );
		}

		public void ExpandAll()
		{
			Expand();
			foreach ( var child in ChildNodes )
			{
				child.ExpandAll();
			}
		}

		public void CollapseAll()
		{
			Collapse();
			foreach ( var child in ChildNodes )
			{
				child.CollapseAll();
			}
		}

		private void UpdateExpanderIcon()
		{
			if ( Expander == null ) return;
			
			if ( !ChildNodes.Any() )
			{
				Expander.SetClass( "empty", true );
				Expander.SetClass( "expanded", false );
				Expander.SetClass( "collapsed", false );
			}
			else
			{
				Expander.SetClass( "empty", false );
				Expander.SetClass( "expanded", IsExpanded );
				Expander.SetClass( "collapsed", !IsExpanded );
			}
		}

		private bool IsEventTargetExpanderOrChild( Panel eventTarget )
		{
			if ( eventTarget == null || Expander == null ) return false;
			if ( eventTarget == Expander ) return true;

			var parent = eventTarget.Parent;
			while ( parent != null )
			{
				if ( parent == Expander ) return true;
				parent = parent.Parent;
			}
			return false;
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );

			if ( IsEventTargetExpanderOrChild( e.Target as Panel ) )
			{
				// ToggleExpand is already called by the event listener on Expander
			}
			else
			{
				ParentTreeView.SelectItem( this );
			}
			e.StopPropagation();
		}

		protected override void OnRightClick( MousePanelEvent e )
		{
			base.OnRightClick( e );
			ParentTreeView.SelectItem( this );
			ParentTreeView.OnNodeRightClick?.Invoke( this, e );
			e.StopPropagation();
		}

		protected override void OnDoubleClick( MousePanelEvent e )
		{
			base.OnDoubleClick( e );
			if ( !IsEventTargetExpanderOrChild( e.Target as Panel ) )
			{
				ParentTreeView.OnNodeActivated?.Invoke( this );
			}
		}

		public void SetSelected( bool selected )
		{
			IsSelected = selected;
			SetClass( "selected", selected );
		}
	}

	public Panel? ItemContainer { get; private set; }
	public List<TreeViewNode> RootNodes { get; } = new();
	public TreeViewNode? SelectedNode { get; private set; }

	public Action<TreeViewNode>? OnNodeSelected { get; set; }
	public Action<TreeViewNode>? OnNodeActivated { get; set; } // Double-click
	public Action<TreeViewNode>? OnNodeExpanded { get; set; }
	public Action<TreeViewNode>? OnNodeCollapsed { get; set; }
	public Action<TreeViewNode, MousePanelEvent>? OnNodeRightClick { get; set; }

	public TreeView()
	{
		AddClass( "treeview" );
		// Use Panel instead of ScrollPanel - scrolling will be handled by CSS
		ItemContainer = AddChild<Panel>( "treeview-item-container" );
		ItemContainer.Style.OverflowY = OverflowMode.Scroll;
		ItemContainer.Style.OverflowX = OverflowMode.Scroll;
	}

	public TreeViewNode AddRootNode( string text, string? iconName = null, object? data = null )
	{
		var node = new TreeViewNode( this, null, text, iconName, data );
		RootNodes.Add( node );
		ItemContainer?.AddChild( node );
		return node;
	}

	public void RemoveRootNode( TreeViewNode node )
	{
		if ( RootNodes.Remove( node ) )
		{
			node.Delete();
		}
	}

	public void ClearNodes()
	{
		foreach ( var node in RootNodes.ToList() )
		{
			node.Delete();
		}
		RootNodes.Clear();
		SelectedNode = null;
	}

	public void SelectItem( TreeViewNode? node )
	{
		if ( SelectedNode == node && node != null )
		{
			return;
		}

		if ( SelectedNode != null )
		{
			SelectedNode.SetSelected( false );
		}

		SelectedNode = node;

		if ( SelectedNode != null )
		{
			SelectedNode.SetSelected( true );
		}
		OnNodeSelected?.Invoke( SelectedNode );
	}

	public TreeViewNode? FindNode( Func<TreeViewNode, bool> predicate )
	{
		foreach ( var rootNode in RootNodes )
		{
			var found = FindNodeRecursive( rootNode, predicate );
			if ( found != null ) return found;
		}
		return null;
	}

	private TreeViewNode? FindNodeRecursive( TreeViewNode currentNode, Func<TreeViewNode, bool> predicate )
	{
		if ( predicate( currentNode ) )
		{
			return currentNode;
		}
		foreach ( var child in currentNode.ChildNodes )
		{
			var found = FindNodeRecursive( child, predicate );
			if ( found != null ) return found;
		}
		return null;
	}
}
