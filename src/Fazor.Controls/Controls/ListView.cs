using Sandbox;
using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A Win32-inspired ListView control for XGUI, usable from C# and Razor.
/// Port from XGUI-3.
/// </summary>
[Library("listview")]
public class ListView : Panel
{
	public enum ListViewMode
	{
		List,
		Details,
		Icons
	}

	public class ListViewColumn
	{
		public string Header { get; set; } = "";
		public string Field { get; set; } = "";
		public int Width { get; set; } = 100;
	}

	public class ListViewItem : Panel
	{
		private ListView ParentListView { get; }
		public object? Data { get; }
		public List<string> SubItems { get; }
		public bool IsSelected { get; private set; }
		private List<Panel> Cells { get; } = new();
		public FazorIconPanel? IconPanel;
		private TextEntry? _renameEntry;

		public ListViewItem( ListView parent, object? data, List<string> subItems )
		{
			ParentListView = parent;
			Data = data;
			SubItems = subItems;

			AddClass( "listview-item" );

			// Initial render based on parent's current view mode
			UpdateViewMode( ParentListView.ViewMode );
		}

		public bool Draggable = true;

		private bool _isDragging = false;
		private bool _leftMouseDown = false;
		private Vector2 _dragStartScreen;
		private Vector2 _dragStartLocal;
		// Invokable events
		public Action<ItemDragEvent>? OnDragStartEvent;
		public Action<ItemDragEvent>? OnDragEvent;
		public Action<ItemDragEvent>? OnDragEndEvent;

		protected override void OnMouseDown( MousePanelEvent e )
		{
			base.OnMouseDown( e );
			if ( e.MouseButton == MouseButtons.Left )
			{
				SelectSelf();
				_leftMouseDown = true;
			}
			if ( e.MouseButton == MouseButtons.Left && Draggable )
			{
				// Use local position for drag tracking in Fazor (no global Mouse.Position available)
				_dragStartScreen = e.LocalPosition;
				_dragStartLocal = MousePosition;
				_isDragging = false; // Will become true on move
			}
		}

		protected override void OnMouseMove( MousePanelEvent e )
		{
			base.OnMouseMove( e );
			if ( _leftMouseDown && Draggable )
			{
				if ( !_isDragging )
				{
					// Start drag if mouse moved enough (e.g., 3px threshold)
					if ( (e.LocalPosition - _dragStartScreen).Length > 3 )
					{
						_isDragging = true;
						OnDragStartEvent?.Invoke( new ItemDragEvent
						{
							LocalGrabPosition = _dragStartLocal,
							ScreenGrabPosition = _dragStartScreen,
							LocalPosition = MousePosition,
							ScreenPosition = e.LocalPosition,
							MouseDelta = e.LocalPosition - _dragStartScreen
						} );
					}
				}
				else
				{
					// Continue drag
					OnDragEvent?.Invoke( new ItemDragEvent
					{
						LocalGrabPosition = _dragStartLocal,
						ScreenGrabPosition = _dragStartScreen,
						LocalPosition = MousePosition,
						ScreenPosition = e.LocalPosition,
						MouseDelta = e.LocalPosition - _dragStartScreen
					} );
				}
			}
		}

		protected override void OnMouseUp( MousePanelEvent e )
		{
			base.OnMouseUp( e );
			if ( e.MouseButton == MouseButtons.Left )
			{
				_leftMouseDown = false;
			}
			if ( _isDragging && Draggable )
			{
				_isDragging = false;
				OnDragEndEvent?.Invoke( new ItemDragEvent
				{
					LocalGrabPosition = _dragStartLocal,
					ScreenGrabPosition = _dragStartScreen,
					LocalPosition = MousePosition,
					ScreenPosition = e.LocalPosition,
					MouseDelta = e.LocalPosition - _dragStartScreen
				} );
			}
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );
			SelectSelf();
		}

		protected override void OnDoubleClick( MousePanelEvent e )
		{
			base.OnDoubleClick( e );
			OnItemDoubleClicked();
		}

