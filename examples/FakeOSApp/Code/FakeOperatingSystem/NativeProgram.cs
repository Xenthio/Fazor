using FakeDesktop;
using FakeOperatingSystem.OSFileSystem;
using Sandbox;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace FakeOperatingSystem;

public abstract class NativeProgram
{
	internal System.IO.TextWriter StandardOutput;
	internal System.IO.TextWriter StandardError;
	internal System.IO.TextReader StandardInput;

	// The hexadecimal representation of our minimal Win32 executable
	// that displays a message box when run on a real system
	protected static readonly string ExeTemplateHex =
		"4D5A90000300000004000000FFFF" +
		"0000B80000000000000040000000" +
		"0000000000000000000000000000" +
		"0000000000000000000000000000" +
		"00000000C00000000E1FBA0E00B4" +
		"09CD21B8014CCD21546869732070" +
		"726F6772616D2063616E6E6F7420" +
		"62652072756E20696E20444F5320" +
		"6D6F64652E0D0D0A240000000000" +
		"0000110505C755646B9455646B94" +
		"55646B9421E56A9556646B945564" +
		"6A9454646B9470ED6F9554646B94" +
		"70ED699554646B94526963685564" +
		"6B94000000000000000050450000" +
		"4C01030022E31968000000000000" +
		"0000E00002010B010E2C20000000" +
		"C001000000000000300200003002" +
		"0000500200000000400010000000" +
		"1000000006000000000000000600" +
		"0000000000001004000030020000" +
		"0000000002004085000010000010" +
		"0000000010000010000000000000" +
		"100000000000000000000000B003" +
		"0000280000000000000000000000" +
		"0000000000000000000000000000" +
		"00000004000010000000B8020000" +
		"1C00000000000000000000000000" +
		"0000000000000000000000000000" +
		"0000000000000000000000000000" +
		"0000500200000800000000000000" +
		"0000000000000000000000000000" +
		"0000000000002E74657874000000" +
		"1900000030020000200000003002" +
		"0000000000000000000000000000" +
		"200000602E72646174610000AA01" +
		"000050020000B001000050020000" +
		"0000000000000000000000004000" +
		"00402E72656C6F63000010000000" +
		"0004000010000000000400000000" +
		"0000000000000000000040000042" +
		"6A40685802400068680240006A00" +
		"FF155002400033C0C21000000000" +
		"00000000E0030000000000004475" +
		"6D6D792045584500000000000000" +
		"54686973206973206E6F74206120" +
		"7265616C2065786563757461626C" +
		"652066696C652E0A497420697320" +
		"6D65616E7420746F206265207573" +
		"65642077697468696E2078677569" +
		"2D74657374332E00000000000000" +
		"22E31968000000000D000000BC00" +
		"0000F4020000F402000018000000" +
		"038003800000000000000000EC02" +
		"0000080000003002000019000000" +
		"0000000030020000190000002E74" +
		"657874246D6E0000000050020000" +
		"080000002E696461746124350000" +
		"0000580200007C0000002E726461" +
		"74610000D4020000200000002E72" +
		"6461746124766F6C746D64000000" +
		"F4020000BC0000002E7264617461" +
		"247A7A7A646267000000B0030000" +
		"140000002E696461746124320000" +
		"0000C4030000140000002E696461" +
		"7461243300000000D80300000800" +
		"00002E6964617461243400000000" +
		"E00300001A0000002E6964617461" +
		"243600000000D803000000000000" +
		"00000000EE030000500200000000" +
		"0000000000000000000000000000" +
		"00000000E0030000000000008D02" +
		"4D657373616765426F7841005553" +
		"455233322E646C6C000000000000" +
		"0000000000001000000033323832" +
		"40320000";

	// Convert the hex string to a byte array when first used
	protected static readonly byte[] ExeTemplateBytes = HexStringToByteArray( ExeTemplateHex );

	public abstract string FilePath { get; }
	public virtual bool ConsoleApp { get; } = false;
	public abstract void Main( NativeProcess process, Win32LaunchOptions launchOptions = default );

