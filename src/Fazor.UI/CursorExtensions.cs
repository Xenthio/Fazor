using Sandbox.UI;

namespace Fazor.UI;

/// <summary>
/// Extension methods for cursor handling in Fazor
/// </summary>
public static class CursorExtensions
{
    /// <summary>
    /// Maps a Sandbox.UI StandardCursor enum value to Silk.NET's StandardCursor enum.
    /// </summary>
    /// <remarks>
    /// Due to Silk.NET's limited StandardCursor support, several cursor types fall back to Arrow:
    /// - HourGlass, WaitArrow (no waiting cursors)
    /// - SizeNWSE, SizeNESW (no diagonal resize cursors)
    /// - SizeALL (no move-all cursor)
    /// - No (no prohibited cursor)
    /// - Up (rarely used)
    /// HandClosed uses Hand as the closest available match.
    /// </remarks>
    public static Silk.NET.Input.StandardCursor ToSilkCursor(this StandardCursor cursor)
    {
        return cursor switch
        {
            StandardCursor.Arrow => Silk.NET.Input.StandardCursor.Arrow,
            StandardCursor.IBeam => Silk.NET.Input.StandardCursor.IBeam,
            StandardCursor.Crosshair => Silk.NET.Input.StandardCursor.Crosshair,
            StandardCursor.Hand => Silk.NET.Input.StandardCursor.Hand,
            StandardCursor.SizeWE => Silk.NET.Input.StandardCursor.HResize,
            StandardCursor.SizeNS => Silk.NET.Input.StandardCursor.VResize,
            
            // Map other cursors to closest Silk.NET equivalent or Arrow for unsupported
            StandardCursor.HourGlass => Silk.NET.Input.StandardCursor.Arrow, // No hourglass in Silk.NET
            StandardCursor.WaitArrow => Silk.NET.Input.StandardCursor.Arrow, // No wait arrow in Silk.NET
            StandardCursor.Up => Silk.NET.Input.StandardCursor.Arrow, // Rarely used
            StandardCursor.SizeNWSE => Silk.NET.Input.StandardCursor.Arrow, // No diagonal resize in Silk.NET
            StandardCursor.SizeNESW => Silk.NET.Input.StandardCursor.Arrow, // No diagonal resize in Silk.NET
            StandardCursor.SizeALL => Silk.NET.Input.StandardCursor.Arrow, // No move-all cursor in Silk.NET
            StandardCursor.No => Silk.NET.Input.StandardCursor.Arrow, // No prohibited cursor in Silk.NET
            StandardCursor.HandClosed => Silk.NET.Input.StandardCursor.Hand, // Use open hand as closest match
            
            _ => Silk.NET.Input.StandardCursor.Arrow,
        };
    }
}
