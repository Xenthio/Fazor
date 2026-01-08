using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;

namespace FakeOperatingSystem.OSFileSystem
{
	/// <summary>
	/// Core virtual file system interface that handles actual file operations
	/// </summary>
	public interface IVirtualFileSystem
	{
		// Mount point management
		void RegisterMountPoint( string mountName, string path, BaseFileSystem fileSystem = null );
		bool IsMountPointValid( string mountPoint );

		// Basic file operations
		bool FileExists( string path );
		bool DirectoryExists( string path );
		Stream OpenRead( string path );
		Stream OpenWrite( string path );
		byte[] ReadAllBytes( string path );
		string ReadAllText( string path );
		void WriteAllBytes( string path, byte[] contents );
		void WriteAllText( string path, string contents );
		VirtualFileAttributes GetAttributes( string path );
		void SetAttributes( string path, VirtualFileAttributes attributes );
		bool HasAttribute( string path, VirtualFileAttributes attribute );

		// Directory operations
		IEnumerable<string> GetFiles( string path, string searchPattern = "*" );
		IEnumerable<string> GetDirectories( string path );
		void CreateDirectory( string path );
		void DeleteFile( string path );
		void DeleteDirectory( string path, bool recursive = false );

		// Path operations
		string ResolvePath( string path );
		string GetFileName( string path );
		string GetDirectoryName( string path );
		string GetFileNameWithoutExtension( string path );
		string GetExtension( string path );

		// Misc
		long FileSize( string path );
		IEnumerable<string> FindFile( string sourceDir );
		IEnumerable<string> FindDirectory( string sourceDir );
		public bool MoveFile( string source, string destination );
		public bool MoveDirectory( string source, string destination );


		// Event for when the file system changes
		event System.Action<string> OnFileSystemChanged;
		void NotifyChange( string path );
	}
}


/// <summary>
/// Provides attributes for files and directories.
/// </summary>
[Flags]
public enum VirtualFileAttributes
{
	None = 0,
	/// <summary>
	/// The file is read-only. ReadOnly is supported on Windows, Linux, and macOS.
	/// On Linux and macOS, changing the ReadOnly flag is a permissions operation.
	/// </summary>
	ReadOnly = 1,
	/// <summary>
	/// The file is hidden, and thus is not included in an ordinary directory listing.
	/// Hidden is supported on Windows, Linux, and macOS.
	/// </summary>
	Hidden = 2,
	/// <summary>
	/// The file is a system file. That is, the file is part of the operating system
	/// or is used exclusively by the operating system.
	/// </summary>
	System = 4,
	/// <summary>
	/// The file is a directory. Directory is supported on Windows, Linux, and macOS.
	/// </summary>
	Directory = 16,
	/// <summary>
	/// This file is marked to be included in incremental backup operation.
	/// this attribute is set whenever the file is modified, and backup software should clear
	/// it when processing the file during incremental backup.
	/// </summary>
	Archive = 32,
	/// <summary>
	/// Reserved for future use.
	/// </summary>
	Device = 64,
	/// <summary>
	/// The file is a standard file that has no special attributes. This attribute is
	/// valid only if it is used alone. Normal is supported on Windows, Linux, and macOS.
	/// </summary>
	Normal = 128,
	/// <summary>
	/// The file is temporary. A temporary file contains data that is needed while an
	/// application is executing but is not needed after the application is finished.
	/// File systems try to keep all the data in memory for quicker access rather than
	/// flushing the data back to mass storage. A temporary file should be deleted by
	/// the application as soon as it is no longer needed.
	/// </summary>
	Temporary = 256,
	/// <summary>
	/// The file is a sparse file. Sparse files are typically large files whose data
	/// consists of mostly zeros.
	/// </summary>
	SparseFile = 512,
	/// <summary>
	/// The file contains a reparse point, which is a block of user-defined data associated
	/// with a file or a directory. ReparsePoint is supported on Windows, Linux, and
	/// macOS.
	/// </summary>
	ReparsePoint = 1024,
	/// <summary>
	/// The file is compressed. 
	/// </summary>
	Compressed = 2048,
	/// <summary>
	/// The file is offline. The data of the file is not immediately available.
	/// </summary>
	Offline = 4096,
	/// <summary>
	/// The file or directory is not to be indexed by the operating system's content indexing service.
	/// </summary>
	NotContentIndexed = 8192,
	/// <summary>
	/// The file or directory is encrypted. For a file, this means that all data in the
	/// file is encrypted. For a directory, this means that encryption is the default
	/// for newly created files and directories.
	/// </summary>
	Encrypted = 16384,
	/// <summary>
	/// The file or directory includes data integrity support. When this value is applied
	/// to a file, all data streams in the file have integrity support. When this value
	/// is applied to a directory, all new files and subdirectories within that directory,
	/// by default, include integrity support.
	/// </summary>
	IntegrityStream = 32768,
	/// <summary>
	/// The file or directory is excluded from the data integrity scan. When this value
	/// is applied to a directory, by default, all new files and subdirectories within
	/// that directory are excluded from data integrity.
	/// </summary>
	NoScrubData = 131072
}
