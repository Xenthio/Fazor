using FakeOperatingSystem;
using FakeOperatingSystem.OSFileSystem; // Required for IVirtualFileSystem
using FakeOperatingSystem.Setup;    // Required for OsSetup
using Sandbox;
using System.Threading.Tasks;

public static class FakeSystemRoot // Keep it static as an entry point
{
	/// <summary>
	/// Checks if setup is needed and runs it.
	/// This would typically be called by FakeOSLoader.
	/// </summary>
	public static async Task EnsureSystemRootExists( IVirtualFileSystem vfs, Registry registry )
	{
		// A simple check. In a real VFS, "C:/Windows" would map to your "FakeSystemRoot/Windows"
		if ( !vfs.DirectoryExists( "C:/Windows" ) )
		{
			Log.Info( "Performing initial FakeOS setup..." );
			var setup = new OSSetup( vfs, registry ); // Pass existing registry if available, OsSetup can create if null
			await setup.RunInitialSetup();
			Log.Info( "FakeOS setup complete." );
		}
		else
		{
			Log.Info( "FakeOS system root found." );
		}
	}

	// Console commands can remain here for now, but they'll use the new setup logic.
	// They might need access to the VFS and Registry instances from FakeOSLoader.
	// For simplicity, these might be better invoked after FakeOSLoader has initialized VFS and Registry.

	[ConCmd( "xguitest_force_recreate_system_root" )]
	public static async void ForceRecreateSystemRootCommand()
	{
		if ( VirtualFileSystem.Instance == null )
		{
			Log.Error( "Cannot force recreate system root: VirtualFileSystem not initialized." );
			return;
		}
		// This command implies deleting the VFS content and re-running setup.
		// Deleting "C:/" in VFS should clear the mapped "FakeSystemRoot" in FileSystem.Data
		Log.Info( "Forcing system root recreation..." );
		VirtualFileSystem.Instance.DeleteDirectory( "C:/", true ); // Example: Deletes all under C:/ in VFS

		// Re-run setup. This assumes Registry.Instance might also need to be reset or re-initialized.
		// If Registry holds onto old hive objects, it might need a ClearHives() or similar method.
		// Or, FakeOSLoader could re-initialize both VFS and Registry before calling this.
		var setup = new OSSetup( VirtualFileSystem.Instance, null ); // Pass null for registry to re-initialize
		await setup.RunInitialSetup();
		Log.Info( "System root recreation complete." );
	}

	[ConCmd( "xguitest_delete_system_root" )]
	public static void DeleteSystemRootCommand()
	{
		if ( VirtualFileSystem.Instance == null )
		{
			Log.Error( "Cannot delete system root: VirtualFileSystem not initialized." );
			return;
		}
		Log.Info( "Deleting system root via VFS C:/ ..." );
		VirtualFileSystem.Instance.DeleteDirectory( "C:/", true );
		Log.Info( "System root deleted. FakeOS will likely be non-functional until next setup/reboot." );
	}
}
