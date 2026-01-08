using Sandbox.UI;

namespace XGUI;

/// <summary>
/// A panel with scrolling functionality.
/// </summary>
public class ScrollPanel : Panel
{
    public Panel? VerticalScrollbar { get; private set; }
    public Panel? HorizontalScrollbar { get; private set; }
    public Panel? ScrollArea { get; private set; }
    public Panel? ScrollThumb { get; private set; }

    public float ScrollStep { get; set; } = 50f;
    public bool DisableScrollBounce { get; set; } = true;

    public ScrollPanel()
    {
        AddClass("scrollpanel");
        Style.OverflowY = OverflowMode.Scroll;
        Style.OverflowX = OverflowMode.Hidden;
        Style.FlexDirection = FlexDirection.Column;
    }

    /// <summary>
    /// Scroll to the top of the panel.
    /// </summary>
    public void ScrollToTop()
    {
        ScrollOffset = new Vector2(ScrollOffset.x, 0);
    }

    /// <summary>
    /// Scroll to the bottom of the panel.
    /// </summary>
    public void ScrollToBottom()
    {
        ScrollOffset = new Vector2(ScrollOffset.x, float.MaxValue);
    }

    /// <summary>
    /// Scroll by a delta amount.
    /// </summary>
    public void ScrollBy(float deltaX, float deltaY)
    {
        ScrollOffset = new Vector2(
            ScrollOffset.x + deltaX,
            ScrollOffset.y + deltaY
        );
    }

    public override void OnMouseWheel(Vector2 delta)
    {
        base.OnMouseWheel(delta);
        ScrollBy(0, -delta.y * ScrollStep);
    }
}

/// <summary>
/// XGUI ComboBox control - dropdown select control.
/// </summary>
public class ComboBox : Panel
{
    private Panel? _selectedPanel;
    private Panel? _dropdownPanel;
    private bool _isOpen;
    private string _value = "";
    
    public string Value 
    { 
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                ValueChanged?.Invoke(value);
                UpdateDisplay();
            }
        }
    }
    
    public Action<string>? ValueChanged { get; set; }
    public List<string> Options { get; } = new();

    public ComboBox()
    {
        AddClass("combobox");
        
        _selectedPanel = AddChild<Panel>("combobox-selected");
        _selectedPanel.AddEventListener("onclick", ToggleDropdown);
        
        _dropdownPanel = AddChild<Panel>("combobox-dropdown");
        _dropdownPanel.Style.Display = DisplayMode.None;
    }
    
    private void UpdateDisplay()
    {
        if (_selectedPanel != null)
        {
            _selectedPanel.DeleteChildren();
            _selectedPanel.AddChild(new Label { Text = _value });
        }
    }
    
    public void AddOption(string option)
    {
        Options.Add(option);
        
        if (_dropdownPanel != null)
        {
            var optionPanel = new Button { Text = option };
            optionPanel.AddEventListener("onclick", () => SelectOption(option));
            _dropdownPanel.AddChild(optionPanel);
        }
    }
    
    private void SelectOption(string option)
    {
        Value = option;
        CloseDropdown();
    }
    
    private void ToggleDropdown()
    {
        if (_isOpen)
            CloseDropdown();
        else
            OpenDropdown();
    }
    
    private void OpenDropdown()
    {
        _isOpen = true;
        if (_dropdownPanel != null)
            _dropdownPanel.Style.Display = DisplayMode.Flex;
    }
    
    private void CloseDropdown()
    {
        _isOpen = false;
        if (_dropdownPanel != null)
            _dropdownPanel.Style.Display = DisplayMode.None;
    }
}

/// <summary>
/// XGUI GroupBox control - a bordered group with title.
/// </summary>
public class GroupBox : Panel
{
    private Label? _titleLabel;
    private Panel? _contentPanel;
    
    public string Title
    {
        get => _titleLabel?.Text ?? "";
        set
        {
            if (_titleLabel != null)
                _titleLabel.Text = value;
        }
    }

    public GroupBox()
    {
        AddClass("groupbox");
        
        _titleLabel = AddChild<Label>("groupbox-title");
        _contentPanel = AddChild<Panel>("groupbox-content");
    }
    
    /// <summary>
    /// Add a child to the content area.
    /// </summary>
    public new void AddChild(Panel child)
    {
        _contentPanel?.AddChild(child);
    }
}

/// <summary>
/// XGUI RadioButton control.
/// </summary>
public class RadioButton : Panel
{
    private string? _groupName;
    private bool _isChecked;
    
    public string? GroupName
    {
        get => _groupName;
        set => _groupName = value;
    }
    
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                SetClass("checked", value);
                if (value)
                    OnChecked?.Invoke();
            }
        }
    }
    
    public string Text { get; set; } = "";
    public Action? OnChecked { get; set; }

    public RadioButton()
    {
        AddClass("radiobutton");
        AddEventListener("onclick", () => IsChecked = true);
    }
}

/// <summary>
/// XGUI CheckBox control.
/// </summary>
public class CheckBox : Panel
{
    private bool _isChecked;
    
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                SetClass("checked", value);
                OnChanged?.Invoke(value);
            }
        }
    }
    
    public string Text { get; set; } = "";
    public Action<bool>? OnChanged { get; set; }

    public CheckBox()
    {
        AddClass("checkbox");
        AddEventListener("onclick", () => IsChecked = !IsChecked);
    }
}

/// <summary>
/// XGUI TreeView control - stub implementation.
/// </summary>
public class TreeView : Panel
{
    public TreeView()
    {
        AddClass("treeview");
    }
    
    public List<TreeViewNode> Nodes { get; } = new();
    
    public TreeViewNode AddNode(string text, string? iconName = null)
    {
        var node = new TreeViewNode(text, iconName);
        Nodes.Add(node);
        AddChild(node);
        return node;
    }
}

/// <summary>
/// XGUI TreeViewNode - represents a node in a TreeView.
/// </summary>
public class TreeViewNode : Panel
{
    public string Text { get; set; }
    public string? IconName { get; set; }
    public bool IsExpanded { get; set; }
    public List<TreeViewNode> Children { get; } = new();
    public XGUIIconPanel? IconPanel { get; private set; }
    
    public TreeViewNode(string text, string? iconName = null)
    {
        Text = text;
        IconName = iconName;
        AddClass("treeview-node");
    }
    
    public TreeViewNode AddChild(string text, string? iconName = null)
    {
        var node = new TreeViewNode(text, iconName);
        Children.Add(node);
        base.AddChild(node);
        return node;
    }
}

/// <summary>
/// XGUI Backdrop panel - used for modal backgrounds.
/// </summary>
public class Backdrop : Panel
{
    public Backdrop()
    {
        AddClass("backdrop");
        Style.Position = PositionMode.Absolute;
        Style.Left = 0;
        Style.Top = 0;
        Style.Right = 0;
        Style.Bottom = 0;
    }
}
