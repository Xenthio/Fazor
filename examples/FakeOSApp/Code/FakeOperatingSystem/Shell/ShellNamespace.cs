using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.User; // Added for UserProfileHelper
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FakeOperatingSystem.Shell;

/// <summary>
/// Manages the shell namespace hierarchy (special folders, virtual folders, etc.)
/// </summary>
public class ShellNamespace
{
	// Singleton instance
	public static ShellNamespace Instance { get; private set; }

	// Constants for special folder names
	public const string DESKTOP = "Desktop";
	public const string MY_COMPUTER = "My Computer";
	public const string MY_DOCUMENTS = "My Documents";
	public const string RECYCLE_BIN = "Recycle Bin";
	public const string NETWORK_NEIGHBORHOOD = "Network Neighborhood";
	public const string CONTROL_PANEL = "Control Panel";
	public const string PRINTERS = "Printers";
	public const string DIAL_UP_NETWORKING = "Dial-Up Networking";
	public const string SCHEDULED_TASKS = "Scheduled Tasks";

	// The core VFS for file operations
	private IVirtualFileSystem _vfs;

	// Registry of shell folders
	private Dictionary<string, ShellFolder> _shellFolders = new();

	public ShellNamespace( IVirtualFileSystem vfs )
	{
		Instance = this;
		_vfs = vfs;
		_desktopSettingsAppletInstance = new DesktopSettingsApplet();

		// Set up the shell namespace hierarchy
		InitializeNamespace();
	}
	private DesktopSettingsApplet _desktopSettingsAppletInstance;
	private void InitializeNamespace()
	{
		// Desktop (root of shell namespace)
		RegisterShellFolder( new ShellFolder
		{
			Name = DESKTOP,
			Path = DESKTOP,
			RealPath = UserProfileHelper.GetDesktopPath(),
			Type = ShellFolderType.SpecialFolder,
			IconName = "desktop",
			HandlePropertiesClick = () => // Assign the custom action here
			{
				_desktopSettingsAppletInstance?.Launch();
			}
		} );

		// My Computer
		RegisterShellFolder( new ShellFolder
		{
			Name = MY_COMPUTER,
			Path = $"{DESKTOP}/{MY_COMPUTER}",
			Type = ShellFolderType.SpecialFolder,
			IconName = "mycomputer",
			IsVirtual = true
		} );

		// My Documents
		RegisterShellFolder( new ShellFolder
		{
			Name = MY_DOCUMENTS,
			Path = $"{DESKTOP}/{MY_DOCUMENTS}",
			RealPath = UserProfileHelper.GetMyDocumentsPath(),
			Type = ShellFolderType.SpecialFolder,
			IconName = "mydocuments"
		} );

		// Internet Explorer
		RegisterShellFolder( new ShellFolder
		{
			Name = "Internet Explorer",
			Path = $"{DESKTOP}/Internet Explorer",
			RealPath = "C:/Program Files/Internet Explorer/iexplore.exe",
			Type = ShellFolderType.ShellExecute,
			IconName = "iexplore"
		} );

		// Recycle Bin
		RegisterShellFolder( new ShellFolder
		{
			Name = RECYCLE_BIN,
			Path = $"{DESKTOP}/{RECYCLE_BIN}",
			RealPath = "C:/Recycled",
			Type = ShellFolderType.SpecialFolder,
			IconName = "recyclebinempty"
		} );

		// Network Neighborhood
		RegisterShellFolder( new ShellFolder
		{
			Name = NETWORK_NEIGHBORHOOD,
			Path = $"{DESKTOP}/{NETWORK_NEIGHBORHOOD}",
			Type = ShellFolderType.SpecialFolder,
			IconName = "networkneighborhood",
			IsVirtual = true
		} );

		// Mount drives in My Computer
		RegisterShellFolder( new ShellFolder
		{
			Name = "(C:)",
			Path = $"{DESKTOP}/{MY_COMPUTER}/C:",
			RealPath = "C:",
			Type = ShellFolderType.Drive,
			IconName = "drive"
		} );

		RegisterShellFolder( new ShellFolder
		{
			Name = "Mounted (FS:A:)",
			Path = $"{DESKTOP}/{MY_COMPUTER}/FS:A:",
			RealPath = "FS:A:",
			Type = ShellFolderType.Drive,
			IconName = "drive"
		} );

		RegisterShellFolder( new ShellFolder
		{
			Name = "OrgData (FS:B:)",
			Path = $"{DESKTOP}/{MY_COMPUTER}/FS:B:",
			RealPath = "FS:B:",
			Type = ShellFolderType.Drive,
			IconName = "drive"
		} );

		RegisterShellFolder( new ShellFolder
		{
			Name = "Data (FS:C:)",
			Path = $"{DESKTOP}/{MY_COMPUTER}/FS:C:",
			RealPath = "FS:C:",
			Type = ShellFolderType.Drive,
			IconName = "drive"
		} );

		// Control Panel
		RegisterShellFolder( new ShellFolder
		{
			Name = CONTROL_PANEL,
			Path = $"{DESKTOP}/{MY_COMPUTER}/{CONTROL_PANEL}",
			Type = ShellFolderType.ControlPanel,
			IconName = "controlpanel",
			IsVirtual = true
		} );

		// Printers
		RegisterShellFolder( new ShellFolder
		{
			Name = "Printers",
			Path = $"{DESKTOP}/{MY_COMPUTER}/{PRINTERS}",
			Type = ShellFolderType.ControlPanel,
			IconName = "printer",
			IsVirtual = true
		} );

		// Dial-Up Networking
		RegisterShellFolder( new ShellFolder
		{
			Name = "Dial-Up Networking",
			Path = $"{DESKTOP}/{MY_COMPUTER}/{DIAL_UP_NETWORKING}",
			Type = ShellFolderType.ControlPanel,
			IconName = "dialup",
			IsVirtual = true
		} );

		// Scheduled Tasks
		RegisterShellFolder( new ShellFolder
		{
			Name = SCHEDULED_TASKS,
			Path = $"{DESKTOP}/{MY_COMPUTER}/{SCHEDULED_TASKS}",
			Type = ShellFolderType.ControlPanel,
			IconName = "tasks",
			IsVirtual = true
		} );

		// Add Control Panel applets
		AddControlPanelApplets();
	}

