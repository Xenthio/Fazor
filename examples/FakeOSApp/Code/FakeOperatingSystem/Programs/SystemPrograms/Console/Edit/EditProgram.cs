using FakeOperatingSystem;
using FakeOperatingSystem.OSFileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class EditProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/edit.exe";
	public override bool ConsoleApp => true;

	// Key Codes based on ConsoleHostReader and ConsolePanel
	private const int KEY_NULL_INTRO = 0x00;

	private const char SCAN_CODE_UP = (char)0x48;
	private const char SCAN_CODE_DOWN = (char)0x50;
	private const char SCAN_CODE_LEFT = (char)0x4B;
	private const char SCAN_CODE_RIGHT = (char)0x4D;
	private const char SCAN_CODE_HOME = (char)0x47;
	private const char SCAN_CODE_END = (char)0x4F;
	private const char SCAN_CODE_DELETE = (char)0x53;
	// Note: PageUp/PageDown are not uniquely identifiable with current ConsolePanel, they send ESC

	private const int KEY_ENTER = '\n';       // ConsolePanel sends \n for enter button
	private const int KEY_BACKSPACE = '\b';
	private const int KEY_ESCAPE = 27;
	private const int KEY_TAB = '\t';
	// Ctrl+S, Ctrl+O etc. are not directly supported by ConsolePanel -> ConsoleHostReader yet.

	// ANSI Escape Codes
	private const string ANSI_CLEAR_SCREEN = "\x1B[2J";
	private const string ANSI_CURSOR_HOME = "\x1B[H";
	// private const string ANSI_HIDE_CURSOR = "\x1B[?25l"; // ConsolePanel consumes but doesn't act
	// private const string ANSI_SHOW_CURSOR = "\x1B[?25h"; // ConsolePanel consumes but doesn't act

	private List<StringBuilder> _buffer = new List<StringBuilder>();
	private string _currentFilePath = "UNTITLED1";
	private bool _isUntitled = true;
	private bool _isDirty = false;

	private int _cursorBufferRow = 0; // Cursor's current line in the _buffer
	private int _cursorBufferCol = 0; // Cursor's current column in the _buffer line

	private int _viewTopRow = 0;      // First line of _buffer visible in the editor window
	private int _viewLeftCol = 0;     // First column of _buffer lines visible (for horizontal scroll)

	// Editor display dimensions (could be made dynamic if console size was known)
	// Assuming a standard 80x25 console, minus menu/borders/status
	private int _textDisplayWidth = 78; // Width for text content (inside borders)
	private int _textDisplayHeight = 20; // Height for text content

	private string _statusMessage = "";
	private DateTime _statusMessageTime;

	private bool _fullRedrawNeeded = true; // True if the entire screen needs to be redrawn
	private int _singleLineToUpdate = -1;  // Buffer index of the line to update, -1 if none or full redraw
	private bool _statusLineNeedsUpdate = true; // True if only the status line needs an update

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		string filePathArgument = launchOptions?.Arguments?.Trim();
		if ( !string.IsNullOrWhiteSpace( filePathArgument ) )
		{
			_currentFilePath = filePathArgument;
			_isUntitled = false;
			LoadFile();
		}
		else
		{
			_buffer.Add( new StringBuilder() ); // Start with one empty line for new file
		}
		_isDirty = false; // Reset dirty flag
		_fullRedrawNeeded = true; // Ensure initial draw

		// process.StandardOutput.Write(ANSI_HIDE_CURSOR); // Attempt to hide ConsolePanel's default caret

		bool running = true;
		while ( running )
		{
			// Check for status message timeout and clear if necessary
			if ( !string.IsNullOrWhiteSpace( _statusMessage ) &&
				(_statusMessageTime != default && (DateTime.UtcNow - _statusMessageTime).TotalSeconds > 5) )
			{
				_statusMessage = ""; // Clear timed-out message
				_statusLineNeedsUpdate = true;
			}

			if ( _fullRedrawNeeded )
			{
				DrawScreen( process.StandardOutput ); // Draws everything, including status and positions cursor
				_fullRedrawNeeded = false;
				_singleLineToUpdate = -1;
				_statusLineNeedsUpdate = false; // DrawScreen handles status line and cursor positioning
			}
			else
			{
				if ( _singleLineToUpdate != -1 )
				{
					RedrawTextLine( process.StandardOutput, _singleLineToUpdate );
					_singleLineToUpdate = -1;
					// After redrawing a line, the status (like Col number) might change
					_statusLineNeedsUpdate = true;
				}
				if ( _statusLineNeedsUpdate )
				{
					UpdateStatusLineOnly( process.StandardOutput );
					_statusLineNeedsUpdate = false;
				}
				PositionCursor( process.StandardOutput ); // Always ensure cursor is correctly positioned after any partial update
			}

			int keyCode = process.StandardInput.Read();

			if ( keyCode == -1 ) // EOF or error
			{
				running = false;
				break;
			}
			running = ProcessKeyInput( process, keyCode );
		}

		// process.StandardOutput.Write(ANSI_SHOW_CURSOR); // Restore ConsolePanel's caret (if it worked)
		process.StandardOutput.Write( ANSI_CLEAR_SCREEN ); // Clear screen on exit
		process.StandardOutput.Write( ANSI_CURSOR_HOME );
		process.StandardOutput.WriteLine( "Exited edit.com." ); // Final message
	}

	private void LoadFile()
	{
		_buffer.Clear();
		if ( !_isUntitled && VirtualFileSystem.Instance.FileExists( _currentFilePath ) )
		{
			try
			{
				string[] lines = VirtualFileSystem.Instance.ReadAllText( _currentFilePath )
												.Replace( "\r\n", "\n" ) // Normalize newlines
												.Split( '\n' );
				foreach ( var line in lines )
				{
					_buffer.Add( new StringBuilder( line ) );
				}
				if ( _buffer.Count == 0 ) _buffer.Add( new StringBuilder() ); // Ensure at least one line
				SetStatusMessage( $"Loaded '{_currentFilePath}'" );
			}
			catch ( Exception e )
			{
				_buffer.Add( new StringBuilder() ); // Start fresh
				SetStatusMessage( $"Error loading: {e.Message.Split( '\n' ).FirstOrDefault()}" );
			}
		}
		else
		{
			_buffer.Add( new StringBuilder() );
			if ( !_isUntitled ) SetStatusMessage( $"New file: '{_currentFilePath}'" );
		}
		_cursorBufferRow = 0;
		_cursorBufferCol = 0;
		_viewTopRow = 0;
		_viewLeftCol = 0;
		_isDirty = false;
		_fullRedrawNeeded = true; // Content changed, requires full redraw
	}

	private void SaveFile( string path = null )
	{
		string savePath = path ?? _currentFilePath;
		if ( string.IsNullOrWhiteSpace( savePath ) || savePath.Equals( "UNTITLED1", StringComparison.OrdinalIgnoreCase ) )
		{
			SetStatusMessage( "Save As: Filename needed (not implemented). Press ESC." );
			return;
		}

		try
		{
			List<string> linesToSave = _buffer.Select( sb => sb.ToString() ).ToList();
			VirtualFileSystem.Instance.WriteAllText( savePath, string.Join( "\r\n", linesToSave ) );
			_currentFilePath = savePath;
			_isUntitled = false;
			bool oldIsDirty = _isDirty;
			_isDirty = false;
			if ( oldIsDirty && !_isDirty ) // If it was dirty and now it's not
			{
				_fullRedrawNeeded = true; // To update title bar (remove '*')
			}
			SetStatusMessage( $"File saved: {savePath}" );
		}
		catch ( Exception e )
		{
			SetStatusMessage( $"Error saving: {e.Message.Split( '\n' ).FirstOrDefault()}" );
		}
	}

	private bool ProcessKeyInput( NativeProcess process, int keyCode )
	{
		// _statusMessage = ""; // Clear old status message on new input - Handled by timeout or explicit SetStatusMessage
		bool oldIsDirty = _isDirty;
		// Variable to track if a simple, single-line modification occurred that doesn't require full redraw
		bool simpleSingleLineEdit = false;
		int editedLineBufferIndex = _cursorBufferRow; // Assume current line is edited for simple cases

		if ( keyCode == KEY_NULL_INTRO ) // Extended key
		{
			keyCode = process.StandardInput.Read(); // Read the actual scan code
			switch ( keyCode )
			{
				case SCAN_CODE_UP:
					if ( _cursorBufferRow > 0 ) _cursorBufferRow--;
					break;
				case SCAN_CODE_DOWN:
					if ( _cursorBufferRow < _buffer.Count - 1 ) _cursorBufferRow++;
					break;
				case SCAN_CODE_LEFT:
					if ( _cursorBufferCol > 0 ) _cursorBufferCol--;
					else if ( _cursorBufferRow > 0 )
					{
						_cursorBufferRow--;
						_cursorBufferCol = _buffer[_cursorBufferRow].Length;
					}
					break;
				case SCAN_CODE_RIGHT:
					if ( _cursorBufferCol < CurrentLineLength() ) _cursorBufferCol++;
					else if ( _cursorBufferRow < _buffer.Count - 1 )
					{
						_cursorBufferRow++;
						_cursorBufferCol = 0;
					}
					break;
				case SCAN_CODE_HOME:
					_cursorBufferCol = 0;
					break;
				case SCAN_CODE_END:
					_cursorBufferCol = CurrentLineLength();
					break;
				case SCAN_CODE_DELETE:
					if ( _cursorBufferCol < CurrentLineLength() )
					{
						_buffer[_cursorBufferRow].Remove( _cursorBufferCol, 1 );
						_isDirty = true;
						simpleSingleLineEdit = true;
					}
					else if ( _cursorBufferRow < _buffer.Count - 1 ) // Delete at end of line, merge with next
					{
						_buffer[_cursorBufferRow].Append( _buffer[_cursorBufferRow + 1].ToString() );
						_buffer.RemoveAt( _cursorBufferRow + 1 );
						_isDirty = true;
						_fullRedrawNeeded = true; // Structural change
					}
					break;
				default: // Unknown scan code
					break;
			}
		}
		else // Normal key
		{
			switch ( keyCode )
			{
				case KEY_ENTER:
					StringBuilder currentLine = _buffer[_cursorBufferRow];
					string remainder = currentLine.ToString().Substring( _cursorBufferCol );
					currentLine.Remove( _cursorBufferCol, currentLine.Length - _cursorBufferCol );
					_buffer.Insert( _cursorBufferRow + 1, new StringBuilder( remainder ) );
					_cursorBufferRow++;
					_cursorBufferCol = 0;
					_isDirty = true;
					_fullRedrawNeeded = true; // Structural change (split line, new line added)
					break;
				case KEY_BACKSPACE:
					if ( _cursorBufferCol > 0 )
					{
						_buffer[_cursorBufferRow].Remove( _cursorBufferCol - 1, 1 );
						_cursorBufferCol--;
						_isDirty = true;
						simpleSingleLineEdit = true;
					}
					else if ( _cursorBufferRow > 0 ) // Backspace at start of line, merge with previous
					{
						string lineToAppend = _buffer[_cursorBufferRow].ToString();
						_buffer.RemoveAt( _cursorBufferRow );
						_cursorBufferRow--;
						_cursorBufferCol = CurrentLineLength();
						_buffer[_cursorBufferRow].Append( lineToAppend );
						_isDirty = true;
						_fullRedrawNeeded = true; // Structural change
					}
					break;
				case KEY_TAB:
					const int tabSize = 4;
					string tabSpaces = new string( ' ', tabSize );
					_buffer[_cursorBufferRow].Insert( _cursorBufferCol, tabSpaces );
					_cursorBufferCol += tabSize;
					_isDirty = true;
					simpleSingleLineEdit = true;
					break;
				case KEY_ESCAPE:
					if ( _isDirty )
					{
						SetStatusMessage( "Unsaved changes! Press ESC again to quit without saving, or F2 to Save." );
						int nextKey = process.StandardInput.Read();
						if ( nextKey == KEY_ESCAPE ) return false;
						else if ( nextKey == KEY_NULL_INTRO && process.StandardInput.Read() == 0x3C ) // F2
						{
							SaveFile(); // SaveFile sets _fullRedrawNeeded
							if ( !_isDirty ) return false;
						}
					}
					else
					{
						return false; // Exit if not dirty
					}
					break;
				default:
					if ( keyCode >= 32 && keyCode <= 126 ) // Printable ASCII
					{
						_buffer[_cursorBufferRow].Insert( _cursorBufferCol, (char)keyCode );
						_cursorBufferCol++;
						_isDirty = true;
						simpleSingleLineEdit = true;
					}
					break;
			}
		}

		if ( _isDirty && !oldIsDirty ) // If file just became dirty
		{
			_fullRedrawNeeded = true; // Need full redraw to update '*' in title bar
		}

		EnsureCursorInBounds();
		ScrollToViewCursor(); // This can set _fullRedrawNeeded if view scrolls

		if ( !_fullRedrawNeeded && simpleSingleLineEdit )
		{
			_singleLineToUpdate = editedLineBufferIndex;
		}

		_statusLineNeedsUpdate = true; // Always assume status might need update (e.g. cursor pos)

		return true; // Continue running
	}

	private int CurrentLineLength()
	{
		if ( _cursorBufferRow >= 0 && _cursorBufferRow < _buffer.Count )
		{
			return _buffer[_cursorBufferRow].Length;
		}
		return 0;
	}

	private void EnsureCursorInBounds()
	{
		_cursorBufferRow = Math.Max( 0, Math.Min( _cursorBufferRow, _buffer.Count - 1 ) );
		_cursorBufferCol = Math.Max( 0, Math.Min( _cursorBufferCol, CurrentLineLength() ) );
	}

	private void ScrollToViewCursor()
	{
		bool viewChanged = false;
		// Adjust vertical scroll
		if ( _cursorBufferRow < _viewTopRow )
		{
			_viewTopRow = _cursorBufferRow;
			viewChanged = true;
		}
		if ( _cursorBufferRow >= _viewTopRow + _textDisplayHeight )
		{
			_viewTopRow = _cursorBufferRow - _textDisplayHeight + 1;
			viewChanged = true;
		}

		// Adjust horizontal scroll (basic)
		if ( _cursorBufferCol < _viewLeftCol )
		{
			_viewLeftCol = _cursorBufferCol;
			viewChanged = true;
		}
		if ( _cursorBufferCol >= _viewLeftCol + _textDisplayWidth )
		{
			_viewLeftCol = _cursorBufferCol - _textDisplayWidth + 1;
			viewChanged = true;
		}
		_viewLeftCol = Math.Max( 0, _viewLeftCol );

		if ( viewChanged )
		{
			_fullRedrawNeeded = true;
		}
	}

	private void SetStatusMessage( string message )
	{
		_statusMessage = message;
		_statusMessageTime = DateTime.UtcNow;
		_statusLineNeedsUpdate = true; // Ensure status line updates when a message is set
	}

	private void RedrawTextLine( TextWriter output, int bufferLineIndexToRedraw )
	{
		// Check if the line to redraw is visible
		if ( bufferLineIndexToRedraw < _viewTopRow || bufferLineIndexToRedraw >= _viewTopRow + _textDisplayHeight )
		{
			return; // Line is not visible
		}

		int screenRowForText = (bufferLineIndexToRedraw - _viewTopRow) + 3; // 1-based console row for the text line
		output.Write( $"\x1B[{screenRowForText};2H" ); // Move to start of text content area (col 2 for left border '│')

		string line = _buffer[bufferLineIndexToRedraw].ToString();
		string visiblePart = "";
		if ( _viewLeftCol < line.Length )
		{
			visiblePart = line.Substring( _viewLeftCol, Math.Min( line.Length - _viewLeftCol, _textDisplayWidth ) );
		}

		output.Write( visiblePart.PadRight( _textDisplayWidth ) ); // Pad with spaces to fill the width
	}

	private void UpdateStatusLineOnly( TextWriter output )
	{
		int statusConsoleRow = 1 + 1 + _textDisplayHeight + 1 + 1; // Menu, TopBorder, Text, BottomBorder, StatusLine
		output.Write( $"\x1B[{statusConsoleRow};1H" ); // Move to start of status line

		string status = _statusMessage;
		if ( string.IsNullOrWhiteSpace( status ) || ((DateTime.UtcNow - _statusMessageTime).TotalSeconds > 5 && _statusMessageTime != default) )
		{
			if ( !string.IsNullOrWhiteSpace( _statusMessage ) && (DateTime.UtcNow - _statusMessageTime).TotalSeconds > 5 )
			{
				_statusMessage = ""; // Clear the timed-out message
			}
			status = $"Ln {_cursorBufferRow + 1}, Col {_cursorBufferCol + 1}";
		}
		output.Write( status.PadRight( _textDisplayWidth + 2 ) ); // Pad to cover full width
	}

	private void PositionCursor( TextWriter output )
	{
		int relativeViewRow = _cursorBufferRow - _viewTopRow;
		int relativeViewCol = _cursorBufferCol - _viewLeftCol;

		relativeViewRow = Math.Max( 0, Math.Min( relativeViewRow, _textDisplayHeight - 1 ) );
		relativeViewCol = Math.Max( 0, Math.Min( relativeViewCol, _textDisplayWidth - 1 ) );

		int targetConsoleRow = relativeViewRow + 3; // Menu line is 1, Top border is 2. Text area starts at console row 3.
		int targetConsoleCol = relativeViewCol + 2; // Left border char is col 1. Text area starts at console col 2.

		output.Write( $"\x1B[{targetConsoleRow};{targetConsoleCol}H" );
	}

	private void DrawScreen( TextWriter output )
	{
		StringBuilder screen = new StringBuilder();
		screen.Append( ANSI_CLEAR_SCREEN );
		screen.Append( ANSI_CURSOR_HOME );

		// 1. Menu Bar (Static for now, add F-key hints later if possible)
		string menuText = " File  Edit  Search  View  Options  Help - F2:Save ESC:Exit";
		screen.Append( menuText.PadRight( _textDisplayWidth + 2 ) ); // +2 for borders
		screen.Append( "\n" );

		// 2. Top Border with Filename
		string dirtyMarker = _isDirty ? "*" : "";
		string title = $" {_currentFilePath.ToUpper()}{dirtyMarker} ";
		if ( title.Length > _textDisplayWidth - 2 ) title = title.Substring( 0, _textDisplayWidth - 5 ) + "... ";

		int dashesTotal = _textDisplayWidth - title.Length;
		int dashesLeft = Math.Max( 0, dashesTotal / 2 );
		int dashesRight = Math.Max( 0, dashesTotal - dashesLeft );

		screen.Append( "┌" );
		screen.Append( new string( '─', dashesLeft ) );
		screen.Append( title );
		screen.Append( new string( '─', dashesRight ) );
		screen.Append( "┐\n" );

		// 3. Text Area
		for ( int i = 0; i < _textDisplayHeight; i++ ) // i is 0-based screen row within text area
		{
			screen.Append( "│" ); // Left border
			int bufferLineIndex = _viewTopRow + i;
			if ( bufferLineIndex < _buffer.Count )
			{
				string line = _buffer[bufferLineIndex].ToString();
				string visiblePart = "";
				if ( _viewLeftCol < line.Length )
				{
					visiblePart = line.Substring( _viewLeftCol );
				}

				for ( int j = 0; j < _textDisplayWidth; j++ ) // j is 0-based screen column within text area
				{
					int charIndexInVisiblePart = j;
					if ( charIndexInVisiblePart < visiblePart.Length )
					{
						screen.Append( visiblePart[charIndexInVisiblePart] );
					}
					else
					{
						screen.Append( ' ' );
					}
				}
			}
			else
			{
				screen.Append( new string( ' ', _textDisplayWidth ) ); // Empty line
			}
			// Scrollbar (simplified)
			char scrollChar = ' ';
			if ( i == 0 )
			{
				if ( _viewTopRow > 0 )
					scrollChar = '↑';
			}
			else if ( i == _textDisplayHeight - 1 )
			{
				if ( _viewTopRow + _textDisplayHeight < _buffer.Count )
					scrollChar = '↓';
			}
			else
			{
				if ( _buffer.Count > _textDisplayHeight ) // Only show scrollbar body if actual content exceeds display height
					scrollChar = '█';
			}
			screen.Append( scrollChar );
			screen.Append( "\n" );
		}

		// 4. Bottom Border
		screen.Append( "└" );
		screen.Append( new string( '─', _textDisplayWidth ) );
		screen.Append( "┘\n" );

		// 5. Status Line
		string status = _statusMessage;
		if ( string.IsNullOrWhiteSpace( status ) || (DateTime.UtcNow - _statusMessageTime).TotalSeconds > 5 )
		{
			status = $"Ln {_cursorBufferRow + 1}, Col {_cursorBufferCol + 1}";
		}
		screen.Append( status.PadRight( _textDisplayWidth + 2 ) );
		screen.Append( "\n" );

		// 6. Position ConsolePanel's cursor
		// Calculate editor cursor's position in 0-based view-relative coordinates
		int relativeViewRow = _cursorBufferRow - _viewTopRow;
		int relativeViewCol = _cursorBufferCol - _viewLeftCol;

		// Clamp to be within the visible text area, just in case ScrollToViewCursor wasn't perfect
		// or if cursor is at the very end of a line that might be equal to _textDisplayWidth
		relativeViewRow = Math.Max( 0, Math.Min( relativeViewRow, _textDisplayHeight - 1 ) );
		relativeViewCol = Math.Max( 0, Math.Min( relativeViewCol, _textDisplayWidth - 1 ) ); // Allow cursor at end of line up to width-1

		// Convert to 1-based console screen coordinates
		// Menu line is 1, Top border is 2. Text area starts at console row 3.
		// Left border char is col 1. Text area starts at console col 2.
		int targetConsoleRow = relativeViewRow + 3;
		int targetConsoleCol = relativeViewCol + 2;

		screen.Append( $"\x1B[{targetConsoleRow};{targetConsoleCol}H" );

		output.Write( screen.ToString() );
	}
}
