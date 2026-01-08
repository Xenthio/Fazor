using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeOperatingSystem.OSFileSystem;

/// <summary>
/// Implementation of the virtual file system that focuses purely on file operations
/// </summary>
public class VirtualFileSystem : IVirtualFileSystem
{
	// Singleton instance
	public static VirtualFileSystem Instance { get; private set; }

	// Mount points map virtual drive letters to real paths
	private Dictionary<string, MountPoint> _mountPoints = new( StringComparer.OrdinalIgnoreCase );

	// Default file system for operations not associated with a specific mount
	private BaseFileSystem _defaultFileSystem;

	public event Action<string> OnFileSystemChanged;

	public VirtualFileSystem( BaseFileSystem defaultFileSystem )
	{
		Instance = this;
		_defaultFileSystem = defaultFileSystem;

		// Set up default mount points
		RegisterMountPoint( "C:", "FakeSystemRoot" );
		RegisterMountPoint( "FS:A:", "/", FileSystem.Mounted );
		RegisterMountPoint( "FS:B:", "/", FileSystem.OrganizationData );
		RegisterMountPoint( "FS:C:", "/", FileSystem.Data );
	}

	public void RegisterMountPoint( string mountName, string path, BaseFileSystem fileSystem = null )
	{
		// Ensure directory exists for the mount point, creating it if necessary
		if ( fileSystem == null )
		{
			fileSystem = _defaultFileSystem;
		}
		if ( !fileSystem.DirectoryExists( path ) )
		{
			Log.Warning( $"Mount point path '{path}' does not exist. Creating it." );
			fileSystem.CreateDirectory( path );
		}
		Log.Info( $"Registering mount point '{mountName}' at path '{path}' with file system '{fileSystem.GetType().Name}'." );

		_mountPoints[mountName] = new MountPoint
		{
			Name = mountName,
			RealPath = path,
			FileSystem = fileSystem
		};
	}

	public bool IsMountPointValid( string mountPoint )
	{
		return _mountPoints.ContainsKey( mountPoint );
	}

	/// <summary>
	/// Resolves a path with mount points to a real path and file system
	/// </summary>
	public PathResolution ResolveMountPoint( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			return new PathResolution
			{
				MountPoint = null,
				RealPath = path,
				FileSystem = _defaultFileSystem
			};
		}

		// Normalize the path
		path = path.Replace( '\\', '/' );

		// Handle shell namespace paths first

		// Find the mount point
		foreach ( var mount in _mountPoints.Keys.OrderByDescending( k => k.Length ) )
		{
			if ( path.StartsWith( mount, StringComparison.OrdinalIgnoreCase ) )
			{
				var mountPoint = _mountPoints[mount];
				string relativePath = path.Substring( mount.Length ).TrimStart( '/' );
				string fullPath = Path.Combine( mountPoint.RealPath, relativePath ).Replace( '\\', '/' );

				return new PathResolution
				{
					MountPoint = mountPoint,
					RealPath = fullPath,
					FileSystem = mountPoint.FileSystem ?? _defaultFileSystem
				};
			}
		}

