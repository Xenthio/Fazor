namespace Sandbox.UI;

/// <summary>
/// Panel input handling methods.
/// Based on S&box's Panel.Input.cs from engine/Sandbox.Engine/Systems/UI/Panel/Panel.Input.cs
/// </summary>
public partial class Panel
{
    /// <summary>
    /// Current mouse position local to this panels top left corner.
    /// </summary>
    public Vector2 MousePosition
    {
        get
        {
            if (FindRootPanel() is not RootPanel root)
                return default;

            var mp = root.MousePos;

            // Apply GlobalMatrix transform if present (matches S&box)
            if (GlobalMatrix.HasValue)
            {
                mp = GlobalMatrix.Value.Transform(mp);
            }

            return mp - Box.Rect.Position;
        }
    }

    /// <summary>
    /// Called by <see cref="PanelInput.CheckHover(Panel, Vector2, ref Panel)" /> to transform
    /// the current mouse position using the panel's LocalMatrix (by default). This can be overriden for special cases.
    /// </summary>
    public virtual Vector2 GetTransformPosition(Vector2 pos)
    {
        return LocalMatrix?.Transform(pos) ?? pos;
    }

    /// <summary>
    /// Whether given screen position is within this panel. This will accurately handle border radius as well.
    /// </summary>
    /// <param name="pos">The position to test, in screen coordinates.</param>
    public bool IsInside(Vector2 pos)
    {
        var rect = Box.Rect;

        if (pos.x < rect.Left || pos.x > rect.Right) return false;
        if (pos.y < rect.Top || pos.y > rect.Bottom) return false;

        var s = ComputedStyle;
        if (s == null) return true;

        // Adjust position relative to the rect for border-radius testing
        pos.x -= rect.Left;
        pos.y -= rect.Top;

        // Pre-calculate common values for radius calculations
        var halfSize = (rect.Width + rect.Height) * 0.5f;
        var maxRadiusX = rect.Width / 2.0f;
        var maxRadiusY = rect.Height / 2.0f;

        // Test top-left corner radius
        if (s.BorderTopLeftRadius.HasValue && s.BorderTopLeftRadius.Value.Unit > 0)
        {
            var r = s.BorderTopLeftRadius.Value.GetPixels(halfSize);
            r = MathF.Min(MathF.Min(r, maxRadiusX), maxRadiusY);
            var c = new Vector2(r, r);
            if (pos.x < c.x && pos.y < c.y && Vector2.Distance(pos, c) > r)
                return false;
        }

        // Test top-right corner radius
        if (s.BorderTopRightRadius.HasValue && s.BorderTopRightRadius.Value.Unit > 0)
        {
            var r = s.BorderTopRightRadius.Value.GetPixels(halfSize);
            r = MathF.Min(MathF.Min(r, maxRadiusX), maxRadiusY);
            var c = new Vector2(rect.Width - r, r);
            if (pos.x > c.x && pos.y < c.y && Vector2.Distance(pos, c) > r)
                return false;
        }

        // Test bottom-right corner radius
        if (s.BorderBottomRightRadius.HasValue && s.BorderBottomRightRadius.Value.Unit > 0)
        {
            var r = s.BorderBottomRightRadius.Value.GetPixels(halfSize);
            r = MathF.Min(MathF.Min(r, maxRadiusX), maxRadiusY);
            var c = new Vector2(rect.Width - r, rect.Height - r);
            if (pos.x > c.x && pos.y > c.y && Vector2.Distance(pos, c) > r)
                return false;
        }

        // Test bottom-left corner radius
        if (s.BorderBottomLeftRadius.HasValue && s.BorderBottomLeftRadius.Value.Unit > 0)
        {
            var r = s.BorderBottomLeftRadius.Value.GetPixels(halfSize);
            r = MathF.Min(MathF.Min(r, maxRadiusX), maxRadiusY);
            var c = new Vector2(r, rect.Height - r);
            if (pos.x < c.x && pos.y > c.y && Vector2.Distance(pos, c) > r)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Whether the given rect is inside this panels bounds. (<see cref="Box.Rect"/>)
    /// </summary>
    /// <param name="rect">The rect to test, which should have screen-space coordinates.</param>
    /// <param name="fullyInside"><see langword="true"/> to test if the given rect is completely inside the panel. <see langword="false"/> to test for an intersection.</param>
    public bool IsInside(Rect rect, bool fullyInside)
    {
        if (fullyInside)
        {
            return rect.Left >= Box.Rect.Left && rect.Right <= Box.Rect.Right &&
                   rect.Top >= Box.Rect.Top && rect.Bottom <= Box.Rect.Bottom;
        }
        else
        {
            // Intersection test
            return !(rect.Right < Box.Rect.Left || rect.Left > Box.Rect.Right ||
                     rect.Bottom < Box.Rect.Top || rect.Top > Box.Rect.Bottom);
        }
    }

    /// <summary>
    /// False by default, can this element accept keyboard focus. If an element accepts
    /// focus it'll be able to receive keyboard input.
    /// </summary>
    public bool AcceptsFocus { get; set; }

    /// <summary>
    /// Describe what to do with keyboard input. The default is InputMode.UI which means that when
    /// focused, this panel will receive Keys Typed and Button Events.
    /// </summary>
    public PanelInputType ButtonInput { get; set; }

    /// <summary>
    /// False by default. Anything that is capable of accepting IME input should return true. Which is probably just a TextEntry.
    /// </summary>
    public virtual bool AcceptsImeInput => false;

    /// <summary>
    /// Give input focus to this panel.
    /// </summary>
    public bool Focus()
    {
        return InputFocus.Set(this);
    }

    /// <summary>
    /// Remove input focus from this panel.
    /// </summary>
    public bool Blur()
    {
        return InputFocus.Clear(this);
    }

    /// <summary>
    /// Called when any button, mouse (except for mouse4/5) and keyboard, are pressed or depressed while hovering this panel.
    /// </summary>
    public virtual void OnButtonEvent(ButtonEvent e)
    {
        Parent?.OnButtonEvent(e);
    }

    /// <summary>
    /// Called when a printable character has been typed (pressed) while this panel has input focus.
    /// </summary>
    public virtual void OnKeyTyped(char k)
    {
        Parent?.OnKeyTyped(k);
    }

    /// <summary>
    /// Called when any keyboard button has been typed (pressed) while this panel has input focus.
    /// </summary>
    public virtual void OnButtonTyped(ButtonEvent e)
    {
        Parent?.OnButtonTyped(e);
    }

    /// <summary>
    /// Called when the user presses CTRL+V while this panel has input focus.
    /// </summary>
    public virtual void OnPaste(string text)
    {
        Parent?.OnPaste(text);
    }

    /// <summary>
    /// If we have a value that can be copied to the clipboard, return it here.
    /// </summary>
    public virtual string? GetClipboardValue(bool cut)
    {
        if (AllowChildSelection)
            return CollectSelectedChildrenText(this);

        if (Parent != null)
            return Parent.GetClipboardValue(cut);

        return null;
    }

    /// <summary>
    /// Called when the player scrolls their mouse wheel while hovering this panel.
    /// </summary>
    /// <param name="value">The scroll wheel delta. Positive values are scrolling down, negative - up.</param>
    public virtual void OnMouseWheel(Vector2 value)
    {
        if (TryScroll(value))
            return;

        Parent?.OnMouseWheel(value);
    }

    /// <summary>
    /// Called from <see cref="OnMouseWheel"/> to try to scroll.
    /// </summary>
    /// <param name="value">The scroll wheel delta.</param>
    /// <returns>Return true to NOT propagate the event to the <see cref="Parent"/>.</returns>
    public bool TryScroll(Vector2 value)
    {
        if (ComputedStyle == null) return false;
        if (!HasScrollY && !HasScrollX) return false;

        // If we're not scrolling in the same direction that this panel overflows in, ignore
        if (ComputedStyle.OverflowX != OverflowMode.Scroll && value.x != 0) return false;
        if (ComputedStyle.OverflowY != OverflowMode.Scroll && value.y != 0) return false;

        var velocityAdd = Vector2.Zero;

        if (ComputedStyle.OverflowX == OverflowMode.Scroll && HasScrollX) velocityAdd += new Vector2(value.x * -20, 0);
        if (ComputedStyle.OverflowY == OverflowMode.Scroll && HasScrollY) velocityAdd += new Vector2(0, value.y * 20);

        velocityAdd *= (1 + ScrollVelocity.Length / 100.0f);
        ScrollVelocity += velocityAdd;

        if (velocityAdd.Length.AlmostEqual(0))
            return false;

        return true;
    }

    /// <summary>
    /// Scroll to the bottom, if the panel has scrolling enabled.
    /// </summary>
    /// <returns>Whether we scrolled to the bottom or not.</returns>
    public bool TryScrollToBottom()
    {
        if (ComputedStyle == null) return false;
        if (!HasScrollY) return false;

        ScrollOffset = new Vector2(ScrollOffset.x, ScrollSize.y);
        IsScrollAtBottom = true;
        ScrollVelocity = new Vector2(0, 0);
        return true;
    }

    internal static Panel? MouseCapture { get; private set; }

    /// <summary>
    /// Captures the mouse cursor while active.
    /// </summary>
    /// <param name="b">Whether to enable or disable the capture.</param>
    public void SetMouseCapture(bool b)
    {
        if (b)
        {
            MouseCapture = this;
            return;
        }

        if (MouseCapture == this)
        {
            MouseCapture = null;
            return;
        }
    }

    /// <summary>
    /// Whether this panel is capturing the mouse cursor.
    /// </summary>
    public bool HasMouseCapture => MouseCapture == this;

    //
    // These are used by the input system as an optimization
    //
    internal Vector2 WorldCursor;
    internal float WorldDistance = float.MaxValue;

    /// <summary>
    /// Get the panel at a specific screen position (used for inspector picking)
    /// </summary>
    public Panel? GetPanelAt(Vector2 position, bool checkVisible = true, bool checkPointerEvents = true)
    {
        if (checkVisible && !IsVisible)
            return null;

        if (checkPointerEvents && ComputedStyle?.PointerEvents == PointerEvents.None)
            return null;

        if (!IsInside(position))
            return null;

        // Check children first (reverse order for z-index)
        if (_children != null)
        {
            for (int i = _children.Count - 1; i >= 0; i--)
            {
                var child = _children[i];
                if (child != null)
                {
                    var result = child.GetPanelAt(position, checkVisible, checkPointerEvents);
                    if (result != null)
                        return result;
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Transform a ray in 3D space to a position on the panel. This is used for world panel input.
    /// </summary>
    /// <param name="ray">The ray in 3D world space to test against this panel.</param>
    /// <param name="position">Position on the panel where the intersection happened, local to the panel's top left corner.</param>
    /// <param name="distance">Distance from the ray's origin to the intersection in 3D space.</param>
    /// <returns>Return true if a hit/intersection was detected.</returns>
    public virtual bool RayToLocalPosition(Ray ray, out Vector2 position, out float distance)
    {
        position = default;
        distance = default;

        return false;
    }
}
