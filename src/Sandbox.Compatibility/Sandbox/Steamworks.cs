namespace Steamworks;

/// <summary>
/// Steamworks stub - minimal compatibility layer.
/// </summary>
public static class SteamClient
{
    public static bool IsValid => false;
    public static ulong SteamId => 0;
    public static string Name => "Player";
}

/// <summary>
/// Steam Friends stub.
/// </summary>
public static class SteamFriends
{
    public static IEnumerable<Friend> GetFriends() => Array.Empty<Friend>();
}

/// <summary>
/// Friend representation.
/// </summary>
public class Friend
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsOnline { get; set; }
}
