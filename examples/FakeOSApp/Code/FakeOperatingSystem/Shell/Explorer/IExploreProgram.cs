using FakeOperatingSystem;
using FakeOperatingSystem.Shell.Explorer;

public class IExploreProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Program Files/Internet Explorer/Iexplore.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var window = new IExplore();
		process.RegisterWindow( window );
	}
}