		// No mount point found - treat as a path on the default file system
		return new PathResolution
		{
			MountPoint = null,
			RealPath = path,
			FileSystem = _defaultFileSystem
		};
	}

	public string ResolvePath( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.RealPath;
	}

	public bool FileExists( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			return false;
		}
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.FileExists( resolution.RealPath );
	}

	public bool DirectoryExists( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			return false;
		}
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.DirectoryExists( resolution?.RealPath );
	}

	public Stream OpenRead( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.OpenRead( resolution.RealPath );
	}

	public Stream OpenWrite( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.OpenWrite( resolution.RealPath );
	}

	public byte[] ReadAllBytes( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.ReadAllBytes( resolution.RealPath ).ToArray();
	}

	public async Task<byte[]> ReadAllBytesAsync( string path )
	{
		var resolution = ResolveMountPoint( path );
		return await resolution.FileSystem.ReadAllBytesAsync( resolution.RealPath );
	}

	public string ReadAllText( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.ReadAllText( resolution.RealPath );
	}

	public void WriteAllBytes( string path, byte[] contents )
	{
		var resolution = ResolveMountPoint( path );

		resolution.FileSystem.OpenWrite( resolution.RealPath ).Write( contents, 0, contents.Length );

		//resolution.FileSystem.WriteAllBytes( resolution.RealPath, contents );
	}

	public void WriteAllText( string path, string contents )
	{
		var resolution = ResolveMountPoint( path );
		resolution.FileSystem.WriteAllText( resolution.RealPath, contents );
	}

	public IEnumerable<string> GetFiles( string path, string searchPattern = "*" )
	{
		var resolution = ResolveMountPoint( path );
		var files = resolution.FileSystem.FindFile( resolution.RealPath );

		if ( resolution.MountPoint != null )
		{
			string mountPrefix = resolution.MountPoint.Name;
			string baseVirtualPath = path.TrimEnd( '/' );

			return files.Select( f =>
			{
				// Normalize
				string normalizedRealPath = resolution.RealPath.TrimEnd( '/' );
				string normalizedFile = f.TrimEnd( '/' );

				string relativePath = "";
				if ( normalizedFile.StartsWith( normalizedRealPath, StringComparison.OrdinalIgnoreCase ) )
				{
					int startIndex = normalizedRealPath.Length;
					if ( normalizedFile.Length > startIndex && normalizedFile[startIndex] == '/' )
						startIndex++;
					relativePath = normalizedFile.Substring( startIndex );
				}
				else
				{
					relativePath = Path.GetFileName( f );
				}

				// Always return full virtual path
				return $"{baseVirtualPath}/{relativePath.TrimStart( '/' )}";
			} );
		}

		return files;
	}

	public IEnumerable<string> GetDirectories( string path )
	{
		var resolution = ResolveMountPoint( path );
		var underlyingSystemDirs = resolution.FileSystem.FindDirectory( resolution.RealPath );

		string baseVirtualPath = path.TrimEnd( '/' );
		// Ensure baseVirtualPath is consistently formatted for concatenation,
		// e.g., "C:" or "C:/Users". Appending "/." or "/.." will work correctly.

		var finalResults = new List<string>();

		// Add "." (current directory)
		// Path will be like "C:/CurrentFolder/."
		finalResults.Add( baseVirtualPath + "/." );

		// Add ".." (parent directory)
		// Only add if not at a root directory (e.g., "C:").
		// GetDirectoryName("C:") returns null.
		// GetDirectoryName("C:/Users") returns "C:/".
		if ( GetDirectoryName( baseVirtualPath ) != null )
		{
			finalResults.Add( baseVirtualPath + "/.." );
		}

		// Process actual child directories using the original logic
		IEnumerable<string> actualChildVirtualPaths;
		if ( resolution.MountPoint != null )
		{
			actualChildVirtualPaths = underlyingSystemDirs.Select( d_underlying =>
			{
				string normalizedRealPath = resolution.RealPath.TrimEnd( '/' ); // e.g., "FakeSystemRoot/Users"
				string normalizedDirFromSystem = d_underlying.Replace( '\\', '/' ).TrimEnd( '/' ); // e.g., "FakeSystemRoot/Users/Alice" or "Alice"

				string relativeNameSegment;
				if ( normalizedDirFromSystem.StartsWith( normalizedRealPath + "/", StringComparison.OrdinalIgnoreCase ) )
				{
					// Full path from underlying system, extract relative part
					relativeNameSegment = normalizedDirFromSystem.Substring( normalizedRealPath.Length ).TrimStart( '/' );
				}
				else if ( normalizedDirFromSystem.Equals( normalizedRealPath, StringComparison.OrdinalIgnoreCase ) )
				{
					// This is the directory itself, not a child.
					return null;
				}
				else if ( !normalizedDirFromSystem.Contains( "/" ) )
				{
					// Simple name (relative to normalizedRealPath)
					relativeNameSegment = normalizedDirFromSystem;
				}
				else
				{
					// Some other path format, take the last part as the name.
					relativeNameSegment = GetFileName( normalizedDirFromSystem );
				}

				// Skip if the name segment is empty, or represents "." or ".." to avoid duplicates
				if ( string.IsNullOrWhiteSpace( relativeNameSegment ) || relativeNameSegment == "." || relativeNameSegment == ".." )
				{
					return null;
				}

				// Construct full virtual path: e.g., "C:/Users" + "/" + "Alice"
				return baseVirtualPath + "/" + relativeNameSegment.TrimStart( '/' );
			} ).Where( p => p != null );
		}
		else
		{
			// No mount point, paths are direct from the default file system.
			// Filter out any "." or ".." that might come from the underlying system.
			actualChildVirtualPaths = underlyingSystemDirs.Where( d =>
			{
				var name = GetFileName( d ); // VFS GetFileName uses Path.GetFileName
				return name != "." && name != "..";
			} );
		}

		finalResults.AddRange( actualChildVirtualPaths );
		return finalResults;
	}

	public void CreateDirectory( string path )
	{
		var resolution = ResolveMountPoint( path );
		resolution.FileSystem.CreateDirectory( resolution.RealPath );
	}

	public void DeleteFile( string path )
	{
		var resolution = ResolveMountPoint( path );
		resolution.FileSystem.DeleteFile( resolution.RealPath );
	}

	public void DeleteDirectory( string path, bool recursive = false )
	{
		var resolution = ResolveMountPoint( path );

		if ( recursive )
		{
			// Delete all files and subdirectories recursively
			foreach ( var file in GetFiles( path ) )
			{
				DeleteFile( file );
			}

			foreach ( var dir in GetDirectories( path ) )
			{
				// Ensure we don't try to delete "." or ".." which are now part of GetDirectories
				var dirName = GetFileName( dir );
				if ( dirName == "." || dirName == ".." ) continue;
				DeleteDirectory( dir, true );
			}
		}

		resolution.FileSystem.DeleteDirectory( resolution.RealPath );
	}

	public string GetFileName( string path )
	{
		return Path.GetFileName( path );
	}

	public string GetDirectoryName( string path )
	{
		return Path.GetDirectoryName( path )?.Replace( '\\', '/' );
	}

	public string GetFileNameWithoutExtension( string path )
	{
		return Path.GetFileNameWithoutExtension( path );
	}

	public string GetExtension( string path )
	{
		return Path.GetExtension( path );
	}

	public long FileSize( string path )
	{
		var resolution = ResolveMountPoint( path );
		return resolution.FileSystem.FileSize( resolution.RealPath );
	}

	public long ModifiedDate( string path )
	{
		//stub
		return DateTime.UtcNow.ToFileTimeUtc();
	}

	public IEnumerable<string> FindFile( string sourceDir )
	{
		var resolution = ResolveMountPoint( sourceDir );
		return resolution.FileSystem.FindFile( resolution.RealPath );
	}
	public IEnumerable<string> FindDirectory( string sourceDir )
	{
		var resolution = ResolveMountPoint( sourceDir );
		return resolution.FileSystem.FindDirectory( resolution.RealPath );
	}

	public long RecursiveDirectorySize( string path )
	{
		if ( DirectoryExists( path ) == false )
		{
			return 0;
		}
		var resolution = ResolveMountPoint( path );

		long totalSize = 0;
		// Get all files in the directory
		var files = resolution.FileSystem.FindFile( resolution.RealPath );
		foreach ( var file in files )
		{
			totalSize += resolution.FileSystem.FileSize( Path.Combine( resolution.RealPath, file ) );
		}
		// Get all subdirectories and their sizes
		var directories = resolution.FileSystem.FindDirectory( resolution.RealPath ); // Original FindDirectory for raw list
		foreach ( var dir in directories )
		{
			// Ensure we don't recurse into "." or ".." if underlying system returns them
			var dirName = GetFileName( dir );
			if ( dirName == "." || dirName == ".." ) continue;
			totalSize += RecursiveDirectorySize( Path.Combine( resolution.RealPath, dir ) );
		}
		return totalSize;
	}

	/// <summary>
	/// Recursively counts files and directories in the given path
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public (int, int) RecursiveCount( string path )
	{
		var resolution = ResolveMountPoint( path );
		int fileCount = 0;
		int dirCount = 0;

		var files = resolution.FileSystem.FindFile( resolution.RealPath );
		foreach ( var file in files )
		{
			fileCount++;
		}

		var directories = resolution.FileSystem.FindDirectory( resolution.RealPath );
		foreach ( var dir in directories )
		{
			// Ensure we don't recurse into "." or ".."
			var dirName = GetFileName( dir );
			if ( dirName == "." || dirName == ".." ) continue;
			dirCount++;
			var (subFileCount, subDirCount) = RecursiveCount( Path.Combine( path, dir ) );
			fileCount += subFileCount;
			dirCount += subDirCount;
		}
		return (fileCount, dirCount);
	}

	public long GetFreeSpace( string path )
	{
		// stub, lets take away used space (RecursiveDirectorySize) from 24gb
		var resolution = ResolveMountPoint( path );
		long totalSize = 24 * 1024 * 1024 * 1024L; // 24 GB
		long usedSpace = RecursiveDirectorySize( resolution.RealPath ); // Use RealPath for underlying calculation
		return totalSize - usedSpace;
	}

	public bool CopyFile( string source, string destination )
	{
		var resolution = ResolveMountPoint( source );
		var destResolution = ResolveMountPoint( destination );
		var srcContents = resolution.FileSystem.ReadAllBytes( resolution.RealPath ).ToArray();
		if ( srcContents != null )
		{
			var stream = destResolution.FileSystem.OpenWrite( destResolution.RealPath );
			stream.Write( srcContents, 0, srcContents.Length );
			stream.Close();
			return true;
		}
		return false;
	}

	public bool MoveFile( string source, string destination )
	{
		var resolution = ResolveMountPoint( source );
		var destResolution = ResolveMountPoint( destination );
		if ( CopyFile( source, destination ) )
		{
			DeleteFile( source );
			return true;
		}
		return false;
	}

	public bool CopyDirectory( string source, string destination )
	{
		var resolution = ResolveMountPoint( source );
		var destResolution = ResolveMountPoint( destination );
		if ( !DirectoryExists( destination ) )
		{
			CreateDirectory( destination );
		}
		foreach ( var file in GetFiles( source ) )
		{
			CopyFile( file, Path.Combine( destination, GetFileName( file ) ) );
		}
		foreach ( var dir in GetDirectories( source ) )
		{
			if ( GetFileName( dir ) == "." || GetFileName( dir ) == ".." )
			{
				continue; // Skip current and parent directory references
			}
			CopyDirectory( dir, Path.Combine( destination, GetFileName( dir ) ) );
		}
		return true;
	}

	public bool MoveDirectory( string source, string destination )
	{
		var resolution = ResolveMountPoint( source );
		var destResolution = ResolveMountPoint( destination );
		if ( CopyDirectory( source, destination ) )
		{
			DeleteDirectory( source );
			return true;
		}
		return false;
	}

	// E.G. "C:/foo/bar" -> "C:/"
	public string GetPathRoot( string path )
	{
		var resolution = ResolveMountPoint( path );
		if ( resolution.MountPoint != null )
		{
			return resolution.MountPoint.Name + "/";
		}
		else
		{
			return Path.GetPathRoot( path )?.Replace( '\\', '/' );
		}
	}

	public string GetFullPath( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			return path;
		}

		path = path.Replace( '\\', '/' );

		string driveOrMountPrefix = "";
		string pathAfterPrefix = path;

		// Find the longest matching mount point prefix
		var bestMatchMount = _mountPoints.Keys
			.Where( key => path.StartsWith( key, StringComparison.OrdinalIgnoreCase ) )
			.OrderByDescending( key => key.Length )
			.FirstOrDefault();

		if ( bestMatchMount != null )
		{
			driveOrMountPrefix = path.Substring( 0, bestMatchMount.Length );
			pathAfterPrefix = path.Substring( bestMatchMount.Length );
		}
		else if ( path.StartsWith( "/" ) )
		{
			// Path starts with '/' but not a known mount prefix (e.g., "/foo/bar")
			// driveOrMountPrefix remains empty, pathAfterPrefix is the full path.
		}
		// Else: path is like "foo/bar" (purely relative without even a leading slash).
		// GetFullPath assumes the input is already 'rooted' in the virtual sense.
		// If CommandProgram.ResolvePath feeds it, this case should be handled as it prepends currentDir.

		// Ensure pathAfterPrefix starts with a '/' if there's a drive/mount prefix
		// and pathAfterPrefix is not empty and doesn't already start with '/'.
		// e.g., "C:foo" -> pathAfterPrefix becomes "/foo"
		if ( !string.IsNullOrEmpty( driveOrMountPrefix ) &&
			!string.IsNullOrEmpty( pathAfterPrefix ) &&
			pathAfterPrefix[0] != '/' )
		{
			pathAfterPrefix = "/" + pathAfterPrefix;
		}

		var segments = new List<string>();
		// Only split if pathAfterPrefix is not just "/" or empty.
		// If pathAfterPrefix is "/" or "", segments list will remain empty.
		if ( !string.IsNullOrEmpty( pathAfterPrefix ) && pathAfterPrefix != "/" )
		{
			segments.AddRange( pathAfterPrefix.Split( new[] { '/' }, StringSplitOptions.RemoveEmptyEntries ) );
		}


		var processedSegments = new List<string>();
		foreach ( var segment in segments )
		{
			if ( segment == ".." )
			{
				if ( processedSegments.Count > 0 )
				{
					processedSegments.RemoveAt( processedSegments.Count - 1 );
				}
				// If processedSegments is empty, ".." at the root level is ignored.
			}
			else if ( segment == "." )
			{
				// Ignore current directory segment.
			}
			else
			{
				processedSegments.Add( segment );
			}
		}

		StringBuilder fullPath = new StringBuilder( driveOrMountPrefix );
		if ( processedSegments.Count == 0 )
		{
			// Path resolves to the root of the drive/mount or the general root "/"
			// Ensure it ends with a single slash.
			if ( string.IsNullOrEmpty( driveOrMountPrefix ) && pathAfterPrefix.StartsWith( "/" ) )
			{
				// Input was like "/" or "/foo/.."
				fullPath.Append( '/' );
			}
			else if ( !string.IsNullOrEmpty( driveOrMountPrefix ) )
			{
				// Input was like "C:" or "C:/foo/.."
				fullPath.Append( '/' );
			}
			// If driveOrMountPrefix is empty and pathAfterPrefix was also empty or not starting with '/',
			// it implies a relative path that resolved to empty, which is unusual for GetFullPath.
			// However, given inputs are expected to be rooted, this state should mean root.
			// If input was "foo/.." (purely relative, resolving to root of relative context),
			// and driveOrMountPrefix is empty, fullPath is currently empty.
			// This case should ideally not be hit if inputs are always virtually absolute.
			// If it does, it means current dir, which GetFullPath usually doesn't return as just "/".
			// For safety, if fullPath is still empty, and original path was not empty, make it "." or "/"?
			// Let's stick to the logic that it forms an absolute path. If it's empty, it means root.
			if ( fullPath.Length == 0 && !string.IsNullOrEmpty( path ) ) fullPath.Append( '/' );


		}
		else
		{
			foreach ( var segment in processedSegments )
			{
				fullPath.Append( '/' );
				fullPath.Append( segment );
			}
		}

		// Handle case where input was just a drive letter like "C:" which should be "C:/"
		// Or if input was "/" which should be "/"
		if ( fullPath.Length == 0 && !string.IsNullOrEmpty( driveOrMountPrefix ) )
		{
			return driveOrMountPrefix + "/";
		}
		if ( fullPath.Length == 0 && path == "/" )
		{
			return "/";
		}


		return fullPath.ToString();
	}

	private const string SystemFolderName = "[SYSTEM]";
	private const string MetadataFilePrefix = "$";
	private const string AttributesFileName = MetadataFilePrefix + "Attributes"; // $Attributes
	private const string LabelFileName = MetadataFilePrefix + "Label";       // $Label

	private string SanitizeRelativePathForAttributeFilename( string relativePath )
	{
		if ( string.IsNullOrEmpty( relativePath ) )
		{
			// This case should ideally be handled before calling,
			// as attributes for the mount root itself are stored differently.
			return null;
		}
		// Replace directory separators. Consider more robust sanitization for production.
		// For example, characters like ':', '*', '?', '"', '<', '>', '|' are also invalid in Windows filenames.
		string sanitized = relativePath.Replace( '/', '_' ).Replace( '\\', '_' );

		// Ensure it doesn't accidentally look like the mount-level attribute files
		if ( sanitized.Equals( AttributesFileName.Substring( MetadataFilePrefix.Length ), StringComparison.OrdinalIgnoreCase ) ||
			sanitized.Equals( LabelFileName.Substring( MetadataFilePrefix.Length ), StringComparison.OrdinalIgnoreCase ) )
		{
			// Prepend an extra underscore to avoid collision if a file is literally named "Attributes" or "Label" at root
			sanitized = "_" + sanitized;
		}

		return MetadataFilePrefix + sanitized; // e.g., "$Users_Alice_file.txt"
	}

	private string GetItemAttributeFileRealPath( PathResolution pathRes, string normalizedFullVirtualPath, out BaseFileSystem targetFs, out string mountRootRealPath )
	{
		targetFs = null;
		mountRootRealPath = null;

		if ( pathRes.MountPoint == null )
		{
			// Item is not under a registered mount point. This MFT-like mechanism won't apply.
			return null;
		}

		targetFs = pathRes.MountPoint.FileSystem;
		mountRootRealPath = pathRes.MountPoint.RealPath; // The real path of the mount point's root directory.

		string itemRelativePath = normalizedFullVirtualPath.Substring( pathRes.MountPoint.Name.Length ).TrimStart( '/' );

		if ( string.IsNullOrEmpty( itemRelativePath ) )
		{
			// This is the mount root itself. Its attributes are in MountPoint.Attributes,
			// not a separate item attribute file.
			return null;
		}

		string attributeFileName = SanitizeRelativePathForAttributeFilename( itemRelativePath );
		if ( attributeFileName == null )
		{
			return null; // Should not happen if itemRelativePath was not empty.
		}

		string systemFolderRealPath = Path.Combine( mountRootRealPath, SystemFolderName );
		return Path.Combine( systemFolderRealPath, attributeFileName );
	}

	public VirtualFileAttributes GetAttributes( string path )
	{
		if ( string.IsNullOrWhiteSpace( path ) )
		{
			return VirtualFileAttributes.Normal; // Or VirtualFileAttributes.None if preferred for non-existent
		}

		var resolution = ResolveMountPoint( path );
		string fullVirtualPath = GetFullPath( path ); // Normalize path for consistent processing

		// 1. Check if it's the mount point root itself
		if ( resolution.MountPoint != null )
		{
			string mountRelativePathCheck = fullVirtualPath.Substring( resolution.MountPoint.Name.Length ).TrimStart( '/' );
			if ( string.IsNullOrEmpty( mountRelativePathCheck ) ) // Path is "C:" or "C:/"
			{
				return resolution.MountPoint.Attributes;
			}
		}

		// 2. Try to get attributes from the [SYSTEM] folder MFT-like store for mounted items
		string itemAttributeFileRealPath = GetItemAttributeFileRealPath( resolution, fullVirtualPath, out BaseFileSystem fs, out _ );

		if ( itemAttributeFileRealPath != null && fs != null )
		{
			if ( fs.FileExists( itemAttributeFileRealPath ) )
			{
				try
				{
					string content = fs.ReadAllText( itemAttributeFileRealPath );
					if ( int.TryParse( content, out int attrValue ) )
					{
						return (VirtualFileAttributes)attrValue;
					}
					// else: Log error or handle malformed attributes file
				}
				catch ( Exception ex )
				{
					// Log error, e.g., Log.Warning($"Failed to read attribute file {itemAttributeFileRealPath}: {ex.Message}");
				}
			}
			// Attribute file not found in [SYSTEM] folder, provide defaults based on existence
			if ( fs.FileExists( resolution.RealPath ) ) // Check actual item existence
			{
				return VirtualFileAttributes.Archive; // Default for a file
			}
			if ( fs.DirectoryExists( resolution.RealPath ) ) // Check actual item existence
			{
				return VirtualFileAttributes.Directory; // Default for a directory
			}
			return VirtualFileAttributes.Normal; // Item doesn't exist, and no attribute file
		}
		else
		{
			// Path is not under a mount point (resolution.MountPoint == null) or it's the mount root (handled above).
			// Provide basic defaults for items on the _defaultFileSystem not accessed via a mount.
			if ( resolution.FileSystem.FileExists( resolution.RealPath ) )
			{
				return VirtualFileAttributes.Archive;
			}
			if ( resolution.FileSystem.DirectoryExists( resolution.RealPath ) )
			{
				return VirtualFileAttributes.Directory;
			}
		}
		return VirtualFileAttributes.Normal; // Path does not exist
	}

	public void SetAttributes( string path, VirtualFileAttributes attributes )
	{
		if ( string.IsNullOrWhiteSpace( path ) ) return;

		var resolution = ResolveMountPoint( path );
		string fullVirtualPath = GetFullPath( path );

		// 1. Handle setting attributes for the mount point root itself
		if ( resolution.MountPoint != null )
		{
			string mountRelativePathCheck = fullVirtualPath.Substring( resolution.MountPoint.Name.Length ).TrimStart( '/' );
			if ( string.IsNullOrEmpty( mountRelativePathCheck ) )
			{
				resolution.MountPoint.Attributes = attributes;
				string systemFolderPath = Path.Combine( resolution.MountPoint.RealPath, SystemFolderName );
				string mountAttributesFilePath = Path.Combine( systemFolderPath, AttributesFileName );
				try
				{
					if ( !resolution.MountPoint.FileSystem.DirectoryExists( systemFolderPath ) )
					{
						resolution.MountPoint.FileSystem.CreateDirectory( systemFolderPath );
						// Consider setting [SYSTEM] folder attributes (e.g., Hidden, System) on the underlying FS if possible/desired.
					}
					resolution.MountPoint.FileSystem.WriteAllText( mountAttributesFilePath, ((int)attributes).ToString() );
					NotifyChange( fullVirtualPath );
				}
				catch ( Exception ex )
				{
					// Log error, e.g., Log.Error($"Failed to save attributes for mount {resolution.MountPoint.Name}: {ex.Message}");
				}
				return;
			}
		}

		// 2. Try to set attributes in the [SYSTEM] folder MFT-like store for mounted items
		string itemAttributeFileRealPath = GetItemAttributeFileRealPath( resolution, fullVirtualPath, out BaseFileSystem fs, out string mountRootRealPath );

		if ( itemAttributeFileRealPath != null && fs != null && mountRootRealPath != null )
		{
			string systemFolderPathOnFs = Path.Combine( mountRootRealPath, SystemFolderName );
			try
			{
				if ( !fs.DirectoryExists( systemFolderPathOnFs ) )
				{
					fs.CreateDirectory( systemFolderPathOnFs );
					// Consider setting [SYSTEM] folder attributes here too.
				}

				// Ensure the correct Directory/Archive flag based on item type before saving
				if ( fs.DirectoryExists( resolution.RealPath ) )
				{
					attributes |= VirtualFileAttributes.Directory;
					attributes &= ~VirtualFileAttributes.Archive; // Directories shouldn't typically have Archive
				}
				else if ( fs.FileExists( resolution.RealPath ) )
				{
					attributes |= VirtualFileAttributes.Archive;
					attributes &= ~VirtualFileAttributes.Directory;
				}
				// If the item doesn't exist, we're setting attributes for a potential future item.
				// The caller should ensure Directory/Archive flags are appropriate.

				fs.WriteAllText( itemAttributeFileRealPath, ((int)attributes).ToString() );
				NotifyChange( GetDirectoryName( fullVirtualPath ) ); // Notify change in parent dir
				NotifyChange( fullVirtualPath ); // Notify change for the item itself
			}
			catch ( Exception ex )
			{
				// Log error, e.g., Log.Error($"Failed to save attribute file {itemAttributeFileRealPath}: {ex.Message}");
			}
		}
		else
		{
			// Path is not under a mount point or is the mount root (already handled).
			// Currently, no mechanism to set custom attributes for non-mounted _defaultFileSystem items.
			// If underlying fs.SetAttributes(resolution.RealPath, attributes) existed, it could be called here.
			// For now, this is a no-op for such paths.
			// Log.Warning($"SetAttributes: Path '{fullVirtualPath}' is not on a mount point or is a mount root; custom item attributes not stored via [SYSTEM] MFT.");
		}
	}

	public bool HasAttribute( string path, VirtualFileAttributes attribute )
	{
		VirtualFileAttributes attributes = GetAttributes( path );
		return (attributes & attribute) == attribute;
	}


	public void NotifyChange( string path )
	{
		OnFileSystemChanged?.Invoke( path );
	}
}

/// <summary>
/// Represents a mount point in the virtual file system
/// </summary>
public class MountPoint
{
	/// <summary>
	/// The name of the mount point (e.g., "C:", "FS:A:")
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The label of the mount point (e.g., "System", "Data Drive")
	/// Defaults to Name if not specified by a $Label file.
	/// </summary>
	public string Label { get; set; }

	/// <summary>
	/// The real path this mount point maps to
	/// </summary>
	public string RealPath { get; set; }

	/// <summary>
	/// The file system to use for this mount point
	/// </summary>
	public BaseFileSystem FileSystem { get; set; }

	/// <summary>
	/// Attributes of the mount point itself (e.g., ReadOnly, System).
	/// </summary>
	public VirtualFileAttributes Attributes { get; set; } = VirtualFileAttributes.Directory;
}

/// <summary>
/// Contains the result of resolving a virtual path to a real path
/// </summary>
public class PathResolution
{
	/// <summary>
	/// The mount point for the path, if any
	/// </summary>
	public MountPoint MountPoint { get; set; }

	/// <summary>
	/// The real path after resolution
	/// </summary>
	public string RealPath { get; set; }

	/// <summary>
	/// The file system to use for operations
	/// </summary>
	public BaseFileSystem FileSystem { get; set; }
}
