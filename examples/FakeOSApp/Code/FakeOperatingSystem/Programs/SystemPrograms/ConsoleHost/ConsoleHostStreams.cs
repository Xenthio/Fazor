using System;
using System.Collections.Generic; // For List
using System.IO;
using System.Linq; // For LastOrDefault
using System.Text;
using System.Threading.Tasks;

namespace FakeOperatingSystem.Console;

public class ConsoleHostWriter : TextWriter
{
	private readonly Action<char> _writeAction;
	public override Encoding Encoding => Encoding.UTF8;

	public ConsoleHostWriter( Action<char> writeAction )
	{
		_writeAction = writeAction;
	}

	public override void Write( char value )
	{
		_writeAction?.Invoke( value );
	}
}

public class ConsoleHostReader : TextReader
{
	public string CurrentLine => _lineBuilder.ToString();
	private StringBuilder _lineBuilder = new();
	private TaskCompletionSource<string> _lineInputTaskSource = new();
	private TaskCompletionSource<int> _charReadTaskSource;

	public int CaretPositionInLine { get; private set; } = 0;

	private bool _expectingScanCode = false;
	private const char EXTENDED_KEY_INTRO = (char)0x00;

	private const char SCAN_CODE_UP = (char)0x48;
	private const char SCAN_CODE_DOWN = (char)0x50;
	private const char SCAN_CODE_LEFT = (char)0x4B;
	private const char SCAN_CODE_RIGHT = (char)0x4D;
	private const char SCAN_CODE_HOME = (char)0x47;
	private const char SCAN_CODE_END = (char)0x4F;
	private const char SCAN_CODE_DELETE = (char)0x53;

	// --- Internal Command History (for ReadLine) ---
	private List<string> _commandHistory = new List<string>();
	private int _currentHistoryIndex = -1;
	private string _userInputBeforeHistoryNav = null;
	private const int MAX_HISTORY_ITEMS = 50;
	private bool _isNavigatingHistory = false;


	public void SubmitChar( char c )
	{
		// Prioritize direct Read() if active
		var currentCharReadTcs = _charReadTaskSource;
		if ( currentCharReadTcs != null && !currentCharReadTcs.Task.IsCompleted )
		{
			currentCharReadTcs.SetResult( (int)c );
			// Ensure Read() operates in a raw mode, not influenced by ReadLine's state
			_expectingScanCode = false;
			_isNavigatingHistory = false;
			return; // Character consumed by Read(), bypass ReadLine logic
		}

		// If not a direct Read(), process for ReadLine features
		ProcessCharForReadLine( c );
	}

