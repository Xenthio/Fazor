using Sandbox;
using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// An item in a SelectList control.
/// Port from XGUI-3.
/// </summary>
[Library("listoption")]
public class ListOption : Panel
{
	private bool _selected;

	public bool Selected
	{
		get => _selected;
		set
		{
			SetClass( "selected", value );
			_selected = value;
		}
	}

	public SelectList? ParentList { get; set; }

	public ListOption()
	{
		AddClass( "listoption" );
	}

	protected override void OnClick( MousePanelEvent e )
	{
		base.OnClick( e );
		if ( ParentList == null )
		{
			Console.WriteLine( "ListOption used outside of SelectList" );
			return;
		}

		ParentList.OptionSelected( this );
	}
}
