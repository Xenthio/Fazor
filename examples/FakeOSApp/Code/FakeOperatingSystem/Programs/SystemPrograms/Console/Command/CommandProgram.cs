using FakeOperatingSystem;
using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.Utils;
using Sandbox;
using System;
using System.Collections.Generic; // Added for IEnumerable (though not strictly needed for current ReadAllData)
using System.IO;
using System.Linq;
using System.Text; // Added for StringBuilder

/// <summary>
/// Mimics the behaviour of cmd.exe (MS-DOS Style Command Prompt) in Windows.
/// </summary>
public class CommandProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/cmd.exe";
	public override bool ConsoleApp => true;
	public string cd = @"C:\";
	public string Prompt = "$P$G";
	public bool EchoEnabled = true;

	BaseProcess process;

	// --- Start of I/O Abstraction ---

	private interface IStreamSource
	{
		string Name { get; }
		/// <summary>
		/// Reads all data from the source.
		/// For console, reads until EOF (Ctrl+Z).
		/// For files, reads entire content.
		/// For NUL, returns an EOF marker.
		/// </summary>
		/// <param name="shell">The command shell instance for context (e.g., StandardInput/Output).</param>
		/// <returns>Byte array of the data, or null if source is empty or an error occurs that's handled internally.</returns>
		byte[] ReadAllData( CommandProgram shell );
		bool Exists( CommandProgram shell ); // To check if the source is valid (e.g., file exists)
	}

	private interface IStreamSink
	{
		string Name { get; }
		/// <summary>
		/// Writes all provided data to the sink.
		/// For console, prints the text.
		/// For files, writes to the file.
		/// For NUL, discards the data.
		/// </summary>
		/// <param name="shell">The command shell instance for context.</param>
		/// <param name="data">The byte array of data to write.</param>
		void WriteAllData( CommandProgram shell, byte[] data );
		void ReportCopy( CommandProgram shell, int filesCopied );
	}

	private class FileSource : IStreamSource
	{
		public string Path { get; }
		public string Name => Path;

		public FileSource( string path )
		{
			Path = path;
		}

		public bool Exists( CommandProgram shell )
		{
			return VirtualFileSystem.Instance.FileExists( Path );
		}

		public byte[] ReadAllData( CommandProgram shell )
		{
			if ( !Exists( shell ) )
			{
				shell.StandardOutput.WriteLine( $"The system cannot find the file specified: {Path}" );
				return null;
			}
			return VirtualFileSystem.Instance.ReadAllBytes( Path );
		}
	}

	private class FileSink : IStreamSink
	{
		public string Path { get; }
		public string Name => Path;

		public FileSink( string path )
		{
			Path = path;
		}

		public void WriteAllData( CommandProgram shell, byte[] data )
		{
			// Ensure the directory for the file exists, if applicable (basic version)
			string directory = VirtualFileSystem.Instance.GetDirectoryName( Path );
			if ( !string.IsNullOrEmpty( directory ) && !VirtualFileSystem.Instance.DirectoryExists( directory ) )
			{
				try
				{
					VirtualFileSystem.Instance.CreateDirectory( directory );
				}
				catch ( Exception ex )
				{
					shell.StandardOutput.WriteLine( $"Error creating directory for {Path}: {ex.Message}" );
					return; // Stop if directory cannot be created
				}
			}
			VirtualFileSystem.Instance.WriteAllBytes( Path, data );
		}

		public void ReportCopy( CommandProgram shell, int filesCopied )
		{
			shell.StandardOutput.WriteLine( $"        {filesCopied} file(s) copied." );
		}
	}

	private class ConsoleSource : IStreamSource
	{
		public string Name => "CON";
		public bool Exists( CommandProgram shell ) => true; // CON always exists

		public byte[] ReadAllData( CommandProgram shell )
		{
			var stringBuilder = new StringBuilder();
			var lineBuilder = new StringBuilder();

			// Store original echo state and disable it if we are not already echoing commands
			// This is tricky because ReadLine itself might handle echo based on the underlying stream.
			// For simplicity, we assume StandardInput.ReadLine() behaves as expected.
			// If direct character input without echo was needed, StandardInput.Read() would be used with manual echo.

			/*			while ( (lineContent = shell.StandardInput.ReadLine()) != null ) // Ctrl+Z then Enter typically signals EOF
						{
							stringBuilder.AppendLine( lineContent );
						}*/

			// Read char by char, submitting each newline (also echoing on newline too)
			while ( true )
			{
				int charCode = shell.StandardInput.Read();
				if ( charCode == 0x1A )
				{
					break;
				}
				if ( charCode == '\n' )
				{
					stringBuilder.AppendLine( lineBuilder.ToString() );
					if ( shell.EchoEnabled )
					{
						shell.StandardOutput.Write( '\n' ); // Echo the character
					}
					lineBuilder.Clear(); // Clear the line builder for the next line
				}
				else if ( charCode == '\r' )
				{
					// Ignore carriage return, as it doesn't need to be echoed
				}
				else
				{
					lineBuilder.Append( (char)charCode );
					if ( shell.EchoEnabled )
					{
						shell.StandardOutput.Write( (char)charCode ); // Echo the character
					}
				}
			}

			return DOSEncodingHelper.GetBytesCp437( stringBuilder.ToString() );
		}
	}

	private class ConsoleSink : IStreamSink
	{
		public string Name => "CON";

		public void WriteAllData( CommandProgram shell, byte[] data )
		{
			var text = DOSEncodingHelper.GetStringCp437KeepControl( data );
			bool endsWithNewline = false;
			bool wroteSomething = false;
			foreach ( var c in text )
			{
				if ( c == '\u001A' ) // Stop at DOS EOF marker
				{
					break;
				}
				shell.StandardOutput.Write( c );
				endsWithNewline = (c == '\n');
				wroteSomething = true;
			}
			if ( wroteSomething && !endsWithNewline )
			{
				shell.StandardOutput.WriteLine(); // Ensure prompt is on a new line
			}
		}
		public void ReportCopy( CommandProgram shell, int filesCopied ) { /* No report for CON output */ }
	}

	private class NullSource : IStreamSource
	{
		public string Name => "NUL";
		public bool Exists( CommandProgram shell ) => true; // NUL always exists

		public byte[] ReadAllData( CommandProgram shell )
		{
			return new byte[] { 0x1A }; // Represents an empty stream with an EOF marker
		}
	}

	private class NullSink : IStreamSink
	{
		public string Name => "NUL";
		public void WriteAllData( CommandProgram shell, byte[] data ) { /* Discard data */ }
		public void ReportCopy( CommandProgram shell, int filesCopied ) { /* No report for NUL output */ }
	}

	private IStreamSource GetStreamSource( string sourceArg, out string errorMessage )
	{
		errorMessage = null;
		sourceArg = sourceArg.Trim().ToUpperInvariant(); // Normalize device names

		if ( sourceArg == "CON" ) return new ConsoleSource();
		if ( sourceArg == "NUL" ) return new NullSource();
		// Add other devices like PRN here if needed

		string resolvedPath = ResolvePath( cd, sourceArg );
		var sourceHandler = new FileSource( resolvedPath );
		if ( !sourceHandler.Exists( this ) )
		{
			// Check if it's a directory, which we don't support as a simple source for copy
			if ( VirtualFileSystem.Instance.DirectoryExists( resolvedPath ) )
			{
				errorMessage = $"Cannot copy from a directory: {sourceArg}. Specify a file.";
				return null;
			}
			errorMessage = $"The system cannot find the file specified: {sourceArg}";
			return null;
		}
		return sourceHandler;
	}

	private IStreamSink GetStreamSink( string destArg, string sourceNameForContext, out string errorMessage )
	{
		errorMessage = null;
		destArg = destArg.Trim(); // Keep case for file paths, but ToUpper for devices
		string upperDestArg = destArg.ToUpperInvariant();

		if ( upperDestArg == "CON" ) return new ConsoleSink();
		if ( upperDestArg == "NUL" ) return new NullSink();
		// Add other devices like PRN here if needed

		string resolvedDestPath = ResolvePath( cd, destArg );

		// Handle case: COPY source.txt C:\ExistingFolder\
		// The destination should become C:\ExistingFolder\source.txt
		if ( VirtualFileSystem.Instance.DirectoryExists( resolvedDestPath ) )
		{
			if ( string.IsNullOrEmpty( sourceNameForContext ) || sourceNameForContext.Equals( "CON", StringComparison.OrdinalIgnoreCase ) || sourceNameForContext.Equals( "NUL", StringComparison.OrdinalIgnoreCase ) )
			{
				errorMessage = "File name cannot be blank."; // Or a more specific error
				return null;
			}
			// Get the filename part of the source (if it was a file)
			string sourceFileName = VirtualFileSystem.Instance.GetFileName( sourceNameForContext ); // This assumes sourceNameForContext is a path if it's not CON/NUL
			if ( string.IsNullOrEmpty( sourceFileName ) && !sourceNameForContext.Equals( "CON", StringComparison.OrdinalIgnoreCase ) && !sourceNameForContext.Equals( "NUL", StringComparison.OrdinalIgnoreCase ) )
			{
				// If sourceNameForContext was a path but GetFileName failed (e.g. it was "C:/"), use a default or error
				// For simplicity, let's assume GetFileName works for valid file source paths.
				// If sourceNameForContext is a device name, this case is handled by the check above.
			}


			resolvedDestPath = ResolvePath( resolvedDestPath, sourceFileName );
		}

		// After potential modification, check if the target is STILL a directory (e.g. COPY file.txt C:\folder\subfolder where subfolder is a dir)
		// This check is more about preventing overwriting a directory as if it were a file.
		// The FileSink itself doesn't create directories, WriteAllBytes would fail if path is a dir.
		// This logic is a bit complex; real CMD might ask "Overwrite <filename> (Yes/No/All)?" if file exists.
		// For now, we assume it's a file path.
		if ( VirtualFileSystem.Instance.DirectoryExists( resolvedDestPath ) )
		{
			errorMessage = $"Access is denied. Cannot write to a directory as if it's a file: {resolvedDestPath}";
			return null;
		}

		return new FileSink( resolvedDestPath );
	}

	// --- End of I/O Abstraction ---

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		if ( !String.IsNullOrWhiteSpace( launchOptions?.WorkingDirectory ) )
			cd = launchOptions.WorkingDirectory;
		this.process = process;

		if ( launchOptions != null && !string.IsNullOrWhiteSpace( launchOptions.Arguments ) )
		{
			Log.Info( launchOptions.Arguments );
			string fullCommandInput = launchOptions.Arguments;
			bool terminateAfter = true; // Default: execute command and exit (like /C or direct command)
			string commandToExecute = fullCommandInput;

			// Check for /C or /K switches, which are standard for cmd.exe
			string tempCommand = fullCommandInput.TrimStart();
			if ( tempCommand.StartsWith( "/C ", StringComparison.OrdinalIgnoreCase ) )
			{
				commandToExecute = tempCommand.Substring( 3 ).TrimStart();
				terminateAfter = true;
			}
			else if ( tempCommand.StartsWith( "/K ", StringComparison.OrdinalIgnoreCase ) )
			{
				commandToExecute = tempCommand.Substring( 3 ).TrimStart();
				terminateAfter = false;
			}
			// If no /C or /K, commandToExecute remains fullCommandInput, 
			// and terminateAfter remains true (execute and exit).

			if ( !string.IsNullOrWhiteSpace( commandToExecute ) )
			{
				var parts = commandToExecute.Split( new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries );
				string programName = parts[0];
				string programArgs = parts.Length > 1 ? parts[1] : "";

				// LaunchProgram will handle if programName is a .bat, .cmd, .exe, .com or internal command.
				// It calls ExecuteBatchFile internally if a batch file is identified.
				LaunchProgram( programName, programArgs, false ); // isStartCommand = false for direct execution semantics

				if ( terminateAfter )
				{
					return; // Exit CommandProgram after execution
				}
				// If !terminateAfter (i.e., /K was used or similar), fall through to the interactive loop.
			}
		}

		StandardOutput.WriteLine( "" );
		StandardOutput.WriteLine( "" );
		StandardOutput.WriteLine( $"{FakeOSLoader.VersionString}" );
		StandardOutput.WriteLine( "   FakeOS Command Prompt [Type 'help' for commands]" );
		StandardOutput.WriteLine( "" );

		// Interactive loop
		while ( true )
		{
			if ( EchoEnabled ) StandardOutput.Write( GetFormattedPrompt() );
			var line = StandardInput.ReadLine();
			if ( line == null ) // User pressed Ctrl+Z (EOF) at the prompt itself
			{
				// In some shells, this might exit. Here, we'll just loop for another command.
				// If COPY CON was active, its ReadLine() would get null and terminate input.
				continue;
			}

			//if ( EchoEnabled ) StandardOutput.WriteLine( line );

			// replace environment variables in the command line
			line = EnvironmentManager.ExpandEnvironmentVariables( line );

			var commandLine = line.Trim();
			if ( string.IsNullOrEmpty( commandLine ) )
				continue;

			var parts = commandLine.Split( ' ', 2, StringSplitOptions.RemoveEmptyEntries );
			var command = parts[0].ToLowerInvariant();
			var args = parts.Length > 1 ? parts[1] : "";
			ParseCommand( command, args );
			StandardOutput.WriteLine( "" ); // Newline after command output (unless command was 'echo off' then prompt)
		}
	}

	public string GetFormattedPrompt()
	{
		string formattedPrompt = Prompt;
		formattedPrompt = formattedPrompt.Replace( "$Q", "=" );
		formattedPrompt = formattedPrompt.Replace( "$$", "$" );
		formattedPrompt = formattedPrompt.Replace( "$T", $"{DateTime.Now.ToString( "HH:mm:ss" )}" );
		formattedPrompt = formattedPrompt.Replace( "$D", $"{DateTime.Now.ToString( "dd/MM/yyyy" )}" );
		formattedPrompt = formattedPrompt.Replace( "$P", cd );
		formattedPrompt = formattedPrompt.Replace( "$V,", FakeOSLoader.VersionString );
		formattedPrompt = formattedPrompt.Replace( "$N", VirtualFileSystem.Instance.ResolveMountPoint( cd ).MountPoint.Name );
		formattedPrompt = formattedPrompt.Replace( "$G", ">" );
		formattedPrompt = formattedPrompt.Replace( "$L", "<" );
		formattedPrompt = formattedPrompt.Replace( "$B", "|" );
		formattedPrompt = formattedPrompt.Replace( "$_", "\n" );
		formattedPrompt = formattedPrompt.Replace( "$E", "\u001B" ); // Escape character
		formattedPrompt = formattedPrompt.Replace( "$H", "\u0008" ); // Backspace character
		formattedPrompt = formattedPrompt.Replace( "$A", "&" );
		formattedPrompt = formattedPrompt.Replace( "$C", "(" );
		formattedPrompt = formattedPrompt.Replace( "$F", ")" );
		formattedPrompt = formattedPrompt.Replace( "$S", " " );

		return formattedPrompt;
	}

	public void ParseCommand( string command, string args )
	{

		switch ( command )
		{
			case "exit":
				StandardOutput.WriteLine( "Exiting..." );
				ProcessManager.Instance.TerminateProcess( process );
				return;

			case "echo":
				if ( string.IsNullOrWhiteSpace( args ) )
				{
					StandardOutput.WriteLine( $"ECHO is {(EchoEnabled ? "on" : "off")}" );
					return;
				}
				string lowerArgs = args.ToLowerInvariant();
				if ( lowerArgs == "on" )
				{
					EchoEnabled = true;
					return;
				}
				else if ( lowerArgs == "off" )
				{
					EchoEnabled = false;
					return;
				}
				StandardOutput.WriteLine( args );
				break;

			case "help":
				StandardOutput.WriteLine( "For more information on a specific command, type HELP command-name" );
				StandardOutput.WriteLine( "CD <dir>           Change directory or display the current directory." );
				StandardOutput.WriteLine( "COPY <src> <dest>  Copy files. src/dest can be filename, CON, or NUL." );
				StandardOutput.WriteLine( "CLS                Clear the screen." );
				StandardOutput.WriteLine( "DEL <file>         Delete a file." );
				StandardOutput.WriteLine( "DIR                List files in current directory" );
				StandardOutput.WriteLine( "ECHO <text|on|off> Print text or toggle command echoing" );
				StandardOutput.WriteLine( "EXIT               Exit the shell" );
				StandardOutput.WriteLine( "HELP               Show this help" );
				StandardOutput.WriteLine( "MKDIR <dir>        Create a directory." );
				StandardOutput.WriteLine( "MOVE <src> <dest>  Move a file." );
				StandardOutput.WriteLine( "PAUSE              Pause execution and wait for a key press." );
				StandardOutput.WriteLine( "PROMPT [<text>]    Change the command prompt." );
				StandardOutput.WriteLine( "REN <old> <new>    Rename a file." );
				StandardOutput.WriteLine( "RMDIR <dir>        Remove a directory." );
				StandardOutput.WriteLine( "TITLE <text>       Set the window title." );
				StandardOutput.WriteLine( "TYPE <file>        Display the contents of a text file." );
				StandardOutput.WriteLine( "VER                Display OS version." );
				break;

			case "cd..": // Specific handling for cd.. without space
				args = "..";
				goto case "cd"; // Fallthrough to cd logic
			case "cd.": // Specific handling for cd. without space
				args = ".";
				goto case "cd"; // Fallthrough to cd logic
			case "cd":
				if ( string.IsNullOrWhiteSpace( args ) )
				{
					// 'cd' with no args prints current directory
					StandardOutput.WriteLine( FormatDir( cd ) );
					return;
				}

				string targetDir = args;
				if ( targetDir == "." ) // 'cd .' does nothing
				{
					return;
				}

				string newDir;
				if ( targetDir == ".." )
				{
					newDir = VirtualFileSystem.Instance.GetDirectoryName( cd );
					if ( newDir == null ) // Already at root
					{
						// cd remains unchanged, or could give an error if preferred
						return;
					}
				}
				else
				{
					newDir = ResolvePath( cd, targetDir );
				}

				if ( VirtualFileSystem.Instance.DirectoryExists( newDir ) )
				{
					cd = VirtualFileSystem.Instance.GetFullPath( newDir ); // Normalize path
					cd = FormatDir( cd ); // Ensure trailing slash consistency if desired, or remove.
										  // For $P$G, a trailing slash is common. Let's ensure it for directories.
					if ( !cd.EndsWith( "\\" ) )
					{
						cd += "\\";
					}
				}
				else
				{
					StandardOutput.WriteLine( $"The system cannot find the path specified." );
				}
				break;

			case "dir":
				{
					string dirPath = string.IsNullOrWhiteSpace( args ) ? cd : ResolvePath( cd, args );
					if ( VirtualFileSystem.Instance.DirectoryExists( dirPath ) )
					{
						var drive = VirtualFileSystem.Instance.ResolveMountPoint( dirPath ).MountPoint;
						if ( drive != null )
						{
							if ( string.IsNullOrEmpty( drive.Label ) )
							{
								StandardOutput.WriteLine( $" Volume in drive {drive.Name} has no label." );
							}
							else
							{
								StandardOutput.WriteLine( $" Volume in drive {drive.Name} is {drive.Label}." );
							}
							StandardOutput.WriteLine( $" Volume Serial Number is 0000-0001" );
						}

						StandardOutput.WriteLine( "" );
						StandardOutput.WriteLine( $" Directory of {FormatDir( dirPath )}" );
						StandardOutput.WriteLine( "" );

						long totalFileSize = 0;
						int fileCount = 0;
						int dirCount = 0;

						// Parent directory entries "." and ".."
						// StandardOutput.WriteLine( $"{DateTime.Now.ToString("dd/MM/yyyy  hh:mm tt"),-20} {"<DIR>",8} {"",8} ." );
						// StandardOutput.WriteLine( $"{DateTime.Now.ToString("dd/MM/yyyy  hh:mm tt"),-20} {"<DIR>",8} {"",8} .." );


						foreach ( var childDir in VirtualFileSystem.Instance.GetDirectories( dirPath ) )
						{
							var dirName = VirtualFileSystem.Instance.GetFileName( childDir );
							var time = VirtualFileSystem.Instance.ModifiedDate( childDir );
							var dateTime = DateTime.FromFileTime( time );
							var formattedDate = dateTime.ToString( "dd/MM/yyyy  hh:mm tt" );
							StandardOutput.WriteLine( $"{formattedDate,-20} {"<DIR>",8} {"",8} {dirName}" );
							dirCount++;
						}
						foreach ( var childFile in VirtualFileSystem.Instance.GetFiles( dirPath ) )
						{
							var fileName = VirtualFileSystem.Instance.GetFileName( childFile );
							var fileSize = VirtualFileSystem.Instance.FileSize( childFile );
							totalFileSize += fileSize;
							fileCount++;
							var time = VirtualFileSystem.Instance.ModifiedDate( childFile );
							var dateTime = DateTime.FromFileTime( time );
							var formattedDate = dateTime.ToString( "dd/MM/yyyy  hh:mm tt" );
							StandardOutput.WriteLine( $"{formattedDate,-20} {fileSize.ToString( "N0" ),17} {fileName}" );
						}
						StandardOutput.WriteLine( $"{fileCount.ToString( "N0" ),16} File(s){totalFileSize.ToString( "N0" ),15} bytes" );
						StandardOutput.WriteLine( $"{dirCount.ToString( "N0" ),16} Dir(s) {VirtualFileSystem.Instance.GetFreeSpace( dirPath ).ToString( "N0" ),15} bytes free" );
					}
					else
					{
						StandardOutput.WriteLine( "File Not Found" ); // DOS uses "File Not Found" for dir [non_existent_path]
					}
				}
				break;

			case "ver":
				StandardOutput.WriteLine( "" );
				StandardOutput.WriteLine( $"{FakeOSLoader.VersionString}" );
				break;

			case "cls":
				StandardOutput.Write( "\u001b[2J\u001b[H" ); // Clears screen and moves cursor to home
				break;

			case "mkdir":
			case "md":
				if ( string.IsNullOrWhiteSpace( args ) ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				VirtualFileSystem.Instance.CreateDirectory( ResolvePath( cd, args ) );
				break;

			case "rmdir":
			case "rd":
				if ( string.IsNullOrWhiteSpace( args ) ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				string dirToDel = ResolvePath( cd, args );
				if ( VirtualFileSystem.Instance.DirectoryExists( dirToDel ) )
				{
					// Add check for /S for recursive delete if desired later
					if ( VirtualFileSystem.Instance.GetFiles( dirToDel ).Any() || VirtualFileSystem.Instance.GetDirectories( dirToDel ).Any() )
					{
						StandardOutput.WriteLine( "The directory is not empty." );
					}
					else
					{
						VirtualFileSystem.Instance.DeleteDirectory( dirToDel );
					}
				}
				else
				{
					StandardOutput.WriteLine( $"The system cannot find the path specified." );
				}
				break;

			case "del":
			case "erase":
				if ( string.IsNullOrWhiteSpace( args ) ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				// Add wildcard support later if needed
				string fileToDel = ResolvePath( cd, args );
				if ( VirtualFileSystem.Instance.FileExists( fileToDel ) )
				{
					VirtualFileSystem.Instance.DeleteFile( fileToDel );
				}
				else
				{
					StandardOutput.WriteLine( $"Could Not Find {FormatDir( fileToDel )}" );
				}
				break;

			case "copy":
				var copyArgsList = new List<string>();
				string remainingArgs = args;
				// Basic parsing for two arguments, doesn't handle "file with spaces" well without quotes.
				// Real CMD parsing is more complex. This handles `copy src dest` and `copy "src with space" "dest with space"` if quotes are part of args.
				// For simplicity, we'll split by space, assuming no paths with spaces unless they are quoted by the shell already.
				// A robust parser would handle quotes. For now, simple split.
				var argParts = args.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );


				if ( argParts.Length == 0 ) { StandardOutput.WriteLine( "The file name is not valid." ); break; }
				if ( argParts.Length == 1 ) { StandardOutput.WriteLine( "The file cannot be copied onto itself." ); break; } // Or "The syntax of the command is incorrect."

				// Assuming simple "copy source destination"
				// More complex parsing for "file1+file2 dest" or /A /B switches is not handled here.
				string sourceArg = argParts[0];
				string destArg = argParts[argParts.Length - 1]; // Last arg is destination

				if ( argParts.Length > 2 )
				{
					// Handle concatenation: file1+file2+file3 dest
					// For now, this is not supported with the new IStreamSource/Sink directly unless they are enhanced.
					// The current refactor focuses on single source to single sink.
					// We can treat "file1+file2" as a single source string and let GetStreamSource fail if it's not a valid single file/device.
					// Or, implement concatenation logic here.
					// For now, let's assume the first arg is the only source if more than 2 args are present without a '+'
					if ( args.Contains( "+" ) )
					{
						StandardOutput.WriteLine( "Concatenating files with '+' is not yet supported in this version of COPY." );
						break;
					}
					// If no '+', maybe it's `copy file "destination folder with spaces\file"`
					// This simple split won't work well. Let's assume for now:
					// argParts[0] is source, string.Join(" ", argParts.Skip(1)) is destination
					destArg = string.Join( " ", argParts.Skip( 1 ) );
				}


				string error;
				IStreamSource streamSource = GetStreamSource( sourceArg, out error );
				if ( streamSource == null )
				{
					StandardOutput.WriteLine( error ?? "Invalid source." );
					break;
				}

				// Pass the original source argument string for context if dest is a directory
				string sourceNameContext = sourceArg;
				if ( streamSource is FileSource fs ) sourceNameContext = fs.Path;


				IStreamSink streamSink = GetStreamSink( destArg, sourceNameContext, out error );
				if ( streamSink == null )
				{
					StandardOutput.WriteLine( error ?? "Invalid destination." );
					break;
				}

				// Prevent copying a file onto itself (simple check by resolved name)
				if ( streamSource is FileSource actualFileSource && streamSink is FileSink actualFileSink )
				{
					if ( VirtualFileSystem.Instance.GetFullPath( actualFileSource.Path ).Equals( VirtualFileSystem.Instance.GetFullPath( actualFileSink.Path ), StringComparison.OrdinalIgnoreCase ) )
					{
						StandardOutput.WriteLine( "The file cannot be copied onto itself." );
						StandardOutput.WriteLine( "        0 file(s) copied." );
						break;
					}
				}


				try
				{
					byte[] dataToCopy = streamSource.ReadAllData( this );
					if ( dataToCopy != null ) // dataToCopy can be null if FileSource.Exists was false and reported error
					{
						streamSink.WriteAllData( this, dataToCopy );
						streamSink.ReportCopy( this, 1 );
					}
					// If ReadAllData returned null because the source didn't exist, an error was already printed by FileSource.
				}
				catch ( Exception ex )
				{
					StandardOutput.WriteLine( $"Error during copy: {ex.Message}" );
					Log.Error( ex.StackTrace );
				}
				break;

			case "ren":
			case "rename":
				var renArgs = args.Split( new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries );
				if ( renArgs.Length < 2 ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				string oldName = ResolvePath( cd, renArgs[0] );
				string newName = ResolvePath( cd, renArgs[1] ); // newName is just the new name, not full path usually for ren

				// REN command expects the new name to be just a name, not a path.
				// If newName contains path separators, it's an error or treated as moving to a different dir (which is MOVE's job)
				// For simplicity, let's assume newName is just a filename.
				// A more robust REN would parse newName carefully.
				string newFileNameOnly = VirtualFileSystem.Instance.GetFileName( newName );
				if ( string.IsNullOrEmpty( newFileNameOnly ) || newName.Contains( "/" ) || newName.Contains( "\\" ) && newName != newFileNameOnly )
				{
					StandardOutput.WriteLine( "The syntax of the command is incorrect." ); // Or "A duplicate file name exists, or the file cannot be found."
					break;
				}


				if ( VirtualFileSystem.Instance.FileExists( oldName ) )
				{
					string oldDir = VirtualFileSystem.Instance.GetDirectoryName( oldName );
					string resolvedNewName = ResolvePath( oldDir, newFileNameOnly ); // Combine old dir with new name

					if ( VirtualFileSystem.Instance.FileExists( resolvedNewName ) )
					{
						StandardOutput.WriteLine( "A duplicate file name exists, or the file cannot be found." );
						break;
					}
					VirtualFileSystem.Instance.MoveFile( oldName, resolvedNewName );
				}
				else if ( VirtualFileSystem.Instance.DirectoryExists( oldName ) )
				{
					// Renaming directories
					string oldDirName = VirtualFileSystem.Instance.GetDirectoryName( oldName );
					string resolvedNewDirName = ResolvePath( oldDirName, newFileNameOnly );
					if ( VirtualFileSystem.Instance.DirectoryExists( resolvedNewDirName ) || VirtualFileSystem.Instance.FileExists( resolvedNewDirName ) )
					{
						StandardOutput.WriteLine( "A duplicate file name exists, or the file cannot be found." );
						break;
					}
					VirtualFileSystem.Instance.MoveDirectory( oldName, resolvedNewDirName );
				}
				else
				{
					StandardOutput.WriteLine( $"The system cannot find the file specified." );
				}
				break;

			case "move":
				var moveArgs = args.Split( new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries );
				if ( moveArgs.Length < 2 ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				string moveSource = ResolvePath( cd, moveArgs[0] );
				string moveDest = ResolvePath( cd, moveArgs[1] );

				if ( VirtualFileSystem.Instance.FileExists( moveSource ) )
				{
					// If moveDest is an existing directory, move the file into it.
					if ( VirtualFileSystem.Instance.DirectoryExists( moveDest ) )
					{
						moveDest = ResolvePath( moveDest, VirtualFileSystem.Instance.GetFileName( moveSource ) );
					}
					// Add overwrite checks if necessary
					VirtualFileSystem.Instance.MoveFile( moveSource, moveDest );
					StandardOutput.WriteLine( "        1 file(s) moved." );
				}
				else if ( VirtualFileSystem.Instance.DirectoryExists( moveSource ) )
				{
					// Moving a directory
					// If moveDest is an existing directory, it means move moveSource into moveDest
					if ( VirtualFileSystem.Instance.DirectoryExists( moveDest ) )
					{
						moveDest = ResolvePath( moveDest, VirtualFileSystem.Instance.GetFileName( moveSource ) );
					}
					// Add checks for destination already exists, or trying to move a dir into itself, etc.
					VirtualFileSystem.Instance.MoveDirectory( moveSource, moveDest );
					StandardOutput.WriteLine( "        1 dir(s) moved." );
				}
				else
				{
					StandardOutput.WriteLine( $"The system cannot find the file specified." );
				}
				break;

			case "type":
				if ( string.IsNullOrWhiteSpace( args ) ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				string typePath = ResolvePath( cd, args );

				if ( typePath.Equals( "CON", StringComparison.OrdinalIgnoreCase ) )
				{
					break;
				}
				if ( typePath.Equals( "NUL", StringComparison.OrdinalIgnoreCase ) )
				{
					// Typing NUL does nothing, or prints a blank line.
					break;
				}


				if ( VirtualFileSystem.Instance.FileExists( typePath ) )
				{
					var fileContents = VirtualFileSystem.Instance.ReadAllBytes( typePath );
					var text = DOSEncodingHelper.GetStringCp437KeepControl( fileContents );
					bool endsWithNewline = false;
					foreach ( var c in text )
					{
						if ( c == '\u001A' ) // DOS EOF
						{
							break;
						}
						StandardOutput.Write( c );
						endsWithNewline = (c == '\n');
					}
					if ( !endsWithNewline ) StandardOutput.WriteLine(); // Ensure prompt on new line
				}
				else
				{
					StandardOutput.WriteLine( $"The system cannot find the file specified." );
				}
				break;

			case "start":
				// Basic start: just try to launch the program.
				// Real 'start' has many options (new window, /wait, title, etc.)
				if ( string.IsNullOrWhiteSpace( args ) ) { StandardOutput.WriteLine( "The syntax of the command is incorrect." ); break; }
				// First word of args is program, rest are its arguments
				var startParts = args.Split( new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries );
				string progToStart = startParts[0];
				string progArgs = startParts.Length > 1 ? startParts[1] : "";
				LaunchProgram( progToStart, progArgs, true ); // true for 'start' behavior (e.g. new window if GUI)
				break;

			case "prompt":
				if ( string.IsNullOrWhiteSpace( args ) )
				{
					Prompt = "$P$G"; // Reset to default
				}
				else
				{
					Prompt = args;
				}
				break;

			case "pause":
				Pause();
				break;

			case "title":
				// This escape sequence might only work if the hosting console supports it.
				StandardOutput.Write( "\u001B]0;" + args + "\u0007" );
				break;

			// Search in cwd and path for operable programs or batch files
			default:
				LaunchProgram( command, args, false ); // false for direct launch behavior
				break;
		}
	}

	private void ExecuteBatchFile( string batchFilePath, string allBatchArgs )
	{
		var originalEchoState = EchoEnabled; // Save current echo state
		if ( !VirtualFileSystem.Instance.FileExists( batchFilePath ) )
		{
			StandardOutput.WriteLine( $"Batch file not found: {batchFilePath}" );
			return;
		}

		byte[] batchFileBytes = VirtualFileSystem.Instance.ReadAllBytes( batchFilePath );
		// Assuming DOSEncodingHelper.GetStringCp437 can handle null/empty if ReadAllBytes returns that.
		// Or, add a null check for batchFileBytes.
		if ( batchFileBytes == null )
		{
			StandardOutput.WriteLine( $"Batch file is empty or could not be read: {batchFilePath}" );
			return;
		}
		string batchContent = DOSEncodingHelper.GetStringCp437KeepControl( batchFileBytes );
		string[] lines = batchContent.Split( new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None );

		List<string> argList = new List<string>();
		if ( !string.IsNullOrWhiteSpace( allBatchArgs ) )
		{
			// Simple space splitting. Does not handle quoted arguments within allBatchArgs robustly.
			argList.AddRange( allBatchArgs.Split( new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries ) );
		}

		bool localEchoOffForLine;

		foreach ( string rawLine in lines )
		{
			string currentLine = rawLine.TrimStart(); // Trim leading whitespace

			currentLine = EnvironmentManager.ExpandEnvironmentVariables( currentLine ); // Expand environment variables

			// Skip genuinely empty lines (after TrimStart, could still be all whitespace if not using Trim())
			if ( string.IsNullOrWhiteSpace( currentLine ) )
			{
				// If echo is on, cmd.exe echoes blank lines from a batch file.
				if ( EchoEnabled ) StandardOutput.WriteLine( GetFormattedPrompt() ); // Echoes prompt then newline
				continue;
			}

			localEchoOffForLine = false;
			if ( currentLine.StartsWith( "@" ) )
			{
				localEchoOffForLine = true;
				currentLine = currentLine.Substring( 1 );
			}

			// Handle comments (REM and ::)
			// Note: TrimStart was used, so "  REM comment" is now "REM comment"
			string upperLineForCommentCheck = currentLine.Trim().ToUpperInvariant(); // Trim for accurate REM/:: check
			if ( upperLineForCommentCheck.StartsWith( "REM " ) || upperLineForCommentCheck == "REM" || currentLine.TrimStart().StartsWith( "::" ) )
			{
				if ( EchoEnabled && !localEchoOffForLine )
				{
					StandardOutput.WriteLine( GetFormattedPrompt() + rawLine.TrimEnd() ); // Echo original line (or trimmed original)
				}
				continue;
			}

			string processedLine = currentLine;
			processedLine = processedLine.Replace( "%0", batchFilePath ); // %0 is the batch file name
			processedLine = processedLine.Replace( "%*", allBatchArgs ?? "" ); // %* is all arguments as a single string

			for ( int i = 0; i < 9; i++ ) // %1 to %9
			{
				if ( i < argList.Count )
				{
					processedLine = processedLine.Replace( $"%{i + 1}", argList[i] );
				}
				else
				{
					processedLine = processedLine.Replace( $"%{i + 1}", "" ); // Unprovided args are empty strings
				}
			}

			if ( EchoEnabled && !localEchoOffForLine )
			{
				StandardOutput.WriteLine( GetFormattedPrompt() + processedLine.TrimEnd() );
			}

			var parts = processedLine.Trim().Split( new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries );
			var command = parts.Length > 0 ? parts[0] : "";
			var commandArgs = parts.Length > 1 ? parts[1] : "";

			if ( string.IsNullOrEmpty( command ) ) continue;

			// If the command is "exit" (and not "exit /b" which isn't fully supported yet to only exit batch),
			// TerminateProcess will be called, and this loop should effectively stop.
			ParseCommand( command, commandArgs );
			// StandardOutput.WriteLine(""); // This is usually handled by the main loop or ParseCommand itself.
			// Adding it here might cause double newlines.
		}
		// Restore original echo state after batch execution
		EchoEnabled = originalEchoState;
	}

	private void LaunchProgram( string programName, string programArgs, bool isStartCommand )
	{
		string originalCommandName = programName; // For error messages

		List<string> filesToTry = new List<string>();
		bool specificExtensionProvided = programName.Contains( "." );

		if ( specificExtensionProvided )
		{
			// If user types "myprog.exe" or "script.bat", try that directly.
			filesToTry.Add( programName );
		}
		else
		{
			// No extension provided, try common ones in typical PATHEXT order.
			// e.g., if user types "myprog", try "myprog.COM", "myprog.EXE", "myprog.BAT", "myprog.CMD".
			filesToTry.Add( programName + ".COM" );
			filesToTry.Add( programName + ".EXE" );
			filesToTry.Add( programName + ".BAT" );
			filesToTry.Add( programName + ".CMD" );
			// Optionally, try the name as-is if it might be an executable without a common extension (less typical for Windows).
			// filesToTry.Add(programName); 
		}

		string foundPath = null;
		bool isBatchFile = false;

		// Search Locations: 1. Current Directory, 2. System Directory
		List<string> searchDirs = new List<string> { cd };

		// add PATH directories if available
		string pathEnv = EnvironmentManager.GetEnvironmentVariable( "PATH" );
		var pathDirs = pathEnv?.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries ) ?? Array.Empty<string>();
		if ( pathDirs.Length > 0 )
		{
			searchDirs.AddRange( pathDirs.Select( dir => ResolvePath( cd, dir ) ) );
		}

		foreach ( string fileToAttempt in filesToTry )
		{
			foreach ( string dir in searchDirs )
			{
				string candidatePath = ResolvePath( dir, fileToAttempt );
				if ( VirtualFileSystem.Instance.FileExists( candidatePath ) )
				{
					foundPath = candidatePath;
					if ( foundPath.EndsWith( ".BAT", StringComparison.OrdinalIgnoreCase ) ||
						foundPath.EndsWith( ".CMD", StringComparison.OrdinalIgnoreCase ) )
					{
						isBatchFile = true;
					}
					goto ProgramFound; // Exit both loops
				}
			}
		}

		ProgramFound:
		if ( foundPath != null )
		{
			if ( isBatchFile )
			{
				ExecuteBatchFile( foundPath, programArgs );
			}
			else // Assumed to be an executable (.EXE, .COM, etc.)
			{
				try
				{
					var childOptions = new Win32LaunchOptions
					{
						Arguments = programArgs,
						WorkingDirectory = cd,
						ParentProcessId = process.ProcessId,
						StandardOutputOverride = StandardOutput,
						StandardInputOverride = StandardInput
					};

					var newProcess = ProcessManager.Instance.OpenExecutable( foundPath, childOptions );
					if ( newProcess != null )
					{
						if ( newProcess.IsConsoleProcess )
						{
							// If 'start' command, it runs async unless /WAIT.
							// If direct command, it runs sync.
							if ( !isStartCommand || (programArgs != null && programArgs.ToUpperInvariant().Contains( "/WAIT" )) )
							{
								while ( newProcess.Status == ProcessStatus.Running )
								{
									GameTask.Delay( 100 ).Wait(); // Yield execution, wait for process to complete
								}
							}
						}
						// For GUI apps, OpenExecutable typically doesn't block.
					}
					else
					{
						StandardOutput.WriteLine( $"Failed to launch '{originalCommandName}'. The system could not start the process." );
					}
				}
				catch ( Exception ex )
				{
					StandardOutput.WriteLine( $"Failed to launch '{originalCommandName}': {ex.Message}" );
				}
			}
		}
		else
		{
			StandardOutput.WriteLine( $"'{originalCommandName}' is not recognized as an internal or external command,\noperable program or batch file." );
		}
	}


	private void Pause()
	{
		// If echo is off, we still want to see the pause message.
		bool originalEcho = EchoEnabled;
		EchoEnabled = true;
		StandardOutput.Write( "Press any key to continue . . . " );
		EchoEnabled = originalEcho;

		StandardInput.Read(); // Wait for a single key press (character)
		StandardOutput.WriteLine( "" ); // Move to next line after key press
	}

	private string ResolvePath( string currentDir, string input )
	{
		if ( string.IsNullOrEmpty( input ) ) return currentDir;

		// Normalize input slashes
		input = input.Replace( '\\', '/' );

		// If input is an absolute path (e.g., "C:/folder", "/Windows")
		if ( input.Contains( ":/" ) || input.StartsWith( "/" ) )
		{
			if ( input.StartsWith( "/" ) ) // e.g. /Windows -> C:/Windows (assuming C: if no drive)
			{
				// This needs to resolve to the root of the current drive if currentDir is e.g. C:\Users
				string currentDrive = VirtualFileSystem.Instance.GetPathRoot( currentDir ); // e.g. "C:/"
				if ( string.IsNullOrEmpty( currentDrive ) ) currentDrive = "C:/"; // Default
				return VirtualFileSystem.Instance.GetFullPath( currentDrive + input.Substring( 1 ) );
			}
			return VirtualFileSystem.Instance.GetFullPath( input );
		}

		// Relative path
		string combinedPath = Path.Combine( currentDir, input ); // System.IO.Path.Combine
		return VirtualFileSystem.Instance.GetFullPath( combinedPath );
	}

	private string FormatDir( string dir )
	{
		if ( string.IsNullOrEmpty( dir ) ) return "";
		string formatted = dir.Replace( '/', '\\' );
		// if (formatted.Length > 0 && !formatted.EndsWith("\\"))
		// {
		// 	formatted += "\\"; // Ensure trailing slash for directories in output like 'cd'
		// }
		return formatted;
	}
}
