using FakeOperatingSystem.Shell;
using FakeOperatingSystem.Shell.ControlPanel.DeskCpl;
using XGUI;

public class DesktopSettingsApplet : IControlPanelApplet
{
	public string Name => "Display";
	public string IconName => "display";
	public string Description => "Change screen resolution and appearance.";

	public void Launch()
	{
		Log.Info( "Launching Desktop Settings applet." );
		var dialog = new DeskCplDialog();
		XGUISystem.Instance.Panel.AddChild( dialog );
	}
}
