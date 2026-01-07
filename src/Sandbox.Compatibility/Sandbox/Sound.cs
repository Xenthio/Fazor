namespace Sandbox;

/// <summary>
/// S&box-compatible sound system stub.
/// In Fazor, we don't have native audio support yet, so these are stubs.
/// </summary>
public class Sound
{
    /// <summary>
    /// The sound handle (stub).
    /// </summary>
    public int Handle { get; private set; }
    
    private Sound(int handle)
    {
        Handle = handle;
    }
    
    /// <summary>
    /// Plays a sound file.
    /// </summary>
    public static Sound PlayFile(SoundFile file)
    {
        // Stub - no actual audio playback
        Log.Debug($"[Sound] Would play: {file.Path}");
        return new Sound(0);
    }
    
    /// <summary>
    /// Plays a sound by name.
    /// </summary>
    public static Sound Play(string soundName)
    {
        // Stub - no actual audio playback
        Log.Debug($"[Sound] Would play: {soundName}");
        return new Sound(0);
    }
    
    /// <summary>
    /// Stops the sound.
    /// </summary>
    public void Stop()
    {
        // Stub
    }
    
    /// <summary>
    /// Sets the volume.
    /// </summary>
    public void SetVolume(float volume)
    {
        // Stub
    }
}

/// <summary>
/// S&box-compatible sound file reference.
/// </summary>
public class SoundFile
{
    /// <summary>
    /// The file path.
    /// </summary>
    public string Path { get; private set; }
    
    private SoundFile(string path)
    {
        Path = path;
    }
    
    /// <summary>
    /// Loads a sound file from the given path.
    /// </summary>
    public static SoundFile Load(string path)
    {
        return new SoundFile(path);
    }
}
