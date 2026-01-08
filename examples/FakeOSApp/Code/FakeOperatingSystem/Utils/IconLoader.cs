using FakeOperatingSystem.Experiments.Ambitious.X86.Win32; // For PELoader and ImageResourceUtils
using FakeOperatingSystem.OSFileSystem;
using Sandbox; // For Log, Texture, FileSystem, ImageFormat
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FakeOperatingSystem.Utils;

public static class IconLoader
{
	// Cache for failed icon lookups to avoid repeated attempts.
	private static readonly ConcurrentDictionary<string, bool> _failedLookupsCache = new ConcurrentDictionary<string, bool>();
	// Cache for successfully parsed PE resources to avoid re-reading/re-parsing the same executable.
	private static readonly ConcurrentDictionary<string, List<PELoader.PEResourceEntry>> _peResourceCache = new ConcurrentDictionary<string, List<PELoader.PEResourceEntry>>();

	internal struct GrpIconDirEntry
	{
		public byte Width;      // Width, in pixels, of the image
		public byte Height;     // Height, in pixels, of the image
		public byte ColorCount; // Number of colors in image (0 if >=8bpp)
		public byte Reserved;   // Reserved (must be 0)
		public ushort Planes;   // Color Planes
		public ushort BitCount; // Bits per pixel
		public uint BytesInRes; // How many bytes in this resource?
		public ushort ID;       // the ID of the RT_ICON resource
	}

	public static async Task<string> LoadIconAndGetPath( string executablePath, object iconGroupIdOrName, int desiredWidth, int desiredHeight )
	{
		// Check if its a fake EXE file
		if ( NativeProgram.IsNativeProgramExe( executablePath ) )
			return null;

		object idForHash = (iconGroupIdOrName is int intId && intId == 0) ? "default_icon" : iconGroupIdOrName;
		string uniqueIdentifier = $"{executablePath}|{idForHash}|{desiredWidth}|{desiredHeight}";
		string hashedFileName;

		using ( var sha256 = SHA256.Create() )
		{
			byte[] hashBytes = sha256.ComputeHash( Encoding.UTF8.GetBytes( uniqueIdentifier ) );
			// Optimized hex string conversion
			hashedFileName = $"{Convert.ToHexString( hashBytes ).ToLowerInvariant()}.png";
		}

		string iconDumpDirectory = "IconDump";
		string iconFilePath = $"{iconDumpDirectory}/{hashedFileName}";
		string fullVirtualPath = $"data:{iconFilePath}";

		if ( _failedLookupsCache.ContainsKey( uniqueIdentifier ) )
		{
			//Log.Info( $"[IconLoader] Icon lookup previously failed for '{uniqueIdentifier}', returning null (cached failure)." );
			return null;
		}

		if ( FileSystem.Data.FileExists( iconFilePath ) )
		{
			//Log.Info( $"[IconLoader] Icon already exists, returning cached path: '{fullVirtualPath}'" );
			return fullVirtualPath;
		}

		var iconTexture = await LoadIconFromExeAsync( executablePath, iconGroupIdOrName, desiredWidth, desiredHeight );
		if ( iconTexture == null )
		{
			Log.Warning( $"[IconLoader] Failed to load icon for '{executablePath}' (group: {iconGroupIdOrName}). Caching this failure." );
			_failedLookupsCache.TryAdd( uniqueIdentifier, true );
			return null;
		}

		try
		{
			FileSystem.Data.CreateDirectory( iconDumpDirectory );
			var data = iconTexture.GetBitmap( 0 ).ToPng();
			using ( var stream = FileSystem.Data.OpenWrite( iconFilePath ) )
			{
				await stream.WriteAsync( data, 0, data.Length );
			}
			//Log.Info( $"[IconLoader] Successfully saved icon to '{iconFilePath}'. Returning path: '{fullVirtualPath}'" );
			return fullVirtualPath;
		}
		catch ( Exception ex )
		{
			Log.Error( $"[IconLoader] Failed to save icon to '{iconFilePath}': {ex.Message}. Caching this failure." );
			_failedLookupsCache.TryAdd( uniqueIdentifier, true );
			return null;
		}
		finally
		{
			iconTexture?.Dispose();
		}
	}

