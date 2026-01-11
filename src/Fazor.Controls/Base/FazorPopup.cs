using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// Extended popup panel with positioning modes and additional features.
/// Based on XGUI-3's XGUIPopup but simplified for Fazor.
/// </summary>
public class FazorPopup : BasePopup
{
	/// <summary>
	/// Which panel triggered this popup.
	/// </summary>
	public Panel? PopupSource { get; set; }

	/// <summary>
	/// Positioning mode for this popup.
	/// </summary>
	public PositionMode Position { get; set; }

	/// <summary>
	/// Offset away from PopupSource based on Position.
	/// </summary>
	public float PopupSourceOffset { get; set; }

	/// <summary>
	/// Dictates where a Popup is positioned.
	/// </summary>
	public enum PositionMode
	{
		/// <summary>
		/// To the left of the source panel, centered.
		/// </summary>
		Left,

		/// <summary>
		/// To the left of the source panel, aligned to the bottom.
		/// </summary>
		LeftBottom,

		Right,
		RightBottom,

		/// <summary>
		/// Above the source panel, aligned to the left.
		/// </summary>
		AboveLeft,

		/// <summary>
		/// Below the source panel, aligning on the left.
		/// </summary>
		BelowLeft,

		/// <summary>
		/// Below the source panel, centered horizontally.
		/// </summary>
		BelowCenter,

		/// <summary>
		/// Below the source panel, stretch to the width of the PopupSource.
		/// </summary>
		BelowStretch,

		/// <summary>
		/// Position where the mouse cursor is currently
		/// </summary>
		UnderMouse
	}

	public FazorPopup()
	{
		AddClass( "fazor-popup" );
		AcceptsFocus = false;
	}

	/// <summary>
	/// Create a popup with positioning relative to a source panel
	/// </summary>
	public FazorPopup( Panel sourcePanel, PositionMode position, float offset )
	{
		AddClass( "fazor-popup" );
		SetPositioning( sourcePanel, position, offset );
		AcceptsFocus = false;
	}

	/// <summary>
	/// Sets PopupSource, Position and PopupSourceOffset.
	/// Applies relevant CSS classes.
	/// </summary>
	public void SetPositioning( Panel sourcePanel, PositionMode position, float offset )
	{
		PopupSource = sourcePanel;
		Position = position;
		PopupSourceOffset = offset;

		AddClass( "dropdown-panel" );
		
		switch ( Position )
		{
			case PositionMode.Left:
				AddClass( "left" );
				break;
			case PositionMode.LeftBottom:
				AddClass( "left-bottom" );
				break;
			case PositionMode.AboveLeft:
				AddClass( "above-left" );
				break;
			case PositionMode.BelowLeft:
				AddClass( "below-left" );
				break;
			case PositionMode.BelowCenter:
				AddClass( "below-center" );
				break;
			case PositionMode.BelowStretch:
				AddClass( "below-stretch" );
				break;
		}

		// Open the popup using BasePopup's Open method
		if ( sourcePanel != null )
		{
			var preferBelow = position == PositionMode.BelowLeft || 
			                 position == PositionMode.BelowCenter || 
			                 position == PositionMode.BelowStretch;
			Open( sourcePanel, preferBelow );
		}
	}

	/// <summary>
	/// Closes all popups and marks this one as a success.
	/// </summary>
	public void Success()
	{
		AddClass( "success" );
		CloseAll();
	}

	/// <summary>
	/// Closes all popups and marks this one as a failure.
	/// </summary>
	public void Failure()
	{
		AddClass( "failure" );
		CloseAll();
	}
}
