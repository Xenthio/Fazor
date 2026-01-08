using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.Shell;
using FakeOperatingSystem.Shell.ControlPanel.DeskCpl;
using Sandbox;
using FakeOperatingSystem.Logon;
using System.Linq;
using System.Threading.Tasks;
using XGUI;

namespace FakeOperatingSystem;

public class FakeOSLoader : Component
{
	public static int VersionNumber = 0;
	public static string VersionString = "XGUI-3 FakeOS v0.0 (no versioning)";

	public static bool UserSystemEnabled = true; // Set to false for single-user mode

	public static FakeOSLoader Instance;
	public VirtualFileSystem VirtualFileSystem;
	public ShellNamespace ShellNamespace;
	ProcessManager _processManager;
	public Registry Registry;
	public UserManager UserManager;


	protected override void OnStart()
	{
		XGUISystem.Instance.SetGlobalTheme( "/XGUI/DefaultStyles/Computer95.scss" );

		Startup();
		base.OnStart();
	}

	public async Task Startup() // Changed to async Task
	{

		// Initialize the virtual file system
		VirtualFileSystem = new VirtualFileSystem( FileSystem.Data );

		// Do core OS file setup (no file copying setup process or ui for now)
		await FakeSystemRoot.EnsureSystemRootExists( VirtualFileSystem, Registry );


		await Boot();
	}

	/// <summary>
	/// This would be boot.
	/// </summary>
	public async Task Boot() // Changed to async Task
	{
		Instance = this;
		Registry = new Registry();
		UserManager = new UserManager();
		UserManager.LoadUsers(); // Load existing users first

		// Check if user system is enabled in registry
		// Request a nullable int (int?). If the value doesn't exist, GetValue will return default(int?), which is null.
		int? userProfilesSetting = Registry.GetValue<int?>( @"HKEY_LOCAL_MACHINE\Network\Logon", "UserProfiles", null );

		if ( userProfilesSetting == null ) // Now this correctly checks if the value was not found or was explicitly null
		{
			// First boot or registry value missing: show create user dialog
			ShowCreateUserDialog();
			return; // Wait for dialog result before continuing boot
		}
		else
		{
			// Value exists, proceed to use it
			UserSystemEnabled = userProfilesSetting.Value == 1;
		}

		// If user system is enabled, but no user is set (e.g. after first boot dialog was skipped then re-enabled manually)
		// or if it's just a normal boot with users enabled.
		if ( UserSystemEnabled )
		{
			if ( UserManager.Users.Any() )
			{
				ShowLogonDialog(); // Show logon dialog instead of auto-login
				return;
			}
			else
			{
				Log.Warning( "User system enabled, but no users found. Showing create user dialog." );
				ShowCreateUserDialog();
				return;
			}
		}
		else
		{
			// Single-user mode: set up global folders
			//await UserManager.SetupUserProfile( null ); // Await here
			Registry.LoadUserHive( "Default", @"C:\Windows\USER.DAT" );
			// Apply default theme if no user specific theme logic for single user mode
			// Or apply a specific theme for single user mode from HKLM if desired
		}

		ContinueBoot();
	}

