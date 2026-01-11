using Sandbox;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A panel that the user can enter text into.
/// Based on S&box's TextEntry control.
/// </summary>
[Library("textentry")]
public class TextEntry : Panel
{
    /// <summary>
    /// The label that contains the text
    /// </summary>
    public Label Label { get; set; }
    private bool _disabled = false;
    private bool _numeric = false;
    private string _placeholder = "";
    /// <summary>
    /// Is the text entry disabled?
    /// </summary>
    public bool Disabled
    {
        get => _disabled;
        set
        {
            _disabled = value;
            AcceptsFocus = !value;
            SetClass("disabled", value);
        }
    }
    /// <summary>
    /// Access to the raw text in the text entry
    /// </summary>
    public string Text
    {
        get => Label?.Text ?? "";
        set
        {
            if (Label != null)
                Label.Text = value ?? "";
        }
    }
    /// <summary>
    /// Amount of characters in the text
    /// </summary>
    public int TextLength => Label?.TextLength ?? 0;
    /// <summary>
    /// Position of the text cursor/caret within the text
    /// </summary>
    public int CaretPosition
    {
        get => Label?.CaretPosition ?? 0;
        set
        {
            if (Label != null)
                Label.CaretPosition = value;
        }
    }
    /// <summary>
    /// The color used for text selection highlight
    /// </summary>
    public Color SelectionColor
    {
        get => Label?.SelectionColor ?? Color.Cyan.WithAlpha(0.39f);
        set
        {
            if (Label != null)
                Label.SelectionColor = value;
        }
    }
    /// <summary>
    /// The value of the text entry
    /// </summary>
    public string Value
    {
        get => Text;
        set => Text = value;
    }
    /// <summary>
    /// Makes it possible to enter new lines
    /// </summary>
    public bool Multiline { get; set; } = false;
    /// <summary>
    /// If true, only numeric input is allowed
    /// </summary>
    public bool Numeric
    {
        get => _numeric;
        set
        {
            _numeric = value;
            SetClass("numeric", value);
        }
    }
    /// <summary>
    /// Format for numeric values (e.g., "0.###")
    /// </summary>
    public string NumberFormat { get; set; } = "0.###";
    /// <summary>
    /// Minimum value for numeric input
    /// </summary>
    public float? MinValue { get; set; }
    /// <summary>
    /// Maximum value for numeric input
    /// </summary>
    public float? MaxValue { get; set; }
    /// <summary>
    /// Placeholder text when empty
    /// </summary>
    public string Placeholder
    {
        get => _placeholder;
        set
        {
            _placeholder = value;
            SetClass("has-placeholder", !string.IsNullOrEmpty(value));
        }
    }
    /// <summary>
    /// Called when text is changed
    /// </summary>
    public event Action<string>? OnTextEdited;
    /// <summary>
    /// TextEntry always has content (it needs DrawContent to be called for caret rendering)
    /// </summary>
    public override bool HasContent => true;
    public TextEntry()
    {
        AddClass("textentry");
        ElementName = "textentry";
        AcceptsFocus = true;
        Label = AddChild(new Label("", "content-label"));
        Label.Tokenize = false;
        Label.Multiline = false; // Single line by default
        Label.Style.WhiteSpace = WhiteSpace.Pre; // Preserve whitespace (matches S&box)
    }
    public override void SetProperty(string name, string value)
    {
        switch (name)
        {
            case "text":
            case "value":
                Text = value;
                return;
            case "placeholder":
                Placeholder = value;
                return;
            case "numeric":
                Numeric = value == "true" || value == "1";
                return;
            case "disabled":
                Disabled = value == "true" || value == "1";
                return;
            case "multiline":
                Multiline = value == "true" || value == "1";
                return;
            case "format":
            case "numberformat":
                NumberFormat = value;
                return;
            case "min":
            case "minvalue":
                if (float.TryParse(value, out float min))
                    MinValue = min;
                return;
            case "max":
            case "maxvalue":
                if (float.TryParse(value, out float max))
                    MaxValue = max;
                return;
        }
        base.SetProperty(name, value);
    }
    public override void SetContent(string? value)
    {
        Text = value ?? "";
    }
    /// <summary>
    /// Called when value changes
    /// </summary>
    protected virtual void OnValueChanged()
    {
        CreateValueEvent("value", Text);
        OnTextEdited?.Invoke(Text);
    }
    /// <summary>
    /// Format numeric text
    /// </summary>
    protected string FixNumeric()
    {
        if (!Numeric) return Text;
        if (float.TryParse(Text, out float val))
        {
            // Clamp to min/max
            if (MinValue.HasValue && val < MinValue.Value)
                val = MinValue.Value;
            if (MaxValue.HasValue && val > MaxValue.Value)
                val = MaxValue.Value;
            return val.ToString(NumberFormat);
        }
        return Text;
    }
    /// <summary>
    /// Handle character input
    /// </summary>
    public override void OnKeyTyped(char k)
    {
        if (Disabled)
            return;
        // Handle backspace
        if (k == '\b')
        {
            if (Label.HasSelection())
            {
                Label.ReplaceSelection("");
                OnValueChanged();
            }
            else if (CaretPosition > 0)
            {
                Label.MoveCaretPos(-1);
                Label.RemoveText(CaretPosition, 1);
                OnValueChanged();
            }
            Label.ScrollToCaret(); // Scroll to keep caret visible
            return;
        }
        // Handle enter/return
        if (k == '\n' || k == '\r')
        {
            if (!Multiline)
            {
                // Blur/unfocus would go here
                return;
            }
        }
        // Don't allow control characters (except newline for multiline)
        if (char.IsControl(k) && k != '\n')
            return;
        // Check for numeric only
        if (Numeric && !char.IsDigit(k) && k != '.' && k != '-')
            return;
        // Replace selection or insert at caret
        if (Label.HasSelection())
        {
            Label.ReplaceSelection(k.ToString());
        }
        else
        {
            Text ??= "";
            Label.InsertText(k.ToString(), CaretPosition);
            Label.MoveCaretPos(1);
        }
        Label.ScrollToCaret(); // Scroll to keep caret visible
        OnValueChanged();
    }
    /// <summary>
    /// Handle keyboard button events (backspace, delete, arrow keys, etc.)
    /// Based on S&box TextEntry.OnButtonTyped
    /// </summary>
    public override void OnButtonTyped(ButtonEvent e)
    {
        if (Disabled)
            return;
        e.StopPropagation = true;
        var button = e.Button;
        // Handle selection deletion first
        if (Label.HasSelection() && (button == "delete" || button == "backspace"))
        {
            Label.ReplaceSelection("");
            OnValueChanged();
            return;
        }
        // Handle delete
        if (button == "delete")
        {
            if (CaretPosition < TextLength)
            {
                if (e.HasCtrl)
                {
                    Label.MoveToWordBoundaryRight(true);
                    Label.ReplaceSelection(string.Empty);
                    OnValueChanged();
                    Label.ScrollToCaret();
                    return;
                }
                Label.RemoveText(CaretPosition, 1);
                OnValueChanged();
            }
            Label.ScrollToCaret();
            return;
        }
        // Handle backspace
        if (button == "backspace")
        {
            if (CaretPosition > 0)
            {
                if (e.HasCtrl)
                {
                    Label.MoveToWordBoundaryLeft(true);
                    Label.ReplaceSelection(string.Empty);
                    OnValueChanged();
                    return;
                }
                Label.MoveCaretPos(-1);
                Label.RemoveText(CaretPosition, 1);
                OnValueChanged();
            }
            return;
        }
        // Handle Ctrl+A (select all)
        if (button == "a" && e.HasCtrl)
        {
            Label.SelectionStart = 0;
            Label.SelectionEnd = TextLength;
            return;
        }
        // Handle Home key
        if (button == "home")
        {
            if (!e.HasCtrl)
            {
                Label.MoveToLineStart(e.HasShift);
            }
            else
            {
                Label.SetCaretPosition(0, e.HasShift);
            }
            Label.ScrollToCaret();
            return;
        }
        // Handle End key
        if (button == "end")
        {
            if (!e.HasCtrl)
            {
                Label.MoveToLineEnd(e.HasShift);
            }
            else
            {
                Label.SetCaretPosition(TextLength, e.HasShift);
            }
            Label.ScrollToCaret();
            return;
        }
        // Handle left arrow
        if (button == "left")
        {
            if (!e.HasCtrl)
            {
                if (Label.HasSelection() && !e.HasShift)
                    Label.SetCaretPosition(Label.SelectionStart);
                else
                    Label.MoveCaretPos(-1, e.HasShift);
            }
            else
            {
                Label.MoveToWordBoundaryLeft(e.HasShift);
            }
            Label.ScrollToCaret();
            return;
        }
        // Handle right arrow
        if (button == "right")
        {
            if (!e.HasCtrl)
            {
                if (Label.HasSelection() && !e.HasShift)
                    Label.SetCaretPosition(Label.SelectionEnd);
                else
                    Label.MoveCaretPos(1, e.HasShift);
            }
            else
            {
                Label.MoveToWordBoundaryRight(e.HasShift);
            }
            Label.ScrollToCaret();
            return;
        }
        // Handle up/down arrows
        if (button == "down" || button == "up")
        {
            Label.MoveCaretLine(button == "up" ? -1 : 1, e.HasShift);
            return;
        }
        // Handle enter/return
        if (button == "enter" || button == "keypadenter")
        {
            if (Multiline)
            {
                OnKeyTyped('\n');
                return;
            }
            Blur();
            CreateEvent("onsubmit", Text);
            return;
        }
        // Handle escape
        if (button == "escape")
        {
            Blur();
            CreateEvent("oncancel");
            return;
        }
        // Let parent handle other keys
        base.OnButtonTyped(e);
    }
    /// <summary>
    /// Track time since last focus change for caret blinking
    /// </summary>
    protected float TimeSinceNotInFocus;
    /// <summary>
    /// Track if we're selecting words on drag
    /// </summary>
    private bool SelectingWords = false;
    /// <summary>
    /// Handle mouse down for caret positioning and selection start
    /// </summary>
    protected override void OnMouseDown(MousePanelEvent e)
    {
        e.StopPropagation();
        if (string.IsNullOrEmpty(Text))
            return;
        var pos = Label.GetLetterAtScreenPosition(e.ScreenPosition);
        // Clear selection only if we successfully determined a position (matches S&box behavior)
        if (pos >= 0)
        {
            Label.SelectionStart = 0;
            Label.SelectionEnd = 0;
            Label.SetCaretPosition(pos);
            Label.ScrollToCaret();
        }
    }
    /// <summary>
    /// Handle mouse up to finalize selection
    /// </summary>
    protected override void OnMouseUp(MousePanelEvent e)
    {
        SelectingWords = false;
        var pos = Label.GetLetterAtScreenPosition(e.ScreenPosition);
        if (Label.SelectionEnd > 0) pos = Label.SelectionEnd;
        Label.CaretPosition = Math.Clamp(pos, 0, TextLength);
        Label.ScrollToCaret();
        e.StopPropagation();
    }
    /// <summary>
    /// Handle drag selection (text selection with mouse drag)
    /// </summary>
    protected override void OnDragSelect(SelectionEvent e)
    {
        if (string.IsNullOrEmpty(Text) || Label is null)
            return;
        Label.ShouldDrawSelection = true;
        var tl = new Vector2(e.SelectionRect.Left, e.SelectionRect.Top);
        var br = new Vector2(e.SelectionRect.Right, e.SelectionRect.Bottom);
        Label.SelectionStart = Label.GetLetterAtScreenPosition(tl);
        Label.SelectionEnd = Label.GetLetterAtScreenPosition(br);
        if (SelectingWords)
        {
            var boundaries = Label.GetWordBoundaryIndices();
            var left = boundaries.LastOrDefault(x => x < Label.SelectionStart);
            var right = boundaries.FirstOrDefault(x => x > Label.SelectionEnd);
            left = Math.Min(left, Label.SelectionStart);
            right = Math.Max(right, Label.SelectionEnd);
            Label.SelectionStart = left;
            Label.SelectionEnd = right;
        }
        var pos = Label.GetLetterAtScreenPosition(e.EndPoint);
        Label.CaretPosition = Math.Clamp(pos, 0, TextLength);
        Label.ScrollToCaret();
    }
    /// <summary>
    /// Handle mouse move to prevent event propagation
    /// </summary>
    protected override void OnMouseMove(MousePanelEvent e)
    {
        base.OnMouseMove(e);
        e.StopPropagation();
    }
    /// <summary>
    /// Handle double-click for word selection
    /// </summary>
    protected override void OnDoubleClick(MousePanelEvent e)
    {
        if (string.IsNullOrEmpty(Text))
            return;
        if (e.Button == "mouseleft")
        {
            Label.SelectWord(Label.GetLetterAtScreenPosition(e.ScreenPosition));
            SelectingWords = true;
        }
    }
    /// <summary>
    /// Handle focus - reset timer for caret blinking
    /// </summary>
    protected override void OnFocus(PanelEvent e)
    {
        TimeSinceNotInFocus = 0;
    }
    /// <summary>
    /// Handle blur - apply numeric formatting if needed
    /// </summary>
    protected override void OnBlur(PanelEvent e)
    {
        if (Numeric)
        {
            Text = FixNumeric();
        }
    }
    /// <summary>
    /// Handle paste
    /// </summary>
    public override void OnPaste(string text)
    {
        if (Label.HasSelection())
        {
            Label.ReplaceSelection("");
        }
        var pasteResult = new string(text.Where(c => !char.IsControl(c) || c == '\n').ToArray());
        Text ??= "";
        Label.InsertText(pasteResult, CaretPosition);
        Label.MoveCaretPos(pasteResult.Length);
        Label.ScrollToCaret();
        OnValueChanged();
    }
    /// <summary>
    /// Get clipboard value (cut if requested)
    /// </summary>
    public override string? GetClipboardValue(bool cut)
    {
        var value = Label.GetClipboardValue(cut);
        if (cut)
        {
            OnValueChanged();
        }
        return value;
    }
    /// <summary>
    /// Render the caret when focused
    /// </summary>
    public override void DrawContent(ref RenderState state)
    {
        // Caret rendering is handled by the renderer (e.g. SkiaPanelRenderer.DrawTextEntryCaret).
        // This method ensures selection rendering is enabled when the control has focus.
        Label.ShouldDrawSelection = HasFocus;
    }
    /// <summary>
    /// Tick to update state
    /// </summary>
    public override void Tick()
    {
        base.Tick();
        SetClass("is-multiline", Multiline);
        if (Label != null)
        {
            Label.Multiline = Multiline;
            // Set Selectable based on placeholder state (matches S&box)
            bool isPlaceholder = string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Placeholder);
            Label.Selectable = !isPlaceholder;
        }
        // Update time for caret blinking
        if (HasFocus)
            TimeSinceNotInFocus += 0.016f; // Approximate frame time
        else
            TimeSinceNotInFocus = 0;
    }
}
