using System.Collections.Generic;
using System.Text.RegularExpressions; // For expanding variables

namespace FakeOperatingSystem
{
	public static class EnvironmentManager
	{
		// Define standard registry paths for environment variables
		private const string SystemVariablesPath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Environment";
		private const string UserVariablesPath = @"HKEY_CURRENT_USER\Environment"; // This path is relative to the loaded user's hive

		/// <summary>
		/// Gets the value of an environment variable for the current user context.
		/// It checks user-specific variables first, then system-wide variables.
		/// </summary>
		/// <param name="variableName">The name of the environment variable.</param>
		/// <returns>The value of the environment variable, or null if not found.</returns>
		public static string GetEnvironmentVariable( string variableName )
		{
			if ( string.IsNullOrWhiteSpace( variableName ) )
			{
				return null;
			}

			// Environment variable names are typically case-insensitive in Windows
			// However, registry lookups might be case-sensitive depending on your Registry implementation.
			// For simplicity, we'll assume the provided variableName's casing is what we look for,
			// or that Registry.GetValue is case-insensitive.

			string value = null;

			// 1. Try to get from User-specific variables (HKCU)
			// Ensure Registry.Instance and a user hive are loaded.
			if ( Registry.Instance != null && UserManager.Instance?.CurrentUser != null )
			{
				// Note: UserVariablesPath ("HKEY_CURRENT_USER\Environment") is conceptual.
				// Your Registry.GetValue needs to correctly resolve HKCU to the current user's loaded hive.
				value = Registry.Instance.GetValue<string>( UserVariablesPath, variableName, null );
			}

			// 2. If not found in user variables, try System-wide variables (HKLM)
			if ( string.IsNullOrEmpty( value ) && Registry.Instance != null )
			{
				value = Registry.Instance.GetValue<string>( SystemVariablesPath, variableName, null );
			}

			// Special handling for PATH: Merge User and System PATH
			// Windows typically prepends the user's PATH to the system PATH.
			if ( variableName.Equals( "Path", System.StringComparison.OrdinalIgnoreCase ) )
			{
				string userPath = null;
				string systemPath = null;

				if ( Registry.Instance != null && UserManager.Instance?.CurrentUser != null )
				{
					userPath = Registry.Instance.GetValue<string>( UserVariablesPath, "Path", null );
				}
				if ( Registry.Instance != null )
				{
					systemPath = Registry.Instance.GetValue<string>( SystemVariablesPath, "Path", null );
				}

				if ( !string.IsNullOrEmpty( userPath ) && !string.IsNullOrEmpty( systemPath ) )
				{
					return $"{userPath.TrimEnd( ';' )};{systemPath.TrimEnd( ';' )}";
				}
				return userPath ?? systemPath ?? ""; // Return whichever is available, or empty string
			}


			return value;
		}

		/// <summary>
		/// Expands environment variables in a given string (e.g., "%SystemRoot%\system32").
		/// </summary>
		/// <param name="name">The string containing environment variables.</param>
		/// <returns>The string with environment variables expanded.</returns>
		public static string ExpandEnvironmentVariables( string name )
		{
			if ( string.IsNullOrWhiteSpace( name ) )
			{
				return name;
			}

			return Regex.Replace( name, @"%([^%]+)%", match =>
			{
				string variableName = match.Groups[1].Value;
				string variableValue = GetEnvironmentVariable( variableName );
				return variableValue ?? match.Value; // If variable not found, keep original placeholder
			} );
		}

		public static void SetEnvironmentVariable( string variableName, string value, bool isUserVariable = false )
		{
			if ( string.IsNullOrWhiteSpace( variableName ) )
			{
				return; // Invalid variable name
			}
			if ( Registry.Instance == null )
			{
				return; // No registry available to set the variable
			}
			string path = isUserVariable ? UserVariablesPath : SystemVariablesPath;
			Registry.Instance.SetValue( path, variableName, value );
		}

		/// <summary>
		/// Gets all environment variables for the current user context, merging system and user.
		/// User variables override system variables with the same name.
		/// </summary>
		/// <returns>A dictionary of all environment variables.</returns>
		public static Dictionary<string, string> GetEnvironmentVariables()
		{
			var variables = new Dictionary<string, string>( System.StringComparer.OrdinalIgnoreCase );

			// 1. Load System variables
			if ( Registry.Instance != null )
			{
				var systemVars = Registry.Instance.GetValues( SystemVariablesPath );
				if ( systemVars != null )
				{
					foreach ( var kvp in systemVars )
					{
						if ( kvp.Value is string strValue )
						{
							variables[kvp.Key] = strValue;
						}
					}
				}
			}

			// 2. Load User variables, potentially overriding system ones
			if ( Registry.Instance != null && UserManager.Instance?.CurrentUser != null )
			{
				var userVars = Registry.Instance.GetValues( UserVariablesPath );
				if ( userVars != null )
				{
					foreach ( var kvp in userVars )
					{
						if ( kvp.Value is string strValue )
						{
							variables[kvp.Key] = strValue; // Override or add
						}
					}
				}
			}

			// Ensure PATH is correctly merged if it was individually set by the above logic
			// The GetEnvironmentVariable("Path") call already handles merging.
			string pathVar = GetEnvironmentVariable( "Path" );
			if ( !string.IsNullOrEmpty( pathVar ) )
			{
				variables["Path"] = pathVar;
			}


			return variables;
		}
	}
}
