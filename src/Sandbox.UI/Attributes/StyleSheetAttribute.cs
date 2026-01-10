namespace Sandbox.UI;

/// <summary>
/// Will automatically apply the named stylesheet to the Panel.
/// If no name is provided, looks for a stylesheet with the same name as the component.
/// Examples:
/// - [StyleSheet("MyStyles.scss")] - loads MyStyles.scss
/// - [StyleSheet] - loads ComponentName.scss (e.g., MainWindow.razor -> MainWindow.scss)
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class StyleSheetAttribute : Attribute
{
    /// <summary>
    /// File name of the style sheet file.
    /// If null or empty, the stylesheet name will be automatically derived from the component name.
    /// </summary>
    public string Name;

    public StyleSheetAttribute(string? name = null)
    {
        Name = name ?? "";
    }
}
