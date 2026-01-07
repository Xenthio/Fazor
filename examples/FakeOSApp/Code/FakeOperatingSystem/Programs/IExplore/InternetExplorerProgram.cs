using FakeOperatingSystem;

public class InternetExplorerProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Program Files/Internet Explorer/iexplore.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		// TODO: Replace with your actual Internet Explorer window/panel
		// var window = new InternetExplorerWindow();
		// process.RegisterWindow(window);
		// window.Show();
	}
}
