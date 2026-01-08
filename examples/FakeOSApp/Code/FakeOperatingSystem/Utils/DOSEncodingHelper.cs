using System.Collections.Generic;
using System.Text;

namespace FakeOperatingSystem.Utils;

public static class DOSEncodingHelper
{
	// Codepage 850 mapping (DOS Latin 1)
	private static readonly char[] Cp850Map = new char[256]
	{
		// 0x00 - 0x0F
		'\u0000', '\u263A', '\u263B', '\u2665', '\u2666', '\u2663', '\u2660', '\u2022',
		'\u25D8', '\u25CB', '\u25D9', '\u2642', '\u2640', '\u266A', '\u266B', '\u263C',
		// 0x10 - 0x1F
		'\u25BA', '\u25C4', '\u2195', '\u203C', '\u00B6', '\u00A7', '\u25AC', '\u21A8',
		'\u2191', '\u2193', '\u2192', '\u2190', '\u221F', '\u2194', '\u25B2', '\u25BC',
		// 0x20 - 0x2F
		'\u0020', '\u0021', '\u0022', '\u0023', '\u0024', '\u0025', '\u0026', '\u0027',
		'\u0028', '\u0029', '\u002A', '\u002B', '\u002C', '\u002D', '\u002E', '\u002F',
		// 0x30 - 0x3F
		'\u0030', '\u0031', '\u0032', '\u0033', '\u0034', '\u0035', '\u0036', '\u0037',
		'\u0038', '\u0039', '\u003A', '\u003B', '\u003C', '\u003D', '\u003E', '\u003F',
		// 0x40 - 0x4F
		'\u0040', '\u0041', '\u0042', '\u0043', '\u0044', '\u0045', '\u0046', '\u0047',
		'\u0048', '\u0049', '\u004A', '\u004B', '\u004C', '\u004D', '\u004E', '\u004F',
		// 0x50 - 0x5F
		'\u0050', '\u0051', '\u0052', '\u0053', '\u0054', '\u0055', '\u0056', '\u0057',
		'\u0058', '\u0059', '\u005A', '\u005B', '\u005C', '\u005D', '\u005E', '\u005F',
		// 0x60 - 0x6F
		'\u0060', '\u0061', '\u0062', '\u0063', '\u0064', '\u0065', '\u0066', '\u0067',
		'\u0068', '\u0069', '\u006A', '\u006B', '\u006C', '\u006D', '\u006E', '\u006F',
		// 0x70 - 0x7F
		'\u0070', '\u0071', '\u0072', '\u0073', '\u0074', '\u0075', '\u0076', '\u0077',
		'\u0078', '\u0079', '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F',
		// 0x80 - 0x8F
		'\u00C7', '\u00FC', '\u00E9', '\u00E2', '\u00E4', '\u00E0', '\u00E5', '\u00E7',
		'\u00EA', '\u00EB', '\u00E8', '\u00EF', '\u00EE', '\u00EC', '\u00C4', '\u00C5',
		// 0x90 - 0x9F
		'\u00C9', '\u00E6', '\u00C6', '\u00F4', '\u00F6', '\u00F2', '\u00FB', '\u00F9',
		'\u00FF', '\u00D6', '\u00DC', '\u00F8', '\u00A3', '\u00D8', '\u00D7', '\u0192',
		// 0xA0 - 0xAF
		'\u00E1', '\u00ED', '\u00F3', '\u00FA', '\u00F1', '\u00D1', '\u00AA', '\u00BA',
		'\u00BF', '\u00AE', '\u00AC', '\u00BD', '\u00BC', '\u00A1', '\u00AB', '\u00BB',
		// 0xB0 - 0xBF
		'\u2591', '\u2592', '\u2593', '\u2502', '\u2524', '\u00C1', '\u00C2', '\u00C0',
		'\u00A9', '\u2563', '\u2551', '\u2557', '\u255D', '\u00A2', '\u00A5', '\u2560', // Corrected: \u2560 was \u2550
		// 0xC0 - 0xCF
		'\u255A', '\u2554', '\u2569', '\u2566', '\u2560', '\u2550', '\u256C', '\u00E3', // Corrected: \u2560 was \u255A
		'\u00C3', '\u2567', '\u2568', '\u2564', '\u2565', '\u255E', '\u255F', '\u255A', // Corrected: \u255A was \u2554
		// 0xD0 - 0xDF
		'\u2554', '\u2569', '\u2566', '\u2563', '\u256C', '\u2551', '\u2557', '\u255D', // Corrected: \u2554 was \u2550, \u2569 was \u256C, \u2566 was \u00E3, \u2563 was \u00C3, \u256C was \u2567, \u2551 was \u2568, \u2557 was \u2564, \u255D was \u2565
		'\u00D0', '\u00F0', '\u00CA', '\u00CB', '\u00C8', '\u0131', '\u00CD', '\u00CE',
		// 0xE0 - 0xEF
		'\u00CF', '\u00CC', '\u00D3', '\u00D4', '\u00D2', '\u00D5', '\u00F5', '\u00FE',
		'\u00DE', '\u00DA', '\u00DB', '\u00D9', '\u00FD', '\u00DD', '\u00AF', '\u00B4',
		// 0xF0 - 0xFF
		'\u00AD', '\u00B1', '\u2017', '\u00BE', '\u00B6', '\u00A7', '\u00F7', '\u00B8',
		'\u00B0', '\u00A8', '\u00B7', '\u00B9', '\u00B3', '\u00B2', '\u25A0', '\u00A0'
	};

