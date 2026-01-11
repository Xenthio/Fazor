using Sandbox;
using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A simple list selection control, similar to Avalonia's ListBox.
/// Port from XGUI-3.
/// </summary>
[Library("selectlist")]
public class SelectList : Panel
{
	public ListOption? SelectedOption { get; private set; }
	
	public event Action<ListOption>? OnSelectionChanged;

	public SelectList()
	{
		AddClass( "selectlist" );
	}

	protected override void OnChildAdded( Panel child )
	{
		base.OnChildAdded( child );
		if ( child is ListOption opt )
		{
			opt.ParentList = this;
		}
	}

	public void OptionSelected( ListOption option )
	{
		if ( SelectedOption != null ) 
			SelectedOption.Selected = false;
		
		SelectedOption = option;
		SelectedOption.Selected = true;
		
		OnSelectionChanged?.Invoke( option );
	}
}
