using FakeOperatingSystem.OSFileSystem;
using System;
using System.Collections.Generic;
using System.Globalization; // Required for parsing numbers robustly
using System.Linq; // Required for Enumerable.Empty and .ToList()
using System.Text.Json;

namespace FakeOperatingSystem;

public class RegistryKey
{
	// Initialize with a case-insensitive comparer for SubKeys
	public Dictionary<string, RegistryKey> SubKeys { get; set; } = new( StringComparer.OrdinalIgnoreCase );

	// Initialize with a case-insensitive comparer for Values
	public Dictionary<string, object> Values { get; set; } = new( StringComparer.OrdinalIgnoreCase );
}

public class RegistryHive
{
	public string Name { get; }
	public string FilePath { get; } // Can be a placeholder for "virtual" hives like HKLM/HKU containers
	public RegistryKey Root { get; internal set; } = new(); // Made internal set for constructor flexibility

	// Constructor for hives backed by a single file
	public RegistryHive( string name, string filePath )
	{
		Name = name;
		FilePath = filePath;
		Load();
	}

	// Constructor for "virtual" hives or those populated manually (like HKLM/HKU containers)
	public RegistryHive( string name, RegistryKey rootKey, string placeholderPath = null )
	{
		Name = name;
		Root = rootKey;
		FilePath = placeholderPath; // May not be used for saving if it's just a container
	}

	public void Load()
	{
		if ( string.IsNullOrEmpty( FilePath ) ) // Don't load if no file path (e.g. virtual HKLM root)
		{
			Root = new RegistryKey(); // Ensure it has a root
			return;
		}

		if ( VirtualFileSystem.Instance.FileExists( FilePath ) )
		{
			var json = VirtualFileSystem.Instance.ReadAllText( FilePath );
			Root = JsonSerializer.Deserialize<RegistryKey>( json ) ?? new RegistryKey();
		}
		else
		{
			Root = new RegistryKey();
			Save(); // Save if the file was expected but missing
		}
	}

	public void Save()
	{
		if ( string.IsNullOrEmpty( FilePath ) ) // Don't save if no file path
			return;

		// if path doesn't exist, create the directory structure
		var directory = System.IO.Path.GetDirectoryName( FilePath );
		if ( !string.IsNullOrEmpty( directory ) && !VirtualFileSystem.Instance.DirectoryExists( directory ) )
		{
			VirtualFileSystem.Instance.CreateDirectory( directory );
		}

		var json = JsonSerializer.Serialize( Root, new JsonSerializerOptions { WriteIndented = true } );
		VirtualFileSystem.Instance.WriteAllText( FilePath, json );
	}
}

public class Registry
{
	private Dictionary<string, RegistryHive> _hives = new( StringComparer.OrdinalIgnoreCase );
	// Stores mapping from full component key path (e.g., "HKEY_LOCAL_MACHINE\SYSTEM") to its file path.
	private Dictionary<string, string> _componentHiveFilePaths = new( StringComparer.OrdinalIgnoreCase );
	public static Registry Instance { get; private set; }
	public IEnumerable<string> RootHiveNames => _hives.Keys;
	public IEnumerable<RegistryHive> RootHives => _hives.Values;

