using Sandbox;
using Sandbox.UI;

// Use Sandbox.UI.Vector2 which has lowercase x, y members (matches s&box)
using Vector2 = Sandbox.UI.Vector2;

namespace XGUI;

/// <summary>
/// A Win32-inspired ListView control for XGUI, usable from C# and Razor.
/// </summary>
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
        public object Data { get; }
        public List<string> SubItems { get; }
        public bool IsSelected { get; private set; }
        private List<Panel> Cells { get; } = new();
        public XGUIIconPanel? IconPanel;
        private TextEntry? _renameEntry;

        public ListViewItem(ListView parent, object data, List<string> subItems)
        {
            ParentListView = parent;
            Data = data;
            SubItems = subItems;

            AddClass("listview-item");

            // Initial render based on parent's current view mode
            UpdateViewMode(ParentListView.ViewMode);
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

        protected override void OnMouseDown(MousePanelEvent e)
        {
            base.OnMouseDown(e);
            if (e.MouseButton == MouseButtons.Left)
            {
                SelectSelf();
                _leftMouseDown = true;
            }
            if (e.MouseButton == MouseButtons.Left && Draggable)
            {
                _dragStartScreen = Mouse.Position;
                _dragStartLocal = MousePosition;
                _isDragging = false;
            }
        }

        protected override void OnMouseMove(MousePanelEvent e)
        {
            base.OnMouseMove(e);
            if (_leftMouseDown && Draggable)
            {
                if (!_isDragging)
                {
                    if ((Mouse.Position - _dragStartScreen).Length > 3)
                    {
                        _isDragging = true;
                        OnDragStartEvent?.Invoke(new ItemDragEvent
                        {
                            LocalGrabPosition = _dragStartLocal,
                            ScreenGrabPosition = _dragStartScreen,
                            LocalPosition = MousePosition,
                            ScreenPosition = Mouse.Position,
                            MouseDelta = Mouse.Position - _dragStartScreen
                        });
                    }
                }
                else
                {
                    OnDragEvent?.Invoke(new ItemDragEvent
                    {
                        LocalGrabPosition = _dragStartLocal,
                        ScreenGrabPosition = _dragStartScreen,
                        LocalPosition = MousePosition,
                        ScreenPosition = Mouse.Position,
                        MouseDelta = Mouse.Position - _dragStartScreen
                    });
                }
            }
        }

        protected override void OnMouseUp(MousePanelEvent e)
        {
            base.OnMouseUp(e);
            if (e.MouseButton == MouseButtons.Left)
            {
                _leftMouseDown = false;
            }
            if (_isDragging && Draggable)
            {
                _isDragging = false;
                OnDragEndEvent?.Invoke(new ItemDragEvent
                {
                    LocalGrabPosition = _dragStartLocal,
                    ScreenGrabPosition = _dragStartScreen,
                    LocalPosition = MousePosition,
                    ScreenPosition = Mouse.Position,
                    MouseDelta = Mouse.Position - _dragStartScreen
                });
            }
        }

        protected override void OnClick(MousePanelEvent e)
        {
            base.OnClick(e);
            SelectSelf();
        }

        protected override void OnDoubleClick(MousePanelEvent e)
        {
            base.OnDoubleClick(e);
            OnItemDoubleClicked();
        }

        public void UpdateViewMode(ListViewMode viewMode)
        {
            // Clear existing cells
            DeleteChildren();
            Cells.Clear();

            // Update classes for the view mode
            SetClass("listview-row", viewMode == ListViewMode.Details || viewMode == ListViewMode.List);
            SetClass("listview-icon-item", viewMode == ListViewMode.Icons);

            // Reuse existing icon panel or create a new one
            if (IconPanel == null)
            {
                IconPanel = new XGUIIconPanel();
            }
            else
            {
                var originalIconName = IconPanel.IconName;
                var originalIconType = IconPanel.IconType;
                var originalIconSize = IconPanel.IconSize;
                IconPanel.Delete();
                IconPanel = new XGUIIconPanel
                {
                    IconName = originalIconName,
                    IconType = originalIconType,
                    IconSize = originalIconSize
                };
            }
            IconPanel.SetClass("listview-icon", true);

            if (viewMode == ListViewMode.Details)
            {
                // Create a cell for each column
                for (int i = 0; i < ParentListView.Columns.Count; i++)
                {
                    var cell = new Panel();
                    if (i == 0)
                    {
                        cell.AddChild(IconPanel);
                    }
                    cell.AddClass("listview-cell");
                    cell.Style.Width = ParentListView.Columns[i].Width;

                    string text = (SubItems != null && i < SubItems.Count) ? SubItems[i] : "";
                    var itemtext = cell.AddChild<Panel>("listview-text");
                    itemtext.AddChild(new Label { Text = text });

                    Cells.Add(cell);
                    AddChild(cell);
                }
            }
            else if (viewMode == ListViewMode.List || viewMode == ListViewMode.Icons)
            {
                // Just show the first column
                AddChild(IconPanel);
                string text = (SubItems != null && SubItems.Count > 0) ? SubItems[0] : "";
                var itemtext = AddChild<Panel>("listview-text");
                itemtext.AddChild(new Label { Text = text });
            }

            // Always update selected state
            SetSelected(IsSelected);
        }

        private void SelectSelf()
        {
            ParentListView.SelectItem(this);
        }

        private void OnItemDoubleClicked()
        {
            ParentListView.OnItemActivated?.Invoke(this);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            SetClass("selected", selected);
        }

        public void BeginRename(Action<string> onRenameComplete)
        {
            // Remove existing label(s)
            foreach (var child in Children.ToList())
            {
                if (child.HasClass("listview-text"))
                    child.Delete();
            }

            // Create and add TextEntry
            _renameEntry = new TextEntry
            {
                Text = (SubItems != null && SubItems.Count > 0) ? SubItems[0] : "",
            };
            _renameEntry.Style.Width = Length.Percent(100);
            AddChild(_renameEntry);
            _renameEntry.Focus();

            _renameEntry.AddEventListener("onblur", () => EndRename(onRenameComplete));
            _renameEntry.AddEventListener("onsubmit", () => EndRename(onRenameComplete));
        }

        private void EndRename(Action<string> onRenameComplete)
        {
            if (_renameEntry == null) return;
            string newName = _renameEntry.Text ?? "";
            _renameEntry.Delete();
            _renameEntry = null;

            // Restore label
            string text = newName;
            var itemtext = AddChild<Panel>("listview-text");
            itemtext.AddChild(new Label { Text = text });

            onRenameComplete?.Invoke(newName);
        }

        public class ItemDragEvent
        {
            public Vector2 MouseDelta;
            public Vector2 LocalGrabPosition;
            public Vector2 ScreenGrabPosition;
            public Vector2 LocalPosition;
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
            if (_viewMode != value)
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
        ItemContainer = new ScrollPanel();
        ItemContainer.AddClass("listview-container");
        AddClass("listview");
        InitializeHeader();
    }

    public void AddColumn(string header, string field, int width = 100)
    {
        Columns.Add(new ListViewColumn { Header = header, Field = field, Width = width });
        UpdateHeader();
    }

    public ListViewItem AddItem(object data, List<string> subItems)
    {
        var item = new ListViewItem(this, data, subItems);
        Items.Add(item);

        // If we have a sort column, resort the items
        if (_sortColumnIndex >= 0)
        {
            SortItems();
            UpdateItems();
        }
        else
        {
            // Add to the correct container based on view mode
            ItemContainer?.AddChild(item);
        }
        return item;
    }

    /// <summary>
    /// Selects an item and triggers the selection event.
    /// </summary>
    public void SelectItem(ListViewItem item)
    {
        foreach (var i in Items)
        {
            i.SetSelected(false);
        }

        item.SetSelected(true);
        OnItemSelected?.Invoke(item);
    }

    /// <summary>
    /// Sorts the ListView items by the specified column index.
    /// </summary>
    public void SortByColumn(int columnIndex)
    {
        if (_sortColumnIndex == columnIndex)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortColumnIndex = columnIndex;
            _sortAscending = true;
        }

        SortItems();
        UpdateHeader();
        UpdateItems();
    }

    private void SortItems()
    {
        if (_sortColumnIndex < 0 || _sortColumnIndex >= Columns.Count)
            return;

        var sortedItems = new List<ListViewItem>();

        if (_sortAscending)
        {
            sortedItems = Items
                .OrderBy(item =>
                    item.SubItems != null && _sortColumnIndex < item.SubItems.Count
                    ? item.SubItems[_sortColumnIndex]
                    : "")
                .ToList();
        }
        else
        {
            sortedItems = Items
                .OrderByDescending(item =>
                    item.SubItems != null && _sortColumnIndex < item.SubItems.Count
                    ? item.SubItems[_sortColumnIndex]
                    : "")
                .ToList();
        }

        Items.Clear();
        Items.AddRange(sortedItems);
    }

    private void UpdateHeader()
    {
        var existingHeader = ChildrenOfType<Panel>().FirstOrDefault(p => p.HasClass("listview-header"));
        existingHeader?.Delete();
        InitializeHeader();
    }

    public void InitializeHeader()
    {
        if (ViewMode == ListViewMode.Details && Columns.Any())
        {
            var header = new Panel();
            header.AddClass("listview-header");

            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                var colPanel = new Panel();
                colPanel.AddClass("listview-header-column");
                colPanel.Style.Width = col.Width;

                var headerRow = new Panel();
                headerRow.Style.FlexDirection = FlexDirection.Row;
                headerRow.Style.AlignItems = Align.Center;

                headerRow.AddChild(new Label { Text = col.Header });

                if (_sortColumnIndex == i)
                {
                    var sortIndicator = new Label
                    {
                        Text = _sortAscending ? "▲" : "▼",
                    };
                    sortIndicator.Style.MarginLeft = 4;
                    sortIndicator.Style.FontSize = 8;
                    sortIndicator.AddClass("sort-indicator");
                    sortIndicator.AddClass(_sortAscending ? "sort-up" : "sort-down");
                    headerRow.AddChild(sortIndicator);
                }

                colPanel.AddChild(headerRow);

                int columnIndex = i;
                colPanel.AddEventListener("onclick", () => SortByColumn(columnIndex));

                header.AddChild(colPanel);
            }

            AddChild(header);
        }
    }

    public void ClearList()
    {
        foreach (var child in Children.ToList())
        {
            if (child.ElementName == "element") continue;
            child.Parent = null;
        }
        Items.Clear();
    }

    public void UpdateItems()
    {
        ClearList();
        InitializeHeader();

        if (ViewMode == ListViewMode.Icons)
        {
            ItemContainer = new ScrollPanel();
            ItemContainer.AddClass("listview-container");
            ItemContainer.AddClass("listview-icon-container");
            AddChild(ItemContainer);

            foreach (var item in Items)
            {
                item.UpdateViewMode(ViewMode);
                ItemContainer.AddChild(item);
            }
        }
        else
        {
            ItemContainer = new ScrollPanel();
            ItemContainer.AddClass("listview-container");
            AddChild(ItemContainer);
            foreach (var item in Items)
            {
                item.UpdateViewMode(ViewMode);
                ItemContainer.AddChild(item);
            }
        }
    }
}

