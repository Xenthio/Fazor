using System.Collections.Generic;

namespace FakeOperatingSystem.UI.Dialogs
{
	public class FileDialogOptions
	{
		public string Title { get; set; } = "File Dialog";
		public string InitialDirectory { get; set; } = "C:/My Documents"; // Default to a common directory
		public string DefaultFileName { get; set; } = "";
		public List<FileFilter> Filters { get; set; } = new List<FileFilter>();
		public int DefaultFilterIndex { get; set; } = 0;
		public bool CheckFileExists { get; set; } = false; // For Open dialog
		public bool OverwritePrompt { get; set; } = true;  // For Save dialog
		public DialogMode Mode { get; set; } = DialogMode.Open; // Open or Save
	}

	public class FileFilter
	{
		public string Name { get; set; } // e.g., "Bitmap Files (*.bmp)"
		public string Pattern { get; set; } // e.g., "*.bmp" or "*.bmp;*.dib"

		public FileFilter( string name, string pattern )
		{
			Name = name;
			Pattern = pattern;
		}
	}

	public enum DialogMode
	{
		Open,
		Save
	}
}