	public Registry()
	{
		Instance = this;

		// --- HKEY_CLASSES_ROOT ---
		_hives["HKEY_CLASSES_ROOT"] = new RegistryHive( "HKEY_CLASSES_ROOT", @"C:\Windows\System32\config\CLASSES.DAT" );

		// --- HKEY_LOCAL_MACHINE ---
		var hklmRootKey = new RegistryKey();
		var hklmHive = new RegistryHive( "HKEY_LOCAL_MACHINE", hklmRootKey, @"C:\Windows\System32\config\HKLM_VIRTUAL.hive" );
		_hives["HKEY_LOCAL_MACHINE"] = hklmHive;

		string systemFilePath = @"C:\Windows\System32\config\SYSTEM";
		RegistryKey systemKey = LoadKeyFromFile( systemFilePath );
		if ( systemKey != null )
		{
			hklmRootKey.SubKeys["SYSTEM"] = systemKey;
			_componentHiveFilePaths[$"{hklmHive.Name}\\SYSTEM"] = systemFilePath;
		}

		string softwareFilePath = @"C:\Windows\System32\config\SOFTWARE";
		RegistryKey softwareKey = LoadKeyFromFile( softwareFilePath );
		if ( softwareKey != null )
		{
			hklmRootKey.SubKeys["SOFTWARE"] = softwareKey;
			_componentHiveFilePaths[$"{hklmHive.Name}\\SOFTWARE"] = softwareFilePath;
		}

		string samFilePath = @"C:\Windows\System32\config\SAM";
		RegistryKey samKey = LoadKeyFromFile( samFilePath );
		if ( samKey != null )
		{
			hklmRootKey.SubKeys["SAM"] = samKey;
			_componentHiveFilePaths[$"{hklmHive.Name}\\SAM"] = samFilePath;
		}

		string networkFilePath = @"C:\Windows\System32\config\NETWORK";
		RegistryKey networkKey = LoadKeyFromFile( networkFilePath );
		if ( networkKey != null )
		{
			hklmRootKey.SubKeys["NETWORK"] = networkKey;
			_componentHiveFilePaths[$"{hklmHive.Name}\\NETWORK"] = networkFilePath;
		}
		// Add SECURITY, HARDWARE similarly if you have files for them and update _componentHiveFilePaths.

		// --- HKEY_USERS ---
		var hkuRootKey = new RegistryKey();
		var hkuHive = new RegistryHive( "HKEY_USERS", hkuRootKey, @"C:\Windows\System32\config\HKU_VIRTUAL.hive" );
		_hives["HKEY_USERS"] = hkuHive;

		string defaultUserFilePath = @"C:\Windows\System32\config\DEFAULT";
		RegistryKey defaultUserKey = LoadKeyFromFile( defaultUserFilePath );
		if ( defaultUserKey != null )
		{
			hkuRootKey.SubKeys[".DEFAULT"] = defaultUserKey;
			_componentHiveFilePaths[$"{hkuHive.Name}\\.DEFAULT"] = defaultUserFilePath;
		}

		// --- HKEY_CURRENT_USER ---
		_hives["HKEY_CURRENT_USER"] = new RegistryHive( "HKEY_CURRENT_USER", @"C:\Windows\USER.DAT" );

		// --- HKEY_CURRENT_CONFIG ---
		_hives["HKEY_CURRENT_CONFIG"] = new RegistryHive( "HKEY_CURRENT_CONFIG", @"C:\Windows\System32\config\CONFIG" );
	}

	private RegistryKey LoadKeyFromFile( string filePath )
	{
		if ( VirtualFileSystem.Instance.FileExists( filePath ) )
		{
			var json = VirtualFileSystem.Instance.ReadAllText( filePath );
			return JsonSerializer.Deserialize<RegistryKey>( json ) ?? new RegistryKey();
		}
		else
		{
			var newKey = new RegistryKey();
			SaveKeyToFile( newKey, filePath ); // Use SaveKeyToFile to create it
			Log.Info( $"Registry: Created default empty key file at {filePath}" );
			return newKey;
		}
	}

	// Helper to save a specific RegistryKey structure to a file.
	private void SaveKeyToFile( RegistryKey key, string filePath )
	{
		if ( string.IsNullOrEmpty( filePath ) || key == null )
		{
			Log.Warning( $"Registry: SaveKeyToFile called with invalid arguments. FilePath: '{filePath}'" );
			return;
		}
		var json = JsonSerializer.Serialize( key, new JsonSerializerOptions { WriteIndented = true } );
		VirtualFileSystem.Instance.WriteAllText( filePath, json );
	}

	public void AddHive( string rootHiveName, string filePath )
	{
		if ( _hives.ContainsKey( rootHiveName ) )
		{
			Log.Warning( $"Registry: Hive '{rootHiveName}' already exists. Overwriting." );
		}
		_hives[rootHiveName] = new RegistryHive( rootHiveName, filePath );
		// Note: If this new hive could contain component files, _componentHiveFilePaths might need updating.
		// For now, assuming AddHive is for simple, single-file hives.
	}