/// <summary>
/// XGUI ContextMenu control - stub implementation.
/// </summary>
public class ContextMenu : Panel
{
    public ContextMenu()
    {
        AddClass("contextmenu");
        AddClass("popup");
    }
    
    /// <summary>
    /// Shows the context menu at the specified position.
    /// </summary>
    public void Show(float x, float y)
    {
        Style.Position = PositionMode.Absolute;
        Style.Left = x;
        Style.Top = y;
        Style.Display = DisplayMode.Flex;
    }
    
    /// <summary>
    /// Hides the context menu.
    /// </summary>
    public void Hide()
    {
        Style.Display = DisplayMode.None;
    }
    
    /// <summary>
    /// Adds a menu item.
    /// </summary>
    public void AddItem(string text, Action? onClick = null)
    {
        var item = new Button { Text = text };
        if (onClick != null)
        {
            item.AddEventListener("onclick", onClick);
        }
        AddChild(item);
    }
    
    /// <summary>
    /// Adds a separator.
    /// </summary>
    public void AddSeparator()
    {
        var sep = new Panel();
        sep.AddClass("separator");
        AddChild(sep);
    }
}

/// <summary>
/// XGUI Toolbar control - stub implementation.
/// </summary>
public class Toolbar : Panel
{
    public Toolbar()
    {
        AddClass("toolbar");
        Style.FlexDirection = FlexDirection.Row;
    }
}

