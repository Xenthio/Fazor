using FakeOperatingSystem;
using FakeOperatingSystem.Programs.SystemPrograms.RegEdit;

public class RegEditProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/taskmgr.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		// TODO: Replace with your actual Steam window/panel
		var window = new RegEdit();
		process.RegisterWindow( window );
		// window.Show();
	}
}