	// Codepage 437 mapping (Original IBM PC / DOS)
	private static readonly char[] Cp437Map = new char[256]
	{
		// 0x00 - 0x0F
		'\u0000', '\u263A', '\u263B', '\u2665', '\u2666', '\u2663', '\u2660', '\u2022',
		'\u25D8', '\u25CB', '\u25D9', '\u2642', '\u2640', '\u266A', '\u266B', '\u263C',
		// 0x10 - 0x1F
		'\u25BA', '\u25C4', '\u2195', '\u203C', '\u00B6', '\u00A7', '\u25AC', '\u21A8',
		'\u2191', '\u2193', '\u2192', '\u2190', '\u221F', '\u2194', '\u25B2', '\u25BC',
		// 0x20 - 0x7F (Standard ASCII)
		'\u0020', '\u0021', '\u0022', '\u0023', '\u0024', '\u0025', '\u0026', '\u0027',
		'\u0028', '\u0029', '\u002A', '\u002B', '\u002C', '\u002D', '\u002E', '\u002F',
		'\u0030', '\u0031', '\u0032', '\u0033', '\u0034', '\u0035', '\u0036', '\u0037',
		'\u0038', '\u0039', '\u003A', '\u003B', '\u003C', '\u003D', '\u003E', '\u003F',
		'\u0040', '\u0041', '\u0042', '\u0043', '\u0044', '\u0045', '\u0046', '\u0047',
		'\u0048', '\u0049', '\u004A', '\u004B', '\u004C', '\u004D', '\u004E', '\u004F',
		'\u0050', '\u0051', '\u0052', '\u0053', '\u0054', '\u0055', '\u0056', '\u0057',
		'\u0058', '\u0059', '\u005A', '\u005B', '\u005C', '\u005D', '\u005E', '\u005F',
		'\u0060', '\u0061', '\u0062', '\u0063', '\u0064', '\u0065', '\u0066', '\u0067',
		'\u0068', '\u0069', '\u006A', '\u006B', '\u006C', '\u006D', '\u006E', '\u006F',
		'\u0070', '\u0071', '\u0072', '\u0073', '\u0074', '\u0075', '\u0076', '\u0077',
		'\u0078', '\u0079', '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F',
		// 0x80 - 0x8F
		'\u00C7', '\u00FC', '\u00E9', '\u00E2', '\u00E4', '\u00E0', '\u00E5', '\u00E7',
		'\u00EA', '\u00EB', '\u00E8', '\u00EF', '\u00EE', '\u00EC', '\u00C4', '\u00C5',
		// 0x90 - 0x9F
		'\u00C9', '\u00E6', '\u00C6', '\u00F4', '\u00F6', '\u00F2', '\u00FB', '\u00F9',
		'\u00FF', '\u00D6', '\u00DC', '\u00A2', '\u00A3', '\u00A5', '\u20A7', '\u0192',
		// 0xA0 - 0xAF
		'\u00E1', '\u00ED', '\u00F3', '\u00FA', '\u00F1', '\u00D1', '\u00AA', '\u00BA',
		'\u00BF', '\u2310', '\u00AC', '\u00BD', '\u00BC', '\u00A1', '\u00AB', '\u00BB',
		// 0xB0 - 0xBF
		'\u2591', '\u2592', '\u2593', '\u2502', '\u2524', '\u2561', '\u2562', '\u2556',
		'\u2555', '\u2563', '\u2551', '\u2557', '\u255D', '\u255C', '\u255B', '\u2510',
		// 0xC0 - 0xCF
		'\u2514', '\u2534', '\u252C', '\u251C', '\u2500', '\u253C', '\u255E', '\u255F',
		'\u255A', '\u2554', '\u2569', '\u2566', '\u2560', '\u2550', '\u256C', '\u2567',
		// 0xD0 - 0xDF
		'\u2568', '\u2564', '\u2565', '\u2559', '\u2558', '\u2552', '\u2553', '\u256B',
		'\u256A', '\u2518', '\u250C', '\u2588', '\u2584', '\u258C', '\u2590', '\u2580',
		// 0xE0 - 0xEF
		'\u03B1', '\u00DF', '\u0393', '\u03C0', '\u03A3', '\u03C3', '\u00B5', '\u03C4',
		'\u03A6', '\u0398', '\u03A9', '\u03B4', '\u221E', '\u03C6', '\u03B5', '\u2229',
		// 0xF0 - 0xFF
		'\u2261', '\u00B1', '\u2265', '\u2264', '\u2320', '\u2321', '\u00F7', '\u2248',
		'\u00B0', '\u2219', '\u00B7', '\u221A', '\u207F', '\u00B2', '\u25A0', '\u00A0'
	};
	private static readonly Dictionary<char, byte> Cp437ReverseMap;

