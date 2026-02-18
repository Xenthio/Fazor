using Microsoft.AspNetCore.Components;

namespace Sandbox.UI;

/// <summary>
/// A simple User Interface panel. Can be styled with CSS.
/// Based on s&box's Panel from engine/Sandbox.Engine/Systems/UI/Panel/Panel.cs
/// </summary>
public partial class Panel : IDisposable, IStyleTarget, IComponent
{
    /// <summary>
    /// The element name. This is typically the type name lowercased.
    /// </summary>
    public string ElementName { get; set; }

    /// <summary>
    /// HTML-like id for CSS selectors
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// A collection of stylesheets applied to this panel directly.
    /// </summary>
    public StyleSheetCollection StyleSheet;

    /// <summary>
    /// All stylesheets from this panel and its ancestors.
    /// </summary>
    public IEnumerable<StyleSheet> AllStyleSheets
    {
        get
        {
            foreach (var p in AncestorsAndSelf)
            {
                if (p.StyleSheet.List == null) continue;

                foreach (var sheet in p.StyleSheet.List)
                    yield return sheet;
            }
        }
    }

    /// <summary>
    /// Pseudo-class flags for styling (hover, active, etc.)
    /// </summary>
    public PseudoClass PseudoClass
    {
        get => _pseudoClass;
        set
        {
            if (_pseudoClass == value) return;
            _pseudoClass = value;
            StyleSelectorsChanged(true, true);
        }
    }
    private PseudoClass _pseudoClass = PseudoClass.Intro;

    /// <summary>
    /// Whether this panel has the :focus pseudo class active.
    /// </summary>
    public bool HasFocus => (PseudoClass & PseudoClass.Focus) != 0;

    /// <summary>
    /// Whether this panel has the :active pseudo class active.
    /// </summary>
    public bool HasActive => (PseudoClass & PseudoClass.Active) != 0;

    /// <summary>
    /// Whether this panel has the :hover pseudo class active.
    /// </summary>
    public bool HasHovered => (PseudoClass & PseudoClass.Hover) != 0;

    /// <summary>
    /// Whether this panel has the :intro pseudo class active.
    /// </summary>
    public bool HasIntro => (PseudoClass & PseudoClass.Intro) != 0;

    /// <summary>
    /// Whether this panel has the :outro pseudo class active.
    /// </summary>
    public bool HasOutro => (PseudoClass & PseudoClass.Outro) != 0;

    /// <summary>
    /// Yoga layout node
    /// </summary>
    public YogaWrapper? YogaNode { get; private set; }

    /// <summary>
    /// Whether this panel is valid and not deleted
    /// </summary>
    public bool IsValid() => YogaNode != null && !IsDeleted;

    /// <summary>
    /// Whether this panel is being deleted
    /// </summary>
    public bool IsDeleting { get; private set; }

    /// <summary>
    /// Whether this panel has been deleted
    /// </summary>
    public bool IsDeleted { get; private set; }

    public Panel()
    {
        InitializeEvents();

        YogaNode = new YogaWrapper(this);
        Style = new PanelStyle(this);
        StyleSheet = new StyleSheetCollection(this);
        Transitions = new Transitions(this);

        ElementName = GetType().Name.ToLower();
        Switch(PseudoClass.Empty, true);

        LoadStyleSheet();
    }

    public Panel(Panel? parent) : this()
    {
        if (parent != null)
            Parent = parent;
    }

    public Panel(Panel? parent, string? classnames) : this(parent)
    {
        if (classnames != null)
            AddClass(classnames);
    }

    /// <summary>
    /// Switch a pseudo class on or off.
    /// </summary>
    public bool Switch(PseudoClass c, bool state)
    {
        if (state == ((PseudoClass & c) != 0)) return false;

        if (state)
            PseudoClass |= c;
        else
            PseudoClass &= ~c;

        return true;
    }

    /// <summary>
    /// Switch a pseudo class on or off for a panel and all its ancestors.
    /// This is how s&box applies :hover and :active - the states bubble up to parent panels.
    /// </summary>
    /// <param name="c">The pseudo class to switch</param>
    /// <param name="state">Whether to turn it on or off</param>
    /// <param name="panel">The panel to start from</param>
    /// <param name="unlessAncestorOf">Optional panel - if the target is an ancestor of this panel, don't apply the state</param>
    internal static void Switch(PseudoClass c, bool state, Panel? panel, Panel? unlessAncestorOf = null)
    {
        if (panel == null)
            return;

        foreach (var target in panel.AncestorsAndSelf)
        {
            if (unlessAncestorOf != null && unlessAncestorOf.IsAncestor(target))
                continue;

            target.Switch(c, state);
        }
    }

