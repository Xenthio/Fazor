using FakeDesktop;
using FakeOperatingSystem;

public class WinVerProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/System32/winver.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var window = new AboutDialog()
		{
			AppName = "XGUI-3 Test: FakeOS",
			Message = FakeOSLoader.VersionString
		};
		process.RegisterWindow( window );
	}
}
