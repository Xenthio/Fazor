using System.Numerics;

namespace Sandbox.UI;

/// <summary>
/// Represents a ray with an origin and direction for 3D intersection testing.
/// </summary>
public struct Ray
{
    /// <summary>
    /// The origin point of the ray.
    /// </summary>
    public Vector3 Origin;

    /// <summary>
    /// The direction of the ray (should be normalized).
    /// </summary>
    public Vector3 Direction;

    /// <summary>
    /// Creates a new ray with the specified origin and direction.
    /// </summary>
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }

    /// <summary>
    /// Gets a point along the ray at the specified distance.
    /// </summary>
    public Vector3 GetPoint(float distance)
    {
        return Origin + Direction * distance;
    }
}