    /// <summary>
    /// Return true if this panel isn't hidden by opacity or display mode.
    /// </summary>
    public bool IsVisible { get; internal set; } = true;

    /// <summary>
    /// Return true if this panel itself is visible (not affected by parent).
    /// </summary>
    public bool IsVisibleSelf { get; internal set; } = true;

    /// <summary>
    /// Called every frame.
    /// </summary>
    public virtual void Tick()
    {
    }

    /// <summary>
    /// Called after the parent of this panel has changed.
    /// </summary>
    public virtual void OnParentChanged()
    {
    }

    /// <summary>
    /// Returns true if this panel would like the mouse cursor to be visible.
    /// </summary>
    public virtual bool WantsMouseInput()
    {
        if (ComputedStyle == null) return false;
        if (!IsVisibleSelf) return false;
        if (ComputedStyle.PointerEvents == UI.PointerEvents.All) return true;
        if (_children == null) return false;

        foreach (var child in _children)
        {
            if (child?.WantsMouseInput() ?? false)
                return true;
        }

        return false;
    }

    internal void TickInternal()
    {
        if (IsDeleting)
        {
            SetNeedsPreLayout();
            return;
        }

        try
        {
            if (ParentHasChanged)
            {
                ParentHasChanged = false;
                OnParentChanged();
                StyleSelectorsChanged(true, true);
            }

            var didBuildRenderTree = false;
            var isFirstRender = renderTree == null;

            // Process Razor render tree if dirty
            InternalTreeBinds();
            
            // Handle parameter changes and render tree - matches s&box order
            if (HasRenderTree || _templateBindsChanged)
            {
                if (_templateBindsChanged)
                {
                    _templateBindsChanged = false;
                    razorTreeDirty = true;
                    ParametersChanged(true);
                }
                
                if (razorTreeDirty)
                {
                    InternalRenderTree();
                    didBuildRenderTree = true;
                }
            }

            // Update ::before and ::after pseudo-elements
            UpdateBeforeAfterElements();

            // Tick styles if dirty or animating
            if (Style.IsDirty || HasActiveTransitions || (ComputedStyle?.HasAnimation ?? false) || ScrollVelocity != Vector2.Zero || IsDragScrolling)
            {
                SetNeedsPreLayout();
            }

            // Tick visible children
            if (IsVisible && _children != null && _children.Count > 0)
            {
                for (int i = _children.Count - 1; _children != null && i >= 0; i--)
                {
                    _children[i]?.TickInternal();
                }
            }

            // Defer OnAfterTreeRender so that children are all processed too (matches S&box)
            if (didBuildRenderTree)
            {
                OnAfterTreeRender(isFirstRender);
            }

            RunPendingEvents();
            Tick();
            RunPendingEvents();

            AddScrollVelocity();
            RunClassBinds();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Panel.TickInternal error: {e}");
        }
    }

    /// <summary>
    /// Get the render order index for this panel.
    /// This is used to determine which panel is on top when hit testing.
    /// </summary>
    internal int GetRenderOrderIndex()
    {
        return SiblingIndex + (ComputedStyle?.ZIndex ?? 0);
    }

