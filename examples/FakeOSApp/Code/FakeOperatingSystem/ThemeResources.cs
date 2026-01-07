using Sandbox;
using XGUI;

namespace FakeOperatingSystem;

public class ThemeResources
{

	public static SoundFile ChordSoundFile;

	public static void ReloadAll()
	{
		// Reload all theme resources
		ChordSoundFile = SoundFile.Load( XGUISoundSystem.GetSound( "CHORD" ) );
	}
}
