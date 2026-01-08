using FakeDesktop;
using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.User;
using FakeOperatingSystem.Logon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography; // Added for hashing
using System.Text; // Added for hashing
using System.Threading.Tasks;
using XGUI;
// No longer need System.Text.Json here for users.json

namespace FakeOperatingSystem;
public class UserManager
{
	public static UserManager Instance { get; private set; }
	public List<UserAccount> Users { get; private set; } = new();
	public UserAccount CurrentUser { get; private set; }

	public UserManager()
	{
		Instance = this;
	}

	// Define a registry path for storing user accounts within the SAM hive
	// Path: HKEY_LOCAL_MACHINE\SAM\SAM\Users\<UserName>
	// Note: Real SAM structure is more complex (Domains\Account\Users\<SID>\etc)
	// This is a simplified but thematic approach.
	private const string UsersParentRegistryPath = @"HKEY_LOCAL_MACHINE\SAM\SAM\Users";

	private const string PasswordHashValueName = "PasswordHash"; // Or "V" for realism if desired
	private const string ProfilePathValueName = "ProfilePath";
	private const string RegistryHivePathValueName = "RegistryHivePath";
	// Future: private const string SIDValueName = "SID";
	// Future: private const string UserFlagsValueName = "F";

	/// <summary>
	/// Computes the SHA256 hash of a password.
	/// Returns an empty string if the password is null or empty.
	/// </summary>
	/// <param name="password">The password to hash.</param>
	/// <returns>The SHA256 hash as a hex string, or an empty string.</returns>
	public static string HashPassword( string password )
	{
		if ( string.IsNullOrEmpty( password ) )
		{
			return string.Empty; // Store blank hash for blank password
		}

		using ( var sha256 = SHA256.Create() )
		{
			var hashedBytes = sha256.ComputeHash( Encoding.UTF8.GetBytes( password ) );
			// Convert byte array to a hex string
			var builder = new StringBuilder();
			for ( int i = 0; i < hashedBytes.Length; i++ )
			{
				builder.Append( hashedBytes[i].ToString( "x2" ) );
			}
			return builder.ToString();
		}
	}


	public void LoadUsers()
	{
		Users.Clear();
		if ( Registry.Instance == null )
		{
			Log.Warning( "UserManager: Registry.Instance is null. Cannot load users." );
			return;
		}

		if ( !Registry.Instance.KeyExists( UsersParentRegistryPath ) )
		{
			Log.Info( $"UserManager: Users registry key '{UsersParentRegistryPath}' not found. No users to load." );
			return;
		}

		var userNames = Registry.Instance.GetSubKeyNames( UsersParentRegistryPath );
		if ( userNames == null || !userNames.Any() )
		{
			Log.Info( $"UserManager: No user accounts found under '{UsersParentRegistryPath}'." );
			return;
		}

		foreach ( var userName in userNames )
		{
			string userSpecificPath = Path.Combine( UsersParentRegistryPath, userName );

			var userAccount = new UserAccount
			{
				UserName = userName, // The key name itself is the username
				PasswordHash = Registry.Instance.GetValue<string>( userSpecificPath, PasswordHashValueName, string.Empty ),
				ProfilePath = Registry.Instance.GetValue<string>( userSpecificPath, ProfilePathValueName, string.Empty ),
				RegistryHivePath = Registry.Instance.GetValue<string>( userSpecificPath, RegistryHivePathValueName, string.Empty )
			};
			Users.Add( userAccount );
			Log.Info( $"Loaded user from SAM: {userName}" );
		}
	}

