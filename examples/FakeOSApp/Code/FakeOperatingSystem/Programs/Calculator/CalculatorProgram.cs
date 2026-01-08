namespace FakeOperatingSystem;

public class CalculatorProgram : NativeProgram
{
	public override string FilePath => "C:/Windows/calc.exe";

	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var window = new Calculator();
		process.RegisterWindow( window );
	}
}