	private void AddControlPanelApplets()
	{
		var applets = new[]
		{
			("Network", "network"),
			("System", "system"),
			("Mouse", "mouse"),
			("Keyboard", "keyboard"),
			("Sounds", "sounds"),
			("Add/Remove Programs", "addremove"),
			("Date/Time", "datetime"),
			("Regional Settings", "regional")
		};

		string controlPanelPath = $"{DESKTOP}/{MY_COMPUTER}/{CONTROL_PANEL}";

		RegisterShellFolder( new ShellFolder
		{
			Name = _desktopSettingsAppletInstance.Name,
			Path = $"{controlPanelPath}/{_desktopSettingsAppletInstance.Name}",
			Type = ShellFolderType.ControlPanelApplet,
			IconName = _desktopSettingsAppletInstance.IconName,
			IsVirtual = true,
			Applet = _desktopSettingsAppletInstance
		} );

		foreach ( var (name, icon) in applets )
		{
			RegisterShellFolder( new ShellFolder
			{
				Name = name,
				Path = $"{controlPanelPath}/{name}",
				Type = ShellFolderType.ControlPanelApplet,
				IconName = icon,
				IsVirtual = true
			} );
		}
	}

	public void RegisterShellFolder( ShellFolder folder )
	{
		_shellFolders[folder.Path] = folder;
	}

	/// <summary>
	/// Gets a shell folder by its path
	/// </summary>
	public ShellFolder GetFolder( string path )
	{
		if ( string.IsNullOrEmpty( path ) )
			path = DESKTOP; // Default to Desktop if path is empty

		path = NormalizePath( path );

		// 1. Direct match in _shellFolders (exact registered path)
		if ( _shellFolders.TryGetValue( path, out var folder ) )
		{
			return folder;
		}

		// 2. Check if it's a sub-path of a registered folder with a RealPath
		//    Iterate from longest possible parent path to shortest.
		string tempPath = path;
		int lastSlashIndex;
		while ( (lastSlashIndex = tempPath.LastIndexOf( '/' )) != -1 ) // Check for -1 to handle paths without '/'
		{
			string parentCandidatePath = tempPath.Substring( 0, lastSlashIndex );
			if ( string.IsNullOrEmpty( parentCandidatePath ) ) // Avoid issues if path starts with / or is just "Item"
			{
				// This case might occur if path was "Desktop" and it wasn't found directly,
				// or if path was "SomeItem" (no slashes).
				// If parentCandidatePath becomes empty, it means we've gone above "Desktop" or a root-level item.
				break;
			}

			if ( _shellFolders.TryGetValue( parentCandidatePath, out ShellFolder parentShellFolder ) )
			{
				if ( !string.IsNullOrEmpty( parentShellFolder.RealPath ) )
				{
					// We found a registered parent with a RealPath.
					// The remainder of 'path' is the relative path from this parent.
					string relativePath = path.Substring( parentCandidatePath.Length ).TrimStart( '/' );
					string potentialRealPath = Path.Combine( parentShellFolder.RealPath, relativePath );

					if ( _vfs.DirectoryExists( potentialRealPath ) )
					{
						return new ShellFolder
						{
							Name = System.IO.Path.GetFileName( potentialRealPath ), // Use System.IO.Path for consistency
							Path = path, // The original requested shell path
							RealPath = potentialRealPath,
							Type = ShellFolderType.Directory, // Assume directory if found this way
							IconName = "folder", // Default folder icon
							IsVirtual = false
						};
					}
				}
				// If parentShellFolder has no RealPath, or if the potentialRealPath doesn't exist as a directory,
				// we stop searching up this particular branch.
				break;
			}
			tempPath = parentCandidatePath; // Continue searching with the next parent up
		}

		// If after all checks, no specific shell folder is found, return null.
		// The original fallback that treated 'path' as a direct VFS path was problematic
		// because 'path' is a shell namespace path.
		return null;
	}