	public void SaveUsers()
	{
		if ( Registry.Instance == null )
		{
			Log.Warning( "UserManager: Registry.Instance is null. Cannot save users." );
			return;
		}

		// Ensure the parent path HKEY_LOCAL_MACHINE\SAM\SAM\Users exists.
		// GetKey with create=true will ensure the path is created.
		var (hive, subPathToUsersParent) = Registry.Instance.ResolvePathToHiveAndSubpath( UsersParentRegistryPath );
		Registry.Instance.GetRegistryKey( hive.Root, subPathToUsersParent, true );


		var existingUserNamesInRegistry = Registry.Instance.GetSubKeyNames( UsersParentRegistryPath ).ToList();
		var currentInMemoryUserNames = Users.Select( u => u.UserName ).ToList();

		// Users to delete from registry
		var usersToDelete = existingUserNamesInRegistry.Except( currentInMemoryUserNames, StringComparer.OrdinalIgnoreCase ).ToList();
		foreach ( var userNameToDelete in usersToDelete )
		{
			string userPathToDelete = Path.Combine( UsersParentRegistryPath, userNameToDelete );
			Registry.Instance.DeleteKey( userPathToDelete );
			Log.Info( $"Deleted user from SAM: {userNameToDelete}" );
		}

		// Users to add or update
		// IMPORTANT: Ensure UserAccount.PasswordHash contains the *hashed* password
		// BEFORE adding the UserAccount object to the Users list or calling SaveUsers.
		// This hashing should typically happen where the UserAccount is created or password is set,
		// e.g., in your CreateUserDialog or a password change dialog.
		// Example: newUser.PasswordHash = UserManager.HashPassword(plainTextPassword);
		foreach ( var user in Users )
		{
			string userSpecificPath = Path.Combine( UsersParentRegistryPath, user.UserName );
			// SetValue will create the key if it doesn't exist, or update values if it does.
			Registry.Instance.SetValue( userSpecificPath, PasswordHashValueName, user.PasswordHash ); // Should be already hashed
			Registry.Instance.SetValue( userSpecificPath, ProfilePathValueName, user.ProfilePath );
			Registry.Instance.SetValue( userSpecificPath, RegistryHivePathValueName, user.RegistryHivePath );
			Log.Info( $"Saved/Updated user in SAM: {user.UserName}" );
		}

		// Attempt to delete the old users.json file if it exists, as it's now obsolete.
		string oldJsonPath = @"C:\Windows\users.json";
		if ( VirtualFileSystem.Instance.FileExists( oldJsonPath ) )
		{
			try
			{
				VirtualFileSystem.Instance.DeleteFile( oldJsonPath );
				Log.Info( $"Old '{oldJsonPath}' deleted as user data has been migrated to the SAM registry hive." );
			}
			catch ( Exception ex )
			{
				Log.Warning( $"Could not delete old '{oldJsonPath}': {ex.Message}" );
			}
		}
	}

	public bool Login( string username, string password )
	{
		var user = Users.FirstOrDefault( u => u.UserName.Equals( username, System.StringComparison.OrdinalIgnoreCase ) );
		if ( user == null )
		{
			return false;
		}

		// If the stored hash is empty, it means no password is set for the user.
		// In this case, only an empty input password should succeed.
		if ( string.IsNullOrEmpty( user.PasswordHash ) )
		{
			if ( string.IsNullOrEmpty( password ) )
			{
				CurrentUser = user;
				return true;
			}
			return false;
		}

		// If there's a stored hash, hash the input password and compare.
		string inputPasswordHash = HashPassword( password );
		if ( user.PasswordHash.Equals( inputPasswordHash, StringComparison.OrdinalIgnoreCase ) )
		{
			CurrentUser = user;
			return true;
		}

		return false;
	}

