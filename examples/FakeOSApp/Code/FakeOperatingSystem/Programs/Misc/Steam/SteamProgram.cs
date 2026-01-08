using FakeOperatingSystem;
using FakeSteam;

public class SteamProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Program Files/Steam/steam.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		// TODO: Replace with your actual Steam window/panel
		var window = new GameLauncher();
		process.RegisterWindow( window );
		// window.Show();
	}
}
