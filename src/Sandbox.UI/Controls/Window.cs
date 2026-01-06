namespace Sandbox.UI;

/// <summary>
/// Base class for windows in the application.
/// Derive from this class to create windows that can interact with the native window system.
/// Manages window properties like title, size, and controls, and creates the appropriate UI structure.
/// Based on XGUI-3's Window implementation.
/// </summary>
[Library("window")]
public class Window : Panel
{
    private string _title = "Window";
    private INativeWindow? _nativeWindow; // Reference to the native window interface
    private bool _lastCustomChromeFromCss = false; // Track last CSS value for change detection

    /// <summary>
    /// The window title displayed in the native window title bar and optional in-window title bar
    /// </summary>
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            OnTitleChanged();
        }
    }

    /// <summary>
    /// The title bar of the window (if HasTitleBar is true)
    /// </summary>
    public TitleBar? TitleBar { get; set; }

    /// <summary>
    /// Initial width of the native window (used when creating the window)
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Initial height of the native window (used when creating the window)
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Position of the in-window panel (for floating window UI elements)
    /// Not the same as the native window position
    /// </summary>
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Size of the in-window panel (for floating window UI elements)
    /// </summary>
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Minimum size of the in-window panel
    /// </summary>
    public Vector2 MinSize { get; set; } = new Vector2(100, 50);

    /// <summary>
    /// Whether to use custom chrome (in-window title bar).
    /// When false (default), no title bar is drawn.
    /// When true, draws a title bar inside the window.
    /// Can also be controlled via CSS: --custom-chrome: true;
    /// </summary>
    public bool HasCustomChrome { get; set; } = false;
    
    /// <summary>
    /// Whether the window has a title bar (legacy property, use HasCustomChrome instead)
    /// </summary>
    [Obsolete("Use HasCustomChrome instead")]
    public bool HasTitleBar { get; set; } = false;

    /// <summary>
    /// Whether the window has control buttons
    /// </summary>
    public bool HasControls { get; set; } = true;

    /// <summary>
    /// Whether the window has a minimize button
    /// </summary>
    public bool HasMinimise { get; set; } = false;

    /// <summary>
    /// Whether the window has a maximize button
    /// </summary>
    public bool HasMaximise { get; set; } = false;

    /// <summary>
    /// Whether the window has a close button
    /// </summary>
    public bool HasClose { get; set; } = true;

    /// <summary>
    /// Whether the window can be resized
    /// </summary>
    public bool IsResizable { get; set; } = true;

    /// <summary>
    /// Whether the window can be dragged
    /// </summary>
    public bool IsDraggable { get; set; } = true;

    /// <summary>
    /// Whether to automatically focus the window on creation
    /// </summary>
    public bool AutoFocus { get; set; } = true;

    /// <summary>
    /// When true, this window is treated as a root window that fills its parent,
    /// ignoring Position/Size settings. Used in AI/headless mode where there's no
    /// native window but the Window should still fill the viewport.
    /// </summary>
    public bool IsRootWindow { get; set; } = false;

    /// <summary>
    /// The close button control
    /// </summary>
    public Button? ControlsClose { get; set; }

    /// <summary>
    /// The minimize button control
    /// </summary>
    public Button? ControlsMinimise { get; set; }

    /// <summary>
    /// The maximize button control
    /// </summary>
    public Button? ControlsMaximise { get; set; }

    /// <summary>
    /// The main content panel of the window
    /// </summary>
    public Panel? WindowContent { get; set; }

    /// <summary>
    /// Whether the window is minimized
    /// </summary>
    public bool IsMinimised { get; set; } = false;

    /// <summary>
    /// Whether the window is maximized
    /// </summary>
    public bool IsMaximised { get; set; } = false;

    private Vector2 _preMinimisedSize;
    private Vector2 _preMinimisedPos;
    private Vector2 _preMaximisedSize;
    private Vector2 _preMaximisedPos;

    private bool _dragging = false;
    private float _dragOffsetX = 0;
    private float _dragOffsetY = 0;
    private int _dragStartWindowX = 0;
    private int _dragStartWindowY = 0;
    private int _lastDragWindowX = 0;
    private int _lastDragWindowY = 0;

    /// <summary>
    /// Whether the window is currently being dragged
    /// </summary>
    public bool IsDragging => _dragging;

    // Resize state tracking
    internal bool _resizingRight = false;
    internal bool _resizingLeft = false;
    internal bool _resizingTop = false;
    internal bool _resizingBottom = false;
    internal float _resizeOffsetX1 = 0;
    internal float _resizeOffsetY1 = 0;
    internal float _resizeOffsetX2 = 0;
    internal float _resizeOffsetY2 = 0;
    private int _resizeStartWindowWidth = 0;
    private int _resizeStartWindowHeight = 0;
    private int _resizeStartWindowX = 0;
    private int _resizeStartWindowY = 0;

    public Window()
    {
        AddClass("panel");
        AddClass("window");
        ElementName = "window";

        // Fill the entire root panel by default
        Style.Position = PositionMode.Absolute;
        Style.FlexDirection = FlexDirection.Column;
        Style.Left = 0;
        Style.Top = 0;
        Style.Width = Length.Percent(100);
        Style.Height = Length.Percent(100);

        // Don't create TitleBar here - let CreateTitleBar handle it
        // This allows for truly optional custom title bar
    }

    /// <summary>
    /// Called when the window title changes.
    /// Override this to handle title updates in the native window.
    /// </summary>
    protected virtual void OnTitleChanged()
    {
        // Update the title bar if it exists
        if (TitleBar != null)
        {
            TitleBar.SetTitle(Title);
        }

        // Update the native window title if we have a reference
        UpdateNativeWindowTitle();
    }

    /// <summary>
    /// Sets a reference to the native window for dynamic updates.
    /// Called by the application framework.
    /// </summary>
    public void SetNativeWindow(INativeWindow nativeWindow)
    {
        _nativeWindow = nativeWindow;
        
        // Check if custom chrome is requested (either by property or CSS)
        bool customChromeRequested = HasCustomChrome || HasTitleBar;
        if (!customChromeRequested && ComputedStyle != null)
        {
            var customChromeVar = ComputedStyle.GetCustomProperty("--custom-chrome");
            customChromeRequested = customChromeVar == "true" || customChromeVar == "1";
        }
        
        // Add osdecorated class when using native window (OS handles window chrome)
        // BUT only if custom chrome is not requested
        if (!customChromeRequested)
        {
            AddClass("osdecorated");
        }
        else
        {
            // If custom chrome is requested, make the native window borderless
            _nativeWindow.SetWindowBorder(false);
        }
        
        // When native window is set, clear any panel positioning that was applied during initial layout
        // (since position/size should control native window, not panel styles)
        if (Position != Vector2.Zero)
        {
            // Reset panel position styles - fill the entire root panel instead
            Style.Position = PositionMode.Absolute;
            Style.Left = 0;
            Style.Top = 0;
            Style.Width = Length.Percent(100);
            Style.Height = Length.Percent(100);
        }
        
        UpdateNativeWindowTitle();
        UpdateNativeWindowPosition();
        UpdateNativeWindowSize();
    }

    /// <summary>
    /// Updates the native window title if a native window is set
    /// </summary>
    private void UpdateNativeWindowTitle()
    {
        _nativeWindow?.SetTitle(Title);
    }

    /// <summary>
    /// Updates the native window position if a native window is set and position was explicitly set
    /// </summary>
    private void UpdateNativeWindowPosition()
    {
        // Only set position if it was explicitly specified (not default Vector2.Zero)
        if (_nativeWindow != null && Position != Vector2.Zero)
        {
            _nativeWindow.SetPosition((int)Position.x, (int)Position.y);
        }
    }

    /// <summary>
    /// Updates the native window size if a native window is set
    /// </summary>
    private void UpdateNativeWindowSize()
    {
        _nativeWindow?.SetSize(WindowWidth, WindowHeight);
    }

    /// <summary>
    /// Create a content panel for the window
    /// </summary>
    public Panel CreateWindowContentPanel()
    {
        var contentPanel = AddChild(new Panel(this, "window-content"));
        WindowContent = contentPanel;
        return contentPanel;
    }

    protected override void OnAfterTreeRender(bool firstTime)
    {
        base.OnAfterTreeRender(firstTime);

        if (firstTime)
        {
            // Process root element attributes from Razor markup
            // This allows automatic property binding from <root> element
            ProcessRootElementAttributes();

            // Find window content (look for first non-titlebar child or explicit window-content)
            foreach (var child in Children)
            {
                if (child != null && child.HasClass("window-content"))
                {
                    WindowContent = child;
                    break;
                }
            }

            // If no explicit window-content found, wrap all children (except titlebar) in one
            if (WindowContent == null && ChildrenCount > 0)
            {
                // Create content panel and move all current children into it
                var contentPanel = new Panel();
                contentPanel.AddClass("window-content");
                var childrenToMove = Children.ToList();
                
                foreach (var child in childrenToMove)
                {
                    if (child != TitleBar && child != null)
                    {
                        child.Parent = null;
                        contentPanel.AddChild(child);
                    }
                }
                
                AddChild(contentPanel);
                WindowContent = contentPanel;
            }

            // Create title bar after processing attributes
            CreateTitleBar();

            if (AutoFocus)
            {
                FocusWindow();
                AutoFocus = false;
            }

            // Apply initial size if set and no native window
            // (with native window, size is controlled via windowwidth/windowheight)
            if (_nativeWindow == null && Size != Vector2.Zero)
            {
                Style.Width = Size.x;
                Style.Height = Size.y;
            }
        }

        // Ensure title bar is first child
        if (TitleBar != null && TitleBar.IsValid())
        {
            SetChildIndex(TitleBar, 0);
        }
    }

    /// <summary>
    /// Process attributes from the root element in Razor markup.
    /// This allows declarative property binding like &lt;root title="Hello" windowwidth="800"&gt;
    /// </summary>
    private void ProcessRootElementAttributes()
    {
        // Check for 'title' attribute
        var titleAttr = GetAttribute("title");
        if (!string.IsNullOrEmpty(titleAttr))
        {
            Title = titleAttr;
        }

        // Check for native window size attributes
        var windowWidthAttr = GetAttribute("windowwidth");
        if (!string.IsNullOrEmpty(windowWidthAttr) && int.TryParse(windowWidthAttr, out int ww))
        {
            WindowWidth = ww;
            UpdateNativeWindowSize();
        }

        var windowHeightAttr = GetAttribute("windowheight");
        if (!string.IsNullOrEmpty(windowHeightAttr) && int.TryParse(windowHeightAttr, out int wh))
        {
            WindowHeight = wh;
            UpdateNativeWindowSize();
        }

        // Check for window control flags
        var hasMinimiseAttr = GetAttribute("hasminimise");
        if (!string.IsNullOrEmpty(hasMinimiseAttr))
        {
            HasMinimise = hasMinimiseAttr == "true" || hasMinimiseAttr == "1";
        }

        var hasMaximiseAttr = GetAttribute("hasmaximise");
        if (!string.IsNullOrEmpty(hasMaximiseAttr))
        {
            HasMaximise = hasMaximiseAttr == "true" || hasMaximiseAttr == "1";
        }

        var hasCloseAttr = GetAttribute("hasclose");
        if (!string.IsNullOrEmpty(hasCloseAttr))
        {
            HasClose = hasCloseAttr == "true" || hasCloseAttr == "1";
        }

        var hasTitleBarAttr = GetAttribute("hastitlebar");
        if (!string.IsNullOrEmpty(hasTitleBarAttr))
        {
            HasTitleBar = hasTitleBarAttr == "true" || hasTitleBarAttr == "1";
        }

        var hasCustomChromeAttr = GetAttribute("hascustomchrome");
        if (!string.IsNullOrEmpty(hasCustomChromeAttr))
        {
            HasCustomChrome = hasCustomChromeAttr == "true" || hasCustomChromeAttr == "1";
        }

        // Check for size and position attributes
        var widthAttr = GetAttribute("width");
        if (!string.IsNullOrEmpty(widthAttr) && float.TryParse(widthAttr, out float width))
        {
            Size = new Vector2(width, Size.y);
        }

        var heightAttr = GetAttribute("height");
        if (!string.IsNullOrEmpty(heightAttr) && float.TryParse(heightAttr, out float height))
        {
            Size = new Vector2(Size.x, height);
        }

        var xAttr = GetAttribute("x");
        if (!string.IsNullOrEmpty(xAttr) && float.TryParse(xAttr, out float x))
        {
            Position = new Vector2(x, Position.y);
            // If native window is set, update its position
            UpdateNativeWindowPosition();
        }

        var yAttr = GetAttribute("y");
        if (!string.IsNullOrEmpty(yAttr) && float.TryParse(yAttr, out float y))
        {
            Position = new Vector2(Position.x, y);
            // If native window is set, update its position
            UpdateNativeWindowPosition();
        }

        // Check for draggable and resizable
        var isDraggableAttr = GetAttribute("isdraggable");
        if (!string.IsNullOrEmpty(isDraggableAttr))
        {
            IsDraggable = isDraggableAttr == "true" || isDraggableAttr == "1";
        }

        var isResizableAttr = GetAttribute("isresizable");
        if (!string.IsNullOrEmpty(isResizableAttr))
        {
            IsResizable = isResizableAttr == "true" || isResizableAttr == "1";
        }
    }

    /// <summary>
    /// Create and configure the title bar with controls.
    /// Only creates title bar if HasCustomChrome is true OR if theme CSS sets --custom-chrome: true
    /// </summary>
    public void CreateTitleBar()
    {
        // Check if theme requests custom chrome via CSS custom property
        bool themeRequestsChrome = false;
        if (ComputedStyle != null)
        {
            // Check for --custom-chrome CSS variable
            var customChromeVar = ComputedStyle.GetCustomProperty("--custom-chrome");
            themeRequestsChrome = customChromeVar == "true" || customChromeVar == "1";
        }

        // Don't create title bar unless explicitly requested
        if (!HasCustomChrome && !HasTitleBar && !themeRequestsChrome) return;

        // Create TitleBar if it doesn't exist
        if (TitleBar == null)
        {
            TitleBar = new TitleBar();
            TitleBar.ParentWindow = this;
            AddChild(TitleBar);
        }

        // Set title
        TitleBar.SetTitle(Title);

        // Create control buttons
        if (HasControls)
        {
            var titleElements = TitleBar.TitleElements;
            if (titleElements != null)
            {
                if (HasMinimise)
                {
                    ControlsMinimise = titleElements.AddChild(new Button());
                    ControlsMinimise.AddClass("control");
                    ControlsMinimise.AddClass("minimisebutton");
                    ControlsMinimise.Text = "0"; // Marlett font character
                    ControlsMinimise.AddEventListener("onclick", () => Minimise());
                }

                if (HasMaximise)
                {
                    ControlsMaximise = titleElements.AddChild(new Button());
                    ControlsMaximise.AddClass("control");
                    ControlsMaximise.AddClass("maximisebutton");
                    ControlsMaximise.Text = "1"; // Marlett font character
                    ControlsMaximise.AddEventListener("onclick", () => Maximise());
                }

                if (HasClose)
                {
                    ControlsClose = titleElements.AddChild(new Button());
                    ControlsClose.AddClass("control");
                    ControlsClose.AddClass("closebutton");
                    ControlsClose.Text = "r"; // Marlett font character
                    ControlsClose.AddEventListener("onclick", () => Close());
                }
            }
        }
    }

    /// <summary>
    /// Minimize the window
    /// </summary>
    public void Minimise()
    {
        if (!IsMinimised)
        {
            _preMinimisedSize = new Vector2(Box.Rect.Width, Box.Rect.Height);
            _preMinimisedPos = Position;

            // Calculate position for minimized window (bottom of parent)
            if (Parent != null)
            {
                var offset = 0f;
                // Offset for other minimized windows
                foreach (var child in Parent.Children)
                {
                    if (child is Window win && win.IsMinimised && win != this)
                    {
                        offset += 180;
                    }
                }
                Position = new Vector2(offset, Parent.Box.Rect.Height - 30);
            }

            Style.Width = 180;
            Style.Height = 30;
            IsMinimised = true;
        }
        else
        {
            // Restore
            Style.Width = _preMinimisedSize.x;
            Style.Height = _preMinimisedSize.y;
            Position = _preMinimisedPos;
            IsMinimised = false;
        }
    }

    /// <summary>
    /// Maximize the window
    /// </summary>
    public void Maximise()
    {
        if (!IsMaximised)
        {
            _preMaximisedSize = new Vector2(Box.Rect.Width, Box.Rect.Height);
            _preMaximisedPos = Position;

            Position = Vector2.Zero;

            if (Parent != null)
            {
                Style.Width = Parent.Box.Rect.Width;
                Style.Height = Parent.Box.Rect.Height;
            }

            IsMaximised = true;
        }
        else
        {
            // Restore
            Style.Width = _preMaximisedSize.x;
            Style.Height = _preMaximisedSize.y;
            Position = _preMaximisedPos;
            IsMaximised = false;
        }
    }

    /// <summary>
    /// Close the window
    /// </summary>
    public void Close()
    {
        OnClose();
        OnCloseAction?.Invoke();
        
        // Close native window if present
        _nativeWindow?.Close();
        
        Delete();
    }

    /// <summary>
    /// Action to invoke when window is closed
    /// </summary>
    public Action? OnCloseAction { get; set; }

    /// <summary>
    /// Called when the window is closed. Override to handle close events.
    /// </summary>
    public virtual void OnClose()
    {
        // Override this in derived classes
    }

    /// <summary>
    /// Focus this window and bring it to front
    /// </summary>
    public void FocusWindow()
    {
        // Note: AcceptsFocus and Focus() are not implemented in base Panel yet
        // TODO: Implement focus system
        
        // Move to front by changing child index
        if (Parent != null)
        {
            Parent.SetChildIndex(this, 0);
        }
    }

    /// <summary>
    /// Start dragging the window. For native windows, uses screen coordinates.
    /// </summary>
    public void StartDrag(Vector2 mousePos)
    {
        if (!IsDraggable) return;

        if (_nativeWindow != null)
        {
            // For native windows, use SCREEN coordinates to avoid feedback loop
            var (screenMouseX, screenMouseY) = _nativeWindow.GetScreenMousePosition();
            var (winX, winY) = _nativeWindow.GetPosition();
            _dragOffsetX = screenMouseX - winX; // Offset from window origin to mouse
            _dragOffsetY = screenMouseY - winY;
            _lastDragWindowX = winX;
            _lastDragWindowY = winY;
        }
        else
        {
            // For in-panel windows, store offset from panel position
            _dragOffsetX = mousePos.x - Box.Rect.Left;
            _dragOffsetY = mousePos.y - Box.Rect.Top;
        }
        _dragging = true;
    }

    /// <summary>
    /// Stop dragging the window
    /// </summary>
    public void StopDrag()
    {
        _dragging = false;
    }

    /// <summary>
    /// Update drag position
    /// </summary>
    public void UpdateDrag(Vector2 mousePos)
    {
        if (!_dragging) return;

        if (_nativeWindow != null)
        {
            // For native windows, use SCREEN coordinates
            var (screenMouseX, screenMouseY) = _nativeWindow.GetScreenMousePosition();
            var newX = screenMouseX - (int)_dragOffsetX;
            var newY = screenMouseY - (int)_dragOffsetY;
            
            // Only update if position actually changed (reduces redundant API calls)
            // Use cached last position to avoid expensive GetPosition() calls
            if (newX != _lastDragWindowX || newY != _lastDragWindowY)
            {
                _nativeWindow.SetPosition(newX, newY);
                _lastDragWindowX = newX;
                _lastDragWindowY = newY;
            }
        }
        else
        {
            // For in-panel windows, use local mouse position
            var newPosition = new Vector2(
                mousePos.x - _dragOffsetX,
                mousePos.y - _dragOffsetY
            );
            
            // Only update if position actually changed
            if (Position != newPosition)
            {
                Position = newPosition;
            }
        }
    }

    // -------------
    // Resizing (ported from XGUI-3)
    // -------------
    
    /// <summary>
    /// Start resizing from mouse position. Determines which edges to resize based on proximity.
    /// For borderless windows with padding, detects edges in the outer padding area.
    /// </summary>
    public void StartResize(Vector2 mousePos)
    {
        if (!IsResizable) return;
        
        var rect = Box.Rect;
        const float Distance = 5; // Match XGUI-3
        
        // Standard resize detection - check if mouse is within Distance pixels of edges
        if (mousePos.y >= rect.Bottom - Distance) _resizingBottom = true;
        if (mousePos.x >= rect.Right - Distance) _resizingRight = true;
        if (mousePos.y <= rect.Top + Distance) _resizingTop = true;
        if (mousePos.x <= rect.Left + Distance) _resizingLeft = true;
        
        if (_nativeWindow != null)
        {
            // For native windows, store screen mouse position
            var (screenMouseX, screenMouseY) = _nativeWindow.GetScreenMousePosition();
            _resizeOffsetX1 = screenMouseX;
            _resizeOffsetY1 = screenMouseY;
            
            var (winW, winH) = _nativeWindow.GetSize();
            var (winX, winY) = _nativeWindow.GetPosition();
            _resizeStartWindowWidth = winW;
            _resizeStartWindowHeight = winH;
            _resizeStartWindowX = winX;
            _resizeStartWindowY = winY;
        }
        else
        {
            // For in-panel windows, store client mouse position
            _resizeOffsetX1 = mousePos.x;
            _resizeOffsetY1 = mousePos.y;
            
            _resizeStartWindowWidth = (int)rect.Width;
            _resizeStartWindowHeight = (int)rect.Height;
            _resizeStartWindowX = (int)Position.x;
            _resizeStartWindowY = (int)Position.y;
        }
    }
    
    /// <summary>
    /// Stop resizing
    /// </summary>
    public void StopResize()
    {
        _resizingBottom = false;
        _resizingRight = false;
        _resizingTop = false;
        _resizingLeft = false;
    }
    
    /// <summary>
    /// Update resize based on mouse position
    /// </summary>
    public void UpdateResize(Vector2 mousePos, Vector2 localMousePos)
    {
        if (!IsResizable) return;
        
        var rect = Box.Rect;
        const float Distance = 5; // Match XGUI-3
        
        // Standard cursor detection - check if mouse is near edges
        bool almostBottom = mousePos.y >= rect.Bottom - Distance;
        bool almostRight = mousePos.x >= rect.Right - Distance;
        bool almostTop = mousePos.y <= rect.Top + Distance;
        bool almostLeft = mousePos.x <= rect.Left + Distance;
        
        // Set cursor based on resize position
        if ((almostLeft && almostBottom) || (_resizingLeft && _resizingBottom)) Style.Cursor = "nesw-resize";
        else if ((almostRight && almostTop) || (_resizingRight && _resizingTop)) Style.Cursor = "nesw-resize";
        else if ((almostRight && almostBottom) || (_resizingRight && _resizingBottom)) Style.Cursor = "nwse-resize";
        else if ((almostLeft && almostTop) || (_resizingLeft && _resizingTop)) Style.Cursor = "nwse-resize";
        else if (almostBottom || _resizingBottom) Style.Cursor = "ns-resize";
        else if (almostRight || _resizingRight) Style.Cursor = "ew-resize";
        else if (almostTop || _resizingTop) Style.Cursor = "ns-resize";
        else if (almostLeft || _resizingLeft) Style.Cursor = "ew-resize";
        else Style.Cursor = "unset";
        
        if (!IsResizing) return;
        
        float deltaX, deltaY;
        
        if (_nativeWindow != null)
        {
            // For native windows, calculate delta using screen coordinates
            var (screenMouseX, screenMouseY) = _nativeWindow.GetScreenMousePosition();
            deltaX = screenMouseX - _resizeOffsetX1;
            deltaY = screenMouseY - _resizeOffsetY1;
        }
        else
        {
            // For in-panel windows, use client coordinates
            deltaX = mousePos.x - _resizeOffsetX1;
            deltaY = mousePos.y - _resizeOffsetY1;
        }
        
        int newWidth = _resizeStartWindowWidth;
        int newHeight = _resizeStartWindowHeight;
        int newX = _resizeStartWindowX;
        int newY = _resizeStartWindowY;
        
        // Apply resize based on which edges are being dragged
        if (_resizingRight)
        {
            newWidth = _resizeStartWindowWidth + (int)deltaX;
        }
        if (_resizingBottom)
        {
            newHeight = _resizeStartWindowHeight + (int)deltaY;
        }
        if (_resizingLeft)
        {
            newWidth = _resizeStartWindowWidth - (int)deltaX;
            newX = _resizeStartWindowX + (int)deltaX;
        }
        if (_resizingTop)
        {
            newHeight = _resizeStartWindowHeight - (int)deltaY;
            newY = _resizeStartWindowY + (int)deltaY;
        }
        
        // Enforce minimum size
        if (newWidth < MinSize.x)
        {
            if (_resizingLeft)
            {
                newX = _resizeStartWindowX + _resizeStartWindowWidth - (int)MinSize.x;
            }
            newWidth = (int)MinSize.x;
        }
        if (newHeight < MinSize.y)
        {
            if (_resizingTop)
            {
                newY = _resizeStartWindowY + _resizeStartWindowHeight - (int)MinSize.y;
            }
            newHeight = (int)MinSize.y;
        }
        
        // Apply to native window or panel
        if (_nativeWindow != null)
        {
            _nativeWindow.SetSize(newWidth, newHeight);
            if (_resizingLeft || _resizingTop)
            {
                _nativeWindow.SetPosition(newX, newY);
            }
        }
        else
        {
            Style.Width = newWidth;
            Style.Height = newHeight;
            Size = new Vector2(newWidth, newHeight);
            if (_resizingLeft || _resizingTop)
            {
                Position = new Vector2(newX, newY);
            }
        }
        
        WindowWidth = newWidth;
        WindowHeight = newHeight;
    }
    
    /// <summary>
    /// Whether any resize is in progress
    /// </summary>
    public bool IsResizing => _resizingRight || _resizingLeft || _resizingTop || _resizingBottom;

    // -------------
    // Mouse Event Handlers
    // -------------
    
    /// <summary>
    /// Handle mouse down - focus window and start resize
    /// </summary>
    protected override void OnMouseDown(MousePanelEvent e)
    {
        base.OnMouseDown(e);
        
        // Focus the window when clicked
        FocusWindow();
        
        // For borderless windows with custom chrome, native hit testing may handle resize
        // On Windows with WM_NCHITTEST, the OS handles resize automatically
        // On other platforms, fallback to manual resize if needed
        if (HasCustomChrome || HasTitleBar)
        {
            // Skip manual resize - may be handled by native hit testing
            return;
        }
        
        // Start resize if applicable (for windows with native borders)
        var mousePos = FindRootPanel()?.MousePosition ?? Vector2.Zero;
        StartResize(mousePos);
    }
    
    /// <summary>
    /// Handle mouse up - stop resize
    /// </summary>
    protected override void OnMouseUp(MousePanelEvent e)
    {
        base.OnMouseUp(e);
        StopResize();
        StopDrag();
    }
    
    /// <summary>
    /// Handle mouse move - update resize
    /// </summary>
    protected override void OnMouseMove(MousePanelEvent e)
    {
        base.OnMouseMove(e);
        
        // For borderless windows with custom chrome, native hit testing may handle resize
        // On Windows with WM_NCHITTEST, the OS handles resize automatically
        // On other platforms, fallback to manual resize if needed
        if (HasCustomChrome || HasTitleBar)
        {
            // Skip manual resize - may be handled by native hit testing
            return;
        }
        
        var mousePos = FindRootPanel()?.MousePosition ?? Vector2.Zero;
        var localMousePos = Parent?.MousePosition ?? Vector2.Zero;
        UpdateResize(mousePos, localMousePos);
    }

    public override void Tick()
    {
        base.Tick();

        // Monitor CSS --custom-chrome property for dynamic theme changes
        UpdateCustomChromeFromCss();

        // Only apply position/size override if explicitly set (non-zero)
        // Apply position and size to style for floating window behavior
        // BUT only if there's no native window (for window-in-window scenarios)
        // AND this is not marked as a root window (AI/headless mode)
        if (_nativeWindow == null && !IsRootWindow && (Position != Vector2.Zero || Size != Vector2.Zero))
        {
            Style.Position = PositionMode.Absolute;
            
            if (Position != Vector2.Zero)
            {
                Style.Left = Position.x;
                Style.Top = Position.y;
            }
            
            if (Size != Vector2.Zero)
            {
                Style.Width = Size.x;
                Style.Height = Size.y;
            }
        }

        // Update classes
        SetClass("minimised", IsMinimised);
        SetClass("maximised", IsMaximised);
        
        // Check focus state - use native window's focus if available, otherwise use panel focus
        bool isFocused = _nativeWindow?.IsFocused ?? HasFocus;
        SetClass("unfocused", !isFocused);
    }

    /// <summary>
    /// Check the CSS --custom-chrome property and update the title bar accordingly.
    /// This allows themes to dynamically switch between native and custom chrome.
    /// </summary>
    private void UpdateCustomChromeFromCss()
    {
        if (ComputedStyle == null) return;

        // Check for --custom-chrome CSS variable
        var customChromeVar = ComputedStyle.GetCustomProperty("--custom-chrome");
        bool cssRequestsCustomChrome = customChromeVar == "true" || customChromeVar == "1";

        // Only update if the CSS value has changed (avoid repeated processing)
        if (cssRequestsCustomChrome != _lastCustomChromeFromCss)
        {
            _lastCustomChromeFromCss = cssRequestsCustomChrome;
            
            // Determine if custom chrome should be shown (from property OR CSS)
            bool shouldHaveCustomChrome = HasCustomChrome || HasTitleBar || cssRequestsCustomChrome;
            bool currentlyHasCustomChrome = TitleBar != null && TitleBar.IsValid();

            if (shouldHaveCustomChrome && !currentlyHasCustomChrome)
            {
                // Need to create title bar
                CreateTitleBar();
                
                // Update native window to be borderless when using custom chrome
                if (_nativeWindow != null)
                {
                    _nativeWindow.SetWindowBorder(false);
                    RemoveClass("osdecorated");
                }
            }
            else if (!shouldHaveCustomChrome && currentlyHasCustomChrome)
            {
                // Need to remove title bar (CSS no longer requests it and properties don't require it)
                TitleBar?.Delete();
                TitleBar = null;
                
                // Update native window to have native border
                if (_nativeWindow != null)
                {
                    _nativeWindow.SetWindowBorder(true);
                    AddClass("osdecorated");
                }
            }
        }
    }

    /// <summary>
    /// The current theme applied to this window
    /// </summary>
    public string CurrentTheme { get; protected set; } = "";

    /// <summary>
    /// Set the theme for this window by loading a stylesheet
    /// </summary>
    public void SetTheme(string theme)
    {
        var parent = this.Parent;

        // Remove existing style sheets (except .razor.scss ones) 
        foreach (var style in AllStyleSheets.ToList())
        {
            if (!style.FileName.EndsWith(".razor.scss") && !style.FileName.EndsWith(".cs.scss"))
            {
                StyleSheet.Remove(style.FileName);
            }
        }

        CurrentTheme = theme;
        
        // Resolve the stylesheet path using the same logic as StyleSheet attributes
        var resolvedPath = ResolveThemePath(theme);
        if (resolvedPath == null)
        {
            Console.WriteLine($"Error: Theme stylesheet not found: {theme}");
            return;
        }
        
        var styleToApply = UI.StyleSheet.FromFile(resolvedPath);

        // Apply the new style
        StyleSheet.Add(styleToApply);

        // Force immediate style update
        Style.Dirty();

        // Force a complete rebuild by temporarily removing from parent and re-adding
        // This is more aggressive but guarantees a full refresh
        Parent = null;
        Parent = parent;

        // Force layout recalculation - traverse child hierarchy
        ForceStyleUpdateRecursive(this);
    }
    
    /// <summary>
    /// Resolve a theme path to an absolute file path
    /// Uses the same search logic as StyleSheet attributes
    /// </summary>
    private string? ResolveThemePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        // Normalize path separators
        path = path.Replace('\\', '/');
        
        // Remove leading slash for relative resolution
        var relativePath = path.TrimStart('/');

        // Get the base directories to search
        var searchPaths = GetThemeSearchPaths();

        foreach (var basePath in searchPaths)
        {
            if (string.IsNullOrEmpty(basePath))
                continue;

            var fullPath = System.IO.Path.Combine(basePath, relativePath);
            fullPath = System.IO.Path.GetFullPath(fullPath);
            
            if (System.IO.File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    /// <summary>
    /// Gets a list of directories to search for theme stylesheets
    /// </summary>
    private IEnumerable<string> GetThemeSearchPaths()
    {
        var type = GetType();

        // 1. Assembly location (where DLLs and output files are)
        var assemblyLocation = type.Assembly.Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            var assemblyDir = System.IO.Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDir))
            {
                yield return assemblyDir;
                yield return System.IO.Path.Combine(assemblyDir, "Assets");
            }
        }

        // 2. Current working directory
        var currentDir = System.IO.Directory.GetCurrentDirectory();
        yield return currentDir;
        yield return System.IO.Path.Combine(currentDir, "Assets");

        // 3. Application base directory
        var appBase = System.AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appBase))
        {
            yield return appBase;
            yield return System.IO.Path.Combine(appBase, "Assets");
        }
    }

    private void ForceStyleUpdateRecursive(Panel panel)
    {
        // Mark this panel's style as dirty to force recalculation
        panel.Style.Dirty();

        // Update all immediate children
        foreach (var child in panel.Children)
        {
            if (child == null || !child.IsValid()) continue;

            // Mark the child's style as dirty
            child.Style.Dirty();

            // Recursively update this child's children
            ForceStyleUpdateRecursive(child);
        }
    }

    public override void SetProperty(string name, string value)
    {
        switch (name)
        {
            case "title":
                Title = value;
                return;

            case "windowwidth":
                if (int.TryParse(value, out int ww))
                {
                    WindowWidth = ww;
                    UpdateNativeWindowSize();
                }
                return;

            case "windowheight":
                if (int.TryParse(value, out int wh))
                {
                    WindowHeight = wh;
                    UpdateNativeWindowSize();
                }
                return;

            case "hastitlebar":
                HasTitleBar = value == "true" || value == "1";
                SetClass("notitlebar", !HasTitleBar);
                
                // Dynamically create or remove title bar
                if (HasTitleBar && TitleBar == null)
                {
                    CreateTitleBar();
                }
                else if (!HasTitleBar && TitleBar != null)
                {
                    TitleBar.Delete();
                    TitleBar = null;
                }
                return;

            case "hascustomchrome":
                HasCustomChrome = value == "true" || value == "1";
                SetClass("customchrome", HasCustomChrome);
                
                // Dynamically create or remove title bar
                if (HasCustomChrome && TitleBar == null)
                {
                    CreateTitleBar();
                }
                else if (!HasCustomChrome && TitleBar != null)
                {
                    TitleBar.Delete();
                    TitleBar = null;
                }
                return;

            case "hasminimise":
                HasMinimise = value == "true" || value == "1";
                return;

            case "hasmaximise":
                HasMaximise = value == "true" || value == "1";
                return;

            case "hasclose":
                HasClose = value == "true" || value == "1";
                return;

            case "isresizable":
                IsResizable = value == "true" || value == "1";
                return;

            case "isdraggable":
                IsDraggable = value == "true" || value == "1";
                return;

            case "autofocus":
                AutoFocus = value == "true" || value == "1";
                return;

            case "x":
                if (float.TryParse(value, out float x))
                {
                    Position = new Vector2(x, Position.y);
                    UpdateNativeWindowPosition();
                }
                return;

            case "y":
                if (float.TryParse(value, out float y))
                {
                    Position = new Vector2(Position.x, y);
                    UpdateNativeWindowPosition();
                }
                return;
                return;

            case "minwidth":
                if (float.TryParse(value, out float minWidth))
                    MinSize = new Vector2(minWidth, MinSize.y);
                return;

            case "minheight":
                if (float.TryParse(value, out float minHeight))
                    MinSize = new Vector2(MinSize.x, minHeight);
                return;
        }

        base.SetProperty(name, value);
    }
}
