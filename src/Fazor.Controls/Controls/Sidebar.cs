using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A sidebar container, essentially a TabContainer styled as a sidebar navigation menu.
/// Port from XGUI-3.
/// </summary>
public class Sidebar : TabContainer
{
	public Sidebar()
	{
		RemoveClass( "TabContainer" );
		AddClass( "Sidebar" );
	}
}