/// <summary>
/// XGUI ToolbarButton control - stub implementation.
/// </summary>
public class ToolbarButton : Button
{
    public ToolbarButton()
    {
        AddClass("toolbarbutton");
    }
}

/// <summary>
/// XGUI FileBrowserView control - displays files in various view modes.
/// </summary>
public class FileBrowserView : Panel
{
    public FileBrowserView()
    {
        AddClass("filebrowserview");
        ListView = new ListView();
        AddChild(ListView);
    }
    
    public ListView ListView { get; protected set; }
    public FileBrowserViewMode ViewMode { get; set; } = FileBrowserViewMode.Icons;
    public string CurrentPath { get; set; } = "";
    public bool HideFileExtensions { get; set; }
    
    public event Action<string>? OnPathChanged;
    public event Action<string>? OnFileSelected;
    public event Action<string>? OnFileActivated;
    
    /// <summary>
    /// Whether the view wants drag scrolling.
    /// </summary>
    protected virtual bool WantsDragScrolling => true;
    
    /// <summary>
    /// Add a file to the view.
    /// </summary>
    public virtual void AddFileToView(string file, bool isFullPath = false, string nameOverride = "")
    {
        var displayName = string.IsNullOrEmpty(nameOverride) 
            ? System.IO.Path.GetFileName(file) 
            : nameOverride;
            
        if (HideFileExtensions)
        {
            displayName = System.IO.Path.GetFileNameWithoutExtension(displayName);
        }
        
        ListView.AddItem(file, new List<string> { displayName });
    }
    
