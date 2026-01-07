namespace Sandbox.Services;

/// <summary>
/// Stats service stub - for accessing player statistics.
/// </summary>
public static class Stats
{
    /// <summary>
    /// Get a stat value for the local player.
    /// </summary>
    public static int GetInt(string statName) => 0;
    
    /// <summary>
    /// Get a stat value for the local player.
    /// </summary>
    public static float GetFloat(string statName) => 0f;
    
    /// <summary>
    /// Set a stat value for the local player.
    /// </summary>
    public static void SetInt(string statName, int value) { }
    
    /// <summary>
    /// Set a stat value for the local player.
    /// </summary>
    public static void SetFloat(string statName, float value) { }
    
    /// <summary>
    /// Increment a stat value.
    /// </summary>
    public static void Increment(string statName, int amount = 1) { }
}

/// <summary>
/// Achievements service stub.
/// </summary>
public static class Achievements
{
    /// <summary>
    /// Check if an achievement is unlocked.
    /// </summary>
    public static bool IsUnlocked(string achievementName) => false;
    
    /// <summary>
    /// Unlock an achievement.
    /// </summary>
    public static void Unlock(string achievementName) { }
}

/// <summary>
/// Leaderboards service stub.
/// </summary>
public static class Leaderboards
{
    /// <summary>
    /// Get a leaderboard by name.
    /// </summary>
    public static Task<Leaderboard?> GetAsync(string name)
    {
        return Task.FromResult<Leaderboard?>(null);
    }
}

/// <summary>
/// Leaderboard entry.
/// </summary>
public class Leaderboard
{
    public string Name { get; set; } = "";
    public int EntryCount { get; set; }
}
