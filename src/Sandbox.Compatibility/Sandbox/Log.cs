namespace Sandbox;

/// <summary>
/// S&box-compatible logging utility.
/// </summary>
public static class Log
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public static void Info(object message)
    {
        Console.WriteLine($"[INFO] {message}");
    }
    
    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public static void Warning(object message)
    {
        Console.WriteLine($"[WARN] {message}");
    }
    
    /// <summary>
    /// Logs an error message.
    /// </summary>
    public static void Error(object message)
    {
        Console.WriteLine($"[ERROR] {message}");
    }
    
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public static void Debug(object message)
    {
        Console.WriteLine($"[DEBUG] {message}");
    }
    
    /// <summary>
    /// Logs a trace message.
    /// </summary>
    public static void Trace(object message)
    {
        Console.WriteLine($"[TRACE] {message}");
    }
}