	public static async Task<Texture> LoadIconFromExeAsync( string executablePath, object iconGroupIdOrName, int desiredWidth, int desiredHeight )
	{
		if ( string.IsNullOrEmpty( executablePath ) )
		{
			Log.Warning( $"[IconLoader] Executable path is null or empty." );
			return null;
		}

		string actualExecutablePath = executablePath;
		if ( !VirtualFileSystem.Instance.FileExists( actualExecutablePath ) )
		{
			var gamePath = VirtualFileSystem.Instance.GetFullPath( actualExecutablePath );
			if ( !VirtualFileSystem.Instance.FileExists( gamePath ) )
			{
				Log.Warning( $"[IconLoader] Executable path not found: {actualExecutablePath} (also tried {gamePath})" );
				return null;
			}
			actualExecutablePath = gamePath;
		}

		// Check if its a fake EXE file
		if ( NativeProgram.IsNativeProgramExe( executablePath ) )
			return null;

		List<PELoader.PEResourceEntry> allResources;

		if ( _peResourceCache.TryGetValue( actualExecutablePath, out var cachedResources ) )
		{
			allResources = cachedResources;
			//Log.Info( $"[IconLoader] Using cached PE resources for '{actualExecutablePath}'" );
		}
		else
		{
			byte[] fileBytes;
			try
			{
				fileBytes = await VirtualFileSystem.Instance.ReadAllBytesAsync( actualExecutablePath );
			}
			catch ( Exception ex )
			{
				Log.Error( $"[IconLoader] Error reading executable file '{actualExecutablePath}': {ex.Message}" );
				return null;
			}

			if ( fileBytes == null || fileBytes.Length == 0 )
			{
				Log.Warning( $"[IconLoader] Executable file is empty: {actualExecutablePath}" );
				return null;
			}

			var peLoader = new PELoader();
			if ( !peLoader.ParseAllResources( fileBytes, out allResources ) || !allResources.Any() )
			{
				Log.Warning( $"[IconLoader] Failed to parse resources or no resources found in '{actualExecutablePath}'. Caching this PE parse failure." );
				// Cache an empty list or a specific marker for PE parse failure to avoid re-parsing a known bad/empty PE for resources.
				_peResourceCache.TryAdd( actualExecutablePath, new List<PELoader.PEResourceEntry>() );
				return null;
			}
			_peResourceCache.TryAdd( actualExecutablePath, allResources );
		}

		// If cached resources were empty (e.g. from a previous parse failure for this EXE)
		if ( allResources == null || !allResources.Any() )
		{
			// Log.Warning( $"[IconLoader] No resources available (possibly cached empty) for '{actualExecutablePath}'." );
			return null;
		}


		uint groupIconId = 0;
		string groupIconName = null;
		PELoader.PEResourceEntry groupIconEntry = null;
		bool isDefaultIconRequest = false;

		if ( iconGroupIdOrName is int intId && intId == 0 )
		{
			isDefaultIconRequest = true;
			groupIconEntry = allResources
				.Where( r => r.Type == 14 )
				.OrderBy( r => r.Name )
				.FirstOrDefault();
			if ( groupIconEntry != null ) groupIconId = groupIconEntry.Name;
		}
		else if ( iconGroupIdOrName is string nameStr )
		{
			if ( nameStr.StartsWith( "#" ) && uint.TryParse( nameStr.AsSpan( 1 ), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out uint parsedId ) )
			{
				groupIconId = parsedId;
			}
			else
			{
				Log.Warning( $"[IconLoader] String icon group name '{nameStr}' provided. String name lookup is not robustly supported." );
				groupIconName = nameStr;
			}
		}
		else if ( iconGroupIdOrName is uint idAsUint ) groupIconId = idAsUint;
		else if ( iconGroupIdOrName is ushort idAsUshort ) groupIconId = idAsUshort;
		else if ( iconGroupIdOrName is int idAsInt && idAsInt > 0 ) groupIconId = (uint)idAsInt;
		else
		{
			Log.Error( $"[IconLoader] Invalid iconGroupIdOrName type: {iconGroupIdOrName?.GetType()} (Value: '{iconGroupIdOrName}')" );
			return null;
		}

		if ( !isDefaultIconRequest && groupIconEntry == null )
		{
			// String name lookup would require PEResourceEntry to support string names and PELoader to populate them.
			// if (groupIconName != null) { /* ... find by string name ... */ }
			if ( groupIconEntry == null && groupIconId != 0 )
			{
				groupIconEntry = allResources.FirstOrDefault( r => r.Type == 14 && r.Name == groupIconId );
			}
		}

		if ( groupIconEntry == null )
		{
			string requestedIdentifier = isDefaultIconRequest ? "default (lowest ID)" : iconGroupIdOrName.ToString();
			Log.Warning( $"[IconLoader] RT_GROUP_ICON resource not found. Executable='{Path.GetFileName( actualExecutablePath )}', Group='{requestedIdentifier}' (ID: {groupIconId})" );
			return null;
		}

		uint actualGroupIconIdForLog = groupIconEntry.Name;
		var iconDirEntries = new List<GrpIconDirEntry>();
		try
		{
			using var ms = new MemoryStream( groupIconEntry.Data );
			using var reader = new BinaryReader( ms );
			ushort idReserved = reader.ReadUInt16();
			ushort idType = reader.ReadUInt16();
			ushort idCount = reader.ReadUInt16();

			if ( idReserved != 0 || idType != 1 || idCount == 0 )
			{
				Log.Warning( $"[IconLoader] Invalid GRPICONDIR header in '{Path.GetFileName( actualExecutablePath )}'. GroupID: {actualGroupIconIdForLog}" );
				return null;
			}
			for ( int i = 0; i < idCount; i++ )
			{
				if ( ms.Position + 14 > ms.Length ) break;
				iconDirEntries.Add( new GrpIconDirEntry
				{
					Width = reader.ReadByte(),
					Height = reader.ReadByte(),
					ColorCount = reader.ReadByte(),
					Reserved = reader.ReadByte(),
					Planes = reader.ReadUInt16(),
					BitCount = reader.ReadUInt16(),
					BytesInRes = reader.ReadUInt32(),
					ID = reader.ReadUInt16()
				} );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"[IconLoader] Exception parsing GRPICONDIR for '{Path.GetFileName( actualExecutablePath )}', GroupID='{actualGroupIconIdForLog}': {ex.Message}" );
			return null;
		}

		if ( !iconDirEntries.Any() )
		{
			Log.Warning( $"[IconLoader] No icon entries found in GRPICONDIR for '{Path.GetFileName( actualExecutablePath )}', GroupID='{actualGroupIconIdForLog}'" );
			return null;
		}

		GrpIconDirEntry bestEntry = iconDirEntries
			.OrderBy( entry => Math.Abs( (entry.Width == 0 ? 256 : entry.Width) - desiredWidth ) + Math.Abs( (entry.Height == 0 ? 256 : entry.Height) - desiredHeight ) )
			.ThenByDescending( entry => (entry.Width == 0 ? 256 : entry.Width) >= desiredWidth && (entry.Height == 0 ? 256 : entry.Height) >= desiredHeight )
			.ThenByDescending( entry => entry.BitCount )
			.ThenByDescending( entry => (entry.Width == 0 ? 256 : entry.Width) )
			.FirstOrDefault();

		if ( bestEntry.ID == 0 )
		{
			Log.Warning( $"[IconLoader] Could not determine best icon entry for GroupID '{actualGroupIconIdForLog}'." );
			return null;
		}

		PELoader.PEResourceEntry actualIconResource = allResources.FirstOrDefault( r => r.Type == 3 && r.Name == bestEntry.ID );

		if ( actualIconResource == null )
		{
			Log.Warning( $"[IconLoader] RT_ICON resource not found for ID {bestEntry.ID} from group '{actualGroupIconIdForLog}'." );
			return null;
		}

		if ( ImageResourceUtils.ParseDibAndConvertToRgba( actualIconResource.Data, true, out int iconWidth, out int iconVisualHeight, out byte[] iconRgbaData ) )
		{
			try
			{
				var texture = Texture.Create( iconWidth, iconVisualHeight, ImageFormat.RGBA8888 )
									 .WithData( iconRgbaData )
									 .WithName( $"exe_icon_{Path.GetFileNameWithoutExtension( actualExecutablePath )}_{actualGroupIconIdForLog}_{bestEntry.ID}" )
									 .Finish();
				return texture;
			}
			catch ( Exception ex )
			{
				Log.Error( $"[IconLoader] Failed to create S&box texture for icon ID {bestEntry.ID}: {ex.Message}" );
				return null;
			}
		}
		else
		{
			Log.Warning( $"[IconLoader] Failed to parse DIB data for icon ID {bestEntry.ID} from group '{actualGroupIconIdForLog}'." );
			return null;
		}
	}
}
