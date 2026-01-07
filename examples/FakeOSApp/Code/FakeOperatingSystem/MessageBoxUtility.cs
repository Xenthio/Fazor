using FakeOperatingSystem;
using Sandbox;
using System;
using System.Threading.Tasks;
using XGUI;

namespace FakeDesktop;

public enum MessageBoxIcon
{
	None,
	Information,
	Warning,
	Error,
	Question
}

public enum MessageBoxButtons
{
	OK,
	OKCancel,
	YesNo,
	YesNoCancel,
	RetryCancel,
	AbortRetryIgnore
}

public enum MessageBoxResult
{
	None,
	OK,
	Cancel,
	Yes,
	No,
	Abort,
	Retry,
	Ignore
}
/// <summary>
/// Utility class for displaying Windows 98-style message boxes
/// </summary>
public static class MessageBoxUtility
{

	/// <summary>
	/// Shows a message box with an OK button
	/// </summary>
	public static void Show( string message, string title = "Message" )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Information, MessageBoxButtons.OK, null );
	}

	/// <summary>
	/// Shows an information message box with an OK button
	/// </summary>
	public static void ShowInformation( string message, string title = "Information", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Information, MessageBoxButtons.OK, callback );
	}

	/// <summary>
	/// Shows a warning message box with an OK button
	/// </summary>
	public static void ShowWarning( string message, string title = "Warning", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Warning, MessageBoxButtons.OK, callback );
	}

	/// <summary>
	/// Shows an error message box with an OK button
	/// </summary>
	public static void ShowError( string message, string title = "Error", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Error, MessageBoxButtons.OK, callback );
	}

	/// <summary>
	/// Shows a yes/no confirmation dialog
	/// </summary>
	public static void Confirm( string message, string title = "Confirm", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Question, MessageBoxButtons.YesNo, callback );
	}

	/// <summary>
	/// Shows a yes/no/cancel confirmation dialog
	/// </summary>
	public static void ConfirmWithCancel( string message, string title = "Confirm", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Question, MessageBoxButtons.YesNoCancel, callback );
	}

	/// <summary>
	/// Shows a yes/no/cancel warning dialog
	/// </summary>
	public static void WarningConfirmWithCancel( string message, string title = "Confirm", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Warning, MessageBoxButtons.YesNoCancel, callback );
	}

	/// <summary>
	/// Shows a retry/cancel error dialog
	/// </summary>
	public static void RetryCancel( string message, string title = "Error", Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, MessageBoxIcon.Error, MessageBoxButtons.RetryCancel, callback );
	}

	/// <summary>
	/// Shows an advanced message box with custom options
	/// </summary>
	public static void ShowCustom( string message, string title, MessageBoxIcon icon, MessageBoxButtons buttons, Action<MessageBoxResult> callback = null )
	{
		CreateMessageBox( message, title, icon, buttons, callback );
	}

	/// <summary>
	/// Code execution blocking custom message box.
	/// </summary>
	public static async Task<MessageBoxResult> ShowBlocking( string message, string title, MessageBoxIcon icon, MessageBoxButtons buttons )
	{
		MessageBoxResult result = MessageBoxResult.None;
		CreateMessageBox( message, title, icon, buttons, ( res ) =>
		{
			result = res;
		} );
		while ( result == MessageBoxResult.None )
		{
			await Task.Yield();
		}
		return result;
	}

	private static void CreateMessageBox( string message, string title, MessageBoxIcon icon, MessageBoxButtons buttons, Action<MessageBoxResult> callback )
	{
		var xguiSystem = Game.ActiveScene.GetSystem<XGUISystem>();
		if ( xguiSystem == null || xguiSystem.Panel == null )
			return;

		// Create the message box
		var msgBox = TypeLibrary.Create<MessageBox>( "MessageBox" );
		msgBox.Message = message;
		msgBox.Title = title;
		msgBox.Icon = icon;
		msgBox.Buttons = buttons;

		// Calculate ideal size based on message length
		int messageLength = message.Length;
		int width = Math.Max( 350, Math.Min( 500, 300 + messageLength * 2 ) );
		int height = Math.Max( 150, 120 + (messageLength / 40) * 20 ); // Add height for each ~40 chars

		msgBox.Width = width;
		msgBox.Height = height;

		// Set the callback
		msgBox.SetCallback( callback );

		// Add to the UI
		xguiSystem.Panel.AddChild( msgBox );

		// Center in parent
		msgBox.PositionAtScreenCenter();

		// Play system sound
		// In windows98, they're all chord.
		switch ( icon )
		{
			case MessageBoxIcon.Error:
			case MessageBoxIcon.Warning:
			case MessageBoxIcon.Information:
			case MessageBoxIcon.Question:
				Sound.PlayFile( ThemeResources.ChordSoundFile );
				break;
		}
		/*		switch ( icon )
				{
					case MessageBoxIcon.Error:
						var soundpath = XGUISoundSystem.GetSound( "CHORD" );
						var soundfile = SoundFile.Load( soundpath );
						Sound.PlayFile( soundfile );
						break;
					case MessageBoxIcon.Warning:
						soundpath = XGUISoundSystem.GetSound( "EXCLAMATION" );
						soundfile = SoundFile.Load( soundpath );
						Sound.PlayFile( soundfile );
						break;
					case MessageBoxIcon.Information:
						soundpath = XGUISoundSystem.GetSound( "DING" );
						soundfile = SoundFile.Load( soundpath );
						Sound.PlayFile( soundfile );
						break;
					case MessageBoxIcon.Question:
						soundpath = XGUISoundSystem.GetSound( "CHIMES" );
						soundfile = SoundFile.Load( soundpath );
						Sound.PlayFile( soundfile );
						break;
				}*/

		// Bring to front and focus
		msgBox.FocusWindow();
	}

	// ConfirmWithCancelAsync
	public static async Task<MessageBoxResult> ConfirmWithCancelAsync( string message, string title = "Confirm" )
	{
		return await ShowBlocking( message, title, MessageBoxIcon.Question, MessageBoxButtons.YesNoCancel );
	}
}