	public void LoadUserHive( string userName, string userHiveFilePath )
	{
		_hives["HKEY_CURRENT_USER"] = new RegistryHive( "HKEY_CURRENT_USER", userHiveFilePath );

		if ( _hives.TryGetValue( "HKEY_USERS", out RegistryHive hkuHive ) )
		{
			RegistryKey userSpecificDataRoot = LoadKeyFromFile( userHiveFilePath );
			if ( userSpecificDataRoot != null )
			{
				hkuHive.Root.SubKeys[userName] = userSpecificDataRoot; // User SID or Name as key
				_componentHiveFilePaths[$"{hkuHive.Name}\\{userName}"] = userHiveFilePath;
			}
		}
	}

	// Made public for UserManager
	public (RegistryHive hive, string subPath) ResolvePathToHiveAndSubpath( string keyPath ) => Resolve( keyPath );
	// Made public for UserManager
	public RegistryKey GetRegistryKey( RegistryKey root, string keyPath, bool create ) => GetKey( root, keyPath, create );


	private (RegistryHive hive, string subPath) Resolve( string keyPath )
	{
		RegistryHive matchedHive = null;
		string longestMatch = "";

		foreach ( var hiveNameInDict in _hives.Keys )
		{
			if ( keyPath.StartsWith( hiveNameInDict, StringComparison.OrdinalIgnoreCase ) )
			{
				if ( keyPath.Length == hiveNameInDict.Length || (keyPath.Length > hiveNameInDict.Length && keyPath[hiveNameInDict.Length] == '\\') )
				{
					if ( hiveNameInDict.Length > longestMatch.Length )
					{
						longestMatch = hiveNameInDict;
						matchedHive = _hives[hiveNameInDict];
					}
				}
			}
		}

		if ( matchedHive != null )
		{
			var subPath = keyPath.Length > longestMatch.Length
				? keyPath.Substring( longestMatch.Length ).TrimStart( '\\' )
				: "";
			return (matchedHive, subPath);
		}
		throw new Exception( "Hive not found for key: " + keyPath );
	}

