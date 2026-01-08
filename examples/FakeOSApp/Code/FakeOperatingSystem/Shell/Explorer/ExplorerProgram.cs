using FakeOperatingSystem;
using FakeOperatingSystem.Shell.Explorer;
using System.Linq; // Required for LINQ operations like OfType and Any
using XGUI;    // Required for XGUISystem, XGUIPanel

// Assuming TaskBar and Desktop might be in a namespace like FakeDesktop or FakeOperatingSystem.Shell.UI
// Add appropriate using statement if they are in a specific namespace, e.g.:
// using FakeDesktop; 

public class ExplorerProgram : NativeProgram
{
	public override string FilePath => "C:/Windows/explorer.exe";

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var xguiSystemPanel = XGUISystem.Instance?.Panel;

		if ( xguiSystemPanel == null )
		{
			Log.Error( "[ExplorerProgram] XGUISystem.Instance.Panel is null. Cannot initialize shell components." );
			// Optionally, still create the Explorer window or handle error appropriately
		}
		else
		{
			// Check for and initialize TaskBar if not already present
			if ( !xguiSystemPanel.Children.OfType<TaskBar>().Any() )
			{
				var taskBar = new TaskBar();
				xguiSystemPanel.AddChild( taskBar );
				process.RegisterWindow( taskBar );
			}

			// Check for and initialize Desktop if not already present
			if ( !xguiSystemPanel.Children.OfType<Desktop>().Any() )
			{
				var desktop = new Desktop();
				xguiSystemPanel.AddChild( desktop );
				process.RegisterWindow( desktop );
			}
		}

		// Create and register the Explorer window itself (this is always done per ExplorerProgram instance)
		var window = new Explorer();
		if ( launchOptions != null )
		{
			window.InitialPath = launchOptions.Arguments;
		}
		process.RegisterWindow( window );
	}
}
