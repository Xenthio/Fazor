using System.Reflection;

namespace Sandbox.UI;

/// <summary>
/// Panel partial class: Property setting methods
/// Based on s&box's Panel.Property.cs
/// </summary>
public partial class Panel
{
    private string? _previousPropertyClass;
    private Dictionary<string, string>? _attributes;

    /// <summary>
    /// String value for the panel. Can be used to store simple string data.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Same as <see cref="SetProperty"/>, but first tries to set the property on the panel object using reflection,
    /// then processes any special properties such as <c>class</c>.
    /// This allows setting properties with their native types (bool, int, etc.) without string conversion.
    /// </summary>
    /// <param name="name">Name of the property to modify.</param>
    /// <param name="value">Value to assign to the property.</param>
    public virtual void SetPropertyObject(string name, object? value)
    {
        // Try to find a property with this name using reflection
        var prop = GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        if (prop != null && value != null && prop.PropertyType.IsAssignableFrom(value.GetType()))
        {
            prop.SetValue(this, value);
            return;
        }

        // Fall back to string-based SetProperty
        SetProperty(name, Convert.ToString(value) ?? "");
    }

    /// <summary>
    /// Set a property on the panel, such as special properties (class, id, style and value, etc.)
    /// and properties of the panel's C# class.
    /// </summary>
    /// <param name="name">Name of the property to modify.</param>
    /// <param name="value">Value to assign to the property.</param>
    public virtual void SetProperty(string name, string value)
    {
        if (name == "id")
        {
            Id = value;
            return;
        }

        if (name == "value")
        {
            StringValue = value;
            return;
        }

        if (name == "class")
        {
            if (!string.IsNullOrEmpty(_previousPropertyClass))
            {
                RemoveClass(_previousPropertyClass);
            }

            _previousPropertyClass = value;
            AddClass(value);
            return;
        }

        if (name == "style")
        {
            Style.Set(value);
            return;
        }

        // Store as attribute for derived classes to access
        SetAttribute(name, value);
        
        // Try to set using reflection (like S&box's TypeLibrary.SetProperty)
        TrySetPropertyViaReflection(name, value);
    }

    /// <summary>
    /// Try to set a property via reflection, converting the string value to the appropriate type.
    /// </summary>
    private void TrySetPropertyViaReflection(string name, string value)
    {
        var prop = GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        
        if (prop == null || !prop.CanWrite)
            return;

        try
        {
            object? convertedValue = null;
            var propType = prop.PropertyType;
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propType) ?? propType;
            
            if (underlyingType == typeof(bool))
            {
                convertedValue = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
            }
            else if (underlyingType == typeof(int))
            {
                if (int.TryParse(value, out var intVal))
                    convertedValue = intVal;
            }
            else if (underlyingType == typeof(float))
            {
                if (float.TryParse(value, out var floatVal))
                    convertedValue = floatVal;
            }
            else if (underlyingType == typeof(double))
            {
                if (double.TryParse(value, out var doubleVal))
                    convertedValue = doubleVal;
            }
            else if (underlyingType == typeof(string))
            {
                convertedValue = value;
            }
            else if (underlyingType.IsEnum)
            {
                if (Enum.TryParse(underlyingType, value, true, out var enumVal))
                    convertedValue = enumVal;
            }
            
            if (convertedValue != null)
            {
                prop.SetValue(this, convertedValue);
            }
        }
        catch
        {
            // Silently ignore conversion failures
        }
    }

    /// <summary>
    /// Set an attribute on the panel. Used for custom HTML-like attributes.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="value">Attribute value</param>
    public void SetAttribute(string name, string value)
    {
        _attributes ??= new Dictionary<string, string>();
        _attributes[name.ToLower()] = value;
    }

    /// <summary>
    /// Get an attribute value from the panel.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <returns>Attribute value, or null if not found</returns>
    public string? GetAttribute(string name)
    {
        if (_attributes == null) return null;
        _attributes.TryGetValue(name.ToLower(), out var value);
        return value;
    }

    /// <summary>
    /// Get an attribute value from the panel with a default fallback.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <param name="defaultValue">Default value if attribute not found</param>
    /// <returns>Attribute value, or defaultValue if not found</returns>
    public string? GetAttribute(string name, string? defaultValue)
    {
        return GetAttribute(name) ?? defaultValue;
    }

    /// <summary>
    /// Check if the panel has an attribute.
    /// </summary>
    /// <param name="name">Attribute name</param>
    /// <returns>True if the attribute exists</returns>
    public bool HasAttribute(string name)
    {
        return _attributes?.ContainsKey(name.ToLower()) ?? false;
    }

    /// <summary>
    /// Remove an attribute from the panel.
    /// </summary>
    /// <param name="name">Attribute name</param>
    public void RemoveAttribute(string name)
    {
        _attributes?.Remove(name.ToLower());
    }

    /// <summary>
    /// Set the content of the panel. For Label, this sets the text.
    /// For other panels, this can be used to set inner content.
    /// </summary>
    /// <param name="value">The content value</param>
    public virtual void SetContent(string? value)
    {
        // Base implementation does nothing - derived classes override this
    }

    /// <summary>
    /// Called when parameters are set on the panel (e.g., from Razor).
    /// Override this to handle parameter updates.
    /// </summary>
    protected virtual void OnParametersSet()
    {
        // Base implementation does nothing
    }
}
