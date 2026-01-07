using FakeOperatingSystem.OSFileSystem; // Assuming VirtualFileSystem is here
using System.IO;
using System.Threading.Tasks;
using XGUI; // For XGUISystem

namespace FakeOperatingSystem.Setup
{
	public class OSSetup
	{
		private readonly IVirtualFileSystem _vfs;
		private Registry _registry; // Can be injected or initialized
		private SetupDialog _setupDialog;

		public OSSetup( IVirtualFileSystem vfs, Registry registry = null )
		{
			_vfs = vfs;
			_registry = registry;
		}

		public async Task RunInitialSetup()
		{
			if ( _vfs.DirectoryExists( "C:/Windows" ) ) // Using a common check for an already set up system
			{
				Log.Info( "FakeOS system root already exists. Skipping setup." );
				return;
			}

			_setupDialog = new SetupDialog();
			XGUISystem.Instance.Panel.AddChild( _setupDialog );
			await Task.Delay( 20 ); // Give UI time to render

			_setupDialog.UpdateStatus( "Preparing file system..." );
			CreateCoreDirectories();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Creating system files..." );
			CreateRootFiles();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Copying Windows files..." );
			CopyTemplateWindowsFiles();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Setting up Windows directory..." );
			CreateWindowsDirectories();
			CreateWindowsApplications();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Setting up Program Files..." );
			CreateProgramFilesApplications();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Creating start menu items..." );
			CreateStartMenuItems();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Setting up user config in registry..." );
			SetupDefaultConfigInRegistry();
			await Task.Delay( 20 );

			_setupDialog.Complete();
		}

		public async void SetupDefaultConfigInRegistry()
		{
			if ( _registry == null )
			{
				if ( VirtualFileSystem.Instance == null ) // VFS must exist for Registry constructor
				{
					Log.Error( "[OsSetup] VirtualFileSystem.Instance is null. Cannot initialize Registry. Application registry setup will be skipped." );
					return;
				}
				Log.Info( "[OsSetup] Registry instance not provided, initializing new Registry instance." );
				_registry = new Registry(); // This will set Registry.Instance
				if ( Registry.Instance == null ) // Check if static instance got set
				{
					Log.Error( "[OsSetup] Failed to initialize Registry.Instance after attempting construction. Application registry setup will be skipped." );
					return;
				}
			}

			_setupDialog.UpdateStatus( "Setting up environment variables..." );
			SetupEnvironmentVariables();
			await Task.Delay( 20 );

			_setupDialog.UpdateStatus( "Initializing and registering applications in registry..." );
			InitializeAndRegisterApplicationsInRegistry();
			await Task.Delay( 20 );
		}

		private void CreateCoreDirectories()
		{
			// In VFS, paths are typically like "C:/Windows", not "FakeSystemRoot/Windows"
			// Adjusting paths to be more VFS-like.
			// The VFS implementation itself will map these to the underlying FileSystem.Data structure.
			_vfs.CreateDirectory( "C:/" );
			_vfs.CreateDirectory( "C:/Recycled" );
			_vfs.WriteAllText( "C:/Recycled/desktop.ini", "[.XGUIInfo]\nIcon=recyclebinfull\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,10\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=10" );
			_vfs.CreateDirectory( "C:/Program Files" );
			_vfs.CreateDirectory( "C:/Windows" );
			_vfs.CreateDirectory( "C:/Windows/Desktop" ); // User's desktop often here or under Profiles
		}

		private void CreateRootFiles()
		{
			_vfs.WriteAllText( "C:/Autoexec.bat", "@echo off\n win" );
			_vfs.WriteAllText( "C:/Config.sys", "DEVICE=C:\\WINDOWS\\HIMEM.SYS\nDEVICE=C:\\WINDOWS\\EMM386.EXE" );
			_vfs.WriteAllText( "C:/MSDOS.SYS", "[Paths]\nWinDir=C:\\WINDOWS\nWinBootDir=C:\\WINDOWS\nHostWinBootDrv=C\n\n[Options]\nBootMulti=1\nBootGUI=1\nDoubleBuffer=1\nAutoScan=1\nWinVer=4.10.2222" );
			_vfs.WriteAllText( "C:/boot.ini", "[boot loader]\nTimeout=30\nDefault=multi(0)disk(0)rdisk(0)partition(1)\\WINDOWS\n[operating systems]\nmulti(0)disk(0)rdisk(0)partition(1)\\WINDOWS=\"Microsoft Windows 98 Hybrid NT Edition\" /fastdetect" );
		}