    /// <summary>
    /// Refresh the view.
    /// </summary>
    public virtual void Refresh()
    {
        ListView.ClearList();
        ListView.UpdateItems();
        StateHasChanged();
    }
    
    /// <summary>
    /// Clear the view.
    /// </summary>
    public virtual void Clear()
    {
        ListView.ClearList();
    }
    
    /// <summary>
    /// Navigate to a path.
    /// </summary>
    public virtual void NavigateTo(string path)
    {
        CurrentPath = path;
        Refresh();
        OnPathChanged?.Invoke(path);
    }
}

/// <summary>
/// File browser view modes.
/// </summary>
public enum FileBrowserViewMode
{
    Icons,
    List,
    Details,
    SmallIcons,
    Thumbnails
}

/// <summary>
/// XGUI FileBrowserTree control - stub implementation.
/// </summary>
public class FileBrowserTree : Panel
{
    public FileBrowserTree()
    {
        AddClass("filebrowsertree");
    }
    
    public string CurrentPath { get; set; } = "";
    public event Action<string>? OnPathChanged;
}

/// <summary>
/// XGUI FileItem control - represents a file item in browser views.
/// </summary>
public class FileItem : Panel
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// XGUI WebPanel control - stub implementation.
/// </summary>
public class WebPanel : Panel
{
    public WebPanel()
    {
        AddClass("webpanel");
    }
    
    public string Url { get; set; } = "";
    
    public void Navigate(string url)
    {
        Url = url;
        StateHasChanged();
    }
}
