using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A horizontal separator line for UI layouts.
/// Port from XGUI-3.
/// </summary>
public class Separator : Panel
{
	public Separator()
	{
		AddClass( "separator" );
	}
}

/// <summary>
/// A vertical separator line for UI layouts.
/// Port from XGUI-3.
/// </summary>
public class SeparatorVertical : Panel
{
	public SeparatorVertical()
	{
		AddClass( "separator-vertical" );
	}
}
