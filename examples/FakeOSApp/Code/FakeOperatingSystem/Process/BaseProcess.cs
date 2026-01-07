// code/FakeOperatingSystem/Process/BaseProcess.cs
using System;
using System.Collections.Generic;
using System.IO;
using XGUI;

namespace FakeOperatingSystem;

public enum ProcessStatus
{
	Running,
	Suspended,
	Terminated
}

public abstract class BaseProcess
{
	public int ProcessId { get; internal set; }
	public string ProcessName { get; protected set; }
	public string ProcessFileName { get; protected set; }
	public string ProcessFilePath { get; protected set; }
	public Win32LaunchOptions LaunchOptions { get; protected set; }
	public ProcessManager Manager { get; internal set; }
	public ProcessStatus Status { get; set; } = ProcessStatus.Running;
	public int? ParentProcessId { get; set; }
	public DateTime StartTime { get; set; } = DateTime.Now;
	public List<XGUIPanel> OwnedWindows { get; } = new();

	// --- Console support ---
	public bool IsConsoleProcess { get; protected set; }
	public TextReader StandardInput { get; set; }
	public TextWriter StandardOutput { get; set; }
	public TextWriter StandardError { get; set; }
	public Window ConsoleWindow { get; set; }

	public abstract void Start();
	public abstract void Terminate();
}
