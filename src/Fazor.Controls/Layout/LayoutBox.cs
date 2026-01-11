using Sandbox;
using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A layout box container panel.
/// Port from XGUI-3.
/// </summary>
[Library("layoutbox")]
public class LayoutBox : Panel
{
	public LayoutBox()
	{
		AddClass( "layout-box" );
	}
}

/// <summary>
/// An inset layout box container panel.
/// Port from XGUI-3.
/// </summary>
[Library("layoutbox")]
public class LayoutBoxInset : Panel
{
	public LayoutBoxInset()
	{
		AddClass( "layout-box-inset" );
	}
}
