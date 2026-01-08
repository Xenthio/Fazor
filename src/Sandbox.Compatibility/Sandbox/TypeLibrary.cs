using System.Reflection;

namespace Sandbox;

/// <summary>
/// S&box-compatible type library for runtime type discovery and creation.
/// </summary>
public static class TypeLibrary
{
    private static Dictionary<string, Type>? _typeCache;
    
    private static void EnsureTypeCache()
    {
        if (_typeCache != null) return;
        
        _typeCache = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        // Cache types from all loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Use full name as key
                    _typeCache[type.FullName ?? type.Name] = type;
                    // Also register by simple name (may override)
                    _typeCache[type.Name] = type;
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Some assemblies may not be fully loadable
            }
        }
    }
    
    /// <summary>
    /// Gets a type description by name.
    /// </summary>
    public static TypeDescription? GetType(string typeName)
    {
        EnsureTypeCache();
        
        if (_typeCache!.TryGetValue(typeName, out var type))
        {
            return new TypeDescription(type);
        }
        
        // Try to find by partial name
        var match = _typeCache.FirstOrDefault(kvp => 
            kvp.Key.EndsWith(typeName, StringComparison.OrdinalIgnoreCase) ||
            kvp.Value.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            
        if (match.Value != null)
        {
            return new TypeDescription(match.Value);
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets a type description for the given type.
    /// </summary>
    public static TypeDescription GetType(Type type)
    {
        return new TypeDescription(type);
    }
    
    /// <summary>
    /// Gets a type description for the generic type.
    /// </summary>
    public static TypeDescription GetType<T>()
    {
        return new TypeDescription(typeof(T));
    }
    
    /// <summary>
    /// Gets all types that inherit from the specified type.
    /// </summary>
    public static IEnumerable<TypeDescription> GetTypes<T>()
    {
        EnsureTypeCache();
        
        return _typeCache!.Values
            .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Distinct()
            .Select(t => new TypeDescription(t));
    }
    
    /// <summary>
    /// Gets all types that have the specified attribute.
    /// </summary>
    public static IEnumerable<TypeDescription> GetTypesWithAttribute<TAttribute>() where TAttribute : Attribute
    {
        EnsureTypeCache();
        
        return _typeCache!.Values
            .Where(t => t.GetCustomAttribute<TAttribute>() != null)
            .Distinct()
            .Select(t => new TypeDescription(t));
    }
    
    /// <summary>
    /// Refreshes the type cache.
    /// </summary>
    public static void Refresh()
    {
        _typeCache = null;
        EnsureTypeCache();
    }
}

/// <summary>
/// S&box-compatible type description wrapper.
/// </summary>
public class TypeDescription
{
    private readonly Type _type;
    
    /// <summary>
    /// The underlying System.Type.
    /// </summary>
    public Type TargetType => _type;
    
    /// <summary>
    /// The type name.
    /// </summary>
    public string Name => _type.Name;
    
    /// <summary>
    /// The full type name.
    /// </summary>
    public string FullName => _type.FullName ?? _type.Name;
    
    /// <summary>
    /// The type's title (from TitleAttribute or Name).
    /// </summary>
    public string Title => _type.GetCustomAttribute<TitleAttribute>()?.Value ?? _type.Name;
    
    /// <summary>
    /// Whether the type is abstract.
    /// </summary>
    public bool IsAbstract => _type.IsAbstract;
    
    /// <summary>
    /// Whether the type is an interface.
    /// </summary>
    public bool IsInterface => _type.IsInterface;
    
    public TypeDescription(Type type)
    {
        _type = type;
    }
    
    /// <summary>
    /// Creates an instance of the type.
    /// </summary>
    public object? Create(params object[] args)
    {
        return Activator.CreateInstance(_type, args);
    }
    
    /// <summary>
    /// Creates an instance of the type and casts it to T.
    /// </summary>
    public T? Create<T>(params object[] args)
    {
        var instance = Activator.CreateInstance(_type, args);
        return instance is T typed ? typed : default;
    }
    
    /// <summary>
    /// Gets an attribute from the type.
    /// </summary>
    public TAttribute? GetAttribute<TAttribute>() where TAttribute : Attribute
    {
        return _type.GetCustomAttribute<TAttribute>();
    }
    
    /// <summary>
    /// Checks if the type has the specified attribute.
    /// </summary>
    public bool HasAttribute<TAttribute>() where TAttribute : Attribute
    {
        return _type.GetCustomAttribute<TAttribute>() != null;
    }
    
    /// <summary>
    /// Checks if the type is assignable to the specified type.
    /// </summary>
    public bool IsAssignableTo<T>()
    {
        return typeof(T).IsAssignableFrom(_type);
    }
    
    /// <summary>
    /// Checks if the type is assignable from the specified type.
    /// </summary>
    public bool IsAssignableFrom<T>()
    {
        return _type.IsAssignableFrom(typeof(T));
    }
}
