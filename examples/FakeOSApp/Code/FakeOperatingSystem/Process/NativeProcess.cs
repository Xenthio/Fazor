// code/FakeOperatingSystem/Process/NativeProcess.cs
using Sandbox;
using System;
using System.IO;
using System.Threading.Tasks;
using XGUI;

namespace FakeOperatingSystem;
public class NativeProcess : BaseProcess
{
	public NativeProgram Program { get; }
	private Task _executionTask;

	public NativeProcess( NativeProgram program, Win32LaunchOptions options )
	{
		Program = program;
		LaunchOptions = options;
		ProcessName = program.GetType().Name;
		ProcessFileName = Path.GetFileName( program.FilePath );
		ProcessFilePath = program.FilePath;
		IsConsoleProcess = program.ConsoleApp;
	}

	public override void Start()
	{
		// Let the program create its windows and perform startup logic
		Program.StandardOutput = StandardOutput;
		Program.StandardError = StandardError;
		Program.StandardInput = StandardInput;
		_executionTask = GameTask.RunInThreadAsync( () =>
		{
			try
			{
				Program.Main( this, LaunchOptions );
				if ( Program.ConsoleApp )
				{
					ProcessManager.Instance.TerminateProcess( this );
				}
			}
			catch ( Exception ex )
			{
				Log.Error( $"Error in {ProcessName}: {ex.Message}" );
				Log.Error( ex.StackTrace );
				StandardError?.WriteLine( $"Error: {ex.Message}" );
			}
		} );
	}

	public override void Terminate()
	{
		// Close all owned windows (if not already closed by the program)
		foreach ( var window in OwnedWindows.ToArray() )
		{
			if ( window is Window win )
				win.Close();
			else
				window?.Delete();
		}
		OwnedWindows.Clear();
	}

	public void RegisterWindow( XGUIPanel window )
	{
		if ( window != null && !OwnedWindows.Contains( window ) )
		{
			OwnedWindows.Add( window );
			XGUISystem.Instance.Panel.AddChild( window );
			if ( window is Window win )
			{
				win.OnCloseAction += () =>
				{
					OwnedWindows.Remove( window );
					if ( OwnedWindows.Count <= 0 )
					{
						ProcessManager.Instance.TerminateProcess( this );
					}
				};
			}
		}
	}
}
