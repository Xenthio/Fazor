using Sandbox.UI;

// Use Sandbox.UI.Vector2 which has lowercase x, y members (matches s&box)
using Vector2 = Sandbox.UI.Vector2;

namespace XGUI;

/// <summary>
/// XGUI Window class - A draggable, resizable window panel.
/// This provides s&box/XGUI-3 compatible Window functionality.
/// </summary>
public partial class Window : XGUIPanel
{
    public string Title { get; set; } = "Window";
    public TitleBar? TitleBar { get; set; }
    
    public Vector2 Position { get; set; } = new Vector2(22, 22);
    public Vector2 Size { get; set; }
    public Vector2 MinSize { get; set; } = new Vector2(100, 50);
    
    public int ZIndex { get; set; }
    
    public bool HasControls { get; set; } = true;
    public bool HasTitleBar { get; set; } = true;
    public bool HasMinimise { get; set; } = false;
    public bool HasMaximise { get; set; } = false;
    public bool HasClose { get; set; } = true;
    
    public bool IsResizable { get; set; } = true;
    public bool IsDraggable { get; set; } = true;
    public bool AutoFocus { get; set; } = true;
    
    public Button ControlsClose { get; set; } = new Button();
    public Button ControlsMinimise { get; set; } = new Button();
    public Button ControlsMaximise { get; set; } = new Button();
    
    public Panel? WindowContent { get; set; }
    public Vector2? InitialInnerSize { get; set; } = null;
    
    public Window()
    {
        if (HasTitleBar)
        {
            TitleBar = new TitleBar();
            TitleBar.ParentWindow = this;
            AddChild(TitleBar);
        }
        
        AddClass("panel");
        AddClass("window");
        ElementName = "window";
        Style.Position = PositionMode.Absolute;
        Style.FlexDirection = FlexDirection.Column;
    }
    
    private bool _hasInitInnerSize = false;
    
    protected override void OnAfterTreeRender(bool firstTime)
    {
        base.OnAfterTreeRender(firstTime);
        if (firstTime)
        {
            // Find window content
            var contentPanel = Children.FirstOrDefault(x => x.HasClass("window-content"));
            if (contentPanel != null)
            {
                WindowContent = contentPanel;
            }
            else
            {
                Sandbox.Log.Warning($"The window {this} does not have a child with class window-content");
            }
            
            CreateTitleBar();
            
            this.AddEventListener("onmousedown", ResizeDown);
            this.AddEventListener("onmouseup", ResizeUp);
            this.AddEventListener("onmousemove", ResizeMove);
            
            OverrideButtons();
            
            if (AutoFocus)
            {
                FocusWindow();
                AutoFocus = false;
            }
            
            // If size is set, apply it
            if (Size != Vector2.Zero)
            {
                Style.Width = Size.x;
                Style.Height = Size.y;
            }
        }
        
        if (TitleBar != null && TitleBar.IsValid())
            SetChildIndex(TitleBar, 0);
    }
    
    private void TryInitInnerSize()
    {
        if (_hasInitInnerSize) return;
        if (InitialInnerSize.HasValue && WindowContent != null && Box != null && WindowContent.Box != null)
        {
            float currentWindowWidth = Box.Rect.Width;
            float currentWindowHeight = Box.Rect.Height;
            
            float currentWindowContentWidth = WindowContent.Box.Rect.Width;
            float currentWindowContentHeight = WindowContent.Box.Rect.Height;
            
            if (currentWindowContentHeight == 0 && currentWindowContentWidth == 0)
            {
                return;
            }
            
            float chromeWidth = currentWindowWidth - currentWindowContentWidth;
            float chromeHeight = currentWindowHeight - currentWindowContentHeight;
            
            Size = new Vector2(InitialInnerSize.Value.x + chromeWidth, InitialInnerSize.Value.y + chromeHeight);
            
            Style.Width = Size.x;
            Style.Height = Size.y;
            _hasInitInnerSize = true;
        }
    }
    