	/// <summary>
	/// Sets up the user's profile folders asynchronously. This allows a UI 
	/// to show a "Setting up personalized settings..." message while this method executes.
	/// </summary>
	public async Task SetupUserProfile( UserAccount user )
	{
		PersonalisedSettingsDialog personalizedDialog = new PersonalisedSettingsDialog();
		XGUISystem.Instance.Panel.AddChild( personalizedDialog );
		await Task.Delay( 50 ); // Give the dialog a moment to render

		var vfs = VirtualFileSystem.Instance;
		string effectiveProfilePath;

		if ( FakeOSLoader.UserSystemEnabled && user != null )
		{
			personalizedDialog.UpdateStatus( $"Loading personal settings for {user.UserName}..." );
			await Task.Delay( 200 ); // Simulate work

			if ( string.IsNullOrEmpty( user.ProfilePath ) )
			{
				Log.Error( $"User '{user.UserName}' has no ProfilePath defined. Cannot setup profile." );
				personalizedDialog.Complete();
				return;
			}
			effectiveProfilePath = user.ProfilePath;

			if ( !vfs.DirectoryExists( effectiveProfilePath ) )
				vfs.CreateDirectory( effectiveProfilePath );
			await Task.Yield();

			personalizedDialog.UpdateStatus( "Applying your personal settings..." );
			await Task.Delay( 200 );

			string myDocs = Path.Combine( effectiveProfilePath, "My Documents" );
			if ( !vfs.DirectoryExists( myDocs ) )
			{
				vfs.CreateDirectory( myDocs );
				vfs.WriteAllText( Path.Combine( myDocs, "desktop.ini" ),
					"[.XGUIInfo]\nIcon=mydocuments\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,3\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=3" );
			}
			await Task.Yield();

			string favouritesDir = Path.Combine( effectiveProfilePath, "Favorites" );
			if ( !vfs.DirectoryExists( favouritesDir ) )
			{
				vfs.CreateDirectory( favouritesDir );
				vfs.WriteAllText( Path.Combine( favouritesDir, "desktop.ini" ),
					"[.XGUIInfo]\nIcon=favourites\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,2\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=2" );
			}
			await Task.Yield();

			await CreateStandardUserFolders( effectiveProfilePath, vfs, personalizedDialog );
			await SetupStartMenuItems( user, personalizedDialog );
			await SetupQuickLaunch( user, personalizedDialog );
			await SetupDesktopItems( user, personalizedDialog );
			await SetupUserConfigInRegistry( user, personalizedDialog );
		}
		else // System-wide setup (no specific user, or user system disabled)
		{
			personalizedDialog.UpdateStatus( "Applying system settings..." );
			await Task.Delay( 200 );
			effectiveProfilePath = UserProfileHelper.GetProfilePath(); // Default user or all users path
			if ( !vfs.DirectoryExists( effectiveProfilePath ) )
				vfs.CreateDirectory( effectiveProfilePath );
			await Task.Yield();

			string myDocsGlobal = UserProfileHelper.GetMyDocumentsPath();
			if ( !vfs.DirectoryExists( myDocsGlobal ) )
			{
				vfs.CreateDirectory( myDocsGlobal );
				vfs.WriteAllText( Path.Combine( myDocsGlobal, "desktop.ini" ),
					"[.XGUIInfo]\nIcon=mydocuments\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,3\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=3" );
			}
			await Task.Yield();

			string favouritesGlobal = UserProfileHelper.GetFavoritesPath();
			if ( !vfs.DirectoryExists( favouritesGlobal ) )
			{
				vfs.CreateDirectory( favouritesGlobal );
				vfs.WriteAllText( Path.Combine( favouritesGlobal, "desktop.ini" ),
					"[.XGUIInfo]\nIcon=favourites\n\n[.ShellClassInfo]\nIconResource=C:\\WINDOWS\\system32\\shell32.dll,2\nIconFile=C:\\WINDOWS\\system32\\shell32.dll\nIconIndex=2" );
			}
			await Task.Yield();

			var globalUserAccountRepresentation = new UserAccount { ProfilePath = effectiveProfilePath, UserName = "_GlobalSystem_" };
			await CreateStandardUserFolders( effectiveProfilePath, vfs, personalizedDialog );
			await SetupStartMenuItems( globalUserAccountRepresentation, personalizedDialog );
			await SetupQuickLaunch( globalUserAccountRepresentation, personalizedDialog );
			await SetupDesktopItems( globalUserAccountRepresentation, personalizedDialog );
			await SetupUserConfigInRegistry( globalUserAccountRepresentation, personalizedDialog );
		}

		personalizedDialog.UpdateStatus( "Finalizing settings..." );
		await Task.Delay( 300 ); // Simulate final steps
		personalizedDialog.Complete();
	}

	private async Task CreateStandardUserFolders( string baseProfilePath, IVirtualFileSystem vfs, PersonalisedSettingsDialog dialog )
	{
		if ( string.IsNullOrEmpty( baseProfilePath ) )
		{
			Log.Warning( "CreateStandardUserFolders: baseProfilePath is null or empty. Skipping folder creation." );
			dialog.UpdateStatus( "Error: Profile path missing for folder creation." );
			await Task.Delay( 1000 ); // Give user time to see error
			return;
		}
		dialog.UpdateStatus( "Creating application data folders..." );
		await Task.Delay( 50 );
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Application Data" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Application Data", "Microsoft" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Application Data", "Microsoft", "Internet Explorer" ) ); await Task.Yield();

		dialog.UpdateStatus( "Setting up user environment folders..." );
		await Task.Delay( 50 );
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Cookies" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Desktop" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Favorites" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "History" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Local Settings" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Local Settings", "Application Data" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Local Settings", "Temp" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Local Settings", "Temporary Internet Files" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "My Documents" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "NetHood" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "PrintHood" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Recent" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "SendTo" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Start Menu" ) ); await Task.Yield();
		vfs.CreateDirectory( Path.Combine( baseProfilePath, "Templates" ) ); await Task.Yield();
	}

