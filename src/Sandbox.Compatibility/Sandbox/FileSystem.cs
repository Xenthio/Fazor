namespace Sandbox;

/// <summary>
/// S&box-compatible base file system abstraction.
/// </summary>
public class BaseFileSystem
{
    private readonly string _basePath;
    
    /// <summary>
    /// Gets the base path for this file system.
    /// </summary>
    public string BasePath => _basePath;
    
    public BaseFileSystem(string basePath)
    {
        _basePath = basePath;
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }
    
    /// <summary>
    /// Resolves a relative path to an absolute path.
    /// </summary>
    protected string ResolvePath(string path)
    {
        path = path.Replace('\\', '/').TrimStart('/');
        return Path.Combine(_basePath, path);
    }
    
    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    public virtual bool FileExists(string path)
    {
        return File.Exists(ResolvePath(path));
    }
    
    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    public virtual bool DirectoryExists(string path)
    {
        return Directory.Exists(ResolvePath(path));
    }
    
    /// <summary>
    /// Creates a directory.
    /// </summary>
    public virtual void CreateDirectory(string path)
    {
        Directory.CreateDirectory(ResolvePath(path));
    }
    
    /// <summary>
    /// Deletes a file.
    /// </summary>
    public virtual void DeleteFile(string path)
    {
        File.Delete(ResolvePath(path));
    }
    
    /// <summary>
    /// Deletes a directory.
    /// </summary>
    public virtual void DeleteDirectory(string path, bool recursive = false)
    {
        Directory.Delete(ResolvePath(path), recursive);
    }
    
    /// <summary>
    /// Opens a file for reading.
    /// </summary>
    public virtual Stream OpenRead(string path)
    {
        return File.OpenRead(ResolvePath(path));
    }
    
    /// <summary>
    /// Opens a file for writing.
    /// </summary>
    public virtual Stream OpenWrite(string path, FileMode mode = FileMode.Create)
    {
        var fullPath = ResolvePath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return File.Open(fullPath, mode);
    }
    
    /// <summary>
    /// Reads all bytes from a file.
    /// </summary>
    public virtual ReadOnlySpan<byte> ReadAllBytes(string path)
    {
        return File.ReadAllBytes(ResolvePath(path));
    }
    
    /// <summary>
    /// Reads all bytes from a file asynchronously.
    /// </summary>
    public virtual async Task<byte[]> ReadAllBytesAsync(string path)
    {
        return await File.ReadAllBytesAsync(ResolvePath(path));
    }
    
    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    public virtual string ReadAllText(string path)
    {
        return File.ReadAllText(ResolvePath(path));
    }
    
    /// <summary>
    /// Reads all text from a file asynchronously.
    /// </summary>
    public virtual async Task<string> ReadAllTextAsync(string path)
    {
        return await File.ReadAllTextAsync(ResolvePath(path));
    }
    
    /// <summary>
    /// Writes all bytes to a file.
    /// </summary>
    public virtual void WriteAllBytes(string path, byte[] contents)
    {
        var fullPath = ResolvePath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(fullPath, contents);
    }
    
    /// <summary>
    /// Writes all bytes to a file asynchronously.
    /// </summary>
    public virtual async Task WriteAllBytesAsync(string path, byte[] contents)
    {
        var fullPath = ResolvePath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllBytesAsync(fullPath, contents);
    }
    
    /// <summary>
    /// Writes all text to a file.
    /// </summary>
    public virtual void WriteAllText(string path, string contents)
    {
        var fullPath = ResolvePath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, contents);
    }
    
    /// <summary>
    /// Writes all text to a file asynchronously.
    /// </summary>
    public virtual async Task WriteAllTextAsync(string path, string contents)
    {
        var fullPath = ResolvePath(path);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(fullPath, contents);
    }
    
    /// <summary>
    /// Gets files in a directory.
    /// </summary>
    public virtual IEnumerable<string> FindFile(string pattern = "*", bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(_basePath, pattern, searchOption)
            .Select(f => f.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
    
    /// <summary>
    /// Gets directories in a directory.
    /// </summary>
    public virtual IEnumerable<string> FindDirectory(string pattern = "*", bool recursive = false)
    {
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetDirectories(_basePath, pattern, searchOption)
            .Select(d => d.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
    
    /// <summary>
    /// Copies a file from another file system.
    /// </summary>
    public virtual void CopyFile(string sourcePath, string destPath, BaseFileSystem? sourceFs = null)
    {
        sourceFs ??= this;
        var sourceData = sourceFs.ReadAllBytes(sourcePath);
        WriteAllBytes(destPath, sourceData.ToArray());
    }
    
    /// <summary>
    /// Copies a file from another file system asynchronously.
    /// </summary>
    public virtual async Task CopyFileAsync(string sourcePath, string destPath, BaseFileSystem? sourceFs = null)
    {
        sourceFs ??= this;
        var sourceData = await sourceFs.ReadAllBytesAsync(sourcePath);
        await WriteAllBytesAsync(destPath, sourceData);
    }
}

/// <summary>
/// S&box-compatible static file system access.
/// </summary>
public static class FileSystem
{
    private static BaseFileSystem? _data;
    private static BaseFileSystem? _mounted;
    private static BaseFileSystem? _organizationData;
    
    /// <summary>
    /// The data file system (writable user data).
    /// </summary>
    public static BaseFileSystem Data
    {
        get => _data ??= new BaseFileSystem(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fazor",
            "Data"));
        set => _data = value;
    }
    
    /// <summary>
    /// The mounted file system (read-only assets).
    /// </summary>
    public static BaseFileSystem Mounted
    {
        get => _mounted ??= new BaseFileSystem(AppContext.BaseDirectory);
        set => _mounted = value;
    }
    
    /// <summary>
    /// The organization data file system.
    /// </summary>
    public static BaseFileSystem OrganizationData
    {
        get => _organizationData ??= new BaseFileSystem(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Fazor",
            "OrgData"));
        set => _organizationData = value;
    }
    
    /// <summary>
    /// Initialize file systems with a custom base path.
    /// Call this before using any file system operations if you want custom paths.
    /// </summary>
    public static void Initialize(string basePath)
    {
        var dataPath = Path.Combine(basePath, "Data");
        var assetsPath = basePath;
        var orgDataPath = Path.Combine(basePath, "OrgData");
        
        _data = new BaseFileSystem(dataPath);
        _mounted = new BaseFileSystem(assetsPath);
        _organizationData = new BaseFileSystem(orgDataPath);
    }
}