	private void ShowCreateUserDialog()
	{
		// Pseudocode for dialog
		var dialog = new CreateUserDialog(
			onCreate: async ( username, password ) => // Lambda becomes async
			{
				UserSystemEnabled = true;
				Registry.SetValue( @"HKEY_LOCAL_MACHINE\Network\Logon", "UserProfiles", 1 );
				var user = new UserAccount
				{
					UserName = username,
					PasswordHash = UserManager.HashPassword( password ), // Hash in real code!
					ProfilePath = $@"C:\Documents and Settings\{username}\",
					RegistryHivePath = $@"C:\Documents and Settings\{username}\NTUSER.DAT"
				};
				UserManager.Users.Add( user );
				UserManager.SaveUsers();
				// Create All Users folder
				var vfs = VirtualFileSystem.Instance;
				var allUsers = @"C:\Documents and Settings\All Users";
				if ( !vfs.DirectoryExists( allUsers ) )
					vfs.CreateDirectory( allUsers );

				// Load the user hive into the registry
				Registry.LoadUserHive( user.UserName, user.RegistryHivePath );

				await UserManager.SetupUserProfile( user ); // Await here
				UserManager.Login( username, password ); // This should ideally also set CurrentUser for ContinueBoot

				// After user profile is set up and hive loaded by Login (implicitly via LoadUserHive in Login or here)
				// Apply default theme for new user or let them pick later.
				// For now, new users will get the system default theme.
				// If a specific theme should be set for new users, it can be done here.
				// Example: Registry.Instance.SetValue(DeskCplDialog.UserThemeRegistryPath, DeskCplDialog.UserThemeRegistryValueName, "/XGUI/DefaultStyles/Computer95.scss");

				ContinueBoot(); // This might need to be awaited if it also becomes async
			},
			onSkip: async () => // Lambda becomes async
			{
				UserSystemEnabled = false;
				Registry.SetValue( @"HKEY_LOCAL_MACHINE\Network\Logon", "UserProfiles", 0 );
				await UserManager.SetupUserProfile( null ); // Await here
				ContinueBoot();
			}
		);
		// Show dialog in your UI system
		XGUISystem.Instance.Panel.AddChild( dialog );
	}
	private void ShowLogonDialog()
	{
		var logonDialog = new LogonDialog(
			UserManager.Users, // Pass the list of users
			onLoginSuccess: async ( UserAccount loggedInUser ) =>
			{
				// UserManager.CurrentUser is already set by LogonDialog's call to UserManager.Login
				Log.Info( $"Logged in as: {UserManager.CurrentUser.UserName}" );
				Registry.LoadUserHive( UserManager.CurrentUser.UserName, UserManager.CurrentUser.RegistryHivePath );

				// Apply user's saved theme
				if ( Registry.Instance != null )
				{
					string userThemePathFromRegistry = Registry.Instance.GetValue<string>(
						DeskCplDialog.UserThemeRegistryPath,
						DeskCplDialog.UserThemeRegistryValueName,
						null );

					if ( !string.IsNullOrEmpty( userThemePathFromRegistry ) )
					{
						Log.Info( $"Applying user theme from registry: {userThemePathFromRegistry}" );
						XGUISystem.Instance.SetGlobalTheme( userThemePathFromRegistry );
					}
					else
					{
						// If no user theme is set, the default theme (Computer95.scss set in OnStart) will remain.
						Log.Info( "No user theme found in registry, using system default theme." );
					}
				}
				else
				{
					Log.Warning( "Registry.Instance is null. Cannot load user theme preference." );
				}

				// SetupUserProfile ensures profile folders exist, creates if first login for this user
				//await UserManager.SetupUserProfile( UserManager.CurrentUser );
				ContinueBoot();
			},
			onLoginCancel: () =>
			{
				Log.Info( "Logon cancelled by user." );
				// What to do on cancel? 
				// Option 1: Stay on logon screen (do nothing further here, user can try again or shutdown)
				// Option 2: If no users exist, maybe show CreateUserDialog (but users should exist if we got here)
				// Option 3: Implement a shutdown mechanism.
				// For now, let's assume the LogonDialog remains, or we could re-show it.
				// If you want to allow re-showing, you might call ShowLogonDialog() again,
				// but be careful of infinite loops if there's no other exit.
				// A simple approach is to do nothing, requiring the user to click OK again or a shutdown button.
			}
		);
		XGUISystem.Instance.Panel.AddChild( logonDialog );
	}

	private void ContinueBoot()
	{
		// Logon
		_processManager = new ProcessManager();
		ShellNamespace = new ShellNamespace( VirtualFileSystem );
		FileAssociationManager.Initialize( VirtualFileSystem );
		ThemeResources.ReloadAll();
		_processManager.OpenExecutable( "C:/Windows/explorer.exe", new Win32LaunchOptions() );
		var soundpath = XGUISoundSystem.GetSound( "LOGON" );
		var soundfile = SoundFile.Load( soundpath );
		Sound.PlayFile( soundfile );
	}
}
