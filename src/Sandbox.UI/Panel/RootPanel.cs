namespace Sandbox.UI;

/// <summary>
/// A root panel. Serves as a container for other panels, handles things such as rendering.
/// Based on s&box's RootPanel from engine/Sandbox.Engine/Systems/UI/RootPanel.cs
/// </summary>
public partial class RootPanel : Panel
{
    /// <summary>
    /// Optional callback for processing button events before normal handling.
    /// Used by Panel Inspector to intercept F12 key. Returns true if event was handled.
    /// </summary>
    public static Func<RootPanel, string, bool, KeyboardModifiers, bool>? ButtonEventInterceptor { get; set; }

    /// <summary>
    /// Bounds of the panel (size and position on screen)
    /// </summary>
    public Rect PanelBounds { get; set; } = new Rect(0, 0, 512, 512);

    /// <summary>
    /// If any of our panels are visible and want mouse input
    /// </summary>
    internal bool ChildrenWantMouseInput { get; set; }

    /// <summary>
    /// The scale of this panel and its children
    /// </summary>
    public float Scale { get; protected set; } = 1.0f;

    /// <summary>
    /// System DPI scale factor. Set by the windowing system.
    /// Default is 1.0 (96 DPI). Value of 1.5 means 144 DPI, 2.0 means 192 DPI, etc.
    /// </summary>
    public static float SystemDpiScale { get; set; } = 1.0f;

    /// <summary>
    /// If set to true this panel won't be rendered to the screen like a normal panel
    /// </summary>
    public bool RenderedManually { get; set; }

    /// <summary>
    /// True if this is a world panel
    /// </summary>
    public virtual bool IsWorldPanel { get; set; }

    /// <summary>
    /// Current global mouse position
    /// </summary>
    internal Vector2 MousePos;

    /// <summary>
    /// Input handler for this root panel
    /// </summary>
    internal PanelInput Input { get; private set; }

    public RootPanel()
    {
        Style.Width = Length.Percent(100);
        Style.Height = Length.Percent(100);
        Input = new PanelInput();
    }

    public override void Delete(bool immediate = true)
    {
        base.Delete(immediate);
    }

    /// <summary>
    /// This is called from tests to emulate the regular root panel simulate loop
    /// </summary>
    public void Layout()
    {
        TickInternal();
        PreLayout();
        CalculateLayout();
        PostLayout();
    }

    /// <summary>
    /// Force a complete re-layout of all children.
    /// Call this when the root panel bounds have changed significantly.
    /// </summary>
    public void InvalidateLayout()
    {
        layoutHash = 0;
        StyleSelectorsChanged(true, true);
        SkipAllTransitions();
        
        // Force all Yoga nodes to reinitialize on next layout
        InvalidateYogaRecursive(this);
    }
    
    private static void InvalidateYogaRecursive(Panel panel)
    {
        if (panel.YogaNode != null)
        {
            panel.YogaNode.Initialized = false;
        }
        panel.needsPreLayout = true;
        panel.needsFinalLayout = true;
        
        if (panel._children != null)
        {
            foreach (var child in panel._children)
            {
                if (child != null)
                {
                    InvalidateYogaRecursive(child);
                }
            }
        }
    }

    private int layoutHash;

    /// <summary>
    /// Called before layout to lock the bounds of this root panel
    /// </summary>
    protected virtual void UpdateBounds(Rect rect)
    {
        PanelBounds = rect;
    }

    /// <summary>
    /// Work out scaling here. Uses system DPI if available, otherwise falls back to resolution-based scaling.
    /// </summary>
    protected virtual void UpdateScale(Rect screenSize)
    {
        // Use system DPI scale if set (preferred), otherwise fall back to resolution-based scaling
        if (SystemDpiScale > 0.1f && Math.Abs(SystemDpiScale - 1.0f) > 0.001f)
        {
            Scale = SystemDpiScale;
        }
        else
        {
            // Fall back to resolution-based scaling relative to 1080p
            Scale = screenSize.Height / 1080.0f;
        }
        Scale = Math.Clamp(Scale, 0.1f, 10.0f);
    }

    internal void TickInputInternal()
    {
        ChildrenWantMouseInput = WantsMouseInput();
    }

    internal void PreLayout(Rect screenSize)
    {
        UpdateBounds(screenSize);
        UpdateScale(PanelBounds);
        PreLayout();
    }

    internal void PreLayout()
    {
        var cascade = new LayoutCascade
        {
            Scale = Scale,
            Root = this,
        };

        Style.Left = 0.0f;
        Style.Top = 0.0f;
        Style.Width = Length.Pixels(PanelBounds.Width * (1 / Scale));
        Style.Height = Length.Pixels(PanelBounds.Height * (1 / Scale));

        var hash = HashCode.Combine(PanelBounds.Width, PanelBounds.Height, Scale);
        if (hash != layoutHash)
        {
            layoutHash = hash;
            StyleSelectorsChanged(true, true);
            SkipAllTransitions();
            cascade.SelectorChanged = true;
        }

        BuildStyleRules();

        PushRootValues();

        base.PreLayout(cascade);
    }

