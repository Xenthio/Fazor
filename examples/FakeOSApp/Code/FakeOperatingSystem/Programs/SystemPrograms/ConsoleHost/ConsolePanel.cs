using FakeOperatingSystem.Console;
using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Ensure FakeOperatingSystem and FakeOperatingSystem.Console namespaces are available if ConsoleHostWriter/Reader are in them.
// using FakeOperatingSystem;
// using FakeOperatingSystem.Console;


[StyleSheet] // This will look for ConsolePanel.scss or ConsolePanel.cs.scss
public partial class ConsolePanel : Panel
{
	public Label OutputLabel { get; private set; }
	public Panel ScrollArea { get; private set; }

	private ConsoleHostWriter writer;
	private ConsoleHostReader reader;

	// --- Grid and Caret State ---
	private List<StringBuilder> screenGrid;
	private int caretRow = 0;
	private int caretColumn = 0;
	private bool isCaretVisible = true;
	private const int GridColumns = 80; // Example, make configurable if needed
	private const int GridRows = 25;   // Example, make configurable if needed

	// To track where the current input line via 'reader' starts on the grid
	private int inputStartRow = 0;
	private int inputStartColumn = 0;

	private int characterWidth = 9;
	private int characterHeight = 16;
	private bool prevCharCausedLineWrap = false;


	// public int ReaderCaretPosition => reader?.CaretPositionInLine ?? 0; // Kept for reference, direct usage might change
	// public int CaretPosition => new StringInfo( outputBuffer.ToString() ).LengthInTextElements + ReaderCaretPosition; // Replaced by grid logic

	public TimeSince TimeSinceStart = 0;

	// private string CaretText => (ReaderCaretPosition >= reader?.CurrentLine.Length) ? "\u2002" : string.Empty; // Removed, caret is drawn
	// private string DisplayText => (outputBuffer?.ToString() ?? "") + (reader?.CurrentLine ?? "") + CaretText; // Replaced by UpdateOutputDisplay logic

	private Action<string> SetWindowTitleAction;

	public ConsolePanel()
	{
		AddClass( "console-panel" );
		ScrollArea = Add.Panel( "console-area" );
		OutputLabel = ScrollArea.Add.Label( "" );
		InitializeGrid();
		Focus();
	}

	private void InitializeGrid()
	{
		screenGrid = new List<StringBuilder>( GridRows );
		for ( int i = 0; i < GridRows; i++ )
		{
			screenGrid.Add( new StringBuilder( new string( ' ', GridColumns ) ) );
		}
		caretRow = 0;
		caretColumn = 0;
		inputStartRow = 0;
		inputStartColumn = 0;
		prevCharCausedLineWrap = false;
	}

	public void Initialize( ConsoleHostWriter writer, ConsoleHostReader reader, Action<string> setTitleAction )
	{
		this.writer = writer;
		this.reader = reader;
		SetWindowTitleAction = setTitleAction;
		InitializeGrid(); // Re-initialize grid for new session
		UpdateOutputDisplay();
	}

	public TextWriter GetOutputWriter() => writer;
	public TextReader GetInputReader() => reader;

	public void SetText( string text )
	{
		if ( OutputLabel != null )
		{
			OutputLabel.Text = text;
		}
	}

	public void TryScrollToBottom()
	{
		ScrollArea?.TryScrollToBottom();
	}

	public bool PreferScrollToBottom
	{
		get => ScrollArea?.PreferScrollToBottom ?? false;
		set
		{
			if ( ScrollArea != null )
			{
				ScrollArea.PreferScrollToBottom = value;
			}
		}
	}

	private void ScrollGridUp()
	{
		screenGrid.RemoveAt( 0 );
		screenGrid.Add( new StringBuilder( new string( ' ', GridColumns ) ) );
		if ( inputStartRow > 0 ) inputStartRow--; // Adjust input row if it scrolled off
												  // caretRow is effectively GridRows - 1 after a scroll, or needs adjustment if it was scrolled off.
												  // For simplicity, after scroll, new content appears on the last line.
	}