    public Panel CreateWindowContentPanel()
    {
        var contentPanel = AddChild(new Panel(this, "window-content"));
        WindowContent = contentPanel;
        return contentPanel;
    }
    
    public void OverrideButtons()
    {
        foreach (var button in Descendants.OfType<Button>())
        {
            var focusAllowed = button.GetAttribute("focus", "0");
            if (focusAllowed == "1")
            {
                button.AcceptsFocus = true;
            }
            var autoFocus = button.GetAttribute("autofocus", "0");
            if (autoFocus == "1")
            {
                button.Focus();
                button.AddClass("autofocused");
            }
        }
    }
    
    private Panel? _lastFocus;
    
    public void FocusUpdate()
    {
        // Focus tracking - simplified for Fazor
        // In s&box this uses InputFocus.Current
    }
    
    public void CreateTitleBar()
    {
        if (!HasTitleBar) return;
        
        if (TitleBar == null)
        {
            TitleBar = new TitleBar();
            TitleBar.ParentWindow = this;
        }
        
        AddChild(TitleBar);
        var bg = TitleBar.AddChild(new Panel(TitleBar, "TitleBackground"));
        TitleBar.Style.ZIndex = 100;
        
        ControlsMinimise.AddEventListener("onclick", Minimise);
        ControlsMinimise.Text = "0";
        
        ControlsMaximise.AddEventListener("onclick", Maximise);
        ControlsMaximise.Text = "1";
        
        ControlsClose.AddEventListener("onclick", Close);
        ControlsClose.Text = "r";
    }
    
    public static event Action<Window>? OnMinimised;
    public static event Action<Window>? OnRestored;
    
    public bool IsMinimised { get; set; } = false;
    private Vector2 _preMinimisedSize;
    private Vector2 _preMinimisedPos;
    
    public void Minimise()
    {
        if (!IsMinimised)
        {
            _preMinimisedSize = Box?.Rect.Size ?? Size;
            _preMinimisedPos = Position;
            
            var offset = 0f;
            
            // Offset x for other minimised windows
            if (Parent != null)
            {
                foreach (var window in Parent.Children.OfType<Window>())
                {
                    if (window.IsMinimised)
                    {
                        offset += 180;
                    }
                }
            }
            Position = new Vector2(offset, (Parent?.Box?.Rect.Size.y ?? 0) - 30);
            
            Style.Height = 30;
            Style.Width = 180;
            IsMinimised = true;
            OnMinimised?.Invoke(this);
        }
        else
        {
            IsMinimised = false;
            Style.Width = _preMinimisedSize.x;
            Style.Height = _preMinimisedSize.y;
            Position = _preMinimisedPos;
            OnRestored?.Invoke(this);
        }
    }
    
    public bool IsMaximised { get; set; } = false;
    private Vector2 _preMaximisedSize;
    private Vector2 _preMaximisedPos;
    
    public void Maximise()
    {
        if (!IsMaximised)
        {
            _preMaximisedSize = Box?.Rect.Size ?? Size;
            _preMaximisedPos = Position;
            
            Position = Vector2.Zero;
            
            if (Parent?.Box != null)
            {
                Style.Height = Parent.Box.Rect.Size.y;
                Style.Width = Parent.Box.Rect.Size.x;
            }
            IsMaximised = true;
        }
        else
        {
            IsMaximised = false;
            Style.Width = _preMaximisedSize.x;
            Style.Height = _preMaximisedSize.y;
            Position = _preMaximisedPos;
        }
    }
    
    public void Close()
    {
        OnClose();
        OnCloseAction?.Invoke();
        Delete();
    }
    
    public Action? OnCloseAction { get; set; }
    
    public virtual void OnClose()
    {
        // Override this to do something when the window closes
    }
    