		public void CopyTemplateWindowsFiles()
		{
			// Copy everything from Mounted Filesystem's /FakeOS/Setup/TemplateRoot (should be mounted on FS:A:)

			if ( _vfs is VirtualFileSystem vfs )
			{
				vfs.CopyDirectory( @"FS:A:\FakeOS\Setup\TemplateRoot", @"C:\" );
			}
		}

		private void CreateWindowsDirectories()
		{
			string windowsBase = "C:/Windows";
			_vfs.CreateDirectory( $"{windowsBase}/Web" );
			_vfs.CreateDirectory( $"{windowsBase}/Web/Wallpaper" );
			_vfs.CreateDirectory( $"{windowsBase}/Downloaded Program Files" );
			_vfs.WriteAllText( $"{windowsBase}/Downloaded Program Files/desktop.ini", "[.XGUIInfo]\nIcon=downloadedprogramfiles\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,5\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=5" );
			_vfs.CreateDirectory( $"{windowsBase}/Fonts" );
			_vfs.CreateDirectory( $"{windowsBase}/Help" );
			_vfs.CreateDirectory( $"{windowsBase}/History" );
			_vfs.WriteAllText( $"{windowsBase}/History/desktop.ini", "[.XGUIInfo]\nIcon=history\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,4\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=4" );
			_vfs.CreateDirectory( $"{windowsBase}/Media" );
			_vfs.CreateDirectory( $"{windowsBase}/Offline Web Pages" );
			_vfs.WriteAllText( $"{windowsBase}/Offline Web Pages/desktop.ini", "[.XGUIInfo]\nIcon=offlinepages\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\webcheck.dll,1\nIconFile=C:\\WINDOWS\\system32\\webcheck.dll\nIconIndex=1" );
			_vfs.CreateDirectory( $"{windowsBase}/System" );
			_vfs.CreateDirectory( $"{windowsBase}/System32" );
			_vfs.CreateDirectory( $"{windowsBase}/System32/drivers" );
			_vfs.CreateDirectory( $"{windowsBase}/System32/config" ); // For registry hives
		}

		private void CreateWindowsApplications()
		{
			string windowsDir = "C:/Windows";
			string system32Dir = $"{windowsDir}/System32";

			NativeProgram.CompileIntoExe( typeof( ExplorerProgram ), $"{windowsDir}/explorer.exe" );
			NativeProgram.CompileIntoExe( typeof( NotepadProgram ), $"{windowsDir}/notepad.exe" );
			NativeProgram.CompileIntoExe( typeof( MSPaintProgram ), $"{windowsDir}/mspaint.exe" );
			NativeProgram.CompileIntoExe( typeof( WinVerProgram ), $"{system32Dir}/winver.exe" );
			NativeProgram.CompileIntoExe( typeof( TaskMgrProgram ), $"{system32Dir}/taskmgr.exe" );
			NativeProgram.CompileIntoExe( typeof( ConsoleHostProgram ), $"{system32Dir}/conhost.exe" );
			NativeProgram.CompileIntoExe( typeof( CommandProgram ), $"{system32Dir}/cmd.exe" );
			NativeProgram.CompileIntoExe( typeof( EditProgram ), $"{system32Dir}/edit.exe" );
			NativeProgram.CompileIntoExe( typeof( RegEditProgram ), $"{windowsDir}/regedit.exe" ); // Often in Windows dir
			NativeProgram.CompileIntoExe( typeof( CalculatorProgram ), $"{windowsDir}/calc.exe" ); // Added Calculator 

		}

		private void CreateProgramFilesApplications()
		{
			string programFilesDir = "C:/Program Files";

			string ieDir = $"{programFilesDir}/Internet Explorer";
			_vfs.CreateDirectory( ieDir );
			NativeProgram.CompileIntoExe( typeof( IExploreProgram ), $"{ieDir}/Iexplore.exe" );

			string doomDir = $"{programFilesDir}/Ultimate Doom for Windows 95";
			_vfs.CreateDirectory( doomDir );
			NativeProgram.CompileIntoExe( typeof( Doom95Program ), $"{doomDir}/doom95.exe" );

			string outlookDir = $"{programFilesDir}/Outlook Express";
			_vfs.CreateDirectory( outlookDir ); // Create directory for Outlook
			NativeProgram.CompileIntoExe( typeof( OutlookExpressProgram ), $"{outlookDir}/outlook.exe" );

			string steamDir = $"{programFilesDir}/Steam";
			_vfs.CreateDirectory( steamDir );
			NativeProgram.CompileIntoExe( typeof( SteamProgram ), $"{steamDir}/steam.exe" );
		}

		private void CreateStartMenuItems()
		{
			string startMenuBase = "C:/Windows/Start Menu";
			_vfs.CreateDirectory( startMenuBase );
			_vfs.CreateDirectory( $"{startMenuBase}/Programs" );
			_vfs.CreateDirectory( $"{startMenuBase}/Start Up" );
			// TODO: Create actual .lnk shortcut files here for the installed programs
		}

		private void SetupEnvironmentVariables()
		{
			EnvironmentManager.SetEnvironmentVariable( "windir", @"C:\WINDOWS" );
			EnvironmentManager.SetEnvironmentVariable( "OS", "Windows_NT" );
			EnvironmentManager.SetEnvironmentVariable( "Path", @"C:\WINDOWS\system32;C:\WINDOWS;C:\WINDOWS\System32\Wbem;C:\WINDOWS\command" );
			EnvironmentManager.SetEnvironmentVariable( "PATHEXT", ".COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH" );
		}

		private void InitializeAndRegisterApplicationsInRegistry()
		{
			// Ensure the static instance is the one we are working with if it was created here.
			// Or, if _registry was passed in, Registry.Instance should already be that one.
			var registryInstance = Registry.Instance ?? _registry;
			if ( registryInstance == null )
			{
				Log.Error( "[OsSetup] Registry is not available. Application registry setup will be skipped." );
				return;
			}


			string applicationsRoot = @"HKEY_CLASSES_ROOT\Applications";

			// Notepad
			string notepadExe = "notepad.exe";
			string notepadPath = "C:/Windows/notepad.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, notepadExe ), "", "Notepad" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, notepadExe, "shell", "open", "command" ), "", $"\"{notepadPath}\" \"%1\"" );

			// Paint
			string mspaintExe = "mspaint.exe";
			string mspaintPath = "C:/Windows/mspaint.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, mspaintExe ), "", "Paint" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, mspaintExe, "shell", "open", "command" ), "", $"\"{mspaintPath}\" \"%1\"" );

