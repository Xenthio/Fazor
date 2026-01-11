namespace Sandbox.UI;

using Matrix = System.Numerics.Matrix4x4;

/// <summary>
/// Panel partial class: Layout computation
/// Based on s&box's Panel.Layout.cs
/// </summary>
public partial class Panel
{
    /// <summary>
    /// Access to various bounding boxes of this panel.
    /// </summary>
    public Box Box { get; } = new Box();

    /// <summary>
    /// If true, calls <see cref="DrawContent( ref RenderState )"/>.
    /// </summary>
    public virtual bool HasContent => false;

    /// <summary>
    /// The velocity of the current scroll
    /// </summary>
    public Vector2 ScrollVelocity;

    /// <summary>
    /// Offset of the panel's children position for scrolling purposes.
    /// </summary>
    public Vector2 ScrollOffset { get; set; }

    /// <summary>
    /// Scale of the panel on the screen.
    /// </summary>
    public float ScaleToScreen { get; internal set; } = 1.0f;

    /// <summary>
    /// Inverse scale of <see cref="ScaleToScreen"/>.
    /// </summary>
    public float ScaleFromScreen => 1.0f / ScaleToScreen;

    int LayoutCount = 0;

    /// <summary>
    /// If this panel has transforms, they'll be reflected here
    /// </summary>
    public Matrix? LocalMatrix { get; set; }

    /// <summary>
    /// If this panel or its parents have transforms, they'll be compounded here.
    /// </summary>
    public Matrix? GlobalMatrix { get; set; }

    /// <summary>
    /// The matrix that is applied as a result of transform: styles
    /// </summary>
    public Matrix TransformMatrix { get; set; }

    /// <summary>
    /// The computed style has a non-default backdrop filter property
    /// </summary>
    internal bool HasBackdropFilter { get; private set; }

    /// <summary>
    /// The computed style has a non-default filter property
    /// </summary>
    internal bool HasFilter { get; private set; }

    /// <summary>
    /// The computed style has a renderable background
    /// </summary>
    public bool HasBackground { get; internal set; }

    /// <summary>
    /// Whether the panel can scroll horizontally
    /// </summary>
    public bool HasScrollX => ScrollSize.x > 0 && ComputedStyle?.OverflowX == OverflowMode.Scroll;

    /// <summary>
    /// Whether the panel can scroll vertically
    /// </summary>
    public bool HasScrollY => ScrollSize.y > 0 && ComputedStyle?.OverflowY == OverflowMode.Scroll;

    /// <summary>
    /// The currently calculated opacity.
    /// This is set by multiplying our current style opacity with our parent's opacity.
    /// </summary>
    public float Opacity { get; private set; } = 1.0f;

    /// <summary>
    /// If true, we'll try to stay scrolled to the bottom when the panel changes size
    /// </summary>
    public bool PreferScrollToBottom { get; set; }

    /// <summary>
    /// Whether the scrolling is currently pinned to the bottom of the panel as dictated by <see cref="PreferScrollToBottom"/>.
    /// </summary>
    public bool IsScrollAtBottom { get; private set; }

    /// <summary>
    /// The size of the scrollable area within this panel.
    /// </summary>
    public Vector2 ScrollSize { get; private set; }

    bool IsDragScrolling;
    bool isScrolling;
    Vector2 scrollVelocityVelocity;

    internal bool needsPreLayout = true;
    internal bool needsFinalLayout = true;

    internal void SetNeedsPreLayout()
    {
        if (needsPreLayout) return;
        needsPreLayout = true;
        needsFinalLayout = true;
        Parent?.SetNeedsPreLayout();
    }

