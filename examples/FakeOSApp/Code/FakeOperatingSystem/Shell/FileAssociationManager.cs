using FakeOperatingSystem.OSFileSystem;
using System.Collections.Generic;

namespace FakeOperatingSystem.Shell;

/// <summary>
/// Manages file type associations by reading from and writing to the registry.
/// </summary>
public class FileAssociationManager
{
	// Singleton instance
	public static FileAssociationManager Instance { get; private set; }

	// Reference to the VFS for file operations (though less used directly for associations now)
	private IVirtualFileSystem _vfs;
	private const string HKCR = "HKEY_CLASSES_ROOT";

	public FileAssociationManager( IVirtualFileSystem vfs )
	{
		Instance = this;
		_vfs = vfs; // Still useful for GetExtension

		// Ensure default associations are written to the registry if they don't exist
		// This would typically be part of an OS setup or application installation
		RegisterDefaultAssociations();
	}

	public static void Initialize( IVirtualFileSystem vfs )
	{
		new FileAssociationManager( vfs );
	}

	private void RegisterDefaultAssociations()
	{
		// Text files
		var txtAssociation = new FileAssociation( ".txt", "Text Document", "txt", "C:/Windows/notepad.exe", shouldShowInShellCreateNew: true );
		RegisterAssociation( txtAssociation );

		// HTML files
		var htmlAssociation = new FileAssociation( ".html", "HTML Document", "html", "C:/Program Files/Internet Explorer/iexplore.exe" );
		htmlAssociation.AddAction( "edit", "Edit", "notepad.exe" );
		RegisterAssociation( htmlAssociation );

		// WAD files (for Doom)
		var wadAssociation = new FileAssociation( ".wad", "Doom WAD File", "wad", "C:/Program Files/Ultimate Doom for Windows 95/doom95.exe" );
		RegisterAssociation( wadAssociation );

		// Shortcuts
		var lnkAssociation = new FileAssociation( ".lnk", "Shortcut", "lnk", null );
		// Shortcuts are handled specially in the file browser, but basic association can exist
		RegisterAssociation( lnkAssociation );

		// Ini files
		var iniAssociation = new FileAssociation( ".ini", "INI File", "ini", "C:/Windows/notepad.exe" );
		iniAssociation.AddAction( "edit", "Edit", "C:/Windows/notepad.exe" );
		RegisterAssociation( iniAssociation );

		// BAT files
		var batAssociation = new FileAssociation( ".bat", "Batch File", "bat", "C:/Windows/system32/cmd.exe" );
		batAssociation.AddAction( "open", "Open", "C:/Windows/system32/cmd.exe", "/C %1" ); // Note: DefaultProgram in constructor handles the primary open
		batAssociation.AddAction( "edit", "Edit", "C:/Windows/notepad.exe" );
		RegisterAssociation( batAssociation );

		// BMP files
		var bmpAssociation = new FileAssociation( ".bmp", "Bitmap Image", "bmp", "C:/Windows/mspaint.exe" );
		bmpAssociation.AddAction( "open", "Open", "C:/Windows/mspaint.exe" ); // Default open action
		bmpAssociation.AddAction( "edit", "Edit", "C:/Windows/mspaint.exe" ); // Edit action
		RegisterAssociation( bmpAssociation );

		// PNG files
		var pngAssociation = new FileAssociation( ".png", "PNG Image", "png", "C:/Windows/mspaint.exe" );
		pngAssociation.AddAction( "open", "Open", "C:/Windows/mspaint.exe" ); // Default open action
		pngAssociation.AddAction( "edit", "Edit", "C:/Windows/mspaint.exe" ); // Edit action
		RegisterAssociation( pngAssociation );

		// JPG files
		var jpgAssociation = new FileAssociation( ".jpg", "JPEG Image", "jpg", "C:/Windows/mspaint.exe" );
		jpgAssociation.AddAction( "open", "Open", "C:/Windows/mspaint.exe" ); // Default open action
		jpgAssociation.AddAction( "edit", "Edit", "C:/Windows/mspaint.exe" ); // Edit action
		RegisterAssociation( jpgAssociation );
	}

	public void RegisterAssociation( FileAssociation association )
	{
		if ( association == null || string.IsNullOrWhiteSpace( association.Extension ) )
		{
			Log.Warning( "FileAssociationManager: Attempted to register a null or invalid association." );
			return;
		}

		string ext = association.Extension.StartsWith( "." ) ? association.Extension : "." + association.Extension;
		string extKeyPath = $"{HKCR}\\{ext}";

		// Create the extension key if it doesn't exist
		Registry.Instance.SetValue( extKeyPath, "", null ); // Ensure key exists

		Registry.Instance.SetValue( extKeyPath, "FriendlyName", association.FriendlyName );
		Registry.Instance.SetValue( extKeyPath, "IconName", association.IconName );
		if ( !string.IsNullOrEmpty( association.DefaultProgram ) )
		{
			Registry.Instance.SetValue( extKeyPath, "DefaultProgram", association.DefaultProgram );
		}
		Registry.Instance.SetValue( extKeyPath, "ShouldShowInShellCreateNew", association.ShouldShowInShellCreateNew.ToString() );

		// Store actions under Shell subkey
		string shellKeyPath = $"{extKeyPath}\\Shell";
		foreach ( var actionEntry in association.Actions )
		{
			string verb = actionEntry.Key;
			FileAction action = actionEntry.Value;
			string verbKeyPath = $"{shellKeyPath}\\{verb}";

			if ( !string.IsNullOrEmpty( action.DisplayName ) && action.DisplayName != verb ) // Only store if different from verb for brevity
			{
				Registry.Instance.SetValue( verbKeyPath, "DisplayName", action.DisplayName );
			}
			else // Ensure DisplayName is removed if it's same as verb or empty, to keep registry clean
			{
				Registry.Instance.DeleteValue( verbKeyPath, "DisplayName" );
			}


			string commandKeyPath = $"{verbKeyPath}\\command";
			string commandValue = $"{action.Program} {action.Arguments}";
			Registry.Instance.SetValue( commandKeyPath, "", commandValue );
		}
		Log.Info( $"FileAssociationManager: Registered association for '{ext}' in the registry." );
	}