			// Internet Explorer
			string iexploreExe = "iexplore.exe";
			string iexplorePath = "C:/Program Files/Internet Explorer/iexplore.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, iexploreExe ), "", "Internet Explorer" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, iexploreExe, "shell", "open", "command" ), "", $"\"{iexplorePath}\" \"%1\"" );

			// RegEdit
			string regeditExe = "regedit.exe";
			string regeditPath = "C:/Windows/regedit.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, regeditExe ), "", "Registry Editor" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, regeditExe, "shell", "open", "command" ), "", $"\"{regeditPath}\"" );

			// Doom 95
			string doomExe = "doom95.exe";
			string doomPath = "C:/Program Files/Ultimate Doom for Windows 95/doom95.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, doomExe ), "", "Ultimate Doom for Windows 95" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, doomExe, "shell", "open", "command" ), "", $"\"{doomPath}\"" );

			// Outlook Express
			string outlookExe = "outlook.exe";
			string outlookPath = "C:/Program Files/Outlook Express/outlook.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, outlookExe ), "", "Outlook Express" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, outlookExe, "shell", "open", "command" ), "", $"\"{outlookPath}\"" );

			// Steam
			string steamExe = "steam.exe";
			string steamPath = "C:/Program Files/Steam/steam.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, steamExe ), "", "Steam" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, steamExe, "shell", "open", "command" ), "", $"\"{steamPath}\"" );

			// Explorer
			string explorerExe = "explorer.exe";
			string explorerPath = "C:/Windows/explorer.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, explorerExe ), "", "Windows Explorer" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, explorerExe, "shell", "open", "command" ), "", $"\"{explorerPath}\"" );

			// Console Host
			string conhostExe = "conhost.exe";
			string conhostPath = "C:/Windows/System32/conhost.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, conhostExe ), "", "Console Host" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, conhostExe, "shell", "open", "command" ), "", $"\"{conhostPath}\"" );

			// Command Prompt
			string cmdExe = "cmd.exe";
			string cmdPath = "C:/Windows/System32/cmd.exe";
			registryInstance.SetValue( Path.Combine( applicationsRoot, cmdExe ), "", "Command Prompt" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, cmdExe, "shell", "open", "command" ), "", $"\"{cmdPath}\"" );

			// Calculator
			string calcExe = "calc.exe";
			string calcPath = "C:/Windows/System32/calc.exe"; // Matches FilePath in CalculatorProgram.cs
			registryInstance.SetValue( Path.Combine( applicationsRoot, calcExe ), "", "Calculator" );
			registryInstance.SetValue( Path.Combine( applicationsRoot, calcExe, "shell", "open", "command" ), "", $"\"{calcPath}\"" );

			Log.Info( "Registered applications in the registry." );
		}
	}
}