	private void AdvanceCaret()
	{
		caretColumn++;
		if ( caretColumn >= GridColumns )
		{
			caretColumn = 0;
			caretRow++;
			if ( caretRow >= GridRows )
			{
				caretRow = GridRows - 1;
				ScrollGridUp();
			}
			prevCharCausedLineWrap = true; // This AdvanceCaret call caused a line wrap
		}
		else
		{
			prevCharCausedLineWrap = false; // This AdvanceCaret call did NOT cause a line wrap
		}
	}

	private void EnsureCaretInBounds()
	{
		caretRow = Math.Clamp( caretRow, 0, GridRows - 1 );
		caretColumn = Math.Clamp( caretColumn, 0, GridColumns - 1 );
	}


	void UpdateOutputDisplay()
	{
		// Step 1: Prepare all lines based on the screenGrid and reader state
		List<string> potentialLines = new List<string>( GridRows );
		for ( int r = 0; r < GridRows; r++ )
		{
			string currentLineText;
			if ( reader != null && r == inputStartRow )
			{
				// Construct the input line: part from screenGrid, then reader.CurrentLine
				StringBuilder lineBuilder = new StringBuilder();
				if ( inputStartColumn > 0 && inputStartColumn < GridColumns )
				{
					lineBuilder.Append( screenGrid[r].ToString().Substring( 0, Math.Min( inputStartColumn, screenGrid[r].Length ) ) );
				}
				lineBuilder.Append( reader.CurrentLine ?? "" );

				// Determine the full line content after padding/truncating to GridColumns
				string fullLineContentBeforeConditionalTrim;
				if ( lineBuilder.Length < GridColumns )
				{
					fullLineContentBeforeConditionalTrim = lineBuilder.ToString() + new string( ' ', GridColumns - lineBuilder.Length );
				}
				else if ( lineBuilder.Length > GridColumns )
				{
					fullLineContentBeforeConditionalTrim = lineBuilder.ToString().Substring( 0, GridColumns );
				}
				else
				{
					fullLineContentBeforeConditionalTrim = lineBuilder.ToString();
				}

				// Calculate the global caret column on this full display line
				// This is the point up to which characters (including user-typed spaces) should be preserved.
				int globalCaretColumn = inputStartColumn + reader.CaretPositionInLine;

				// Ensure globalCaretColumn is within the bounds of the fullLineContentBeforeConditionalTrim
				globalCaretColumn = Math.Min( globalCaretColumn, fullLineContentBeforeConditionalTrim.Length );

				string partToKeep = fullLineContentBeforeConditionalTrim.Substring( 0, globalCaretColumn );
				string partToTrim = (globalCaretColumn < fullLineContentBeforeConditionalTrim.Length)
									? fullLineContentBeforeConditionalTrim.Substring( globalCaretColumn )
									: "";

				currentLineText = partToKeep + partToTrim.TrimEnd();
			}
			else
			{
				// For non-input lines (historical output from screenGrid), TrimEnd() is acceptable.
				currentLineText = screenGrid[r].ToString().TrimEnd();
			}
			potentialLines.Add( currentLineText );
		}

		// Step 2: Find the index of the last non-empty line
		int lastNonEmptyLineIndex = -1;
		for ( int i = potentialLines.Count - 1; i >= 0; i-- )
		{
			if ( !string.IsNullOrWhiteSpace( potentialLines[i] ) )
			{
				lastNonEmptyLineIndex = i;
				break;
			}
		}

		// Step 3: Build the fullDisplayText string using only lines up to the last non-empty one
		var fullDisplayText = new StringBuilder();
		if ( lastNonEmptyLineIndex != -1 ) // Proceed only if there's at least one non-empty line
		{
			for ( int r = 0; r <= lastNonEmptyLineIndex; r++ )
			{
				fullDisplayText.Append( potentialLines[r] );
				if ( r < lastNonEmptyLineIndex ) // Add newline only if it's not the very last line to be displayed
				{
					fullDisplayText.Append( '\n' );
				}
			}
		}

		SetText( fullDisplayText.ToString() );
	}


