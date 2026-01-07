// code/FakeOperatingSystem/Process/ProcessManager.cs
using System.Collections.Generic;

namespace FakeOperatingSystem;
public class ProcessManager
{
	public static ProcessManager Instance { get; private set; }
	private List<BaseProcess> _processes = new();
	private int _lastProcessId = 0;
	public ProcessManager()
	{
		Instance?.TerminateAll();
		Instance = this;
	}
	public void RegisterProcess( BaseProcess process )
	{
		if ( !_processes.Contains( process ) )
		{
			process.Manager = this;
			_processes.Add( process );
			process.ProcessId = ++_lastProcessId;
		}
	}
	public void TerminateProcess( BaseProcess process )
	{
		if ( _processes.Contains( process ) )
		{
			process.Terminate();
			_processes.Remove( process );
		}
	}
	public void TerminateAll()
	{
		foreach ( var process in _processes.ToArray() )
		{
			process.Terminate();
		}
		_processes.Clear();
	}
	public IEnumerable<BaseProcess> GetProcesses() => _processes;
	public int GetProcessCount() => _processes.Count;

	/// <summary>
	/// Opens an executable, deciding if it's a NativeProcess or X86PEProcess.
	/// </summary>
	public BaseProcess OpenExecutable( string exePath, Win32LaunchOptions options, bool shellLaunch = false )
	{
		// Try to load as a NativeProgram (fake exe)
		var nativeProgram = NativeProgram.ReadFromExe( exePath );


		// If it's a console app, and we dont have anywhere to send IO, we need to create a console host
		if ( nativeProgram != null &&
			nativeProgram.ConsoleApp &&
			options.StandardOutputOverride == null &&
			options.StandardInputOverride == null )
		{
			var conProcess = OpenExecutable( "C:/Windows/System32/conhost.exe", new Win32LaunchOptions
			{
				Arguments = $"\"{exePath}\" {options.Arguments}",
				ParentProcessId = options.ParentProcessId,
			} );
			return conProcess;
		}

		BaseProcess process;
		if ( nativeProgram != null )
		{
			process = new NativeProcess( nativeProgram, options );
		}
		else
		{
			// Fallback: treat as real PE
			process = new X86PEProcess( exePath, options );
		}
		RegisterProcess( process );

		if ( options.StandardInputOverride != null )
		{
			process.StandardInput = options.StandardInputOverride;
		}
		if ( options.StandardOutputOverride != null )
		{
			process.StandardOutput = options.StandardOutputOverride;
		}
		process.Start();
		return process;
	}

	public IEnumerable<BaseProcess> GetChildProcesses( int parentId )
	{
		foreach ( var process in _processes )
		{
			if ( process.ParentProcessId == parentId )
				yield return process;
		}
	}

	public BaseProcess GetProcessById( int processId )
	{
		foreach ( var process in _processes )
		{
			if ( process.ProcessId == processId )
			{
				return process;
			}
		}
		return null;
	}
}
