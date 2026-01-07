using System.Runtime.InteropServices;

namespace Sandbox;

/// <summary>
/// A 32bit color, commonly used by things like vertex buffers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Color32 : IEquatable<Color32>
{
    /// <summary>
    /// The red color component, in range of 0-255.
    /// </summary>
    public byte r;

    /// <summary>
    /// The green color component, in range of 0-255.
    /// </summary>
    public byte g;

    /// <summary>
    /// The blue color component, in range of 0-255.
    /// </summary>
    public byte b;

    /// <summary>
    /// The alpha/transparency color component, in range of 0 (fully transparent) to 255 (fully opaque).
    /// </summary>
    public byte a;

    /// <summary>
    /// Initialize a color with each component set to given values, in range [0,255]
    /// </summary>
    public Color32(byte r, byte g, byte b, byte a = 255)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    /// <summary>
    /// Initialize a color with each component set to given value, even alpha.
    /// </summary>
    public Color32(byte all)
    {
        this.r = all;
        this.g = all;
        this.b = all;
        this.a = all;
    }

    /// <summary>
    /// Initialize from an integer of the form 0xAABBGGRR.
    /// </summary>
    public Color32(uint raw)
    {
        this.r = (byte)(raw & 255);
        this.g = (byte)((raw >> 8) & 255);
        this.b = (byte)((raw >> 16) & 255);
        this.a = (byte)((raw >> 24) & 255);
    }

    /// <summary>
    /// Initialize from an integer of the form 0xAABBGGRR.
    /// </summary>
    public Color32(int raw)
    {
        this.r = (byte)(raw & 255);
        this.g = (byte)((raw >> 8) & 255);
        this.b = (byte)((raw >> 16) & 255);
        this.a = (byte)((raw >> 24) & 255);
    }

    /// <summary>
    /// A constant representing a fully opaque color white.
    /// </summary>
    public static Color32 White { get; } = new Color32(255, 255, 255);

    /// <summary>
    /// A constant representing a fully opaque color black.
    /// </summary>
    public static Color32 Black { get; } = new Color32(0, 0, 0);

    /// <summary>
    /// A constant representing a fully transparent color.
    /// </summary>
    public static Color32 Transparent { get; } = new Color32(0, 0, 0, 0);
    
    /// <summary>
    /// A constant representing a fully opaque color red.
    /// </summary>
    public static Color32 Red { get; } = new Color32(255, 0, 0);
    
    /// <summary>
    /// A constant representing a fully opaque color green.
    /// </summary>
    public static Color32 Green { get; } = new Color32(0, 255, 0);
    
    /// <summary>
    /// A constant representing a fully opaque color blue.
    /// </summary>
    public static Color32 Blue { get; } = new Color32(0, 0, 255);

    /// <summary>
    /// Converts an integer of the form 0xRRGGBB into the color #RRGGBB with 100% alpha.
    /// </summary>
    public static Color32 FromRgb(uint rgb)
    {
        return new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
    }

    /// <summary>
    /// Converts an integer of the form 0xRRGGBBAA into the color #RRGGBBAA.
    /// </summary>
    public static Color32 FromRgba(uint rgba)
    {
        return new Color32((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
    }

    /// <summary>
    /// Returns a new color with each component being the minimum of the 2 given colors.
    /// </summary>
    public static Color32 Min(Color32 a, Color32 b)
    {
        return new Color32(
            Math.Min(a.r, b.r),
            Math.Min(a.g, b.g),
            Math.Min(a.b, b.b),
            Math.Min(a.a, b.a));
    }

    /// <summary>
    /// Returns a new color with each component being the maximum of the 2 given colors.
    /// </summary>
    public static Color32 Max(Color32 a, Color32 b)
    {
        return new Color32(
            Math.Max(a.r, b.r),
            Math.Max(a.g, b.g),
            Math.Max(a.b, b.b),
            Math.Max(a.a, b.a));
    }

    /// <summary>
    /// String representation of the form "#RRGGBB[AA]".
    /// </summary>
    public string Hex => a >= 255 ? $"#{r:X2}{g:X2}{b:X2}" : $"#{r:X2}{g:X2}{b:X2}{a:X2}";

    /// <summary>
    /// String representation in the form of rgba( r, g, b, a ) css function notation.
    /// </summary>
    public string Rgba => $"rgba( {r}, {g}, {b}, {a / 255f} )";

    /// <summary>
    /// String representation in the form of rgb( r, g, b ) css function notation.
    /// </summary>
    public string Rgb => $"rgb( {r}, {g}, {b} )";

    /// <summary>
    /// Integer representation of the form 0xRRGGBBAA.
    /// </summary>
    public uint RgbaInt => ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | a;

    /// <summary>
    /// Integer representation of the form 0xRRGGBB.
    /// </summary>
    public uint RgbInt => ((uint)r << 16) | ((uint)g << 8) | b;

    /// <summary>
    /// Integer representation of the form 0xAABBGGRR as used by native code.
    /// </summary>
    public uint RawInt => ((uint)a << 24) | ((uint)b << 16) | ((uint)g << 8) | r;

    public override string ToString()
    {
        return $"R:{r:0.00},G:{g:0.00},B:{b:0.00},A:{a:0.00}";
    }

    /// <summary>
    /// Parse a string to a color, in format "255 255 255 255" or "255,255,255". Alpha is optional.
    /// </summary>
    public static Color32? Parse(string value)
    {
        string[] values = value.Split(' ', ',');
        if (values.Length == 3 || values.Length == 4)
        {
            var color = White;
            color.r = byte.Parse(values[0]);
            color.g = byte.Parse(values[1]);
            color.b = byte.Parse(values[2]);

            if (values.Length == 4) color.a = byte.Parse(values[3]);
            return color;
        }

        return null;
    }

    public static bool operator ==(Color32 left, Color32 right) => left.Equals(right);
    public static bool operator !=(Color32 left, Color32 right) => !(left == right);
    public override bool Equals(object? obj) => obj is Color32 color && Equals(color);
    public readonly bool Equals(Color32 o) => (r, g, b, a) == (o.r, o.g, o.b, o.a);
    public readonly override int GetHashCode() => HashCode.Combine(r, g, b, a);
}
