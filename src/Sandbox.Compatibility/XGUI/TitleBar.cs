using Sandbox.UI;

// Use Sandbox.UI.Vector2 which has lowercase x, y members (matches s&box)
using Vector2 = Sandbox.UI.Vector2;

namespace XGUI;

/// <summary>
/// XGUI Title bar for windows.
/// </summary>
public class TitleBar : Panel
{
    /// <summary>
    /// The parent window.
    /// </summary>
    public Window? ParentWindow { get; set; }
    
    /// <summary>
    /// The title text panel.
    /// </summary>
    public Panel? TitleText { get; private set; }
    
    /// <summary>
    /// The title icon panel.
    /// </summary>
    public Panel? TitleIcon { get; private set; }
    
    /// <summary>
    /// The spacer panel for dragging.
    /// </summary>
    public Panel? TitleSpacer { get; private set; }
    
    /// <summary>
    /// Container for control buttons.
    /// </summary>
    public Panel? TitleElements { get; private set; }
    
    public TitleBar()
    {
        AddClass("titlebar");
        
        // Create title icon
        TitleIcon = AddChild(new Panel(this, "TitleIcon"));
        
        // Create title text
        TitleText = AddChild(new Panel(this, "TitleText"));
        
        // Create spacer (for dragging)
        TitleSpacer = AddChild(new Panel(this, "TitleSpacer"));
        TitleSpacer.Style.FlexGrow = 1;
        
        // Create elements container (for buttons)
        TitleElements = AddChild(new Panel(this, "TitleElements"));
        
        // Set up drag events on spacer
        TitleSpacer.AddEventListener("onmousedown", OnDragStart);
        TitleSpacer.AddEventListener("onmouseup", OnDragEnd);
        TitleSpacer.AddEventListener("onmousemove", OnDrag);
        
        // Also allow dragging on the title text
        TitleText.AddEventListener("onmousedown", OnDragStart);
        TitleText.AddEventListener("onmouseup", OnDragEnd);
        TitleText.AddEventListener("onmousemove", OnDrag);
    }
    
    /// <summary>
    /// Sets the title text.
    /// </summary>
    public void SetTitle(string title)
    {
        if (TitleText is Label label)
        {
            label.Text = title;
        }
        else if (TitleText != null)
        {
            // Replace with a label
            var parent = TitleText.Parent;
            var index = parent?.GetChildIndex(TitleText) ?? 0;
            TitleText.Delete();
            
            var newLabel = new Label { Text = title };
            newLabel.AddClass("TitleText");
            parent?.AddChild(newLabel);
            if (parent != null && index >= 0)
                parent.SetChildIndex(newLabel, index);
            TitleText = newLabel;
            
            // Re-add drag events
            TitleText.AddEventListener("onmousedown", OnDragStart);
            TitleText.AddEventListener("onmouseup", OnDragEnd);
            TitleText.AddEventListener("onmousemove", OnDrag);
        }
    }
    
    private bool _dragging = false;
    private Vector2 _dragOffset;
    
    private void OnDragStart()
    {
        ParentWindow?.DragBarDown();
        _dragging = true;
    }
    
    private void OnDragEnd()
    {
        ParentWindow?.DragBarUp();
        _dragging = false;
    }
    
    private void OnDrag()
    {
        if (_dragging)
        {
            ParentWindow?.Drag();
        }
    }
    
    protected override void OnMouseDown(MousePanelEvent e)
    {
        base.OnMouseDown(e);
        OnDragStart();
    }
    
    protected override void OnMouseUp(MousePanelEvent e)
    {
        base.OnMouseUp(e);
        OnDragEnd();
    }
}
