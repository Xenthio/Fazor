namespace Sandbox.UI;

/// <summary>
/// Watch files and dispatch events when they change.
/// Based on S&amp;box's FileWatch pattern for stylesheet and Razor hotloading.
/// Only active in DEBUG builds to avoid overhead in release.
/// </summary>
public sealed class FileWatcher : IDisposable
{
#if DEBUG
    private static readonly object _lock = new();
    private static readonly List<FileWatcher> _watchers = new();
    private static readonly HashSet<string> _changedFiles = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, FileSystemWatcher> _directoryWatchers = new();
    private static float _timeSinceLastChange = 0f;
    private static float _debounceTime = 0.2f; // 200ms debounce to batch rapid file changes
    private static bool _hasChanges = false;

    private readonly List<string> _watchedFiles = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when any watched file changes.
    /// The FileWatcher argument contains the list of changed files.
    /// </summary>
    public event Action<FileWatcher>? OnChanges;

    /// <summary>
    /// Event raised for each individual file that changed.
    /// </summary>
    public event Action<string>? OnChangedFile;

    /// <summary>
    /// The files that changed since the last callback.
    /// </summary>
    public List<string> Changes { get; private set; } = new();

    /// <summary>
    /// Whether this watcher is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Creates a new file watcher.
    /// </summary>
    public FileWatcher()
    {
        lock (_lock)
        {
            _watchers.Add(this);
        }
    }

    /// <summary>
    /// Add a file to watch for changes.
    /// </summary>
    public void AddFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        // Normalize the path
        filePath = NormalizePath(filePath);

        if (!_watchedFiles.Contains(filePath))
        {
            _watchedFiles.Add(filePath);
        }

        // Ensure we have a watcher for this file's directory
        EnsureDirectoryWatcher(filePath);
    }

    /// <summary>
    /// Remove a file from being watched.
    /// </summary>
    public void RemoveFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        filePath = NormalizePath(filePath);
        _watchedFiles.Remove(filePath);
    }

    /// <summary>
    /// Check if this watcher is interested in a specific file.
    /// </summary>
    private bool InterestedInFile(string file)
    {
        if (_watchedFiles.Count == 0)
            return true; // No specific files = watch all

        return _watchedFiles.Any(f => 
            string.Equals(f, file, StringComparison.OrdinalIgnoreCase) ||
            file.EndsWith(f, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensure we have a FileSystemWatcher for the directory containing this file.
    /// </summary>
    private static void EnsureDirectoryWatcher(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            return;

        lock (_lock)
        {
            if (_directoryWatchers.ContainsKey(directory))
                return;

            try
            {
                var watcher = new FileSystemWatcher(directory)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                watcher.Changed += OnFileChanged;
                watcher.Created += OnFileChanged;
                watcher.Renamed += OnFileRenamed;
                watcher.Error += OnWatcherError;

                _directoryWatchers[directory] = watcher;
                
                Console.WriteLine($"[FileWatcher] Started watching: {directory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileWatcher] Failed to watch directory '{directory}': {ex.Message}");
            }
        }
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        AddChangedFile(e.FullPath);
    }

    private static void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        AddChangedFile(e.OldFullPath);
        AddChangedFile(e.FullPath);
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Console.WriteLine($"[FileWatcher] Error: {e.GetException()?.Message}");
    }

    private static void AddChangedFile(string filePath)
    {
        filePath = NormalizePath(filePath);

        // Ignore temp files
        if (filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            filePath.EndsWith("~", StringComparison.OrdinalIgnoreCase))
            return;

        lock (_lock)
        {
            _changedFiles.Add(filePath);
            _timeSinceLastChange = 0f;
            _hasChanges = true;
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        // Get full path and normalize separators
        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            // If GetFullPath fails, just normalize slashes
        }

        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Must be called from the main thread each frame to process file change notifications.
    /// This is typically called from the render loop.
    /// </summary>
    /// <param name="deltaTime">Time since last tick in seconds</param>
    public static void Tick(float deltaTime)
    {
        if (!_hasChanges)
            return;

        _timeSinceLastChange += deltaTime;

        // Wait for debounce period to batch rapid file saves
        if (_timeSinceLastChange < _debounceTime)
            return;

        List<string>? changedFiles = null;
        List<FileWatcher>? watchers = null;

        lock (_lock)
        {
            if (_changedFiles.Count == 0)
            {
                _hasChanges = false;
                return;
            }

            changedFiles = _changedFiles.ToList();
            _changedFiles.Clear();
            _hasChanges = false;

            watchers = _watchers.ToList();
        }

        // Log what changed
        foreach (var file in changedFiles)
        {
            Console.WriteLine($"[FileWatcher] File changed: {file}");
        }

        // Notify watchers
        foreach (var watcher in watchers)
        {
            if (!watcher.Enabled || watcher._disposed)
                continue;

            watcher.TriggerCallback(changedFiles);
        }
    }

    private void TriggerCallback(List<string> changedFiles)
    {
        Changes.Clear();

        foreach (var file in changedFiles)
        {
            if (InterestedInFile(file))
            {
                Changes.Add(file);
            }
        }

        if (Changes.Count == 0)
            return;

        try
        {
            OnChanges?.Invoke(this);

            foreach (var file in Changes)
            {
                OnChangedFile?.Invoke(file);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FileWatcher] Error in callback: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        OnChanges = null;
        OnChangedFile = null;

        lock (_lock)
        {
            _watchers.Remove(this);
        }
    }

    /// <summary>
    /// Dispose all directory watchers. Call on application shutdown.
    /// </summary>
    public static void DisposeAll()
    {
        lock (_lock)
        {
            foreach (var watcher in _directoryWatchers.Values)
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                catch { }
            }
            _directoryWatchers.Clear();

            foreach (var w in _watchers.ToList())
            {
                w.Dispose();
            }
            _watchers.Clear();
            _changedFiles.Clear();
        }
    }
#else
    // Release build stubs - no-op implementations
    public event Action<FileWatcher>? OnChanges;
    public event Action<string>? OnChangedFile;
    public List<string> Changes { get; } = new();
    public bool Enabled { get; set; } = false;

    public void AddFile(string filePath) { }
    public void RemoveFile(string filePath) { }
    public void Dispose() { }
    public static void Tick(float deltaTime) { }
    public static void DisposeAll() { }
#endif
}