    internal virtual void PreLayout(LayoutCascade cascade)
    {
        if (YogaNode == null) return;

        if (!needsPreLayout && !cascade.SelectorChanged && !cascade.ParentChanged)
            return;

        needsPreLayout = false;

        if (IndexesDirty)
        {
            UpdateChildrenIndexes();
        }

        // Build computed style
        ComputedStyle = Style.BuildFinal(ref cascade, out bool changed);
        cascade.ParentStyles = ComputedStyle;

        PushLengthValues();

        ScaleToScreen = cascade.Scale;
        Opacity = (ComputedStyle.Opacity ?? 1.0f) * (Parent?.Opacity ?? 1.0f);
        UpdateVisibility();

        if (changed || !YogaNode.Initialized)
        {
            UpdateYoga();
        }

        // Update HasBackground based on current ComputedStyle
        // This needs to be checked every PreLayout, not just when changed is true
        HasBackground = ComputedStyle.BackgroundColor?.a > 0f ||
            (ComputedStyle.BackgroundGradient?.IsValid ?? false) ||
            (ComputedStyle.BorderLeftColor?.a > 0f && ComputedStyle.BorderLeftWidth?.GetPixels(1.0f) > 0f) ||
            (ComputedStyle.BorderTopColor?.a > 0f && ComputedStyle.BorderTopWidth?.GetPixels(1.0f) > 0f) ||
            (ComputedStyle.BorderRightColor?.a > 0f && ComputedStyle.BorderRightWidth?.GetPixels(1.0f) > 0f) ||
            (ComputedStyle.BorderBottomColor?.a > 0f && ComputedStyle.BorderBottomWidth?.GetPixels(1.0f) > 0f);

        // Update CSS order property
        UpdateOrder();

        if (changed)
        {
            _renderChildrenDirty = true;
        }

        if (!IsVisibleSelf) return;

        if (_children == null || _children.Count == 0) return;

        cascade.ParentChanged = cascade.ParentChanged || changed;

        for (int i = 0; i < _children.Count; i++)
        {
            _children[i].PreLayout(cascade);
        }

        SortChildrenOrder();
    }

    internal void UpdateYoga()
    {
        if (ComputedStyle == null || YogaNode == null) return;

        YogaNode.Width = ComputedStyle.Width;
        YogaNode.Height = ComputedStyle.Height;
        YogaNode.MaxWidth = ComputedStyle.MaxWidth;
        YogaNode.MaxHeight = ComputedStyle.MaxHeight;
        YogaNode.MinWidth = ComputedStyle.MinWidth;
        YogaNode.MinHeight = ComputedStyle.MinHeight;
        YogaNode.Display = ComputedStyle.Display;

        YogaNode.Left = ComputedStyle.Left;
        YogaNode.Right = ComputedStyle.Right;
        YogaNode.Top = ComputedStyle.Top;
        YogaNode.Bottom = ComputedStyle.Bottom;

        YogaNode.MarginLeft = ComputedStyle.MarginLeft;
        YogaNode.MarginRight = ComputedStyle.MarginRight;
        YogaNode.MarginTop = ComputedStyle.MarginTop;
        YogaNode.MarginBottom = ComputedStyle.MarginBottom;

        YogaNode.PaddingLeft = ComputedStyle.PaddingLeft;
        YogaNode.PaddingRight = ComputedStyle.PaddingRight;
        YogaNode.PaddingTop = ComputedStyle.PaddingTop;
        YogaNode.PaddingBottom = ComputedStyle.PaddingBottom;

        YogaNode.BorderLeftWidth = ComputedStyle.BorderLeftWidth;
        YogaNode.BorderTopWidth = ComputedStyle.BorderTopWidth;
        YogaNode.BorderRightWidth = ComputedStyle.BorderRightWidth;
        YogaNode.BorderBottomWidth = ComputedStyle.BorderBottomWidth;

        YogaNode.PositionType = ComputedStyle.Position;
        YogaNode.AspectRatio = ComputedStyle.AspectRatio;
        YogaNode.FlexGrow = ComputedStyle.FlexGrow;
        YogaNode.FlexShrink = ComputedStyle.FlexShrink;
        YogaNode.FlexBasis = ComputedStyle.FlexBasis;
        YogaNode.Wrap = ComputedStyle.FlexWrap;

        YogaNode.AlignContent = ComputedStyle.AlignContent;
        YogaNode.AlignItems = ComputedStyle.AlignItems;
        YogaNode.AlignSelf = ComputedStyle.AlignSelf;
        YogaNode.FlexDirection = ComputedStyle.FlexDirection;
        YogaNode.JustifyContent = ComputedStyle.JustifyContent;
        YogaNode.Overflow = ComputedStyle.Overflow;

        YogaNode.RowGap = ComputedStyle.RowGap;
        YogaNode.ColumnGap = ComputedStyle.ColumnGap;

        YogaNode.Initialized = true;
    }

    private void PushLengthValues()
    {
        Length.CurrentFontSize = ComputedStyle?.FontSize?.GetPixels(16f) ?? 16f;
    }

    internal void UpdateVisibility()
    {
        bool old = IsVisible;

        // Match S&box's CalcVisible behavior exactly
        IsVisibleSelf = ComputedStyle?.CalcVisible() ?? false;
        IsVisibleSelf = IsVisibleSelf || HasActiveTransitions;
        IsVisible = IsVisibleSelf && (Parent?.IsVisible ?? true);

        if (old == IsVisible) return;

        if (Parent != null)
        {
            Parent.IndexesDirty = true;
        }

        var c = _children?.Count ?? 0;
        for (int i = 0; i < c; i++)
        {
            _children![i].UpdateVisibility();
        }

        try
        {
            OnVisibilityChanged();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Warning in OnVisibilityChanged: {e.Message}");
        }
    }