	/// <summary>
	/// Gets all child items (files and folders) in a shell folder
	/// </summary>
	public IEnumerable<ShellItem> GetItems( string folderPath )
	{
		var items = new List<ShellItem>();
		var folder = GetFolder( folderPath );

		if ( folder == null )
			return items;

		// Add registered subfolders
		string prefix = $"{folder.Path}/";
		foreach ( var sf in _shellFolders.Values.Where( f =>
			f.Path.StartsWith( prefix ) &&
			f.Path.Count( c => c == '/' ) == folder.Path.Count( c => c == '/' ) + 1 ) )
		{
			items.Add( new ShellItem
			{
				Name = sf.Name,
				Path = sf.Path,
				RealPath = sf.RealPath,
				IsFolder = true,
				IconName = sf.IconName,
				Type = sf.Type
			} );
		}

		// For non-virtual folders, add real files and folders
		if ( !folder.IsVirtual && folder.RealPath != null )
		{
			// Add directories
			foreach ( var dir in _vfs.GetDirectories( folder.RealPath ) )
			{
				// DO NOT ADD "." AND ".." DIRECTORIES
				if ( Path.GetFileName( dir ) == "." || Path.GetFileName( dir ) == ".." )
					continue;

				string dirName = Path.GetFileName( dir );

				// Skip if this directory is already added as a special folder
				if ( items.Any( i => i.IsFolder && i.Name.Equals( dirName, StringComparison.OrdinalIgnoreCase ) ) )
					continue;

				items.Add( new ShellItem
				{
					Name = dirName,
					Path = $"{folder.Path}/{dirName}",
					RealPath = dir,
					IsFolder = true,
					IconName = "folder",
					Type = ShellFolderType.Directory
				} );
			}

			// Add files
			foreach ( var file in _vfs.GetFiles( folder.RealPath ) )
			{
				string fileName = Path.GetFileName( file );
				string extension = Path.GetExtension( file ).ToLowerInvariant();

				string iconName = "file";
				ShellFolderType type = ShellFolderType.File;

				// Handle shortcuts specially
				if ( extension == ".lnk" )
				{
					iconName = "shortcut";
					type = ShellFolderType.Shortcut;
				}

				items.Add( new ShellItem
				{
					Name = fileName,
					Path = $"{folder.Path}/{fileName}",
					RealPath = file,
					IsFolder = false,
					IconName = iconName,
					Type = type
				} );
			}
		}

		return items;
	}

	/// <summary>
	/// Normalizes a shell path
	/// </summary>
	private string NormalizePath( string path )
	{
		path = path.Replace( '\\', '/' );

		if ( path.StartsWith( "/" ) )
			path = path.Substring( 1 );

		if ( path.EndsWith( "/" ) && path.Length > 1 )
			path = path.Substring( 0, path.Length - 1 );

		return path;
	}
}

/// <summary>
/// The type of a shell folder
/// </summary>
public enum ShellFolderType
{
	Directory,
	File,
	SpecialFolder,
	Drive,
	Shortcut,
	ControlPanel,
	ControlPanelApplet,
	ShellExecute, // For folders that can be launched like files
}

/// <summary>
/// Represents a special folder in the shell namespace
/// </summary>
public class ShellFolder
{
	/// <summary>
	/// Display name of the folder
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Shell namespace path
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Real path in the VFS, if any
	/// </summary>
	public string RealPath { get; set; }

	/// <summary>
	/// Type of folder
	/// </summary>
	public ShellFolderType Type { get; set; }

	/// <summary>
	/// Whether this is a virtual folder with no real files
	/// </summary>
	public bool IsVirtual { get; set; }

	/// <summary>
	/// Icon name for display
	/// </summary>
	public string IconName { get; set; }

	/// <summary>
	/// Launches the folder if it is a control panel applet
	/// </summary>
	public IControlPanelApplet Applet { get; set; } // null unless ControlPanelApplet

	/// <summary>
	/// Custom action to execute when "Properties" is clicked for this folder.
	/// </summary>
	public Action HandlePropertiesClick { get; set; }
}

/// <summary>
/// Represents an item in the shell namespace
/// </summary>
public class ShellItem
{
	/// <summary>
	/// Display name of the item
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Shell namespace path
	/// </summary>
	public string Path { get; set; }

	/// <summary>
	/// Real path in the VFS, if any
	/// </summary>
	public string RealPath { get; set; }

	/// <summary>
	/// Whether this is a folder or file
	/// </summary>
	public bool IsFolder { get; set; }

	/// <summary>
	/// Type of folder/file
	/// </summary>
	public ShellFolderType Type { get; set; }

	/// <summary>
	/// Icon name for display
	/// </summary>
	public string IconName { get; set; }
}
public interface IControlPanelApplet
{
	string Name { get; }
	string IconName { get; }
	string Description { get; }
	void Launch();
}
