namespace Sandbox.UI;

/// <summary>
/// Renderer-agnostic 2D vector. Matches s&box's Vector2.
/// </summary>
public struct Vector2
{
    public float x;
    public float y;

    public Vector2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
    public static Vector2 operator *(Vector2 a, float b) => new(a.x * b, a.y * b);
    public static Vector2 operator /(Vector2 a, float b) => new(a.x / b, a.y / b);
    public static Vector2 operator *(float a, Vector2 b) => new(a * b.x, a * b.y);
    public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);

    public static bool operator ==(Vector2 a, Vector2 b) => a.x == b.x && a.y == b.y;
    public static bool operator !=(Vector2 a, Vector2 b) => !(a == b);

    public static implicit operator Vector2(float value) => new(value, value);
    
    // Conversion to/from System.Numerics.Vector2
    public static implicit operator System.Numerics.Vector2(Vector2 v) => new(v.x, v.y);
    public static implicit operator Vector2(System.Numerics.Vector2 v) => new(v.X, v.Y);

    public bool IsNearZeroLength => Math.Abs(x) < 0.0001f && Math.Abs(y) < 0.0001f;

    public float Length => MathF.Sqrt(x * x + y * y);
    
    /// <summary>
    /// Returns a normalized version of this vector (length of 1).
    /// </summary>
    public Vector2 Normal
    {
        get
        {
            var len = Length;
            if (len < 0.0001f) return Zero;
            return new Vector2(x / len, y / len);
        }
    }
    
    /// <summary>
    /// Calculate the distance between two vectors.
    /// </summary>
    public static float Distance(Vector2 a, Vector2 b)
    {
        var dx = a.x - b.x;
        var dy = a.y - b.y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    
    public Vector2 SnapToGrid(float gridSize)
    {
        return new Vector2(
            MathF.Round(x / gridSize) * gridSize,
            MathF.Round(y / gridSize) * gridSize
        );
    }

    /// <summary>
    /// Smoothly damp a vector towards a target.
    /// </summary>
    public static Vector2 SmoothDamp(Vector2 current, Vector2 target, ref Vector2 currentVelocity, float smoothTime, float deltaTime)
    {
        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        
        float change_x = current.x - target.x;
        float change_y = current.y - target.y;
        
        float temp_x = (currentVelocity.x + omega * change_x) * deltaTime;
        float temp_y = (currentVelocity.y + omega * change_y) * deltaTime;
        
        currentVelocity.x = (currentVelocity.x - omega * temp_x) * exp;
        currentVelocity.y = (currentVelocity.y - omega * temp_y) * exp;
        
        float output_x = target.x + (change_x + temp_x) * exp;
        float output_y = target.y + (change_y + temp_y) * exp;

        return new Vector2(output_x, output_y);
    }

    public override bool Equals(object? obj) => obj is Vector2 v && this == v;
    public override int GetHashCode() => HashCode.Combine(x, y);

    public override string ToString() => $"({x}, {y})";
}