    internal void CalculateLayout()
    {
        if (YogaNode == null) return;

        PushRootValues();
        YogaNode.CalculateLayout();
    }

    internal void PostLayout()
    {
        PushRootValues();
        FinalLayout(Vector2.Zero);
    }

    internal void PushRootValues()
    {
        Length.RootSize = new Vector2(PanelBounds.Width, PanelBounds.Height);
        Length.RootFontSize = ComputedStyle?.FontSize?.GetPixels(16f) ?? 16f;
        Length.RootScale = ScaleToScreen;
    }

    public override void OnLayout(ref Rect layoutRect)
    {
        layoutRect = PanelBounds;
    }

    /// <summary>
    /// Render this panel manually. Must have RenderedManually set to true.
    /// </summary>
    public void RenderManual(IPanelRenderer renderer, float opacity = 1.0f)
    {
        if (!RenderedManually && !IsWorldPanel)
            throw new Exception($"{nameof(RenderedManually)} must be set to true to render this panel manually.");

        renderer.Render(this, opacity);
    }

    internal void SkipAllTransitions()
    {
        // Skip transitions implementation
    }

    /// <summary>
    /// Update input for this root panel
    /// </summary>
    public void UpdateInput(Vector2 mousePosition, bool mouseIsActive)
    {
        Input.Tick(new[] { this }, mousePosition, mouseIsActive);
        InputFocus.Tick();
    }

    /// <summary>
    /// Process a button event (mouse or keyboard)
    /// </summary>
    public void ProcessButtonEvent(string button, bool pressed, KeyboardModifiers modifiers = KeyboardModifiers.None)
    {
        // Check if there's an interceptor (e.g., Panel Inspector)
        if (ButtonEventInterceptor?.Invoke(this, button, pressed, modifiers) == true)
        {
            return; // Event was handled by interceptor
        }

        if (button.StartsWith("mouse"))
        {
            Input.AddMouseButton(button, pressed);
        }
        else
        {
            // Keyboard button event
            var e = new ButtonEvent(button, pressed, modifiers);
            var target = Input.Hovered ?? InputFocus.Current;
            target?.OnButtonEvent(e);

            if (pressed && InputFocus.Current != null)
            {
                InputFocus.Current.OnButtonTyped(e);
            }
        }
    }

    /// <summary>
    /// Process a character typed event
    /// </summary>
    public void ProcessCharTyped(char character)
    {
        InputFocus.Current?.OnKeyTyped(character);
    }

    /// <summary>
    /// Process mouse wheel event
    /// </summary>
    public void ProcessMouseWheel(Vector2 delta, KeyboardModifiers modifiers = KeyboardModifiers.None)
    {
        // Windows apps translate vertical scroll to horizontal if shift is held
        if (modifiers.HasFlag(KeyboardModifiers.Shift))
        {
            delta = new Vector2(-delta.y, 0);
        }

        Input.Hovered?.OnMouseWheel(delta);
    }

    /// <summary>
    /// Get the cursor that should be displayed based on the currently hovered panel's CSS cursor property.
    /// Returns null if no cursor is specified or if it's set to "auto".
    /// </summary>
    public StandardCursor? GetCurrentCursor()
    {
        var hovered = Input.Hovered;
        if (hovered == null) return null;

        // Walk up the hierarchy to find the first panel with a cursor style
        while (hovered != null)
        {
            if (hovered.ComputedStyle?.Cursor != null)
            {
                var cursor = CursorHelper.FromCssString(hovered.ComputedStyle.Cursor);
                if (cursor.HasValue)
                    return cursor.Value;
            }

            hovered = hovered.Parent;
        }

        return null; // Default to system cursor
    }

    /// <summary>
    /// A list of panels waiting to have their styles re-evaluated
    /// </summary>
    private readonly HashSet<Panel> styleRuleUpdates = new();

    /// <summary>
    /// Add panel to list to have styles re-evaluated
    /// </summary>
    internal void AddToBuildStyleRulesList(Panel panel)
    {
        styleRuleUpdates.Add(panel);
    }

    /// <summary>
    /// Run through all panels pending a re-check on their style rules
    /// </summary>
    internal void BuildStyleRules()
    {
        if (styleRuleUpdates.Count == 0) return;

        foreach (var panel in styleRuleUpdates)
        {
            if (!panel.IsValid()) continue;

            if (panel.Style.BuildRulesInThread())
            {
                panel.SetNeedsPreLayout();
            }

            panel.MarkStylesRebuilt();
        }

        styleRuleUpdates.Clear();
    }

    /// <summary>
    /// Dirty all styles on this root panel and its children.
    /// Called when stylesheets are hotloaded to force a complete style rebuild.
    /// </summary>
    internal void DirtyAllStyles()
    {
        DirtyStylesRecursive();
    }

    /// <summary>
    /// Call OnHotloaded on this panel and all children.
    /// Called when razor files or stylesheets are hotloaded.
    /// </summary>
    internal void TriggerHotload()
    {
        OnHotloaded();
    }
}