	public FileAssociation GetAssociation( string extension )
	{
		if ( string.IsNullOrEmpty( extension ) )
			return null;

		if ( !extension.StartsWith( "." ) )
			extension = "." + extension;

		string extKeyPath = $"{HKCR}\\{extension}";

		if ( !Registry.Instance.KeyExists( extKeyPath ) )
		{
			return null;
		}

		string friendlyName = Registry.Instance.GetValue<string>( extKeyPath, "FriendlyName", "File" );
		string iconName = Registry.Instance.GetValue<string>( extKeyPath, "IconName", "default" );
		string defaultProgram = Registry.Instance.GetValue<string>( extKeyPath, "DefaultProgram", null );
		bool shouldShow = bool.TryParse( Registry.Instance.GetValue<string>( extKeyPath, "ShouldShowInShellCreateNew", "false" ), out var b ) && b;

		var association = new FileAssociation( extension, friendlyName, iconName, defaultProgram, shouldShow );

		// Load actions
		string shellKeyPath = $"{extKeyPath}\\Shell";
		if ( Registry.Instance.KeyExists( shellKeyPath ) )
		{
			foreach ( string verb in Registry.Instance.GetSubKeyNames( shellKeyPath ) )
			{
				string verbKeyPath = $"{shellKeyPath}\\{verb}";
				string commandKeyPath = $"{verbKeyPath}\\command";
				string commandValue = Registry.Instance.GetValue<string>( commandKeyPath, "", null );

				if ( !string.IsNullOrEmpty( commandValue ) )
				{
					// Basic parsing of command and arguments.
					// This assumes program path doesn't contain spaces, or arguments are clearly separated.
					// A more robust parser might be needed for complex command lines.
					string program = commandValue;
					string arguments = "";

					// Attempt to split program and arguments. A common pattern is program path followed by arguments.
					// If the program path might contain spaces and isn't quoted, this simple split is naive.
					// For now, we assume DefaultProgram in FileAssociation constructor handles the primary "open"
					// and AddAction correctly sets Program and Arguments.
					// The commandValue from registry is usually "program.exe" "%1" or "program.exe" /arg "%1"

					int firstSpaceIndex = commandValue.IndexOf( ' ' );
					if ( commandValue.StartsWith( "\"" ) ) // Quoted program path
					{
						int closingQuoteIndex = commandValue.IndexOf( '\"', 1 );
						if ( closingQuoteIndex > 0 )
						{
							program = commandValue.Substring( 1, closingQuoteIndex - 1 );
							if ( closingQuoteIndex + 1 < commandValue.Length )
							{
								arguments = commandValue.Substring( closingQuoteIndex + 1 ).TrimStart();
							}
						}
						// else, malformed quoted path, treat whole string as program
					}
					else if ( firstSpaceIndex > 0 ) // Unquoted program path with arguments
					{
						program = commandValue.Substring( 0, firstSpaceIndex );
						arguments = commandValue.Substring( firstSpaceIndex + 1 ).TrimStart();
					}
					// else, no spaces, treat whole string as program

					string displayName = Registry.Instance.GetValue<string>( verbKeyPath, "DisplayName", verb ); // Default to verb if no DisplayName

					// Ensure the action isn't a duplicate of the one potentially created by DefaultProgram in constructor
					if ( association.Actions.TryGetValue( verb, out var existingAction ) )
					{
						// If existing action matches, perhaps from DefaultProgram, update it if necessary or skip
						// For simplicity, we might overwrite or decide based on specificity
						// Here, we'll update if different, assuming registry is more specific
						if ( existingAction.Program != program || existingAction.Arguments != arguments )
						{
							association.AddAction( verb, displayName, program, arguments );
						}
						else
						{
							existingAction.DisplayName = displayName; // Update display name if different
						}
					}
					else
					{
						association.AddAction( verb, displayName, program, arguments );
					}
				}
			}
		}

		// Ensure the default "open" action from DefaultProgram is present if not explicitly defined in Shell
		if ( !string.IsNullOrEmpty( association.DefaultProgram ) && !association.Actions.ContainsKey( "open" ) )
		{
			association.AddAction( "open", "Open", association.DefaultProgram, "\"%1\"" );
		}


		return association;
	}

	public List<FileAssociation> GetAllAssociations()
	{
		var allAssociations = new List<FileAssociation>();
		if ( !Registry.Instance.KeyExists( HKCR ) )
		{
			return allAssociations;
		}

		var extensionKeys = Registry.Instance.GetSubKeyNames( HKCR );
		foreach ( string keyName in extensionKeys )
		{
			if ( keyName.StartsWith( "." ) )
			{
				var association = GetAssociation( keyName );
				if ( association != null )
				{
					allAssociations.Add( association );
				}
			}
		}
		return allAssociations;
	}

	public bool OpenFile( string filePath )
	{
		Log.Info( $"Opening file: {filePath}" );
		string extension = _vfs.GetExtension( filePath ); // VFS is still good for this
		var association = GetAssociation( extension );

		if ( association != null ) // No need to check DefaultProgram here, Execute handles it
		{
			return association.Execute( filePath ); // Execute the default action ("open")
		}
		Log.Warning( $"FileAssociationManager: No association found to open file '{filePath}' with extension '{extension}'." );
		return false;
	}
}
