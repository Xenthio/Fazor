using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A custom Panel implementation that properly supports CSS tag selectors
/// by implementing the ElementTagName property correctly.
/// Port from XGUI-3.
/// </summary>
public class CustomTagPanel : Panel, IStyleTarget
{
	private readonly string _tagName;

	public CustomTagPanel( string tagName )
	{
		_tagName = tagName;
	}

	// Implement IStyleTarget.ElementName to return our custom tag name
	string IStyleTarget.ElementName => _tagName;

	// We need to ensure the HasClasses method works properly for CSS selectors
	bool IStyleTarget.HasClasses( string[] classes )
	{
		if ( Classes == null || !Classes.Any() ) return false;

		foreach ( var cls in classes )
		{
			if ( !Classes.Contains( cls ) )
				return false;
		}

		return true;
	}

	// Other IStyleTarget methods (only if needed)
	string IStyleTarget.Id => GetAttribute( "id" ) ?? "";
	PseudoClass IStyleTarget.PseudoClass => PseudoClass.None;
	IStyleTarget? IStyleTarget.Parent => Parent as IStyleTarget;
	int IStyleTarget.SiblingIndex => 0;
}