    public override void Tick()
    {
        base.Tick();
        TryInitInnerSize();
        
        // Handle mouse release for drag/resize
        // In s&box this uses Input.Released("Attack1")
        
        Drag();
        
        if (Style.Left == null)
        {
            Style.Left = 0;
            Style.Top = 0;
        }
        
        Style.Position = PositionMode.Absolute;
        Style.Left = Position.x * ScaleFromScreen;
        Style.Top = Position.y * ScaleFromScreen;
        
        if (Parent != null)
        {
            Style.ZIndex = (Parent.ChildrenCount - Parent.GetChildIndex(this)) * 10;
        }
        
        SetClass("minimised", IsMinimised);
        SetClass("maximised", IsMaximised);
        SetClass("unfocused", !HasFocus);
        FocusUpdate();
    }
    
    public void FocusWindow()
    {
        AcceptsFocus = true;
        if (!HasFocus)
            Focus();
        Parent?.SetChildIndex(this, 0);
    }
    
    private Vector2 MousePos()
    {
        return FindRootPanel()?.MousePosition ?? Vector2.Zero;
    }
    
    private Vector2 LocalMousePos()
    {
        return Parent?.MousePosition ?? Vector2.Zero;
    }
    
    // Dragging
    private bool _dragging = false;
    private float _xoff = 0;
    private float _yoff = 0;
    
    public void Drag()
    {
        if (!_dragging) return;
        var mousePos = LocalMousePos();
        Position = new Vector2(mousePos.x - _xoff, mousePos.y - _yoff);
        
        // Window edge snapping - simplified version
        if (Parent != null)
        {
            foreach (var window in Parent.Children.OfType<Window>())
            {
                if (window == this) continue;
                
                var snapDistance = 10f;
                
                // Simplified snapping logic
                var window1Left = Position.x;
                var window1Right = Position.x + (Box?.Rect.Size.x ?? 0);
                var window2Left = window.Position.x;
                var window2Right = window.Position.x + (window.Box?.Rect.Size.x ?? 0);
                
                if (Math.Abs(window1Left - window2Right) < snapDistance)
                    Position = new Vector2(window2Right, Position.y);
                if (Math.Abs(window1Right - window2Left) < snapDistance)
                    Position = new Vector2(window2Left - (Box?.Rect.Size.x ?? 0), Position.y);
            }
        }
    }
    
    public void DragBarDown()
    {
        if (!IsDraggable) return;
        
        var mousePos = MousePos();
        
        _xoff = mousePos.x - (Box?.Rect.Left ?? 0);
        _yoff = mousePos.y - (Box?.Rect.Top ?? 0);
        _dragging = true;
    }
    
    public void DragBarUp()
    {
        _dragging = false;
    }
    
    // Focusing
    protected override void OnMouseDown(MousePanelEvent e)
    {
        FocusWindow();
        base.OnMouseDown(e);
    }
    
    // Resizing
    internal bool _draggingR = false;
    internal bool _draggingL = false;
    internal bool _draggingT = false;
    internal bool _draggingB = false;
    
    public void ResizeDown()
    {
        if (!IsResizable) return;
        
        var Distance = 5f;
        var mousePos = MousePos();
        var rect = Box?.Rect ?? new Rect();
        
        if (Math.Abs(mousePos.y - rect.Bottom) < Distance) _draggingB = true;
        if (Math.Abs(mousePos.x - rect.Right) < Distance) _draggingR = true;
        if (Math.Abs(mousePos.y - rect.Top) < Distance) _draggingT = true;
        if (Math.Abs(mousePos.x - rect.Left) < Distance) _draggingL = true;
        
        _xoff1 = mousePos.x - rect.Right;
        _yoff1 = mousePos.y - rect.Bottom;
        _xoff2 = mousePos.x - rect.Left;
        _yoff2 = mousePos.y - rect.Top;
    }
    
    public void ResizeUp()
    {
        _draggingB = false;
        _draggingR = false;
        _draggingT = false;
        _draggingL = false;
    }
    
    internal float _xoff1 = 0;
    internal float _yoff1 = 0;
    internal float _xoff2 = 0;
    internal float _yoff2 = 0;
    