		public void UpdateViewMode( ListViewMode viewMode )
		{
			// Clear existing cells
			DeleteChildren();
			Cells.Clear();

			// Update classes for the view mode
			SetClass( "listview-row", viewMode == ListViewMode.Details || viewMode == ListViewMode.List );
			SetClass( "listview-icon-item", viewMode == ListViewMode.Icons );

			// Reuse existing icon panel or create a new one
			if ( IconPanel == null )
			{
				IconPanel = new FazorIconPanel();
			}
			else
			{
				var originalIconName = IconPanel.IconName;
				var originalIconSize = IconPanel.IconSize;
				IconPanel.Delete();
				IconPanel = new FazorIconPanel
				{
					IconName = originalIconName,
					IconSize = originalIconSize
				};
			}
			IconPanel.SetClass( "listview-icon", true );

			if ( viewMode == ListViewMode.Details )
			{
				// Create a cell for each column
				for ( int i = 0; i < ParentListView.Columns.Count; i++ )
				{
					var cell = new Panel();
					if ( i == 0 )
					{
						cell.AddChild( IconPanel );
					}
					cell.AddClass( "listview-cell" );
					cell.Style.Width = ParentListView.Columns[i].Width;

					string text = (SubItems != null && i < SubItems.Count) ? SubItems[i] : "";
					var itemtext = cell.AddChild<Panel>( "listview-text" );
					itemtext.AddChild( new Label { Text = text } );

					Cells.Add( cell );
					AddChild( cell );
				}
			}
			else if ( viewMode == ListViewMode.List || viewMode == ListViewMode.Icons )
			{
				// Just show the first column
				AddChild( IconPanel );
				string text = (SubItems != null && SubItems.Count > 0) ? SubItems[0] : "";
				var itemtext = AddChild<Panel>( "listview-text" );
				itemtext.AddChild( new Label { Text = text } );
			}

			// Always update selected state
			SetSelected( IsSelected );
		}

		private void SelectSelf()
		{
			ParentListView.SelectItem( this );
		}

		private void OnItemDoubleClicked()
		{
			ParentListView.OnItemActivated?.Invoke( this );
		}

		public void SetSelected( bool selected )
		{
			IsSelected = selected;
			SetClass( "selected", selected );
		}

		public void BeginRename( Action<string> onRenameComplete )
		{
			// Remove existing label(s)
			foreach ( var child in Children.ToList() )
			{
				if ( child.HasClass( "listview-text" ) )
					child.Delete();
			}

			// Create and add TextEntry
			_renameEntry = new TextEntry
			{
				Text = (SubItems != null && SubItems.Count > 0) ? SubItems[0] : "",
				Style = { Width = Length.Percent( 100 ) }
			};
			AddChild( _renameEntry );
			_renameEntry.Focus();

			_renameEntry.AddEventListener( "onblur", () => EndRename( onRenameComplete ) );
			_renameEntry.AddEventListener( "onsubmit", () => EndRename( onRenameComplete ) );
		}

		private void EndRename( Action<string> onRenameComplete )
		{
			if ( _renameEntry == null ) return;
			string newName = _renameEntry.Text;
			_renameEntry.Delete();
			_renameEntry = null;

			// Restore label
			string text = newName;
			var itemtext = AddChild<Panel>( "listview-text" );
			itemtext.AddChild( new Label { Text = text } );

			onRenameComplete?.Invoke( newName );
		}

