using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// A themeable panel designed to be at the root of control hierarchies.
/// Provides theme management capabilities for Fazor controls.
/// </summary>
public class FazorPanel : Panel
{
	public string CurrentTheme = "";

	public FazorPanel()
	{
		AddClass( "fazor-panel" );
	}

	public void SetTheme( string theme )
	{
		var parent = this.Parent;

		// Remove existing style sheets (except .razor.scss ones) 
		foreach ( var style in AllStyleSheets.ToList() )
		{
			if ( !style.FileName.EndsWith( ".razor.scss" ) && !style.FileName.EndsWith( ".cs.scss" ) )
			{
				StyleSheet.Remove( style.FileName );
			}
		}
		CurrentTheme = theme;
		var styleToApply = Sandbox.UI.StyleSheet.FromFile( theme );

		// Apply the new style
		StyleSheet.Add( styleToApply );

		// Force immediate style update
		Style.Dirty();

		// Force a complete rebuild by temporarily removing from parent and re-adding
		Parent = null;
		Parent = parent;

		// Force layout recalculation - traverse child hierarchy
		ForceStyleUpdateRecursive( this );
	}

	private void ForceStyleUpdateRecursive( Panel panel )
	{
		// Mark this panel's style as dirty to force recalculation
		panel.Style.Dirty();

		// Update all immediate children
		foreach ( var child in panel.Children )
		{
			if ( child == null || !child.IsValid() ) continue;

			// Mark the child's style as dirty
			child.Style.Dirty();

			// Recursively update this child's children
			ForceStyleUpdateRecursive( child );
		}
	}
}
