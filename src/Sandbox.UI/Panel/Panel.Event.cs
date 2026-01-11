using System.Reflection;

namespace Sandbox.UI;

/// <summary>
/// Panel event handling methods.
/// Based on s&box's Panel.Event.cs from engine/Sandbox.Engine/Systems/UI/Panel/Panel.Event.cs
/// </summary>
public partial class Panel
{
    internal struct EventCallback
    {
        public string EventName;
        public Action? BaseAction;
        public Action<PanelEvent>? Action;
        public bool Automatic;

        public Action<EventCallback, PanelEvent>? Event;
        public Panel? Panel;
        public Panel? Context;
    }

    internal List<EventCallback>? EventListeners { get; set; }

    /// <summary>
    /// Called on creation and hotload to delete and re-initialize event listeners.
    /// Matches s&box implementation - uses reflection to find methods with PanelEventAttribute.
    /// </summary>
    protected virtual void InitializeEvents()
    {
        EventListeners?.RemoveAll(x => x.Automatic);

        var type = GetType();

        foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var pea = method.GetCustomAttribute<PanelEventAttribute>();
            if (pea == null) continue;

            var eventName = method.Name.ToLower();

            if (eventName.EndsWith("event")) eventName = eventName[..^5];
            if (pea.Name != null) eventName = pea.Name.ToLower();

            var args = method.GetParameters();

            if (args.Length == 1)
            {
                var argType = args[0].ParameterType;
                var argCache = new object[1];

                if (argType == typeof(PanelEvent))
                {
                    AddAutomaticEventListener(eventName, (x) =>
                    {
                        argCache[0] = x;
                        var response = method.Invoke(this, argCache);
                        if (response is bool bReturnedValue)
                        {
                            x.Propagate = x.Propagate && bReturnedValue;
                        }
                    });
                }
                else
                {
                    AddAutomaticEventListener(eventName, (x) =>
                    {
                        argCache[0] = Convert.ChangeType(x.Value, argType)!;
                        var response = method.Invoke(this, argCache);
                        if (response is bool bReturnedValue)
                        {
                            x.Propagate = x.Propagate && bReturnedValue;
                        }
                    });
                }
            }
            else if (args.Length == 0)
            {
                AddAutomaticEventListener(eventName, (x) =>
                {
                    var response = method.Invoke(this, null);
                    if (response is bool bReturnedValue)
                    {
                        x.Propagate = x.Propagate && bReturnedValue;
                    }
                });
            }
            else
            {
                Console.WriteLine($"PanelEvent {method} - couldn't set up (too many arguments)");
            }
        }
    }

    internal void AddAutomaticEventListener(string name, Action<PanelEvent> e)
    {
        EventListeners ??= new List<EventCallback>();

        var ev = new EventCallback
        {
            EventName = name,
            Action = e,
            Automatic = true
        };

        EventListeners.Add(ev);
    }

    /// <summary>
    /// Remove all event listeners for a given event name.
    /// </summary>
    public void RemoveEventListener(string name)
    {
        EventListeners?.RemoveAll(x => x.EventName == name);
    }

    /// <summary>
    /// Remove an event listener by name and handler.
    /// </summary>
    public void RemoveEventListener(string eventName, Action<PanelEvent> handler)
    {
        if (EventListeners == null) return;
        EventListeners.RemoveAll(x => x.EventName == eventName && x.Action == handler);
    }

    /// <summary>
    /// Remove an event listener by name and handler.
    /// </summary>
    public void RemoveEventListener(string eventName, Action handler)
    {
        if (EventListeners == null) return;
        EventListeners.RemoveAll(x => x.EventName == eventName && x.BaseAction == handler);
    }

    /// <summary>
    /// Runs given callback when the given event is triggered.
    /// </summary>
    public void AddEventListener(string eventName, Action<PanelEvent> e)
    {
        AddEventListener(new EventCallback
        {
            EventName = eventName,
            Action = e
        });
    }

    /// <summary>
    /// Runs given callback when the given event is triggered, without access to the <see cref="PanelEvent"/>.
    /// </summary>
    public void AddEventListener(string eventName, Action action)
    {
        AddEventListener(new EventCallback
        {
            EventName = eventName,
            BaseAction = action
        });
    }

    internal void AddEventListener(EventCallback eventCallback)
    {
        EventListeners ??= new List<EventCallback>();
        EventListeners.Add(eventCallback);
    }

    List<PanelEvent>? PendingEvents;

    /// <summary>
    /// Process pending events. This is the public API that calls RunPendingEvents internally.
    /// </summary>
    public void ProcessPendingEvents()
    {
        RunPendingEvents();
    }

    internal void RunPendingEvents()
    {
        if (PendingEvents is null || PendingEvents.Count == 0) return;

        for (int i = 0; i < PendingEvents.Count; i++)
        {
            var e = PendingEvents[i];
            if (e.Time > TimeNow) continue;

            PendingEvents.RemoveAt(i);
            i--;

            try
            {
                OnEvent(e);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"\"{ex.Message}\" when running panel event \"{e.Name}\" from \"{e.Target}\"");
            }
        }
    }

    /// <summary>
    /// Create a new event and pass it to the panels event queue.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="value">Event value.</param>
    /// <param name="debounce">Time, in seconds, to wait before firing the event.</param>
    public virtual void CreateEvent(string name, object? value = null, float? debounce = null)
    {
        var e = PendingEvents?.FirstOrDefault(x => x.Name == name);
        if (e == null)
        {
            e = new PanelEvent(name, this);
            CreateEvent(e);
        }

        e.Value = value;

        if (debounce.HasValue)
        {
            e.Time = (float)(TimeNow + debounce.Value);
        }
    }

    /// <summary>
    /// Pass given event to the event queue.
    /// </summary>
    public virtual void CreateEvent(PanelEvent evnt)
    {
        PendingEvents ??= new List<PanelEvent>();
        PendingEvents.Add(evnt);
    }

    /// <summary>
    /// Call this when the value has changed due to user input etc.
    /// This triggers $"{name}.changed" event, with value being the Value on the event.
    /// </summary>
    protected void CreateValueEvent(string name, object? value)
    {
        CreateEvent($"{name}.changed", value);
    }

    /// <summary>
    /// Called when various <see cref="PanelEvent"/>s happen. Handles event listeners and many standard events by default.
    /// </summary>
    protected virtual void OnEvent(PanelEvent e)
    {
        e.This = this;

        // Handle copy/cut/paste events (matches S&box)
        if (e is CopyEvent || e is CutEvent)
        {
            var text = GetClipboardValue(e is CutEvent);
            if (text != null)
            {
                // Clipboard operations handled by platform layer
                // TODO: Add clipboard support
            }
        }

        if (e is PasteEvent paste)
        {
            OnPaste(paste.ClipboardValue ?? "");
        }

        if (e is MousePanelEvent mpe)
        {
            if (e.Is("onclick")) OnClick(mpe);
            if (e.Is("onmiddleclick")) OnMiddleClick(mpe);
            if (e.Is("onrightclick")) OnRightClick(mpe);
            if (e.Is("onmousedown")) OnMouseDown(mpe);
            if (e.Is("onmouseup")) OnMouseUp(mpe);
            if (e.Is("ondoubleclick")) OnDoubleClick(mpe);
            if (e.Is("onmousemove")) OnMouseMove(mpe);
            if (e.Is("onmouseover")) OnMouseOver(mpe);
            if (e.Is("onmouseout")) OnMouseOut(mpe);

            if (!e.Is("onmousemove"))
            {
                razorTreeDirty = true;
            }
        }

        // Handle drag events (matches S&box)
        if (e is DragEvent de)
        {
            InternalDragEvent(de);
        }

        if (e.Is("onfocus")) OnFocus(e);
        if (e.Is("onblur")) OnBlur(e);
        if (e.Is("onback")) OnBack(e);
        if (e.Is("onforward")) OnForward(e);
        if (e.Is("onescape")) OnEscape(e);

        if (e is SelectionEvent se)
        {
            if (e.Is("ondragselect")) OnDragSelect(se);
        }

        if (!e.Propagate)
            return;

        if (EventListeners != null)
        {
            foreach (var listener in EventListeners)
            {
                if (!e.Is(listener.EventName)) continue;

                listener.Event?.Invoke(listener, e);
                listener.Action?.Invoke(e);
                listener.BaseAction?.Invoke();
            }
        }

        if (!e.Propagate) return;

        Parent?.OnEvent(e);
    }

    /// <summary>
    /// Called when the player releases their left mouse button (Mouse 1) while hovering this panel.
    /// </summary>
    protected virtual void OnClick(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player releases their middle mouse button (Mouse 3) while hovering this panel.
    /// </summary>
    protected virtual void OnMiddleClick(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player releases their right mouse button (Mouse 2) while hovering this panel.
    /// </summary>
    protected virtual void OnRightClick(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player presses down the left or right mouse buttons while hovering this panel.
    /// </summary>
    protected virtual void OnMouseDown(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player releases left or right mouse button.
    /// </summary>
    protected virtual void OnMouseUp(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player double clicks the panel with the left mouse button.
    /// </summary>
    protected virtual void OnDoubleClick(MousePanelEvent e) { }

    /// <summary>
    /// Called when the cursor moves while hovering this panel.
    /// </summary>
    protected virtual void OnMouseMove(MousePanelEvent e) { }

    /// <summary>
    /// Called when the cursor enters this panel.
    /// </summary>
    protected virtual void OnMouseOver(MousePanelEvent e) { }

    /// <summary>
    /// Called when the cursor leaves this panel.
    /// </summary>
    protected virtual void OnMouseOut(MousePanelEvent e) { }

    /// <summary>
    /// Called when the player presses the "Back" button while hovering this panel, which is typically "mouse 5", aka one of the mouse buttons on its side.
    /// </summary>
    protected virtual void OnBack(PanelEvent e) { }

    /// <summary>
    /// Called when the player presses the "Forward" button while hovering this panel, which is typically "mouse 4", aka one of the mouse buttons on its side.
    /// </summary>
    protected virtual void OnForward(PanelEvent e) { }

    /// <summary>
    /// Called when the escape key is pressed
    /// </summary>
    protected virtual void OnEscape(PanelEvent e)
    {
        if (HasFocus)
            Blur();
    }

    /// <summary>
    /// Called when this panel receives input focus.
    /// </summary>
    protected virtual void OnFocus(PanelEvent e) { }

    /// <summary>
    /// Called when this panel loses input focus.
    /// </summary>
    protected virtual void OnBlur(PanelEvent e) { }

    /// <summary>
    /// Called when the user performs a drag selection operation (e.g., text selection with mouse drag).
    /// </summary>
    protected virtual void OnDragSelect(SelectionEvent e) { }
}