    /// <summary>
    /// Called when the visibility of the current panel changes. This could be because our own style changed, or a parent style.
    /// You can check visibility using <see cref="IsVisible"/> and <see cref="IsVisibleSelf"/>.
    /// </summary>
    protected virtual void OnVisibilityChanged()
    {
    }

    /// <summary>
    /// This panel has just been laid out. You can modify its position now.
    /// </summary>
    public virtual void OnLayout(ref Rect layoutRect)
    {
    }

    private int layoutHash;

    /// <summary>
    /// Final layout pass - reads Yoga results and updates Box rects.
    /// </summary>
    public virtual void FinalLayout(Vector2 offset)
    {
        if (ComputedStyle == null) return;
        if (YogaNode == null) return;

        PushLengthValues();

        var hash = HashCode.Combine(offset, ScrollOffset, ScrollVelocity, ComputedStyle?.Transform, Opacity, ComputedStyle.Display);
        if (layoutHash == hash && !YogaNode.HasNewLayout && !needsFinalLayout) return;

        needsFinalLayout = false;
        layoutHash = hash;

        Box.Rect = YogaNode.YogaRect;
        Box.Rect.Position += offset;

        OnLayout(ref Box.Rect);

        Box.Padding = YogaNode.Padding;
        Box.Margin = YogaNode.Margin;
        Box.Border = YogaNode.Border;

        Box.RectOuter = Box.Rect.Grow(YogaNode.Margin.Left, YogaNode.Margin.Top, YogaNode.Margin.Right, YogaNode.Margin.Bottom);
        Box.RectInner = Box.Rect.Shrink(YogaNode.Padding.Left, YogaNode.Padding.Top, YogaNode.Padding.Right, YogaNode.Padding.Bottom);
        Box.ClipRect = Box.Rect.Shrink(YogaNode.Border.Left, YogaNode.Border.Top, YogaNode.Border.Right, YogaNode.Border.Bottom);

        Box.Rect = Box.Rect.Floor();
        Box.RectOuter = Box.RectOuter.Floor();
        Box.RectInner = Box.RectInner.Floor();
        Box.ClipRect = Box.ClipRect.Floor();

        // Build the matrix that is generated from "transform" etc. We do this here after we have the size of the
        // panel - which should be super duper fine.
        TransformMatrix = ComputedStyle.BuildTransformMatrix(Box.Rect.Size);

        // Remove intro pseudo class after first layout
        if (HasIntro)
        {
            Switch(PseudoClass.Intro, false);
        }

        if (ComputedStyle.Display == DisplayMode.None) return;
        // Only skip layout for opacity 0 after first layout - ensures Box values are set at least once
        if (LayoutCount > 0 && Opacity <= 0.0f) return;

        // The initial state should be true for these panels
        // So there is no need to manually scroll to the bottom for scroll to be pinned there by default
        if (LayoutCount == 0 && PreferScrollToBottom)
        {
            IsScrollAtBottom = true;
        }

        bool wasScrollAtBottom = IsScrollAtBottom;

        offset = new Vector2(Box.Rect.Left, Box.Rect.Top) - ScrollOffset.SnapToGrid(1.0f);
        FinalLayoutChildren(offset);

        if (wasScrollAtBottom)
        {
            UpdateScrollPin();
        }
        
        LayoutCount++;
    }

    protected virtual void FinalLayoutChildren(Vector2 offset)
    {
        if (!HasChildren) return;

        for (int i = 0; i < _children!.Count; i++)
        {
            try
            {
                _children[i].FinalLayout(offset);
            }
            catch (Exception e)
            {
                Console.WriteLine($"FinalLayout error: {e}");
            }
        }

        if (ComputedStyle?.Overflow == OverflowMode.Scroll)
        {
            var rect = Box.Rect;
            rect.Position -= ScrollOffset;

            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];
                if (child.IsVisible)
                {
                    rect.Add(child.GetLayoutRect());
                }
            }

            rect.Height += Box.Padding.Bottom;
            rect.Right += Box.Padding.Right;