	private async Task SetupStartMenuItems( UserAccount user, PersonalisedSettingsDialog dialog )
	{
		if ( string.IsNullOrEmpty( user?.ProfilePath ) )
		{
			Log.Warning( $"SetupStartMenuItems: User '{user?.UserName ?? "Unknown"}' ProfilePath is null or empty. Skipping." );
			return;
		}
		dialog.UpdateStatus( "Setting up Start Menu items..." );
		await Task.Delay( 100 );

		string startMenuRoot = Path.Combine( user.ProfilePath, "Start Menu" );
		VirtualFileSystem.Instance.CreateDirectory( startMenuRoot );
		string programsDir = Path.Combine( startMenuRoot, "Programs" );
		VirtualFileSystem.Instance.CreateDirectory( programsDir );
		await Task.Yield();

		CreateShortcut( Path.Combine( programsDir, "Internet Explorer.lnk" ), "C:/Program Files/Internet Explorer/iexplore.exe" );
		// CreateShortcut( Path.Combine( programsDir, "Outlook Express.lnk" ), "C:/Program Files/Outlook Express/outlook.exe" ); 
		CreateShortcut( Path.Combine( programsDir, "Windows Explorer.lnk" ), "C:/Windows/explorer.exe", "explore" );
		CreateShortcut( Path.Combine( programsDir, "Command Prompt.lnk" ), "C:/Windows/System32/cmd.exe" );
		CreateShortcut( Path.Combine( programsDir, "Registry Editor.lnk" ), "C:/Windows/regedit.exe" );
		await Task.Yield();

		string accessoriesDir = Path.Combine( programsDir, "Accessories" );
		VirtualFileSystem.Instance.CreateDirectory( accessoriesDir );
		await Task.Yield();
		CreateShortcut( Path.Combine( accessoriesDir, "Notepad.lnk" ), "C:/Windows/notepad.exe" );
		CreateShortcut( Path.Combine( accessoriesDir, "Paint.lnk" ), "C:/Windows/mspaint.exe" );
		CreateShortcut( Path.Combine( accessoriesDir, "Calculator.lnk" ), "C:/Windows/calc.exe" );
		CreateShortcut( Path.Combine( accessoriesDir, "Task Manager.lnk" ), "C:/Windows/System32/taskmgr.exe" );
		await Task.Yield();

		string gamesDir = Path.Combine( accessoriesDir, "Games" );
		VirtualFileSystem.Instance.CreateDirectory( gamesDir );
		await Task.Yield();
		// CreateShortcut( Path.Combine( gamesDir, "Minesweeper.lnk" ), "C:/Windows/System32/winmine.exe" ); 

		string startupDir = Path.Combine( startMenuRoot, "StartUp" );
		VirtualFileSystem.Instance.CreateDirectory( startupDir );
		await Task.Yield();
	}
	private async Task SetupQuickLaunch( UserAccount user, PersonalisedSettingsDialog dialog )
	{
		if ( string.IsNullOrEmpty( user?.ProfilePath ) )
		{
			Log.Warning( $"SetupQuickLaunch: User '{user?.UserName ?? "Unknown"}' ProfilePath is null or empty. Skipping." );
			return;
		}
		dialog.UpdateStatus( "Configuring Quick Launch bar..." );
		await Task.Delay( 50 );
		string quickLaunchDir = Path.Combine( user.ProfilePath, "Application Data", "Microsoft", "Internet Explorer", "Quick Launch" );
		VirtualFileSystem.Instance.CreateDirectory( quickLaunchDir );
		await Task.Yield();
		CreateShortcut( Path.Combine( quickLaunchDir, "Show Desktop.scf" ), "[Shell]\nCommand=2\nIconFile=explorer.exe,3\n[Taskbar]\nCommand=ToggleDesktop", "Show Desktop" ); // .scf content for Show Desktop
		CreateShortcut( Path.Combine( quickLaunchDir, "Internet Explorer.lnk" ), "C:/Program Files/Internet Explorer/Iexplore.exe" );
		CreateShortcut( Path.Combine( quickLaunchDir, "Outlook Express.lnk" ), "C:/Program Files/Outlook Express/outlook.exe" );
		await Task.Yield();
	}

