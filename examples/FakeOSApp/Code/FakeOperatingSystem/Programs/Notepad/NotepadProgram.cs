// code/FakeOperatingSystem/Programs/Notepad/NotepadProgram.cs
using FakeDesktop;

namespace FakeOperatingSystem;
public class NotepadProgram : NativeProgram
{
	public override string FilePath => "FakeSystemRoot/Windows/Notepad.exe";
	public override void Main( NativeProcess process, Win32LaunchOptions launchOptions = null )
	{
		var window = new Notepad();
		if ( launchOptions != null )
		{
			window.Arguments = launchOptions.Arguments;
		}
		process.RegisterWindow( window ); ;
	}
}