            ConstrainScrolling(rect.Size);
        }
        else
        {
            ScrollOffset = Vector2.Zero;
        }
    }

    Rect GetLayoutRect()
    {
        if (HasChildren && ComputedStyle.Display == DisplayMode.Contents)
        {
            Rect rect = default;
            for (int i = 0; i < _children.Count; i++)
            {
                var child = _children[i];

                if (child.IsVisible)
                {
                    if (i == 0) rect = child.GetLayoutRect();
                    else rect.Add(child.GetLayoutRect());
                }
            }

            return rect;
        }

        return Box.RectOuter;
    }

    private void UpdateScrollPin()
    {
        if (!PreferScrollToBottom)
            return;

        if (IsScrollAtBottom)
            return;

        if (!ScrollVelocity.y.AlmostEqual(0, 0.1f))
            return;

        ScrollOffset = new Vector2(ScrollOffset.x, ScrollSize.y);
        IsScrollAtBottom = true;
        ScrollVelocity.y = 0;
    }

    protected virtual void AddScrollVelocity()
    {
        if (ScrollVelocity.IsNearZeroLength)
        {
            ScrollVelocity = Vector2.Zero;
            return;
        }

        ScrollVelocity = Vector2.SmoothDamp(ScrollVelocity, Vector2.Zero, ref scrollVelocityVelocity, 0.5f, RealTime.SmoothDelta);

        // Bring it to a stop
        if (ScrollVelocity.y.AlmostEqual(0, 0.01f)) ScrollVelocity.y = 0;
        if (ScrollVelocity.x.AlmostEqual(0, 0.01f)) ScrollVelocity.x = 0;
    }

    protected virtual void ConstrainScrolling(Vector2 size)
    {
        if (IsDragScrolling)
            return;

        isScrolling = false;

        size -= Box.Rect.Size;

        var heightChange = size.y - ScrollSize.y;

        ScrollSize = size;
        ScrollSize = ScrollSize.SnapToGrid(1.0f);

        var overflow = ComputedStyle.Overflow;

        if (overflow == OverflowMode.Visible || overflow == OverflowMode.Hidden)
        {
            ScrollOffset = Vector2.Zero;
            return;
        }

        var so = ScrollOffset;

        // add velocity
        so += ScrollVelocity * RealTime.SmoothDelta * 60.0f;

        // Reverse the axis if flex-direction: *-reverse or justify-content: flex-end;
        var axisReversed = ComputedStyle.JustifyContent == Justify.FlexEnd || ComputedStyle.FlexDirection == FlexDirection.RowReverse || ComputedStyle.FlexDirection == FlexDirection.ColumnReverse;

        IsScrollAtBottom = so.y + ScrollVelocity.y >= size.y;
        if (ScrollVelocity.y > 0 && IsScrollAtBottom) so.y += heightChange;

        //
        // TODO - a style to let them turn springy mode off ?
        //

        var constrainSpeed = RealTime.SmoothDelta * 100.0f;

        if (axisReversed)
        {
            if (so.y > 0) so.y = so.y.LerpTo(0, constrainSpeed);
            if (so.x > 0) so.x = so.x.LerpTo(0, constrainSpeed);
            if (so.y < -ScrollSize.y) so.y = so.y.LerpTo(-ScrollSize.y, constrainSpeed);
            if (so.x < -ScrollSize.x) so.x = so.x.LerpTo(-ScrollSize.x, constrainSpeed);
        }
        else
        {
            if (so.y < 0) so.y = so.y.LerpTo(0, constrainSpeed);
            if (so.x < 0) so.x = so.x.LerpTo(0, constrainSpeed);
            if (so.y > ScrollSize.y) so.y = so.y.LerpTo(ScrollSize.y, constrainSpeed);
            if (so.x > ScrollSize.x) so.x = so.x.LerpTo(ScrollSize.x, constrainSpeed);
        }

        if (ScrollOffset == so)
            return;

        ScrollOffset = so;
        isScrolling = true;
    }

    internal void SortChildrenOrder()
    {
        // Sort children by CSS order property and sibling index (for Yoga layout)
        // This LINQ operation matches s&box exactly. While it could be optimized,
        // keeping it identical to s&box ensures consistent behavior.
        if (NeedsOrderSort)
        {
            NeedsOrderSort = false;

            if (_children != null && YogaNode != null)
            {
                // Re-add children to Yoga in the correct order
                foreach (var child in _children.OrderBy(x => x.LastOrder ?? 0).ThenBy(x => x.SiblingIndex))
                {
                    if (child.YogaNode == null)
                        continue;

                    YogaNode.RemoveChild(child.YogaNode);
                    YogaNode.AddChild(child.YogaNode);
                }
            }
        }

        // Sort children by z-index for rendering
        if (_renderChildren != null && _renderChildrenDirty)
        {
            _renderChildren.Sort((x, y) => (x.ComputedStyle?.ZIndex ?? 0) - (y.ComputedStyle?.ZIndex ?? 0));
            _renderChildrenDirty = false;
        }
    }
}