	public override void Tick()
	{
		base.Tick();
		PreferScrollToBottom = true; // This might need adjustment with grid scrolling
									 // If reader is active, ensure display is up-to-date (e.g. for caret blink or async changes)
		if ( reader != null )
		{
			// UpdateOutputDisplay(); // Can be performance intensive, call only when necessary
		}
	}

	public override bool HasContent => true;

	private int GetLinearCaretIndexForDrawing()
	{
		if ( OutputLabel == null || string.IsNullOrEmpty( OutputLabel.Text ) ) return 0;

		// Split the displayed text into lines. OutputLabel.Text contains '\n'.
		string[] renderedLines = OutputLabel.Text.Split( '\n' );

		int logicalCaretRow;
		int logicalCaretColumn;

		if ( reader != null )
		{
			logicalCaretRow = inputStartRow;
			// Caret column is relative to the start of the input area on the grid,
			// plus the caret position within the reader's current line.
			logicalCaretColumn = inputStartColumn + reader.CaretPositionInLine;
		}
		else
		{
			// In direct output mode, caretRow and caretColumn track the cursor position on the grid.
			logicalCaretRow = caretRow;
			logicalCaretColumn = caretColumn;
		}

		// Clamp the logical row to the actual number of rendered lines.
		// renderedLines.Length will be at least 1 if OutputLabel.Text is not empty.
		logicalCaretRow = Math.Clamp( logicalCaretRow, 0, renderedLines.Length - 1 );

		int linearIndex = 0;
		// Sum the lengths of all lines before the target caret row.
		// Add 1 for each newline character.
		for ( int i = 0; i < logicalCaretRow; i++ )
		{
			linearIndex += renderedLines[i].Length + 1; // +1 for the '\n'
		}

		// Get the actual content of the line where the caret is.
		string targetLineContent = renderedLines[logicalCaretRow];

		// Clamp the logical column to the actual length of the trimmed target line.
		// This ensures the caret doesn't go beyond the visible content of the line.
		int actualCaretColumnOnLine = Math.Clamp( logicalCaretColumn, 0, targetLineContent.Length );

		linearIndex += actualCaretColumnOnLine;

		// Clamp the final linear index to be within the bounds of OutputLabel.Text.
		// OutputLabel.GetCaretRect typically expects an index from 0 to Text.Length.
		return Math.Clamp( linearIndex, 0, OutputLabel.Text.Length );
	}