	public T GetValue<T>( string keyPath, string valueName, T defaultValue = default )
	{
		var (hive, subPath) = Resolve( keyPath );
		var key = GetKey( hive.Root, subPath, false );
		if ( key != null && key.Values.TryGetValue( valueName, out var valueObj ) )
		{
			if ( Nullable.GetUnderlyingType( typeof( T ) ) != null && valueObj == null )
				return defaultValue;
			if ( valueObj is T typedValue )
				return typedValue;
			if ( valueObj is JsonElement jsonElement )
			{
				if ( Nullable.GetUnderlyingType( typeof( T ) ) != null && jsonElement.ValueKind == JsonValueKind.Null )
					return defaultValue;
				try
				{
					if ( typeof( T ) == typeof( int ) && jsonElement.TryGetInt32( out int intVal ) ) return (T)(object)intVal;
					if ( typeof( T ) == typeof( int? ) && jsonElement.TryGetInt32( out int intNullableVal ) ) return (T)(object)intNullableVal;
					if ( typeof( T ) == typeof( long ) && jsonElement.TryGetInt64( out long longVal ) ) return (T)(object)longVal;
					if ( typeof( T ) == typeof( long? ) && jsonElement.TryGetInt64( out long longNullableVal ) ) return (T)(object)longNullableVal;
					if ( typeof( T ) == typeof( string ) ) return (T)(object)jsonElement.GetString();
					if ( typeof( T ) == typeof( bool ) && (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False) ) return (T)(object)jsonElement.GetBoolean();
					if ( typeof( T ) == typeof( bool? ) && (jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False) ) return (T)(object)jsonElement.GetBoolean();
					if ( typeof( T ) == typeof( float ) && jsonElement.TryGetSingle( out float floatVal ) ) return (T)(object)floatVal;
					if ( typeof( T ) == typeof( float? ) && jsonElement.TryGetSingle( out float floatNullableVal ) ) return (T)(object)floatNullableVal;
					if ( typeof( T ) == typeof( double ) && jsonElement.TryGetDouble( out double doubleVal ) ) return (T)(object)doubleVal;
					if ( typeof( T ) == typeof( double? ) && jsonElement.TryGetDouble( out double doubleNullableVal ) ) return (T)(object)doubleNullableVal;
					if ( typeof( T ) == typeof( decimal ) && jsonElement.TryGetDecimal( out decimal decimalVal ) ) return (T)(object)decimalVal;
					if ( typeof( T ) == typeof( decimal? ) && jsonElement.TryGetDecimal( out decimal decimalNullableVal ) ) return (T)(object)decimalNullableVal;
					if ( typeof( T ) == typeof( DateTime ) && jsonElement.TryGetDateTime( out DateTime dateTimeVal ) ) return (T)(object)dateTimeVal;
					if ( typeof( T ) == typeof( DateTime? ) && jsonElement.TryGetDateTime( out DateTime dateTimeNullableVal ) ) return (T)(object)dateTimeNullableVal;
					if ( typeof( T ) == typeof( Guid ) && jsonElement.TryGetGuid( out Guid guidVal ) ) return (T)(object)guidVal;
					if ( typeof( T ) == typeof( Guid? ) && jsonElement.TryGetGuid( out Guid guidNullableVal ) ) return (T)(object)guidNullableVal;
					return JsonSerializer.Deserialize<T>( jsonElement.GetRawText() );
				}
				catch ( Exception ex )
				{
					Log.Warning( $"[JsonRegistry] Error converting JsonElement to {typeof( T )} for {keyPath}\\{valueName}: {ex.Message}" );
					return defaultValue;
				}
			}
			try
			{
				return (T)Convert.ChangeType( valueObj, Nullable.GetUnderlyingType( typeof( T ) ) ?? typeof( T ), CultureInfo.InvariantCulture );
			}
			catch { /* Conversion failed */ }
		}
		return defaultValue;
	}

	public void SetValue( string keyPath, string valueName, object value )
	{
		var (hive, subPath) = Resolve( keyPath );
		var key = GetKey( hive.Root, subPath, true ); // Ensure key is created if it doesn't exist
		key.Values[valueName] = value;

		if ( hive.FilePath != null && !hive.FilePath.Contains( "_VIRTUAL." ) )
		{
			hive.Save(); // Standard hive, save the whole hive file (e.g., HKCU's USER.DAT)
		}
		else // Virtual hive (HKLM, HKU) - need to save the specific component file
		{
			var parts = subPath.Split( new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries );
			if ( parts.Length > 0 )
			{
				string componentKeyName = parts[0]; // e.g., "SYSTEM", "SOFTWARE", ".DEFAULT", or a username, or "SAM"
				string fullComponentPath = $"{hive.Name}\\{componentKeyName}";

				if ( _componentHiveFilePaths.TryGetValue( fullComponentPath, out string componentFilePath ) )
				{
					if ( hive.Root.SubKeys.TryGetValue( componentKeyName, out RegistryKey componentRootKey ) )
					{
						SaveKeyToFile( componentRootKey, componentFilePath );
					}
					else
					{
						Log.Error( $"Registry: Component key '{componentKeyName}' not found in hive '{hive.Name}' for saving. Path: {keyPath}" );
					}
				}
				else
				{
					Log.Warning( $"Registry: SetValue on virtual hive '{hive.Name}'. File path for component '{componentKeyName}' not found. Path: {keyPath}" );
				}
			}
			else
			{
				// This means keyPath was just "HKEY_LOCAL_MACHINE" or "HKEY_USERS" itself.
				// Setting a value directly on the virtual root doesn't map to a single component file.
				Log.Warning( $"Registry: SetValue directly on virtual hive root '{hive.Name}'. This change will not be persisted to a component file. Path: {keyPath}" );
			}
		}
	}

