namespace FakeOperatingSystem;

public class MSPaintProgram : NativeProgram
{
	public override string FilePath => "C:/Windows/System32/mspaint.exe";

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var window = new MSPaint();
		process.RegisterWindow( window );
	}
}
