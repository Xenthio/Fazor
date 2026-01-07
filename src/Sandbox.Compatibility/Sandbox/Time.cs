namespace Sandbox;

/// <summary>
/// S&box-compatible TimeSince struct for tracking elapsed time.
/// </summary>
public struct TimeSince
{
    private float _time;
    
    public TimeSince(float seconds)
    {
        _time = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds - seconds;
    }
    
    public static implicit operator float(TimeSince ts)
    {
        return (float)DateTime.UtcNow.TimeOfDay.TotalSeconds - ts._time;
    }
    
    public static implicit operator TimeSince(float seconds)
    {
        return new TimeSince(seconds);
    }
}

/// <summary>
/// S&box-compatible RealTimeSince struct for tracking elapsed real time.
/// </summary>
public struct RealTimeSince
{
    private float _time;
    
    public RealTimeSince(float seconds)
    {
        _time = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds - seconds;
    }
    
    public static implicit operator float(RealTimeSince ts)
    {
        return (float)DateTime.UtcNow.TimeOfDay.TotalSeconds - ts._time;
    }
    
    public static implicit operator RealTimeSince(float seconds)
    {
        return new RealTimeSince(seconds);
    }
}

/// <summary>
/// Construct attribute for marking panels with specific construction behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ConstructAttribute : Attribute
{
}

/// <summary>
/// LayoutBoxInset attribute for specifying panel layout insets.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class LayoutBoxInsetAttribute : Attribute
{
    public float Top { get; }
    public float Right { get; }
    public float Bottom { get; }
    public float Left { get; }
    
    public LayoutBoxInsetAttribute(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }
}
