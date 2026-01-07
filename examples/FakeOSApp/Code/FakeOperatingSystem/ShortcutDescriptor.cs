using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.Shell;
using System;
using System.IO;

namespace FakeDesktop;

/// <summary>
/// Represents a Windows 98-style shortcut (.lnk) file
/// </summary>
public class ShortcutDescriptor
{
	/// <summary>
	/// Target path the shortcut points to
	/// </summary>
	public string TargetPath { get; set; }

	/// <summary>
	/// Working directory for the shortcut target
	/// </summary>
	public string WorkingDirectory { get; set; }

	/// <summary>
	/// Command-line arguments for the target
	/// </summary>
	public string Arguments { get; set; }

	/// <summary>
	/// Icon name to display for this shortcut
	/// </summary>
	public string IconName { get; set; }

	/// <summary>
	/// Optional description/comment for the shortcut
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Window state (normal, minimized, maximized)
	/// </summary>
	public WindowShowState ShowState { get; set; } = WindowShowState.Normal;

	/// <summary>
	/// Parameterless constructor for json
	/// </summary>
	public ShortcutDescriptor()
	{
		// Default values if needed
		TargetPath = "";
		WorkingDirectory = "";
		Arguments = "";
		IconName = "shortcut";
		ShowState = WindowShowState.Normal;
	}

	/// <summary>
	/// Create a basic shortcut descriptor
	/// </summary>
	public ShortcutDescriptor( string targetPath, string iconName = null )
	{
		TargetPath = targetPath;
		IconName = iconName;
		WorkingDirectory = Path.GetDirectoryName( targetPath );
	}

	/// <summary>
	/// Create a shortcut with all properties
	/// </summary>
	public ShortcutDescriptor( string targetPath, string workingDirectory, string arguments, string iconName, WindowShowState showState = WindowShowState.Normal )
	{
		TargetPath = targetPath;
		WorkingDirectory = workingDirectory;
		Arguments = arguments;
		IconName = iconName;
		ShowState = showState;
	}

	/// <summary>
	/// Serializes the shortcut to string for storage in an lnk file
	/// </summary>
	public string ToFileContent()
	{
		return Sandbox.Json.Serialize( this );
	}

	/// <summary>
	/// Reads a shortcut from an lnk file's content
	/// </summary>
	public static ShortcutDescriptor FromFileContent( string content )
	{
		try
		{
			return Sandbox.Json.Deserialize<ShortcutDescriptor>( content );
		}
		catch ( Exception ex )
		{
			Log.Error( $"Failed to parse shortcut: {ex.Message}" );
			return null;
		}
	}

	public bool Resolve()
	{
		try
		{
			Log.Info( $"Resolving shortcut: {TargetPath}" );
			// Check if target is a file
			if ( VirtualFileSystem.Instance.FileExists( TargetPath ) )
			{
				Shell.ShellExecute( TargetPath );
				return true;
			}
			else if ( VirtualFileSystem.Instance.DirectoryExists( TargetPath ) )
			{

				// If the target is a folder, open it in the file browser
				return true;
			}
			else
			{
				MessageBoxUtility.ShowCustom( $"Target path does not exist: {TargetPath}", "Error", MessageBoxIcon.Error, MessageBoxButtons.OK );
				return false;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error resolving shortcut: {ex.Message}" );
			return false;
		}
	}

	/// <summary>
	/// Window show state for shortcuts (matches Windows 98)
	/// </summary>
	public enum WindowShowState
	{
		Normal = 1,
		Minimized = 2,
		Maximized = 3
	}
}