	static DOSEncodingHelper()
	{
		Cp437ReverseMap = new Dictionary<char, byte>( Cp437Map.Length );
		for ( int i = 0; i < Cp437Map.Length; i++ )
		{
			// Handle potential duplicate characters in the map by prioritizing the first occurrence (lower byte value).
			// Standard CP437 doesn't have duplicates, but this is a safeguard.
			if ( !Cp437ReverseMap.ContainsKey( Cp437Map[i] ) )
			{
				Cp437ReverseMap[Cp437Map[i]] = (byte)i;
			}
		}

		// Special handling for control characters that might be represented differently in strings
		// than their direct CP437 visual glyphs (if any were mapped above for 0x00-0x1F, 0x7F).
		// This ensures common C# escape sequences for control chars map to their correct byte values.
		Cp437ReverseMap['\a'] = 0x07; // Bell
		Cp437ReverseMap['\b'] = 0x08; // Backspace
		Cp437ReverseMap['\t'] = 0x09; // Tab
									  // For newline, decide if '\n' should map to LF (0x0A) or CRLF (0x0D, 0x0A).
									  // Typically, for byte streams, you'd handle CR and LF separately if they appear.
									  // If the string contains '\n', we'll map it to LF (0x0A).
									  // If it contains '\r\n', each will be mapped: '\r' to 0x0D, '\n' to 0x0A.
		Cp437ReverseMap['\n'] = 0x0A; // Line Feed
		Cp437ReverseMap['\v'] = 0x0B; // Vertical Tab
		Cp437ReverseMap['\f'] = 0x0C; // Form Feed
		Cp437ReverseMap['\r'] = 0x0D; // Carriage Return
									  // Note: Cp437Map already has entries for 0x0E, 0x0F, 0x1A, 0x1B, etc.
									  // If a string contains '\u000E', it will use the existing map.
	}