	public void DeleteValue( string keyPath, string valueName )
	{
		var (hive, subPath) = Resolve( keyPath );
		var key = GetKey( hive.Root, subPath, false );
		if ( key != null && key.Values.Remove( valueName ) )
		{
			if ( hive.FilePath != null && !hive.FilePath.Contains( "_VIRTUAL." ) )
			{
				hive.Save();
			}
			else // Virtual hive
			{
				var parts = subPath.Split( new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries );
				if ( parts.Length > 0 )
				{
					string componentKeyName = parts[0];
					string fullComponentPath = $"{hive.Name}\\{componentKeyName}";
					if ( _componentHiveFilePaths.TryGetValue( fullComponentPath, out string componentFilePath ) )
					{
						if ( hive.Root.SubKeys.TryGetValue( componentKeyName, out RegistryKey componentRootKey ) )
						{
							SaveKeyToFile( componentRootKey, componentFilePath );
						}
						else
						{
							Log.Error( $"Registry: Component key '{componentKeyName}' not found in hive '{hive.Name}' for saving after DeleteValue. Path: {keyPath}" );
						}
					}
					else
					{
						Log.Warning( $"Registry: DeleteValue on virtual hive '{hive.Name}'. File path for component '{componentKeyName}' not found. Path: {keyPath}" );
					}
				}
				else
				{
					Log.Warning( $"Registry: DeleteValue directly on virtual hive root '{hive.Name}'. This change will not be persisted. Path: {keyPath}" );
				}
			}
		}
	}

	public void DeleteKey( string keyPath, bool recursive = true ) // Added recursive, though current logic implies it
	{
		var (hive, subPath) = Resolve( keyPath );
		var parts = subPath.Split( '\\', StringSplitOptions.RemoveEmptyEntries );

		if ( string.IsNullOrEmpty( subPath ) ) // Attempting to delete a hive root
		{
			if ( hive.FilePath != null && !hive.FilePath.Contains( "_VIRTUAL." ) )
			{
				// Deleting a file-backed hive root (e.g., HKCU). Clear its contents.
				Log.Warning( $"Registry: Attempted to delete root of file-backed hive '{keyPath}'. Clearing root and saving." );
				hive.Root.SubKeys.Clear();
				hive.Root.Values.Clear();
				hive.Save();
			}
			else
			{
				// Attempting to delete a virtual hive root (e.g., HKLM, HKU). This is generally not allowed/supported.
				Log.Error( $"Registry: Deleting the root of a virtual hive ('{keyPath}') is not supported." );
			}
			return;
		}

		// Deleting a subkey or a component key
		string keyNameToRemove = parts[^1];
		string parentSubPath = string.Join( '\\', parts[..^1] );
		var parentKey = GetKey( hive.Root, parentSubPath, false );

		if ( parentKey != null && parentKey.SubKeys.Remove( keyNameToRemove ) )
		{
			if ( hive.FilePath != null && !hive.FilePath.Contains( "_VIRTUAL." ) )
			{
				hive.Save(); // Standard hive, save the whole hive file
			}
			else // Virtual hive (HKLM, HKU)
			{
				// Check if we deleted a top-level component key (e.g., HKLM\SYSTEM)
				// In this case, parentKey is hive.Root, and parts.Length was 1 (subPath was "SYSTEM")
				if ( parentKey == hive.Root && parts.Length == 1 )
				{
					string componentKeyName = keyNameToRemove; // This is the component key, e.g., "SYSTEM"
					string fullComponentPath = $"{hive.Name}\\{componentKeyName}";
					if ( _componentHiveFilePaths.TryGetValue( fullComponentPath, out string componentFilePath ) )
					{
						// If the component file itself should be deleted when the key HKLM\SAM is deleted.
						// VirtualFileSystem.Instance.DeleteFile( componentFilePath );
						// _componentHiveFilePaths.Remove( fullComponentPath );
						// Log.Info( $"Registry: Deleted component key '{componentKeyName}' from '{hive.Name}' and its backing file '{componentFilePath}'." );
						// For now, just save the parent (HKLM_VIRTUAL.hive) which reflects the removal of the SAM subkey from its structure.
						// The actual SAM file would still exist unless explicitly deleted.
						// However, our model is that HKLM is virtual and its children (SYSTEM, SAM) are the files.
						// So, if HKLM\SAM is deleted, the SAM file should be deleted.
						if ( VirtualFileSystem.Instance.FileExists( componentFilePath ) )
						{
							VirtualFileSystem.Instance.DeleteFile( componentFilePath );
							Log.Info( $"Registry: Deleted backing file '{componentFilePath}' for component key '{componentKeyName}'." );
						}
						_componentHiveFilePaths.Remove( fullComponentPath );

					}
					else
					{
						Log.Warning( $"Registry: Deleted component key '{componentKeyName}' from '{hive.Name}', but its backing file path was not found or not managed." );
					}
				}
				// Check if we deleted a subkey within a component (e.g., HKLM\SAM\SomeSubKey)
				// parts.Length will be > 0 because subPath was not empty.
				else if ( parts.Length > 0 )
				{
					string componentKeyName = parts[0]; // The top-level component, e.g., "SAM"
					string fullComponentPath = $"{hive.Name}\\{componentKeyName}";
					if ( _componentHiveFilePaths.TryGetValue( fullComponentPath, out string componentFilePath ) )
					{
						if ( hive.Root.SubKeys.TryGetValue( componentKeyName, out RegistryKey componentRootKey ) )
						{
							SaveKeyToFile( componentRootKey, componentFilePath );
						}
						else
						{
							// This implies the component key itself was removed by a previous operation but not its file,
							// or an inconsistent state.
							Log.Error( $"Registry: Component key '{componentKeyName}' (expected to contain deleted subkey) not found in hive '{hive.Name}' for saving after DeleteKey. Path: {keyPath}" );
						}
					}
					else
					{
						Log.Warning( $"Registry: DeleteKey on virtual hive '{hive.Name}'. File path for component '{componentKeyName}' not found. Path: {keyPath}" );
					}
				}
			}
		}
	}