    /// <summary>
    /// Convert a point from screen space to a point representing a delta on this panel
    /// where the top left is [0,0] and the bottom right is [1,1]
    /// </summary>
    public Vector2 ScreenPositionToPanelDelta(Vector2 pos)
    {
        pos = ScreenPositionToPanelPosition(pos);

        var x = pos.x / Box.Rect.Width;
        var y = pos.y / Box.Rect.Height;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Convert a point from screen space to a position relative to the top left of this panel.
    /// Note: GlobalMatrix transforms FROM screen space TO panel space, so we use it directly here.
    /// This matches s&box implementation exactly.
    /// </summary>
    public Vector2 ScreenPositionToPanelPosition(Vector2 pos)
    {
        if (GlobalMatrix.HasValue)
        {
            pos = GlobalMatrix.Value.Transform(pos);
        }

        var x = pos.x - Box.Rect.Left;
        var y = pos.y - Box.Rect.Top;

        return new Vector2(x, y);
    }

    /// <summary>
    /// Convert a point from local panel space to screen space.
    /// Note: Since GlobalMatrix is screen-to-panel, we use its inverse for panel-to-screen.
    /// This matches s&box implementation exactly.
    /// </summary>
    public Vector2 PanelPositionToScreenPosition(Vector2 pos)
    {
        var screenPos = new Vector2(pos.x + Box.Rect.Left, pos.y + Box.Rect.Top);

        if (GlobalMatrix.HasValue)
        {
            screenPos = GlobalMatrix.Value.Inverted().Transform(screenPos);
        }

        return screenPos;
    }

    /// <summary>
    /// Find and return any children of this panel (including self) within the given rect.
    /// </summary>
    /// <param name="box">The area to look for panels in, in screen-space coordinates.</param>
    /// <param name="fullyInside">Whether we want only the panels that are completely within the given bounds.</param>
    public IEnumerable<Panel> FindInRect(Rect box, bool fullyInside)
    {
        if (!IsVisible)
            yield break;

        if (!IsInside(box, fullyInside))
            yield break;

        yield return this;

        if (!HasChildren)
            yield break;

        foreach (var child in Children)
        {
            foreach (var found in child.FindInRect(box, fullyInside))
            {
                yield return found;
            }
        }
    }

    /// <summary>
    /// Allow selecting child text
    /// </summary>
    public bool AllowChildSelection { get; set; }

    /// <summary>
    /// Collect selected text from child labels.
    /// </summary>
    internal string? CollectSelectedChildrenText(Panel p)
    {
        if (!p.IsVisible)
            return null;

        // TODO: When Label has GetSelectedText(), use it here:
        // if (p is Label label)
        // {
        //     return label.GetSelectedText();
        // }

        string? selection = null;

        var lines = p.ComputedStyle?.FlexDirection == FlexDirection.Column;

        foreach (var child in p.Children)
        {
            var sel = CollectSelectedChildrenText(child);
            if (string.IsNullOrEmpty(sel)) continue;

            if (selection == null) selection = sel;
            else selection = $"{selection}{(lines ? "\n" : " ")}{sel}";
        }

        return selection;
    }

    /// <summary>
    /// If AllowChildSelection is enabled, select all children text.
    /// TODO: This requires Label.ShouldDrawSelection, SelectionStart, SelectionEnd which are not yet implemented.
    /// </summary>
    public void SelectAllInChildren()
    {
        // TODO: Enable when Label has selection support
        /*
        if (this is Label label)
        {
            label.ShouldDrawSelection = true;
            label.SelectionStart = 0;
            label.SelectionEnd = int.MaxValue;
            return;
        }
        */

        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.SelectAllInChildren();
            }
        }
    }

    /// <summary>
    /// Clear any selection in children.
    /// TODO: This requires Label.ShouldDrawSelection which is not yet implemented.
    /// </summary>
    public void UnselectAllInChildren()
    {
        // TODO: Enable when Label has selection support
        /*
        if (this is Label label)
        {
            label.ShouldDrawSelection = false;
            return;
        }
        */

        if (HasChildren)
        {
            foreach (var child in Children)
            {
                child.UnselectAllInChildren();
            }
        }
    }

    /// <summary>
    /// Called when the current language has changed.
    /// This allows you to rebuild anything that might need rebuilding.
    /// </summary>
    public virtual void LanguageChanged()
    {
        foreach (var child in Children)
        {
            child.LanguageChanged();
        }
    }

    /// <summary>
    /// Called when a hotload happened. (Not necessarily on this panel)
    /// </summary>
    public virtual void OnHotloaded()
    {
        LoadStyleSheet();
        InitializeEvents();

        // If our render tree changed, rebuild it
        if (razorLastTreeChecksum != GetRenderTreeChecksum())
        {
            razorLastTreeChecksum = GetRenderTreeChecksum();
            if (renderTree != null)
            {
                renderTree?.Clear();
                razorTreeDirty = true;
            }
        }

        // Remove any null children that may have been deleted
        _children?.RemoveAll(x => x is null);
        _renderChildren?.RemoveAll(x => x is null);

        foreach (var child in Children)
        {
            try
            {
                child.OnHotloaded();
            }
            catch (Exception e)
            {
                Log.Error($"Error in OnHotloaded: {e}");
            }
        }
    }

    /// <summary>
    /// Called when the panel should mark that styles need to be rebuilt
    /// </summary>
    internal void MarkStylesRebuilt()
    {
        inRebuildStyleRulesList = false;
        inRebuildStyleRulesList_Ancestors = false;
        inRebuildStyleRulesList_Children = false;
    }

    bool inRebuildStyleRulesList;
    bool inRebuildStyleRulesList_Ancestors;
    bool inRebuildStyleRulesList_Children;

    /// <summary>
    /// Should be called when something happens that means that this panel's stylesheets need to be
    /// re-evaluated. Like becoming hovered or classes changed. You don't call this when changing styles
    /// directly on the panel, just on anything that will change which stylesheets should get selected.
    /// </summary>
    /// <param name="ancestors">Also re-evaluate all ancestor panels.</param>
    /// <param name="descendants">Also re-evaluate all child panels.</param>
    /// <param name="root">Root panel cache so we don't need to keep looking it up.</param>
    internal virtual void StyleSelectorsChanged(bool ancestors, bool descendants, RootPanel? root = null)
    {
        root ??= FindRootPanel();
        if (root == null)
            return;

        if (ancestors && !inRebuildStyleRulesList_Ancestors)
        {
            inRebuildStyleRulesList_Ancestors = true;
            Parent?.StyleSelectorsChanged(true, false, root);
        }

        if (descendants && !inRebuildStyleRulesList_Children && HasChildren)
        {
            inRebuildStyleRulesList_Children = true;

            foreach (var child in Children)
            {
                child.StyleSelectorsChanged(false, true, root);
            }
        }

        if (!inRebuildStyleRulesList)
        {
            inRebuildStyleRulesList = true;
            root.AddToBuildStyleRulesList(this);
        }
    }

    public override string ToString()
    {
        var classes = _class?.Count > 0 ? $".{string.Join(".", _class)}" : "";
        return $"<{ElementName}{classes}>";
    }

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        YogaNode?.Dispose();
        YogaNode = null;

        GC.SuppressFinalize(this);
    }

    ~Panel()
    {
        Dispose();
    }
    // Source location tracking (for debugging Razor-generated panels)
    public string? SourceFile { get; set; }
    public int SourceLine { get; set; }
    
    // Template slot handling for S&box compatibility
    // When a child has slot="name", it gets passed to OnTemplateSlot on the parent
    // which can handle it appropriately (e.g., TabContainer handles slot="tab")
    public virtual void OnTemplateSlot(Sandbox.Html.Node node, string? slotName, Panel panel)
    {
        // Bubble up to parent if we don't handle it
        Parent?.OnTemplateSlot(node, slotName, panel);
    }
    
    public virtual void OnTemplateSlot(string slotName)
    {
        // TODO: Implement parameter-based slot handling
        // This could be used with [PanelSlot("slotname")] attributes on properties
    }
    
    /// <summary>
    /// True when parameters have been set and OnParametersSet needs to be called
    /// </summary>
    private bool _templateBindsChanged = true;
    private Task? _parametersSetTask;
    
    /// <summary>
    /// Parameter change notification - marks panel for OnParametersSet callback.
    /// Matches S&box's implementation.
    /// </summary>
    public void ParametersChanged(bool immediately)
    {
        _templateBindsChanged = true;
        
        // Task is still running
        if (_parametersSetTask != null && !_parametersSetTask.IsCompleted)
            return;
        
        if (immediately)
        {
            _templateBindsChanged = false;
            razorTreeDirty = true;
            _parametersSetTask = OnParametersSetInternalAsync();
        }
    }
    
    internal async Task OnParametersSetInternalAsync()
    {
        try
        {
            await OnParametersSetAsync();
        }
        catch (TaskCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Log.Warning($"Exception in OnParametersSetAsync: {e.Message}");
        }
        
        if (!IsValid())
            return;
        
        try
        {
            OnParametersSet();
        }
        catch (Exception e)
        {
            Log.Warning($"Exception in OnParametersSet: {e.Message}");
        }
        
        StateHasChanged();
    }
}
    #endregion