		public class ItemDragEvent
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
		}
	}

	public Panel? ItemContainer { get; set; }

	public List<ListViewColumn> Columns { get; } = new();
	public List<ListViewItem> Items { get; } = new();
	private ListViewMode _viewMode = ListViewMode.Details;
	public ListViewMode ViewMode
	{
		get => _viewMode;
		set
		{
			if ( _viewMode != value )
			{
				_viewMode = value;
				UpdateItems();
			}
		}
	}

	public Action<ListViewItem>? OnItemSelected { get; set; }
	public Action<ListViewItem>? OnItemActivated { get; set; }

	// Sorting properties
	private int _sortColumnIndex = -1;
	private bool _sortAscending = true;

	public ListView()
	{
		// Use Panel instead of ScrollPanel - scrolling will be handled by CSS
		ItemContainer = new Panel();
		ItemContainer.AddClass( "listview-container" );
		ItemContainer.Style.OverflowY = OverflowMode.Scroll;
		ItemContainer.Style.OverflowX = OverflowMode.Scroll;
		AddClass( "listview" );
		InitializeHeader();
	}

	public void AddColumn( string header, string field, int width = 100 )
	{
		Columns.Add( new ListViewColumn { Header = header, Field = field, Width = width } );
		UpdateHeader();
	}

	public ListViewItem AddItem( object? data, List<string> subItems )
	{
		var item = new ListViewItem( this, data, subItems );
		Items.Add( item );

		// If we have a sort column, resort the items
		if ( _sortColumnIndex >= 0 )
		{
			SortItems();
			UpdateItems();
		}
		else
		{
			// Add to the correct container based on view mode
			if ( ViewMode == ListViewMode.Icons && ItemContainer != null )
			{
				ItemContainer.AddChild( item );
			}
			else
			{
				ItemContainer?.AddChild( item );
			}
		}
		return item;
	}

	/// <summary>
	/// Selects an item and triggers the selection event.
	/// </summary>
	public void SelectItem( ListViewItem item )
	{
		foreach ( var i in Items )
		{
			i.SetSelected( false );
		}

		item.SetSelected( true );
		OnItemSelected?.Invoke( item );
	}

	/// <summary>
	/// Sorts the ListView items by the specified column index.
	/// </summary>
	public void SortByColumn( int columnIndex )
	{
		// If clicking the same column, toggle direction
		if ( _sortColumnIndex == columnIndex )
		{
			_sortAscending = !_sortAscending;
		}
		else
		{
			_sortColumnIndex = columnIndex;
			_sortAscending = true;
		}

		// Sort the items
		SortItems();

		// Update header to show sort indicators
		UpdateHeader();

		// Refresh the view
		UpdateItems();
	}

	/// <summary>
	/// Sorts the items based on current sort settings.
	/// </summary>
	private void SortItems()
	{
		if ( _sortColumnIndex < 0 || _sortColumnIndex >= Columns.Count )
			return;

		// Create a temporary sorted list
		List<ListViewItem> sortedItems;

		if ( _sortAscending )
		{
			sortedItems = Items
				.OrderBy( item =>
					item.SubItems != null && _sortColumnIndex < item.SubItems.Count
					? item.SubItems[_sortColumnIndex]
					: "" )
				.ToList();
		}
		else
		{
			sortedItems = Items
				.OrderByDescending( item =>
					item.SubItems != null && _sortColumnIndex < item.SubItems.Count
					? item.SubItems[_sortColumnIndex]
					: "" )
				.ToList();
		}

		// Replace items with sorted list
		Items.Clear();
		Items.AddRange( sortedItems );
	}

	/// <summary>
	/// Updates the header based on current columns.
	/// </summary>
	private void UpdateHeader()
	{
		// Find and remove the existing header if it exists
		var existingHeader = ChildrenOfType<Panel>().FirstOrDefault( p => p.HasClass( "listview-header" ) );
		existingHeader?.Delete();

		// Create a new header
		InitializeHeader();
	}

	/// <summary>
	/// Initializes the header with current columns.
	/// </summary>
	public void InitializeHeader()
	{
		if ( ViewMode == ListViewMode.Details && Columns.Any() )
		{
			var header = new Panel();
			header.AddClass( "listview-header" );

			for ( int i = 0; i < Columns.Count; i++ )
			{
				var col = Columns[i];
				var colPanel = new Panel();
				colPanel.AddClass( "listview-header-column" );
				colPanel.Style.Width = col.Width;

				// Make a row for the header text and sort indicator
				var headerRow = new Panel();
				headerRow.Style.FlexDirection = FlexDirection.Row;
				headerRow.Style.AlignItems = Align.Center;

				// Add the header text
				headerRow.AddChild( new Label { Text = col.Header } );

				// Add sort indicator if this is the sort column
				if ( _sortColumnIndex == i )
				{
					var sortIndicator = new Label
					{
						Text = _sortAscending ? "▲" : "▼",
						Style =
						{
							MarginLeft = 4,
							FontSize = 8
						}
					};
					sortIndicator.AddClass( "sort-indicator" );
					sortIndicator.AddClass( _sortAscending ? "sort-up" : "sort-down" );
					headerRow.AddChild( sortIndicator );
				}

				colPanel.AddChild( headerRow );

				// Add click handler for sorting (capture the index)
				int columnIndex = i; // Capture for closure
				colPanel.AddEventListener( "onclick", () => SortByColumn( columnIndex ) );

				header.AddChild( colPanel );
			}

			AddChild( header );
		}
	}

	public void ClearList()
	{
		foreach ( var child in Children.ToList() )
		{
			if ( child.ElementName == "element" ) continue;
			child.Parent = null;
		}
	}

	/// <summary>
	/// Updates all items to match the current view mode.
	/// </summary>
	public void UpdateItems()
	{
		ClearList();
		// Re-add the header for details mode
		InitializeHeader();

		// Special handling for Icons view - use a container for grid layout
		if ( ViewMode == ListViewMode.Icons )
		{
			ItemContainer = new Panel();
			ItemContainer.AddClass( "listview-container" );
			ItemContainer.AddClass( "listview-icon-container" );
			ItemContainer.Style.OverflowY = OverflowMode.Scroll;
			ItemContainer.Style.OverflowX = OverflowMode.Scroll;
			AddChild( ItemContainer );

			// Update all items and add them to the container
			foreach ( var item in Items )
			{
				item.UpdateViewMode( ViewMode );
				ItemContainer.AddChild( item );
			}
		}
		else
		{
			ItemContainer = new Panel();
			ItemContainer.AddClass( "listview-container" );
			ItemContainer.Style.OverflowY = OverflowMode.Scroll;
			ItemContainer.Style.OverflowX = OverflowMode.Scroll;
			AddChild( ItemContainer );
			// For other views, add items directly to the ListView
			foreach ( var item in Items )
			{
				item.UpdateViewMode( ViewMode );
				ItemContainer.AddChild( item );
			}
		}
	}
}
