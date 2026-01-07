namespace Sandbox;

/// <summary>
/// S&box Package system stub - represents a downloadable package.
/// </summary>
public class Package
{
    /// <summary>
    /// The full identifier of the package (org.package)
    /// </summary>
    public string FullIdent { get; set; } = "";
    
    /// <summary>
    /// The package title
    /// </summary>
    public string Title { get; set; } = "";
    
    /// <summary>
    /// The package description
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// The package thumbnail URL
    /// </summary>
    public string Thumb { get; set; } = "";
    
    /// <summary>
    /// The organization that owns the package
    /// </summary>
    public string Org { get; set; } = "";
    
    /// <summary>
    /// The package type (e.g., "game", "addon", "map")
    /// </summary>
    public string PackageType { get; set; } = "";
    
    /// <summary>
    /// Number of downloads
    /// </summary>
    public int Downloads { get; set; }
    
    /// <summary>
    /// Rating score
    /// </summary>
    public float Rating { get; set; }
    
    /// <summary>
    /// Number of favourites
    /// </summary>
    public int Favourites { get; set; }
    
    /// <summary>
    /// Find packages based on a query (stub implementation)
    /// </summary>
    public static Task<PackageResult> FindAsync(string query, int maxResults = 20)
    {
        // Stub implementation - returns empty result
        return Task.FromResult(new PackageResult());
    }
    
    /// <summary>
    /// Get a package by its full identifier
    /// </summary>
    public static Task<Package?> GetAsync(string fullIdent)
    {
        // Stub implementation
        return Task.FromResult<Package?>(null);
    }
}

/// <summary>
/// Result from a package query
/// </summary>
public class PackageResult
{
    /// <summary>
    /// The packages returned by the query
    /// </summary>
    public IEnumerable<Package> Packages { get; set; } = Array.Empty<Package>();
    
    /// <summary>
    /// Total count of matching packages
    /// </summary>
    public int TotalCount { get; set; }
}