	public async Task SetupDesktopItems( UserAccount user, PersonalisedSettingsDialog dialog )
	{
		if ( string.IsNullOrEmpty( user?.ProfilePath ) )
		{
			Log.Warning( $"SetupDesktopItems: User '{user?.UserName ?? "Unknown"}' ProfilePath is null or empty. Skipping." );
			return;
		}
		dialog.UpdateStatus( "Preparing your Desktop..." );
		await Task.Delay( 100 );
		string desktopDir = Path.Combine( user.ProfilePath, "Desktop" );
		VirtualFileSystem.Instance.CreateDirectory( desktopDir );
		await Task.Yield();
		// Standard desktop icons (My Computer, Recycle Bin) are typically special shell items, not .lnk files. we dont make those here!!
		VirtualFileSystem.Instance.CreateDirectory( Path.Combine( desktopDir, "Online Services" ) );
		CreateShortcut( Path.Combine( desktopDir, "Outlook Express.lnk" ), "C:/Program Files/Outlook Express/outlook.exe" ); await Task.Yield();
		CreateShortcut( Path.Combine( desktopDir, "Doom 95.lnk" ), "C:/Program Files/Ultimate Doom for Windows 95/doom95.exe" ); await Task.Yield();
	}

	public async Task SetupUserConfigInRegistry( UserAccount user, PersonalisedSettingsDialog dialog )
	{
		// Creates all default settings for the user in the registry
		if ( user == null || string.IsNullOrEmpty( user.UserName ) )
		{
			Log.Warning( "SetupUserConfigInRegistry: User is null or has no UserName. Skipping." );
			return;
		}
		dialog.UpdateStatus( $"Configuring user settings for {user.UserName}..." );
		await Task.Delay( 20 );

		// -- Control Panel settings (HKEY_CURRENT_USER\Control Panel) --
		// Setup default theme (HKEY_CURRENT_USER\Control Panel\Desktop, value CurrentThemeFile)
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Control Panel\Desktop", "CurrentThemeFile", "/XGUI/DefaultStyles/Computer95.scss" );
		// Setup default wallpaper (HKEY_CURRENT_USER\Control Panel\Desktop, value Wallpaper)
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", "" );
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Control Panel\Desktop", "WallpaperStyle", 0 ); // 0 = Centered, 1 = Tiled, 2 = Stretched
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Control Panel\Desktop", "TileWallpaper", 0 ); // 0 = No tiling, 1 = Tiled wallpaper

		// -- Shell Folders (HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders) --
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Desktop", Path.Combine( user.ProfilePath, "Desktop" ) );
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Personal", Path.Combine( user.ProfilePath, "My Documents" ) );
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Favorites", Path.Combine( user.ProfilePath, "Favorites" ) );
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "Start Menu", Path.Combine( user.ProfilePath, "Start Menu" ) );
		Registry.Instance.SetValue( $@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "AppData", Path.Combine( user.ProfilePath, "Application Data" ) );
	}

	private static void CreateShortcut( string shortcutPath, string targetPath, string iconName = "",
									 string arguments = "", string workingDir = "" )
	{
		// For .scf files, the targetPath is actually the content.
		if ( shortcutPath.EndsWith( ".scf", StringComparison.OrdinalIgnoreCase ) )
		{
			VirtualFileSystem.Instance.WriteAllText( shortcutPath, targetPath );
		}
		else // For .lnk files
		{
			var shortcut = new ShortcutDescriptor(
				targetPath,
				string.IsNullOrEmpty( workingDir ) ? Path.GetDirectoryName( targetPath ) : workingDir,
				arguments,
				iconName
			);
			VirtualFileSystem.Instance.WriteAllText(
				shortcutPath,
				shortcut.ToFileContent()
			);
		}
	}
}