	/// <summary>
	/// Converts a byte array to a string using CP850 (DOS Latin 1) encoding.
	/// </summary>
	/// <param name="bytes">The byte array to convert.</param>
	/// <returns>The decoded string.</returns>
	public static string GetStringCp850( byte[] bytes )
	{
		if ( bytes == null )
		{
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder( bytes.Length );
		foreach ( byte b in bytes )
		{
			sb.Append( Cp850Map[b] );
		}
		return sb.ToString();
	}

	/// <summary>
	/// Converts a byte array to a string using CP437 (Original IBM PC / DOS) encoding.
	/// </summary>
	/// <param name="bytes">The byte array to convert.</param>
	/// <returns>The decoded string.</returns>
	public static string GetStringCp437( byte[] bytes )
	{
		if ( bytes == null )
		{
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder( bytes.Length );
		foreach ( byte b in bytes )
		{
			sb.Append( Cp437Map[b] );
		}
		return sb.ToString();
	}
	/// <summary>
	/// Converts a string to a byte array using CP437 (Original IBM PC / DOS) encoding.
	/// Characters not found in CP437 will be replaced with the CP437 '?' character (byte 0x3F).
	/// </summary>
	/// <param name="text">The string to convert.</param>
	/// <returns>The encoded byte array.</returns>
	public static byte[] GetBytesCp437( string text )
	{
		if ( string.IsNullOrEmpty( text ) )
		{
			return System.Array.Empty<byte>();
		}

		List<byte> byteList = new List<byte>( text.Length );
		foreach ( char c in text )
		{
			if ( Cp437ReverseMap.TryGetValue( c, out byte byteValue ) )
			{
				byteList.Add( byteValue );
			}
			else
			{
				// Character not found in CP437, use '?' (0x3F) as a fallback.
				byteList.Add( 0x3F );
			}
		}
		return byteList.ToArray();
	}
	public static string GetStringCp437KeepControl( byte[] bytes )
	{
		if ( bytes == null )
		{
			return string.Empty;
		}

		StringBuilder sb = new StringBuilder( bytes.Length );
		foreach ( byte b in bytes )
		{
			if ( b == 00 )
			{
				continue;
			}
			else if ( b == 0x07 ) // Bell
			{
				sb.Append( '\a' );
				continue;
			}
			else if ( b == 0x08 ) // Backspace
			{
				sb.Append( '\b' );
				continue;
			}
			else if ( b == 0x09 ) // Tab
			{
				sb.Append( '\t' );
				continue;
			}
			else if ( b == 0x0A ) // Line Feed
			{
				sb.Append( '\n' );
				continue;
			}
			else if ( b == 0x0B ) // Vertical Tab
			{
				sb.Append( '\v' );
				continue;
			}
			else if ( b == 0x0C ) // Form Feed
			{
				sb.Append( '\f' );
				continue;
			}
			else if ( b == 0x0D ) // Carriage Return
			{
				sb.Append( '\n' );
				continue;
			}
			else if ( b == 0x0E ) // Shift Out
			{
				sb.Append( '\u000E' );
				continue;
			}
			else if ( b == 0x0F ) // Shift In
			{
				sb.Append( '\u000F' );
				continue;
			}
			else if ( b == 0x1A ) // Substitute
			{
				sb.Append( '\u001A' );
				continue;
			}
			else if ( b == 0x1B ) // Escape
			{
				sb.Append( '\u001B' );
				continue;
			}
			else if ( b == 0x1C ) // File Separator
			{
				sb.Append( '\u001C' );
				continue;
			}
			else if ( b == 0x1D ) // Group Separator
			{
				sb.Append( '\u001D' );
				continue;
			}
			else if ( b == 0x1E ) // Record Separator
			{
				sb.Append( '\u001E' );
				continue;
			}
			else if ( b == 0x7F ) // Delete
			{
				sb.Append( '\u007F' );
				continue;
			}

			sb.Append( Cp437Map[b] );
		}
		return sb.ToString();
	}

}
