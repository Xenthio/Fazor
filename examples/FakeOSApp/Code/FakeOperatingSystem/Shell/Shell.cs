using FakeDesktop;
using FakeOperatingSystem.OSFileSystem;
using System;
using System.IO;

namespace FakeOperatingSystem.Shell;

public class Shell
{
	public static void ShellExecute( string path )
	{
		// For lnk files, resolve the target path

		if ( path.EndsWith( ".lnk", StringComparison.OrdinalIgnoreCase ) )
		{
			var content = VirtualFileSystem.Instance.ReadAllText( path );
			var shortcut = ShortcutDescriptor.FromFileContent( content );
			if ( shortcut != null )
			{
				shortcut.Resolve();
			}
		}

		// For executables, launch directly
		if ( path.EndsWith( ".exe", StringComparison.OrdinalIgnoreCase ) )
		{
			Log.Info( $"Launching executable: {path}" );
			var launchOptions = new Win32LaunchOptions
			{
				WorkingDirectory = Path.GetDirectoryName( path )
			};
			ProcessManager.Instance?.OpenExecutable( path, launchOptions, shellLaunch: true );
			return;
		}

		// For other files, use file associations
		FileAssociationManager.Instance.OpenFile( path );
	}
}
