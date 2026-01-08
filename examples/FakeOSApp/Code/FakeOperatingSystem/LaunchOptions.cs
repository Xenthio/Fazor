// code/FakeOperatingSystem/Process/Win32LaunchOptions.cs
using System.Collections.Generic;
using System.IO;

namespace FakeOperatingSystem;

/// <summary>
/// Options for launching a Win32 or native process.
/// </summary>
public class Win32LaunchOptions
{
	/// <summary>
	/// The command-line arguments to pass to the process.
	/// </summary>
	public string Arguments { get; set; } = "";

	/// <summary>
	/// The working directory for the process.
	/// </summary>
	public string WorkingDirectory { get; set; } = "";

	/// <summary>
	/// Environment variables for the process.
	/// </summary>
	public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

	/// <summary>
	/// The window state (Normal, Minimized, Maximized).
	/// </summary>
	public WindowState WindowState { get; set; } = WindowState.Normal;

	/// <summary>
	/// If true, the process should be started with administrative privileges.
	/// </summary>
	public bool RunAsAdministrator { get; set; } = false;

	/// <summary>
	/// Optional user name for running the process.
	/// </summary>
	public string UserName { get; set; } = "";

	/// <summary>
	/// Optional domain for the user.
	/// </summary>
	public string Domain { get; set; } = "";

	/// <summary>
	/// Optional password for the user (if needed).
	/// </summary>
	public string Password { get; set; } = "";

	/// <summary>
	/// If true, the process should be started hidden.
	/// </summary>
	public bool StartHidden { get; set; } = false;

	/// <summary>
	/// If true, the process should not create a window.
	/// </summary>
	public bool NoWindow { get; set; } = false;

	/// <summary>
	/// Optional parent process ID (for process trees).
	/// </summary>
	public int? ParentProcessId { get; set; }

	/// <summary>
	/// Optional desktop name (for GUI isolation).
	/// </summary>
	public string Desktop { get; set; } = "";

	/// <summary>
	/// Optional session ID.
	/// </summary>
	public int? SessionId { get; set; }

	/// <summary>
	/// Output steam
	/// </summary>
	public TextWriter StandardOutputOverride { get; set; } = null;

	/// <summary>
	/// Input stream
	/// </summary>
	public TextReader StandardInputOverride { get; set; } = null;
}

/// <summary>
/// Window state for process launch.
/// </summary>
public enum WindowState
{
	Normal,
	Minimized,
	Maximized,
	Hidden
}
