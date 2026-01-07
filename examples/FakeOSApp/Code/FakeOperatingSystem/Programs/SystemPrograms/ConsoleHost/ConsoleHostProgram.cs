using FakeOperatingSystem;

public class ConsoleHostProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/conhost.exe";
	BaseProcess MainChildProcess;

	/// <summary>
	/// Parses a command line to separate the executable from its arguments.
	/// Handles quoted executable paths.
	/// </summary>
	/// <param name="commandLine">The full command line string.</param>
	/// <returns>A tuple containing the executable path and the remaining arguments string.</returns>
	private static (string Executable, string Args) ParseCommandLine( string commandLine )
	{
		if ( string.IsNullOrWhiteSpace( commandLine ) )
		{
			return (null, null);
		}

		string trimmedLine = commandLine.TrimStart();
		string executable;
		string remainingArgs = string.Empty;

		if ( trimmedLine.StartsWith( "\"" ) )
		{
			// Quoted executable path
			// Find the closing quote for the executable path
			int endQuoteIndex = trimmedLine.IndexOf( '"', 1 );
			if ( endQuoteIndex == -1 )
			{
				// Malformed: no closing quote, treat the whole string as the executable (without the leading quote)
				executable = trimmedLine.Substring( 1 );
			}
			else
			{
				executable = trimmedLine.Substring( 1, endQuoteIndex - 1 ); // Get content within quotes
				if ( endQuoteIndex + 1 < trimmedLine.Length )
				{
					remainingArgs = trimmedLine.Substring( endQuoteIndex + 1 ).TrimStart();
				}
			}
		}
		else
		{
			// Unquoted executable path
			int firstSpaceIndex = trimmedLine.IndexOf( ' ' );
			if ( firstSpaceIndex == -1 )
			{
				// No spaces, the whole string is the executable
				executable = trimmedLine;
			}
			else
			{
				executable = trimmedLine.Substring( 0, firstSpaceIndex );
				remainingArgs = trimmedLine.Substring( firstSpaceIndex + 1 ).TrimStart();
			}
		}
		return (executable, remainingArgs);
	}

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var consoleHost = new ConsoleHost();
		process.RegisterWindow( consoleHost );

		// If no arguments, launch cmd.exe as a child process
		if ( launchOptions == null || string.IsNullOrWhiteSpace( launchOptions.Arguments ) )
		{
			// Create launch options for cmd
			var cmdOptions = new Win32LaunchOptions
			{
				Arguments = "",
				ParentProcessId = process.ProcessId,
				StandardOutputOverride = consoleHost.GetOutputWriter(),
				StandardInputOverride = consoleHost.GetInputReader(),
			};

			MainChildProcess = ProcessManager.Instance.OpenExecutable( "C:/Windows/System32/cmd.exe", cmdOptions );
		}
		else
		{
			var (executableToOpen, remainingArguments) = ParseCommandLine( launchOptions.Arguments );

			if ( string.IsNullOrEmpty( executableToOpen ) )
			{
				// Fallback or error handling if executable path cannot be determined
				// For example, launch cmd.exe as a default
				var cmdFallbackOptions = new Win32LaunchOptions
				{
					Arguments = "", // Or pass original arguments if that makes sense
					ParentProcessId = process.ProcessId,
					StandardOutputOverride = consoleHost.GetOutputWriter(),
					StandardInputOverride = consoleHost.GetInputReader(),
				};
				MainChildProcess = ProcessManager.Instance.OpenExecutable( "C:/Windows/System32/cmd.exe", cmdFallbackOptions );
			}
			else
			{
				var cmdOptions = new Win32LaunchOptions
				{
					Arguments = remainingArguments, // Pass only the arguments for the executable
					ParentProcessId = process.ProcessId,
					StandardOutputOverride = consoleHost.GetOutputWriter(),
					StandardInputOverride = consoleHost.GetInputReader(),
					// Consider setting WorkingDirectory if applicable, e.g., from launchOptions or based on executableToOpen
				};

				MainChildProcess = ProcessManager.Instance.OpenExecutable( executableToOpen, cmdOptions );
			}
		}
		consoleHost.OnCloseAction += () =>
		{
			OnClose();
		};
	}

	public void OnClose()
	{
		if ( MainChildProcess != null ) // Add null check for safety
		{
			ProcessManager.Instance.TerminateProcess( MainChildProcess );
		}
	}
}
