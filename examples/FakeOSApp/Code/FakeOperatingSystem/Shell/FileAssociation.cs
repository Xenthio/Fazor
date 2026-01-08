using System.Collections.Generic;

namespace FakeOperatingSystem.Shell;

/// <summary>
/// Represents a file type association in the virtual file system,
/// with data typically loaded from the registry.
/// </summary>
public class FileAssociation
{
	/// <summary>
	/// The file extension this association applies to (e.g., ".txt")
	/// </summary>
	public string Extension { get; set; }

	/// <summary>
	/// A friendly name for the file type (e.g., "Text Document")
	/// </summary>
	public string FriendlyName { get; set; }

	/// <summary>
	/// The icon name to use for this file type
	/// </summary>
	public string IconName { get; set; }

	/// <summary>
	/// The default program to use for opening this file type.
	/// This often forms the basis of the "open" action.
	/// </summary>
	public string DefaultProgram { get; set; }

	/// <summary>
	/// If the file type should be shown in the shell's "Create New" context menu
	/// </summary>
	public bool ShouldShowInShellCreateNew { get; set; } = false;

	/// <summary>
	/// The actions that can be performed on this file type
	/// </summary>
	public Dictionary<string, FileAction> Actions { get; set; } = new Dictionary<string, FileAction>( System.StringComparer.OrdinalIgnoreCase );

	// Removed: public static Dictionary<string, FileAssociation> Associations { get; set; } = new Dictionary<string, FileAssociation>();

	/// <summary>
	/// Create a new file association.
	/// </summary>
	public FileAssociation( string extension, string friendlyName, string iconName, string defaultProgram, bool shouldShowInShellCreateNew = false )
	{
		Extension = extension.StartsWith( "." ) ? extension : "." + extension;
		FriendlyName = friendlyName;
		IconName = iconName;
		DefaultProgram = defaultProgram;
		ShouldShowInShellCreateNew = shouldShowInShellCreateNew;
		// Removed: Associations[Extension] = this;

		// Default "open" action if a default program is specified.
		// This will also be created/overridden by FileAssociationManager when reading from registry if a specific shell\open\command exists.
		if ( !string.IsNullOrEmpty( defaultProgram ) )
		{
			// Ensure Actions dictionary uses a case-insensitive comparer if it wasn't already
			if ( Actions.Comparer != System.StringComparer.OrdinalIgnoreCase )
			{
				Actions = new Dictionary<string, FileAction>( Actions, System.StringComparer.OrdinalIgnoreCase );
			}

			if ( !Actions.ContainsKey( "open" ) ) // Only add if not already present (e.g. from registry load)
			{
				Actions["open"] = new FileAction( "Open", defaultProgram, "\"%1\"" );
			}
		}
	}

	/// <summary>
	/// Add or update an action for this file type.
	/// </summary>
	public void AddAction( string verb, string displayName, string program, string arguments = "\"%1\"" )
	{
		// Ensure Actions dictionary uses a case-insensitive comparer
		if ( Actions.Comparer != System.StringComparer.OrdinalIgnoreCase )
		{
			Actions = new Dictionary<string, FileAction>( Actions, System.StringComparer.OrdinalIgnoreCase );
		}
		Actions[verb.ToLowerInvariant()] = new FileAction( displayName, program, arguments );
	}

	/// <summary>
	/// Execute the default action (typically "open") for this file type on the given file.
	/// </summary>
	public bool Execute( string filePath )
	{
		return ExecuteAction( "open", filePath );
	}

	/// <summary>
	/// Execute a specific action for this file type on the given file.
	/// </summary>
	public bool ExecuteAction( string verb, string filePath )
	{
		if ( !Actions.TryGetValue( verb.ToLowerInvariant(), out var action ) )
		{
			// Fallback: if verb is "open" and DefaultProgram is set but no explicit "open" action exists, try using DefaultProgram.
			// This case should ideally be handled by the constructor or GetAssociation ensuring an "open" action exists if DefaultProgram is set.
			if ( verb.Equals( "open", System.StringComparison.OrdinalIgnoreCase ) && !string.IsNullOrEmpty( DefaultProgram ) )
			{
				Log.Info( $"No explicit '{verb}' action found for {Extension} files, attempting to use DefaultProgram '{DefaultProgram}'." );
				var tempAction = new FileAction( "Open", DefaultProgram, "\"%1\"" );
				return tempAction.Execute( filePath );
			}

			Log.Warning( $"No '{verb}' action found for {Extension} files." );
			return false;
		}

		return action.Execute( filePath );
	}
}

/// <summary>
/// Represents an action that can be performed on a file.
/// </summary>
public class FileAction
{
	/// <summary>
	/// The display name for this action (shown in context menu).
	/// </summary>
	public string DisplayName { get; set; }

	/// <summary>
	/// The program to execute for this action.
	/// </summary>
	public string Program { get; set; }

	/// <summary>
	/// The command line arguments (typically containing %1 for the file path).
	/// </summary>
	public string Arguments { get; set; }

	public FileAction( string displayName, string program, string arguments )
	{
		DisplayName = displayName;
		Program = program;
		Arguments = arguments;
	}

	/// <summary>
	/// Execute this action on the given file.
	/// </summary>
	public bool Execute( string filePath )
	{
		if ( string.IsNullOrEmpty( Program ) )
		{
			Log.Warning( $"FileAction: Cannot execute action '{DisplayName}' because Program is not specified." );
			return false;
		}
		var args = Arguments.Replace( "%1", $"\"{filePath}\"" ); // Ensure filePath is quoted if it contains spaces
																 // ProcessManager.Instance.OpenExecutable( Program, new Win32LaunchOptions { Arguments = args } );
																 // Assuming ProcessManager and Win32LaunchOptions are available in this scope.
																 // If not, you might need to pass them or use a globally accessible instance.
																 // For now, let's simulate or log this call if ProcessManager is not directly usable here.
		Log.Info( $"Executing: {Program} {args}" );
		// Replace with your actual process execution call:
		ProcessManager.Instance.OpenExecutable( Program, new Win32LaunchOptions { Arguments = args } );
		return true;
	}
}
