using Sandbox.UI;

namespace Fazor.Controls;

/// <summary>
/// Icon panel for Fazor controls. Displays icons from various sources.
/// Simplified port from XGUI-3's XGUIIconPanel.
/// </summary>
public class FazorIconPanel : Panel
{
	private string? _iconName;
	private int _iconSize = 16;
	private Image? _iconImage;
	private Label? _materialIconLabel;

	/// <summary>
	/// The name/path of the icon
	/// </summary>
	public string? IconName
	{
		get => _iconName;
		set
		{
			if ( _iconName != value )
			{
				_iconName = value;
				UpdateIcon();
			}
		}
	}

	/// <summary>
	/// The desired size of the icon
	/// </summary>
	public int IconSize
	{
		get => _iconSize;
		set
		{
			if ( _iconSize != value )
			{
				_iconSize = value;
				UpdateIcon();
			}
		}
	}

	public FazorIconPanel()
	{
		AddClass( "fazor-icon-panel" );

		// Create the icon image
		_iconImage = AddChild<Image>();
		_iconImage.AddClass( "icon-image" );

		// Create the material icon label (for Material Icons fonts)
		_materialIconLabel = AddChild<Label>();
		_materialIconLabel.AddClass( "material-icon" );

		// Hide both by default
		_iconImage.Style.Display = DisplayMode.None;
		_materialIconLabel.Style.Display = DisplayMode.None;
	}

	public FazorIconPanel( string iconName, int iconSize = 16 )
		: this()
	{
		_iconName = iconName;
		_iconSize = iconSize;
		UpdateIcon();
	}

	/// <summary>
	/// Update the icon based on current properties
	/// </summary>
	private void UpdateIcon()
	{
		if ( _iconImage == null || _materialIconLabel == null ) return;
		
		if ( string.IsNullOrEmpty( _iconName ) )
		{
			_iconImage.Style.Display = DisplayMode.None;
			_materialIconLabel.Style.Display = DisplayMode.None;
			return;
		}

		// Handle different icon formats
		if ( _iconName.StartsWith( "material:" ) )
		{
			// Material icon (icon font)
			_iconImage.Style.Display = DisplayMode.None;
			_materialIconLabel.Style.Display = DisplayMode.Flex;
			_materialIconLabel.Text = _iconName.Substring( 9 ); // Remove "material:" prefix
			_materialIconLabel.Style.FontSize = Length.Pixels( _iconSize );
		}
		else
		{
			// Image icon - treat as file path or URL
			_iconImage.Style.Display = DisplayMode.Flex;
			_materialIconLabel.Style.Display = DisplayMode.None;
			_iconImage.Style.SetBackgroundImage( _iconName );
			_iconImage.Style.Width = Length.Pixels( _iconSize );
			_iconImage.Style.Height = Length.Pixels( _iconSize );
		}
	}

	/// <summary>
	/// Set the icon by name
	/// </summary>
	public void SetIcon( string iconName, int iconSize = 16 )
	{
		_iconName = iconName;
		_iconSize = iconSize;
		UpdateIcon();
	}
}