    public void ResizeMove()
    {
        var mousePos = MousePos();
        var mousePosLocal = LocalMousePos();
        var rect = Box?.Rect ?? new Rect();
        
        if (IsResizable)
        {
            var Distance = 5f;
            
            var almostBottom = Math.Abs(mousePos.y - rect.Bottom) < Distance;
            var almostRight = Math.Abs(mousePos.x - rect.Right) < Distance;
            var almostTop = Math.Abs(mousePos.y - rect.Top) < Distance;
            var almostLeft = Math.Abs(mousePos.x - rect.Left) < Distance;
            
            if ((almostLeft && almostBottom) || (_draggingL && _draggingB)) Style.Cursor = "nesw-resize";
            else if ((almostRight && almostTop) || (_draggingR && _draggingT)) Style.Cursor = "nesw-resize";
            else if ((almostRight && almostBottom) || (_draggingR && _draggingB)) Style.Cursor = "nwse-resize";
            else if ((almostLeft && almostTop) || (_draggingL && _draggingT)) Style.Cursor = "nwse-resize";
            else if (almostBottom || _draggingB) Style.Cursor = "ns-resize";
            else if (almostRight || _draggingR) Style.Cursor = "ew-resize";
            else if (almostTop || _draggingT) Style.Cursor = "ns-resize";
            else if (almostLeft || _draggingL) Style.Cursor = "ew-resize";
            else Style.Cursor = "unset";
        }
        
        if (_draggingB)
        {
            var newHeight = (mousePos.y - rect.Top) - _yoff1;
            if (newHeight > MinSize.y)
            {
                Style.Height = newHeight;
            }
        }
        
        if (_draggingR)
        {
            var newWidth = (mousePos.x - rect.Left) - _xoff1;
            if (newWidth > MinSize.x)
            {
                Style.Width = newWidth;
            }
        }
        
        if (_draggingT)
        {
            var newHeight = rect.Height - ((mousePos.y - _yoff2) - rect.Top);
            if (newHeight > MinSize.y)
            {
                Style.Height = newHeight;
                Position = new Vector2(Position.x, mousePosLocal.y - _yoff2);
            }
        }
        
        if (_draggingL)
        {
            var newWidth = rect.Width - ((mousePos.x - _xoff2) - rect.Left);
            if (newWidth > MinSize.x)
            {
                Style.Width = newWidth;
                Position = new Vector2(mousePosLocal.x - _xoff2, Position.y);
            }
        }
    }
    
    public override void SetProperty(string name, string value)
    {
        switch (name)
        {
            case "title":
                Title = value;
                return;
            case "hastitlebar":
                HasTitleBar = bool.Parse(value);
                if (!HasTitleBar && TitleBar != null)
                {
                    TitleBar.Delete();
                }
                SetClass("notitlebar", !HasTitleBar);
                return;
            case "hasminimise":
                HasMinimise = bool.Parse(value);
                return;
            case "hasmaximise":
                HasMaximise = bool.Parse(value);
                return;
            case "hasclose":
                HasClose = bool.Parse(value);
                return;
            case "isresizable":
                IsResizable = bool.Parse(value);
                return;
            case "isdraggable":
                IsDraggable = bool.Parse(value);
                return;
            case "autofocus":
                AutoFocus = bool.Parse(value);
                return;
            case "width":
                var w = Length.Parse(value);
                if (w != null)
                    Style.Width = w;
                return;
            case "height":
                var h = Length.Parse(value);
                if (h != null)
                    Style.Height = h;
                return;
            case "x":
                var xVal = Length.Parse(value);
                if (xVal?.Value != null)
                    Position = new Vector2(xVal.Value.Value, Position.y);
                return;
            case "y":
                var yVal = Length.Parse(value);
                if (yVal?.Value != null)
                    Position = new Vector2(Position.x, yVal.Value.Value);
                return;
            case "minwidth":
                var minW = Length.Parse(value);
                if (minW?.Value != null)
                    MinSize = new Vector2(minW.Value.Value, MinSize.y);
                return;
            case "minheight":
                var minH = Length.Parse(value);
                if (minH?.Value != null)
                    MinSize = new Vector2(MinSize.x, minH.Value.Value);
                return;
        }
        
        base.SetProperty(name, value);
    }
}
