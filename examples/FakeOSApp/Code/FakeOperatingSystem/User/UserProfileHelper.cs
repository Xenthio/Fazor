using System.IO; // Required for Path.Combine

namespace FakeOperatingSystem.User
{
	public static class UserProfileHelper
	{
		// Helper to safely get the current user.
		private static UserAccount CurrentUser => FakeOSLoader.Instance?.UserManager?.CurrentUser;

		// Helper to safely get the UserSystemEnabled flag.
		// Defaults to true if FakeOSLoader.Instance is not yet available,
		// though UserSystemEnabled should ideally be checked after FakeOSLoader is initialized.
		private static bool IsUserSystemEnabled => FakeOSLoader.Instance != null ? FakeOSLoader.UserSystemEnabled : true;

		/// <summary>
		/// Gets the root path for the current user's profile.
		/// If the user system is disabled or no user is logged in, returns a system-wide default.
		/// </summary>
		public static string GetProfilePath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				// Ensure the path from UserAccount is already normalized or normalize it here.
				return CurrentUser.ProfilePath.Replace( '\\', '/' );
			}
			// Fallback for when user system is disabled or no specific user context.
			// In a single-user Windows 9x like system, there isn't a "profile" root in the same way.
			// System-wide folders are often directly under C:\Windows.
			// For a generic "profile" in disabled mode, C:\ might be too broad.
			// Let's assume C:\Windows as a generic base if no user profile exists.
			return @"C:/Windows".Replace( '\\', '/' );
		}

		/// <summary>
		/// Gets the path to the "My Documents" folder for the current user.
		/// If the user system is disabled, returns a global "My Documents" path.
		/// </summary>
		public static string GetMyDocumentsPath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return Path.Combine( CurrentUser.ProfilePath, "My Documents" ).Replace( '\\', '/' );
			}
			// Global "My Documents" path when user system is disabled
			return @"C:/My Documents".Replace( '\\', '/' );
		}

		/// <summary>
		/// Gets the path to the Desktop folder.
		/// If the user system is enabled, it's within the user's profile.
		/// If disabled, it's a global Desktop path (e.g., C:\Windows\Desktop).
		/// </summary>
		public static string GetDesktopPath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return Path.Combine( CurrentUser.ProfilePath, "Desktop" ).Replace( '\\', '/' );
			}
			// Global Desktop path when user system is disabled
			return @"C:/Windows/Desktop".Replace( '\\', '/' );
		}

		/// <summary>
		/// Gets the path to the current user's registry hive file (e.g., NTUSER.DAT).
		/// If the user system is disabled, returns the path to the global/default user hive.
		/// </summary>
		public static string GetUserRegistryHivePath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return CurrentUser.RegistryHivePath.Replace( '\\', '/' );
			}
			// Path to the global/default user hive when user system is disabled
			// This should match the default HKEY_CURRENT_USER hive path in Registry.cs constructor
			return @"C:/Windows/USER.DAT".Replace( '\\', '/' );
		}

		// You can add more helpers here as needed, for example:
		public static string GetStartMenuPath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return Path.Combine( CurrentUser.ProfilePath, "Start Menu" ).Replace( '\\', '/' );
			}
			return Path.Combine( GetProfilePath(), "Start Menu" ).Replace( '\\', '/' ); // Uses global profile path if disabled
		}

		public static string GetQuickLaunchPath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return Path.Combine( CurrentUser.ProfilePath, "Application Data", "Microsoft", "Internet Explorer", "Quick Launch" ).Replace( '\\', '/' );
			}
			// For disabled user system, Quick Launch might be in a global "Application Data" or not exist by default.
			// Let's assume it would be under a global profile's Application Data.
			return Path.Combine( GetProfilePath(), "Application Data", "Microsoft", "Internet Explorer", "Quick Launch" ).Replace( '\\', '/' );
		}
		public static string GetFavoritesPath()
		{
			if ( IsUserSystemEnabled && CurrentUser != null )
			{
				return Path.Combine( CurrentUser.ProfilePath, "Favorites" ).Replace( '\\', '/' );
			}
			return Path.Combine( GetProfilePath(), "Favorites" ).Replace( '\\', '/' );
		}

		public static string GetRecycleBinPath()
		{
			// The Recycle Bin is more complex.
			// Windows NT+ uses a hidden folder on each drive ($Recycle.Bin) with subfolders per user SID.
			// Windows 9x used C:\RECYCLED.
			// For simplicity in a fake OS, you might have a single global one,
			// or a per-user one if you implement SIDs.
			// This example returns a system-wide path.
			return @"C:/Recycled".Replace( '\\', '/' );
		}
	}
}
