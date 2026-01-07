namespace Sandbox;

/// <summary>
/// S&box-compatible attribute for specifying a display title for types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public class TitleAttribute : Attribute
{
    /// <summary>
    /// The title text.
    /// </summary>
    public string Value { get; }
    
    public TitleAttribute(string title)
    {
        Value = title;
    }
}

/// <summary>
/// S&box-compatible attribute for marking a property as configurable in editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
public class PropertyAttribute : Attribute
{
    /// <summary>
    /// The display name for the property.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// S&box-compatible attribute for console commands.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ConCmdAttribute : Attribute
{
    /// <summary>
    /// The command name.
    /// </summary>
    public string? Name { get; set; }
}