	private void ProcessCharForReadLine( char c )
	{
		if ( _expectingScanCode )
		{
			_expectingScanCode = false; // Consume the expectation
			switch ( c )
			{
				case SCAN_CODE_LEFT:
					if ( CaretPositionInLine > 0 ) CaretPositionInLine--;
					_isNavigatingHistory = false; // User is editing
					break;
				case SCAN_CODE_RIGHT:
					if ( CaretPositionInLine < _lineBuilder.Length ) CaretPositionInLine++;
					_isNavigatingHistory = false; // User is editing
					break;
				case SCAN_CODE_HOME:
					CaretPositionInLine = 0;
					_isNavigatingHistory = false; // User is editing
					break;
				case SCAN_CODE_END:
					CaretPositionInLine = _lineBuilder.Length;
					_isNavigatingHistory = false; // User is editing
					break;
				case SCAN_CODE_DELETE:
					if ( CaretPositionInLine < _lineBuilder.Length )
					{
						_lineBuilder.Remove( CaretPositionInLine, 1 );
					}
					_isNavigatingHistory = false; // User is editing
					break;
				case SCAN_CODE_UP:
					if ( _commandHistory.Count > 0 )
					{
						if ( !_isNavigatingHistory || _currentHistoryIndex == -1 || _currentHistoryIndex == _commandHistory.Count )
						{
							_userInputBeforeHistoryNav = _lineBuilder.ToString();
							_currentHistoryIndex = _commandHistory.Count - 1;
						}
						else if ( _currentHistoryIndex > 0 )
						{
							_currentHistoryIndex--;
						}
						_lineBuilder.Clear().Append( _commandHistory[_currentHistoryIndex] );
						CaretPositionInLine = _lineBuilder.Length;
						_isNavigatingHistory = true;
					}
					break;
				case SCAN_CODE_DOWN:
					if ( _isNavigatingHistory && _currentHistoryIndex != -1 )
					{
						if ( _currentHistoryIndex < _commandHistory.Count - 1 )
						{
							_currentHistoryIndex++;
							_lineBuilder.Clear().Append( _commandHistory[_currentHistoryIndex] );
						}
						else
						{
							_currentHistoryIndex = _commandHistory.Count;
							_lineBuilder.Clear().Append( _userInputBeforeHistoryNav ?? "" );
							_isNavigatingHistory = false;
						}
						CaretPositionInLine = _lineBuilder.Length;
					}
					break;
				default:
					_isNavigatingHistory = false;
					break;
			}
			return; // Scan code processed for ReadLine
		}

		if ( c == EXTENDED_KEY_INTRO )
		{
			_expectingScanCode = true;
			return; // Wait for the next char (the scan code) for ReadLine
		}

		// Any other key press means user is editing the line (for ReadLine)
		_isNavigatingHistory = false;

		if ( c == '\r' || c == '\n' )
		{
			string lineToSubmit = _lineBuilder.ToString();

			if ( !string.IsNullOrWhiteSpace( lineToSubmit ) )
			{
				if ( _commandHistory.Count == 0 || _commandHistory.Last() != lineToSubmit )
				{
					_commandHistory.Add( lineToSubmit );
				}
				if ( _commandHistory.Count > MAX_HISTORY_ITEMS )
				{
					_commandHistory.RemoveAt( 0 );
				}
			}

			_lineBuilder.Clear();
			CaretPositionInLine = 0;
			_currentHistoryIndex = -1;
			_userInputBeforeHistoryNav = null;
			// _isNavigatingHistory is already false here

			var currentLineInputTcs = _lineInputTaskSource;
			currentLineInputTcs.SetResult( lineToSubmit ); // Complete ReadLine
		}
		else if ( c == '\b' ) // Backspace for ReadLine
		{
			if ( CaretPositionInLine > 0 )
			{
				_lineBuilder.Remove( CaretPositionInLine - 1, 1 );
				CaretPositionInLine--;
			}
		}
		else if ( !char.IsControl( c ) ) // Printable char for ReadLine
		{
			if ( CaretPositionInLine < _lineBuilder.Length )
			{
				_lineBuilder.Insert( CaretPositionInLine, c );
			}
			else
			{
				_lineBuilder.Append( c );
			}
			CaretPositionInLine++;
		}
	}

	public override int Read()
	{
		var tcs = new TaskCompletionSource<int>();
		_charReadTaskSource = tcs;
		// Ensure Read() starts in a raw state, subsequent chars submitted will hit the
		// _charReadTaskSource check in SubmitChar and bypass ReadLine logic.
		// _expectingScanCode and _isNavigatingHistory are reset by the SubmitChar path for Read().

		try
		{
			int charCode = tcs.Task.Result;
			return charCode;
		}
		finally
		{
			if ( _charReadTaskSource == tcs )
			{
				_charReadTaskSource = null;
			}
		}
	}

	public override string ReadLine()
	{
		_lineInputTaskSource = new TaskCompletionSource<string>();
		// ReadLine specific state like _currentHistoryIndex, _userInputBeforeHistoryNav, 
		// and _isNavigatingHistory are managed within ProcessCharForReadLine.
		Task<string> task = _lineInputTaskSource.Task;
		task.Wait(); // Block until ProcessCharForReadLine submits a line

		string line = task.Result;
		return line;
	}
}
