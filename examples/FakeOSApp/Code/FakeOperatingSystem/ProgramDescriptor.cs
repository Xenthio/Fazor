using Sandbox;
using System;
using XGUI;

namespace FakeDesktop;


public class NativeProgramDescriptor
{
	public string TypeName { get; set; }
}

/// <summary>
/// Describes a virtual program that can be launched in the virtual operating system
/// </summary>
[Obsolete( "This class is obsolete and will be removed in a future version." )]
public class ProgramDescriptor
{
	/// <summary>
	/// The name of the program (used for display)
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The filename including .exe extension
	/// </summary>
	public string Filename { get; set; }

	/// <summary>
	/// The icon to use for this program
	/// </summary>
	public string IconName { get; set; }

	/// <summary>
	/// True if this program is a real executable and should be ran in emulated x86 mode
	/// </summary>
	public bool IsRealExecutable { get; set; } = false;

	/// <summary>
	/// Type name of the XGUI panel to create when this program is launched
	/// </summary>
	public string PanelTypeName { get; set; }

	/// <summary>
	/// Optional description of the program
	/// </summary>
	public string Description { get; set; }

	/// <summary>
	/// Optional command line arguments
	/// </summary>
	public string Arguments { get; set; }

	/// <summary>
	/// Optional working directory
	/// </summary>
	public string WorkingDirectory { get; set; }

	/// <summary>
	/// Create a program descriptor for a launchable program
	/// </summary>
	public ProgramDescriptor( string name, string filename, string iconName, string panelTypeName )
	{
		Name = name;
		Filename = filename;
		IconName = iconName;
		PanelTypeName = panelTypeName;

	}

	/// <summary>
	/// Launch this program as a window
	/// </summary>
	public Window Launch()
	{
		try
		{
			if ( IsRealExecutable )
			{
				return null;
			}

			if ( string.IsNullOrEmpty( PanelTypeName ) )
				return null;

			var type = TypeLibrary.GetType( PanelTypeName );
			if ( type == null )
			{
				Log.Warning( $"Program '{Name}' specifies unknown panel type: {PanelTypeName}" );
				return null;
			}

			var window = type.Create<Window>();
			window.AutoFocus = true;

			// Set arguments property if it exists
			var argumentsProperty = type.GetProperty( "Arguments" );
			if ( argumentsProperty != null && !string.IsNullOrEmpty( Arguments ) )
			{
				argumentsProperty.SetValue( window, Arguments );
			}

			Game.ActiveScene.GetSystem<XGUISystem>().Panel.AddChild( window );
			Game.ActiveScene.GetSystem<XGUISystem>().Panel.SetChildIndex( window, 0 );
			window.FocusWindow();

			return window;
		}
		catch ( Exception ex )
		{
			Log.Error( $"Error launching program '{Name}': {ex.Message}" );
			return null;
		}
	}

	/// <summary>
	/// Serializes the program descriptor to string for storage in an exe file
	/// </summary>
	public string ToFileContent()
	{
		// Simple format to store in the exe file
		return System.Text.Json.JsonSerializer.Serialize( this, new System.Text.Json.JsonSerializerOptions
		{
			WriteIndented = true
		} );
	}

	/// <summary>
	/// Reads a program descriptor from an exe file's content
	/// </summary>
	public static ProgramDescriptor FromFileContent( string content )
	{
		try
		{
			return System.Text.Json.JsonSerializer.Deserialize<ProgramDescriptor>( content );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"Failed to parse FakeEXE program descriptor: {ex.Message}" );
			return null;
		}
	}
}