	/// <summary>
	/// Creates a fake executable file with embedded program descriptor for the given NativeProgram type.
	/// </summary>
	public static void CompileIntoExe( Type programType, string path )
	{
		// Use TypeLibrary to get the full type name for this NativeProgram
		var typeDesc = TypeLibrary.GetType( programType );
		string typeName = typeDesc?.Name ?? programType.FullName;

		// Create the descriptor with this program's info
		var descriptor = new NativeProgramDescriptor()
		{
			TypeName = typeName,
		};

		// Serialize descriptor
		string descriptorJson = Json.Serialize( descriptor );
		byte[] descriptorBytes = Encoding.UTF8.GetBytes( descriptorJson );

		// Combine EXE template and descriptor
		byte[] combinedBytes = new byte[ExeTemplateBytes.Length + descriptorBytes.Length];
		Array.Copy( ExeTemplateBytes, combinedBytes, ExeTemplateBytes.Length );
		Array.Copy( descriptorBytes, 0, combinedBytes, ExeTemplateBytes.Length, descriptorBytes.Length );

		// Ensure directory exists using VirtualFileSystem.Instance
		string directory = Path.GetDirectoryName( path );
		if ( !VirtualFileSystem.Instance.DirectoryExists( directory ) )
			VirtualFileSystem.Instance.CreateDirectory( directory );

		// Write to file using VirtualFileSystem.Instance
		using var stream = VirtualFileSystem.Instance.OpenWrite( path );
		stream.Write( combinedBytes, 0, combinedBytes.Length );

		Log.Info( $"Created fake executable at {path} for program type {typeName}" );
	}

	/// <summary>
	/// Reads a NativeProgram from a fake executable file using TypeLibrary reflection.
	/// </summary>
	public static NativeProgram ReadFromExe( string path )
	{
		Log.Info( $"Reading fake executable from: {path}" );
		try
		{
			// Read the file from the virtual file system
			if ( !VirtualFileSystem.Instance.FileExists( path ) )
			{
				Log.Warning( $"Executable not found: {path}" );
				return null;
			}

			Log.Info( $"Opening {path}" );


			byte[] fileBytes = VirtualFileSystem.Instance.ReadAllBytes( path );

			if ( fileBytes.Length <= ExeTemplateBytes.Length )
			{
				Log.Warning( $"File too small to be a valid fake executable: {path}" );
				return null;
			}

			byte[] descriptorBytes = new byte[fileBytes.Length - ExeTemplateBytes.Length];
			Array.Copy( fileBytes, ExeTemplateBytes.Length, descriptorBytes, 0, descriptorBytes.Length );

			string descriptorJson = Encoding.UTF8.GetString( descriptorBytes );

			var descriptor = Json.Deserialize<NativeProgramDescriptor>( descriptorJson );
			if ( descriptor == null || string.IsNullOrEmpty( descriptor.TypeName ) )
				return null;

			var typeDesc = TypeLibrary.GetType( descriptor.TypeName );
			if ( typeDesc == null )
			{
				Log.Warning( $"TypeLibrary could not find type '{descriptor.TypeName}'" );
				return null;
			}

			var instance = typeDesc.Create<NativeProgram>();
			if ( instance == null )
			{
				Log.Warning( $"Failed to instantiate NativeProgram of type '{descriptor.TypeName}'" );
				return null;
			}

			return instance;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to read fake executable '{path}': {ex.Message}" );
			return null;
		}
	}

	public static bool IsNativeProgramExe( string path )
	{
		if ( !VirtualFileSystem.Instance.FileExists( path ) )
			return false;

		byte[] fileBytes;
		try
		{
			fileBytes = VirtualFileSystem.Instance.ReadAllBytes( path );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Error reading file {path} for IsNativeProgramExe check: {ex.Message}" );
			return false; // Could not read the file
		}

		if ( fileBytes.Length < ExeTemplateBytes.Length )
			return false; // File is too small to contain the template

		// Compare the beginning of the file with ExeTemplateBytes
		return fileBytes.Take( ExeTemplateBytes.Length ).SequenceEqual( ExeTemplateBytes );
	}

	protected static byte[] HexStringToByteArray( string hex )
	{
		hex = hex.Replace( " ", "" ).Replace( "\n", "" ).Replace( "\r", "" );
		int length = hex.Length;
		byte[] bytes = new byte[length / 2];

		for ( int i = 0; i < length; i += 2 )
		{
			bytes[i / 2] = Convert.ToByte( hex.Substring( i, 2 ), 16 );
		}

		return bytes;
	}
}


