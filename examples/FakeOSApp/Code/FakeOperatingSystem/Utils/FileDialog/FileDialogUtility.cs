using System.Threading.Tasks; // Required for TaskCompletionSource
using XGUI; // Required for XGUISystem

namespace FakeOperatingSystem.UI.Dialogs
{
	public static class FileDialogUtility
	{
		public static Task<string> ShowOpenFileDialogAsync( FileDialogOptions options = null )
		{
			options ??= new FileDialogOptions();
			options.Mode = DialogMode.Open;
			options.Title = string.IsNullOrEmpty( options.Title ) || options.Title == "File Dialog" ? "Open" : options.Title;
			options.CheckFileExists = true;

			return ShowDialogAsync( options );
		}

		public static Task<string> ShowSaveFileDialogAsync( FileDialogOptions options = null )
		{
			options ??= new FileDialogOptions();
			options.Mode = DialogMode.Save;
			options.Title = string.IsNullOrEmpty( options.Title ) || options.Title == "File Dialog" ? "Save As" : options.Title;
			options.OverwritePrompt = true;

			return ShowDialogAsync( options );
		}

		private static Task<string> ShowDialogAsync( FileDialogOptions options )
		{
			var tcs = new TaskCompletionSource<string>();

			var dialog = new FileDialog(); // Use the default constructor
			dialog.Options = options;      // Set parameters after instantiation
			dialog.OnSuccess = ( selectedPath ) => tcs.TrySetResult( selectedPath );
			dialog.OnCancel = () => tcs.TrySetResult( null );

			XGUISystem.Instance?.Panel?.AddChild( dialog );
			// dialog.Focus(); // Dialog should handle its own focus logic if needed

			return tcs.Task;
		}
	}
}
