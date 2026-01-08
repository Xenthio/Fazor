using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FakeOperatingSystem.Experiments.Ambitious.X86.Win32
{
	public static class DialogTemplateParser
	{
		public class DialogTemplate
		{
			public uint Style;
			public uint ExtendedStyle;
			public ushort ControlCount;
			public short X, Y, Width, Height;
			public object Menu;   // ushort (ordinal) or string
			public object Class;  // ushort (ordinal) or string
			public string Title;
			public string FontFace;
			public ushort? PointSize;
			public byte? FontWeight;
			public byte? Italic;
			public byte? Charset;
			public List<DialogControl> Controls { get; set; } = new();
		}

		public class DialogControl
		{
			public uint Style;
			public uint ExtendedStyle;
			public short X, Y, Width, Height;
			public uint ID;
			public object Class;   // ushort (ordinal) or string
			public object Title;   // ushort (ordinal) or string
			public ushort ExtraDataSize;
			public byte[] ExtraData;
		}

		private const uint DS_SETFONT = 0x40;

		public static DialogTemplate Parse( byte[] data )
		{
			var template = new DialogTemplate();
			if ( data == null || data.Length == 0 )
			{
				// Or throw ArgumentException, depending on desired behavior for empty data
				return template;
			}

			using ( var stream = new MemoryStream( data ) )
			using ( var reader = new BinaryReader( stream, System.Text.Encoding.Unicode ) )
			{
				ushort firstWord = 0;
				ushort secondWord = 0;

				if ( reader.BaseStream.Length >= 4 )
				{
					firstWord = reader.ReadUInt16();
					secondWord = reader.ReadUInt16();
				}
				reader.BaseStream.Seek( 0, SeekOrigin.Begin ); // Reset position

				// DLGTEMPLATEEX starts with dlgVer=1 (WORD) and signature=0xFFFF (WORD)
				bool isEx = (secondWord == 0xFFFF && firstWord == 1);

				if ( isEx )
				{
					// DLGTEMPLATEEX
					reader.ReadUInt16(); // dlgVer (already read for check)
					reader.ReadUInt16(); // signature (already read for check)
					reader.ReadUInt32(); // helpID (discard)

					template.ExtendedStyle = reader.ReadUInt32();
					template.Style = reader.ReadUInt32();
					template.ControlCount = reader.ReadUInt16();
					template.X = reader.ReadInt16();
					template.Y = reader.ReadInt16();
					template.Width = reader.ReadInt16();
					template.Height = reader.ReadInt16();

					template.Menu = ReadSzOrOrd( reader );
					template.Class = ReadSzOrOrd( reader );
					template.Title = ReadUnicodeString( reader );

					if ( (template.Style & DS_SETFONT) != 0 )
					{
						template.PointSize = reader.ReadUInt16();
						ushort weightWord = reader.ReadUInt16(); // DLGTEMPLATEEX has WORD wWeight
						template.FontWeight = (byte)weightWord;  // Cast to byte as per class definition
						template.Italic = reader.ReadByte();
						template.Charset = reader.ReadByte();
						template.FontFace = ReadUnicodeString( reader );
					}
				}
				else
				{
					// DLGTEMPLATE
					template.Style = reader.ReadUInt32();
					template.ExtendedStyle = reader.ReadUInt32(); // This is dwExtendedStyle
					template.ControlCount = reader.ReadUInt16();
					template.X = reader.ReadInt16();
					template.Y = reader.ReadInt16();
					template.Width = reader.ReadInt16();
					template.Height = reader.ReadInt16();

					template.Menu = ReadSzOrOrd( reader );
					template.Class = ReadSzOrOrd( reader );
					template.Title = ReadUnicodeString( reader );

					if ( (template.Style & DS_SETFONT) != 0 )
					{
						template.PointSize = reader.ReadUInt16();
						// DLGTEMPLATE with DS_SETFONT only has PointSize and TypeFace.
						// FontWeight, Italic, Charset remain null.
						template.FontFace = ReadUnicodeString( reader );
					}
				}

				// Parse Controls
				for ( int i = 0; i < template.ControlCount; i++ )
				{
					AlignToDword( reader );
					var control = new DialogControl();

					if ( isEx )
					{
						// DLGITEMTEMPLATEEX
						reader.ReadUInt32(); // helpID (discard)
						control.ExtendedStyle = reader.ReadUInt32();
						control.Style = reader.ReadUInt32();
						control.X = reader.ReadInt16();
						control.Y = reader.ReadInt16();
						control.Width = reader.ReadInt16();
						control.Height = reader.ReadInt16();
						control.ID = reader.ReadUInt32(); // DWORD ID
					}
					else
					{
						// DLGITEMTEMPLATE
						control.Style = reader.ReadUInt32();
						control.ExtendedStyle = reader.ReadUInt32(); // dwExtendedStyle
						control.X = reader.ReadInt16();
						control.Y = reader.ReadInt16();
						control.Width = reader.ReadInt16();
						control.Height = reader.ReadInt16();
						control.ID = reader.ReadUInt16(); // WORD ID (promotes to uint)
					}

					control.Class = ReadSzOrOrd( reader );
					control.Title = ReadSzOrOrd( reader ); // Title can also be sz_Or_Ord for controls

					control.ExtraDataSize = reader.ReadUInt16();
					if ( control.ExtraDataSize > 0 )
					{
						control.ExtraData = reader.ReadBytes( control.ExtraDataSize );
					}
					else
					{
						control.ExtraData = Array.Empty<byte>();
					}
					template.Controls.Add( control );
				}
			}
			return template;
		}

		private static string ReadUnicodeString( BinaryReader reader )
		{
			var sb = new StringBuilder();
			while ( reader.BaseStream.Position < reader.BaseStream.Length )
			{
				// Check if there are enough bytes for a full character
				if ( reader.BaseStream.Position + 2 > reader.BaseStream.Length ) break;

				ushort charCode = reader.ReadUInt16();
				if ( charCode == 0 )
					break;
				sb.Append( (char)charCode );
			}
			return sb.ToString();
		}

		private static object ReadSzOrOrd( BinaryReader reader )
		{
			// Check if there are enough bytes for a WORD
			if ( reader.BaseStream.Position + 2 > reader.BaseStream.Length ) return string.Empty;

			ushort firstWord = reader.ReadUInt16();
			if ( firstWord == 0xFFFF )
			{
				// Check if there are enough bytes for the ordinal ID
				if ( reader.BaseStream.Position + 2 > reader.BaseStream.Length ) return (ushort)0xFFFF; // Should indicate error or malformed
				return reader.ReadUInt16(); // Ordinal ID
			}
			else
			{
				// It's a string. Put the first word (which is the first char) back and read.
				reader.BaseStream.Seek( -2, SeekOrigin.Current );
				return ReadUnicodeString( reader );
			}
		}

		private static void AlignToDword( BinaryReader reader )
		{
			long currentPosition = reader.BaseStream.Position;
			long nextDwordBoundary = (currentPosition + 3) & ~3L;
			if ( nextDwordBoundary > currentPosition )
			{
				if ( reader.BaseStream.Position + (nextDwordBoundary - currentPosition) <= reader.BaseStream.Length )
				{
					reader.BaseStream.Seek( nextDwordBoundary - currentPosition, SeekOrigin.Current );
				}
				else // Not enough data to align, seek to end
				{
					reader.BaseStream.Seek( 0, SeekOrigin.End );
				}
			}
		}
	}
}
