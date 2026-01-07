using FakeOperatingSystem;
using FakeOperatingSystem.Programs.SystemPrograms.TaskMgr;

public class TaskMgrProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/taskmgr.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		// TODO: Replace with your actual Steam window/panel
		var window = new TaskMgr();
		process.RegisterWindow( window );
		// window.Show();
	}
}