	private RegistryKey GetKey( RegistryKey root, string keyPath, bool create )
	{
		if ( string.IsNullOrEmpty( keyPath ) ) return root;
		var parts = keyPath.Split( '\\', StringSplitOptions.RemoveEmptyEntries );
		var current = root;
		foreach ( var part in parts )
		{
			if ( !current.SubKeys.TryGetValue( part, out var next ) )
			{
				if ( create )
				{
					next = new RegistryKey();
					current.SubKeys[part] = next;
				}
				else
				{
					return null;
				}
			}
			current = next;
		}
		return current;
	}

	public IEnumerable<string> GetSubKeyNames( string keyPath )
	{
		var (hive, subPath) = Resolve( keyPath );
		var key = GetKey( hive.Root, subPath, false ); // Do not create if not exists
		if ( key != null )
		{
			return key.SubKeys.Keys.ToList(); // Return a copy
		}
		return Enumerable.Empty<string>();
	}

	public bool KeyExists( string keyPath )
	{
		try
		{
			var (hive, subPath) = Resolve( keyPath );
			var key = GetKey( hive.Root, subPath, false );
			return key != null;
		}
		catch ( Exception ) // Catch "Hive not found" from Resolve
		{
			return false;
		}
	}

	/// <summary>
	/// Retrieves all value names and their data from a specified registry key.
	/// </summary>
	/// <param name="keyPath">The full path of the registry key.</param>
	/// <returns>A dictionary containing the value names and their data. Returns an empty dictionary if the key is not found or has no values.</returns>
	public IReadOnlyDictionary<string, object> GetValues( string keyPath )
	{
		try
		{
			var (hive, subPath) = Resolve( keyPath );
			var key = GetKey( hive.Root, subPath, false ); // Do not create if not exists

			if ( key != null )
			{
				// Return a read-only copy to prevent external modification of the internal dictionary
				return key.Values.ToDictionary( kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase );
			}
		}
		catch ( Exception ex ) // Catch "Hive not found" from Resolve or other potential issues
		{
			Log.Warning( $"Registry: Error getting values for key '{keyPath}': {ex.Message}" );
		}
		return new Dictionary<string, object>( StringComparer.OrdinalIgnoreCase ); // Return empty dictionary on failure or if key not found
	}
}