	public override void DrawContent( ref RenderState state )
	{
		base.DrawContent( ref state );

		if ( OutputLabel == null || !isCaretVisible ) return;
		// If reader is null, caret is at (caretRow, caretColumn) of the grid.
		// If reader is active, caret is at (inputStartRow, inputStartColumn + reader.CaretPositionInLine).

		var blinkRate = 0.8f;
		var blink = (TimeSinceStart * blinkRate) % blinkRate < (blinkRate * 0.5f);

		if ( !blink ) return; // Don't draw if in the "off" part of the blink

		int linearCaretDrawIndex = GetLinearCaretIndexForDrawing();

		var caretRect = OutputLabel.GetCaretRect( linearCaretDrawIndex );



		caretRect.Left = MathX.FloorToInt( caretRect.Left );
		caretRect.Width = characterWidth;
		var charHeight = (OutputLabel.ComputedStyle.FontSize?.Value ?? characterHeight);
		var caretHeight = charHeight / 4;

		caretRect.Top = MathX.FloorToInt( caretRect.Top + charHeight - caretHeight );
		caretRect.Height = caretHeight;

		var color = OutputLabel.ComputedStyle.FontColor ?? Color.White;
		// color = color.WithAlpha( blink ? 1.0f : 0f ); // Blink logic moved up

		Graphics.DrawRoundedRectangle( caretRect, color );
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		base.OnButtonTyped( e );
		if ( reader == null ) return;

		// Submit the character to the reader, which will update its internal state.
		// The reader's state (CurrentLine, CaretPositionInLine) will then be used by UpdateOutputDisplay.
		if ( e.Button == "enter" ) SubmitToReader( '\n' );
		else if ( e.Button == "backspace" ) SubmitToReader( '\b' );
		else if ( e.Button == "tab" ) SubmitToReader( '\t' );
		else if ( e.Button == "escape" ) SubmitToReader( (char)27 );
		else if ( e.Button == "up" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x48 ); }
		else if ( e.Button == "down" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x50 ); }
		else if ( e.Button == "left" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x4B ); }
		else if ( e.Button == "right" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x4D ); }
		else if ( e.Button == "home" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x47 ); }
		else if ( e.Button == "end" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x4F ); }
		else if ( e.Button == "delete" ) { SubmitToReader( (char)0x00 ); SubmitToReader( (char)0x53 ); }
		else if ( e.Button == "pageup" ) SubmitToReader( (char)0x1B );
		else if ( e.Button == "pagedown" ) SubmitToReader( (char)0x1B );
		else if ( e.Button.StartsWith( "f" ) && e.Button.Length > 1 && char.IsDigit( e.Button[1] ) ) SubmitToReader( (char)0x1B );
		else if ( e.HasCtrl && e.Button == "z" ) SubmitToReader( (char)0x1A );

		e.StopPropagation = true;
	}

	public override void OnKeyTyped( char k )
	{
		base.OnKeyTyped( k );
		SubmitToReader( k );
	}

	// Renamed from Submit to avoid confusion with actual line submission
	void SubmitToReader( char c )
	{
		if ( reader == null ) return;

		string lineBeforeSubmit = reader.CurrentLine;
		int caretBeforeSubmit = reader.CaretPositionInLine;

		reader.SubmitChar( c ); // This might trigger ReadLine completion if c is '\n'

		if ( c == '\n' )
		{
			// The line was submitted. Print it to the grid.
			// ConsoleHostReader.SubmitChar already handles history and calls _lineInputTaskSource.SetResult.
			// The line that was just completed is 'lineBeforeSubmit'.
			PrintLineToGrid( lineBeforeSubmit, inputStartRow, inputStartColumn );

			caretColumn = 0; // New line starts at column 0
			caretRow = inputStartRow + 1; // Advance to the next line
			if ( caretRow >= GridRows )
			{
				caretRow = GridRows - 1;
				ScrollGridUp(); // This also adjusts inputStartRow if it was scrolled
			}
			inputStartRow = caretRow;
			inputStartColumn = 0; // New input always starts at column 0 of the new line
			prevCharCausedLineWrap = false; // Explicit newline submission resets this.
		}

		UpdateOutputDisplay();
		TryScrollToBottom(); // May need more nuanced scrolling
	}

	private void PrintLineToGrid( string line, int row, int startCol )
	{
		if ( row < 0 || row >= GridRows ) return;

		// Clear the part of the line in screenGrid that will be overwritten
		for ( int i = startCol; i < GridColumns; ++i )
		{
			if ( i < screenGrid[row].Length ) screenGrid[row][i] = ' ';
			else screenGrid[row].Append( ' ' ); // Should not happen if initialized correctly
		}
		if ( screenGrid[row].Length > GridColumns ) screenGrid[row].Length = GridColumns;


		for ( int i = 0; i < line.Length; ++i )
		{
			if ( startCol + i < GridColumns )
			{
				screenGrid[row][startCol + i] = line[i];
			}
			else break; // Stop if line exceeds grid width
		}
	}


	private enum EscapeSequenceState { None, GotEscape, GotCSI, GotOSC }
	private EscapeSequenceState currentEscapeState = EscapeSequenceState.None;
	private StringBuilder csiParameterBuffer = new StringBuilder();
	private StringBuilder oscStringBuffer = new StringBuilder();
	private bool csiGotQuestionMark = false;

	public void AppendOutput( char c )
	{
		if ( currentEscapeState == EscapeSequenceState.None && c != 0x1B )
		{
			if ( c == '\b' )
			{
				if ( caretColumn > 0 )
				{
					caretColumn--;
					screenGrid[caretRow][caretColumn] = ' '; // Erase char
				}
				// else if (caretRow > 0) { /* Handle backspace to previous line - complex */ }
				prevCharCausedLineWrap = false;
			}
			else if ( c == '\t' )
			{
				int spacesToInsert = 4 - (caretColumn % 4);
				for ( int i = 0; i < spacesToInsert && caretColumn < GridColumns; i++ )
				{
					screenGrid[caretRow][caretColumn] = ' ';
					AdvanceCaret(); // AdvanceCaret will update prevCharCausedLineWrap
				}
			}
			else if ( c == '\r' )
			{
				caretColumn = 0;
				prevCharCausedLineWrap = false;
			}
			else if ( c == '\n' )
			{
				if ( prevCharCausedLineWrap )
				{
					// Previous char caused a wrap, so this \n is "consumed"
					caretColumn = 0;
				}
				else
				{
					// Normal newline
					caretColumn = 0;
					caretRow++;
					if ( caretRow >= GridRows )
					{
						caretRow = GridRows - 1;
						ScrollGridUp();
					}
				}
				prevCharCausedLineWrap = false; // Newline resets the flag.
			}
			else if ( c == 00 || c == '\u001A' || c == '\u000E' ) { /* Ignore */ }
			else // Printable character
			{
				if ( caretRow >= 0 && caretRow < GridRows && caretColumn >= 0 && caretColumn < GridColumns )
				{
					screenGrid[caretRow][caretColumn] = c;
				}
				AdvanceCaret(); // This will set/reset prevCharCausedLineWrap
			}

			// If reader is active, output pushes the input prompt start position
			if ( reader != null )
			{
				inputStartRow = caretRow;
				inputStartColumn = caretColumn;
			}
			UpdateOutputDisplay();
			return;
		}

		if ( c == 0x1B ) // Always handle ESC first
		{
			currentEscapeState = EscapeSequenceState.GotEscape;
			csiParameterBuffer.Clear();
			oscStringBuffer.Clear();
			csiGotQuestionMark = false;
			prevCharCausedLineWrap = false; // Starting an escape sequence resets the flag
			return;
		}

		DoEscape( c );
	}


	public void DoEscape( char c )
	{
		bool bufferModified = false;
		switch ( currentEscapeState )
		{
			case EscapeSequenceState.GotEscape:
				if ( c == '[' ) { currentEscapeState = EscapeSequenceState.GotCSI; csiGotQuestionMark = false; }
				else if ( c == ']' ) { currentEscapeState = EscapeSequenceState.GotOSC; oscStringBuffer.Clear(); }
				else { currentEscapeState = EscapeSequenceState.None; prevCharCausedLineWrap = false; } // Escape sequence aborted
				break;

			case EscapeSequenceState.GotCSI:
				if ( char.IsDigit( c ) || c == ';' )
				{
					csiParameterBuffer.Append( c );
				}
				else if ( c == '?' && csiParameterBuffer.Length == 0 )
				{
					csiGotQuestionMark = true;
				}
				else
				{
					string paramsStr = csiParameterBuffer.ToString();
					string[] ps = paramsStr.Split( ';' );

					if ( csiGotQuestionMark )
					{
						if ( paramsStr == "25" )
						{
							if ( c == 'h' ) { isCaretVisible = true; }
							else if ( c == 'l' ) { isCaretVisible = false; }
						}
					}
					else
					{
						switch ( c )
						{
							case 'J':
								int eraseModeJ = ps.Length > 0 && int.TryParse( ps[0], out int valJ ) ? valJ : 0;
								if ( eraseModeJ == 2 )
								{
									InitializeGrid();
									caretRow = 0; caretColumn = 0;
								}
								else if ( eraseModeJ == 0 )
								{
									for ( int col = caretColumn; col < GridColumns; ++col ) screenGrid[caretRow][col] = ' ';
									for ( int row = caretRow + 1; row < GridRows; ++row ) screenGrid[row] = new StringBuilder( new string( ' ', GridColumns ) );
								}
								else if ( eraseModeJ == 1 )
								{
									for ( int col = 0; col < caretColumn; ++col ) screenGrid[caretRow][col] = ' ';
									for ( int row = 0; row < caretRow; ++row ) screenGrid[row] = new StringBuilder( new string( ' ', GridColumns ) );
								}
								bufferModified = true;
								break;
							case 'H':
								int rowH = ps.Length > 0 && int.TryParse( ps[0], out int rVal ) ? rVal - 1 : 0;
								int colH = ps.Length > 1 && int.TryParse( ps[1], out int cVal ) ? cVal - 1 : 0;
								caretRow = Math.Clamp( rowH, 0, GridRows - 1 );
								caretColumn = Math.Clamp( colH, 0, GridColumns - 1 );
								if ( reader != null )
								{
									inputStartRow = caretRow;
									inputStartColumn = caretColumn;
								}
								bufferModified = true;
								break;
							case 'm':
								break;
							case 'A':
								int countA = ps.Length > 0 && int.TryParse( ps[0], out int valA ) ? valA : 1;
								caretRow = Math.Max( 0, caretRow - countA );
								if ( reader != null ) inputStartRow = caretRow;
								bufferModified = true;
								break;
							case 'B':
								int countB = ps.Length > 0 && int.TryParse( ps[0], out int valB ) ? valB : 1;
								caretRow = Math.Min( GridRows - 1, caretRow + countB );
								if ( reader != null ) inputStartRow = caretRow;
								bufferModified = true;
								break;
							case 'C':
								int countC = ps.Length > 0 && int.TryParse( ps[0], out int valC ) ? valC : 1;
								caretColumn = Math.Min( GridColumns - 1, caretColumn + countC );
								if ( reader != null ) inputStartColumn = caretColumn;
								bufferModified = true;
								break;
							case 'D':
								int countD = ps.Length > 0 && int.TryParse( ps[0], out int valD ) ? valD : 1;
								caretColumn = Math.Max( 0, caretColumn - countD );
								if ( reader != null ) inputStartColumn = caretColumn;
								bufferModified = true;
								break;
						}
					}
					currentEscapeState = EscapeSequenceState.None;
					csiParameterBuffer.Clear();
					csiGotQuestionMark = false;
					prevCharCausedLineWrap = false; // CSI sequence processed, reset flag
				}
				break;

			case EscapeSequenceState.GotOSC:
				if ( c == '\u0007' || (c == 0x1B && oscStringBuffer.Length > 0 && oscStringBuffer[oscStringBuffer.Length - 1] == '\\') )
				{
					string oscCommand = oscStringBuffer.ToString();
					if ( oscCommand.StartsWith( "0;" ) || oscCommand.StartsWith( "2;" ) )
					{
						SetWindowTitleAction?.Invoke( oscCommand.Substring( 2 ) );
					}
					currentEscapeState = EscapeSequenceState.None;
					oscStringBuffer.Clear();
					prevCharCausedLineWrap = false; // OSC sequence finished
				}
				else if ( c == 0x1B ) // Could be an ST (ESC \) or a new ESC starting another sequence
				{
					currentEscapeState = EscapeSequenceState.None; // Assume current OSC is aborted or ending.
					oscStringBuffer.Clear();
					prevCharCausedLineWrap = false;
				}
				else
				{
					oscStringBuffer.Append( c );
				}
				break;
		}
		if ( bufferModified ) { UpdateOutputDisplay(); }
	}
}
