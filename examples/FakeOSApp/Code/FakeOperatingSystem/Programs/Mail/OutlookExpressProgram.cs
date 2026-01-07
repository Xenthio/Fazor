using FakeOperatingSystem;

public class OutlookExpressProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Program Files/Outlook Express/outlook.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		// TODO: Replace with your actual Outlook Express window/panel
		// var window = new OutlookExpressWindow();
		// process.RegisterWindow(window);
		// window.Show();
	}
}
