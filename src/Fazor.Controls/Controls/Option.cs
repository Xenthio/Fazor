using Sandbox;
using Sandbox.UI;
namespace Fazor.Controls;

/// <summary>
/// An option for a <see cref="ComboBox"/>.
/// Based on s&amp;box's Option from game/addons/base/code/UI/Option.cs
/// </summary>
[Library("option")]
public class Option
{
    /// <summary>
    /// The user-friendly text to show.
    /// </summary>
    public string? Title;

    /// <summary>
    /// Icon for this option.
    /// </summary>
    public string? Icon;

    /// <summary>
    /// Subtitle for this option.
    /// </summary>
    public string? Subtitle;

    /// <summary>
    /// Tooltip for this option.
    /// </summary>
    public string? Tooltip;

    /// <summary>
    /// The internal value for this option.
    /// </summary>
    public object? Value;

    public Option()
    {
    }

    public Option(string title, object? value)
    {
        Title = title;
        Value = value;
    }

    public Option(string title, string? icon, object? value)
    {
        Title = title;
        Icon = icon;
        Value = value;
    }
}
