namespace Sandbox.UI;

/// <summary>
/// Copy event - triggered when user presses Ctrl+C.
/// Ported from s&box's Panel/Event/CutCopyPasteEvent.cs
/// </summary>
public class CopyEvent : PanelEvent
{
	internal CopyEvent() : base("copy")
	{
	}
}

/// <summary>
/// Cut event - triggered when user presses Ctrl+X.
/// Ported from s&box's Panel/Event/CutCopyPasteEvent.cs
/// </summary>
public class CutEvent : PanelEvent
{
	internal CutEvent() : base("cut")
	{
	}
}

/// <summary>
/// Paste event - triggered when user presses Ctrl+V.
/// Ported from s&box's Panel/Event/CutCopyPasteEvent.cs
/// </summary>
public class PasteEvent : PanelEvent
{
	public string? ClipboardValue { get; set; }

	internal PasteEvent(string? value) : base("paste")
	{
		ClipboardValue = value;
	}
}

/// <summary>
/// Escape event - triggered when user presses Escape key.
/// Ported from s&box's Panel/Event/CutCopyPasteEvent.cs
/// </summary>
public class EscapeEvent : PanelEvent
{
	internal EscapeEvent() : base("escape")
	{
	}
}
