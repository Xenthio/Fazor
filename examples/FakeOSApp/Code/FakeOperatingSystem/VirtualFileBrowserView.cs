using FakeOperatingSystem;
using FakeOperatingSystem.OSFileSystem;
using FakeOperatingSystem.Shell;
using Sandbox;
using FakeOperatingSystem.Shell;
using FakeOperatingSystem.Shell.Explorer;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using XGUI;
using static XGUI.ListView.ListViewItem;

namespace FakeDesktop;

/// <summary>
/// Extension of FileBrowserView that works with both BaseFileSystem and VirtualFileSystem
/// </summary>
public class VirtualFileBrowserView : FileBrowserView
{
	// Replace the old monolithic VFS with our new components
	private IVirtualFileSystem _vfs;
	private ShellNamespace _shellManager;

	// Track if we're using virtual mode or regular mode
	private bool _usingVirtualMode = true;  // Default to shell namespace browsing

	// Current shell path (when in virtual mode)
	private string _currentShellPath = ShellNamespace.DESKTOP;

	// Navigation history
	private List<string> _navigationHistory = new();
	private int _historyIndex = -1;

	private static readonly List<VirtualFileBrowserView> AllFileBrowsers = new();

	protected override bool WantsDragScrolling => false;

	// Store icon positions per folder (key: file path, value: position)
	private Dictionary<string, Dictionary<string, Vector2>> _iconPositions = new();

	// Registry paths for Explorer Advanced settings
	private const string ExplorerAdvancedPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
	private const string HiddenValueName = "Hidden"; // DWORD: 1 (Show All), 2 (Don't Show Hidden)
	private const string ShowSuperHiddenValueName = "ShowSuperHidden"; // DWORD: 1 (Show System), 0 (Don't Show System)
	private const string HideFileExtValueName = "HideFileExt"; // DWORD: 0 (Show Extensions), 1 (Hide Extensions)

	// Folder specific settings base path
	private const string FolderSettingsBasePath = @"HKEY_CURRENT_USER\Software\FakeOS\Explorer\FolderViews";
	private const string FolderViewModeValueName = "ViewMode";
	private const string FolderAutoArrangeValueName = "AutoArrange";
	private const string FolderFFlagsValueName = "FFlags"; // For future use, like Windows FFLAGS

	// Cached global settings
	private bool _showHiddenFiles = false; // Default: Don't show hidden (Hidden=2)
	private bool _showSystemFiles = false; // Default: Don't show system (ShowSuperHidden=0)
	private bool _hideFileExtensions = true; // Default: Hide extensions (HideFileExt=1)


	public VirtualFileBrowserView() : base()
	{
		AllFileBrowsers.Add( this );
		OnDirectoryOpened += HandleDirectoryOpened;
		OnFileOpened += HandleFileOpened;
		LoadGlobalExplorerSettings(); // Load settings on creation
	}

	public override void OnDeleted()
	{
		AllFileBrowsers.Remove( this );
		base.OnDeleted();
	}

	private void LoadGlobalExplorerSettings()
	{
		if ( Registry.Instance == null ) return;

		// Hidden: 1 = Show, 2 = Don't Show. We want _showHiddenFiles to be true if Hidden is 1.
		_showHiddenFiles = Registry.Instance.GetValue<int>( ExplorerAdvancedPath, HiddenValueName, 2 ) == 1;
		// ShowSuperHidden: 1 = Show, 0 = Don't Show. We want _showSystemFiles to be true if ShowSuperHidden is 1.
		_showSystemFiles = Registry.Instance.GetValue<int>( ExplorerAdvancedPath, ShowSuperHiddenValueName, 0 ) == 1;
		// HideFileExt: 0 = Show, 1 = Hide. We want _hideFileExtensions to be true if HideFileExt is 1.
		_hideFileExtensions = Registry.Instance.GetValue<int>( ExplorerAdvancedPath, HideFileExtValueName, 1 ) == 1;
	}


	/// <summary>
	/// Initialize with a virtual file system
	/// </summary>
	public void Initialize( IVirtualFileSystem vfs, ShellNamespace shellManager, BaseFileSystem defaultFileSystem )
	{
		_vfs = vfs;
		_shellManager = shellManager;
		base.CurrentFileSystem = defaultFileSystem;
		LoadGlobalExplorerSettings(); // Reload in case registry changed since construction

		NavigateToShellPath( ShellNamespace.DESKTOP, sound: false );
	}
	private CancellationTokenSource _navigationCts;
	/// <summary>
	/// Navigate to a shell namespace path
	/// </summary>
	public void NavigateToShellPath( string shellPath, bool sound = true )
	{
		if ( _shellManager == null )
			return;

		LoadGlobalExplorerSettings(); // Refresh global settings on each navigation

		// Save to history
		if ( _historyIndex >= 0 && _historyIndex < _navigationHistory.Count - 1 )
		{
			_navigationHistory.RemoveRange( _historyIndex + 1, _navigationHistory.Count - _historyIndex - 1 );
		}
		_navigationHistory.Add( shellPath );
		_historyIndex = _navigationHistory.Count - 1;

		_currentShellPath = shellPath;
		_usingVirtualMode = true;

		var folder = _shellManager.GetFolder( shellPath );
		if ( folder == null )
		{
			Log.Warning( $"Shell path not found: {shellPath}" );
			return;
		}

		// Load and apply folder-specific settings
		LoadFolderSpecificSettings( shellPath );

		if ( sound ) PlaySingleClickSound();
		_currentContextMenu?.Delete();

		ListView.Items.Clear();
		FileItems.Clear();
		ListView.UpdateItems();

		if ( !string.IsNullOrEmpty( folder.RealPath ) )
		{
			base.CurrentPath = folder.RealPath;
		}
		else
		{
			base.CurrentPath = "shell:" + shellPath;
		}

		RaiseNavigateToEvent( _currentShellPath );

		_navigationCts?.Cancel();
		_navigationCts = new CancellationTokenSource();
		var token = _navigationCts.Token;

		_ = PopulateFromShellPath( shellPath, token );
	}

	public string GetCurrentShellPath()
	{
		return _currentShellPath;
	}

	public void GoBack()
	{
		if ( _historyIndex > 0 )
		{
			_historyIndex--;
			NavigateToShellPath( _navigationHistory[_historyIndex] );
		}
	}

	public void GoForward()
	{
		if ( _historyIndex < _navigationHistory.Count - 1 )
		{
			_historyIndex++;
			NavigateToShellPath( _navigationHistory[_historyIndex] );
		}
	}

	public void GoUp()
	{
		if ( string.IsNullOrEmpty( _currentShellPath ) || _currentShellPath == ShellNamespace.DESKTOP )
			return;

		int lastSlash = _currentShellPath.LastIndexOf( '/' );
		if ( lastSlash > 0 )
		{
			string parentPath = _currentShellPath.Substring( 0, lastSlash );
			NavigateToShellPath( parentPath );
		}
		else
		{
			NavigateToShellPath( ShellNamespace.DESKTOP );
		}
	}

	public bool OpenDirectoryEnabled = true;
	public bool OpenFileEnabled = true;

	/// <summary>
	/// Prevents navigation to other directories when set to false, directories will be opened in Explorer instead.
	/// </summary>
	public bool CanNavigate = true;

	public virtual void HandleDirectoryOpened( string path )
	{
		var folder = _shellManager.GetFolder( path );
		if ( folder == null )
		{
			Log.Warning( $"Shell path not found: {path}" );
			return;
		}

		if ( folder.Type == ShellFolderType.ControlPanelApplet )
		{
			Log.Info( $"Opening Control Panel applet: {folder.Name}" );
			folder.Applet?.Launch();
			return;
		}

		if ( folder.Type == ShellFolderType.ShellExecute )
		{
			Log.Info( $"Executing shell command: {folder.Name}" );
			Shell.ShellExecute( folder.RealPath );
			return;
		}

		if ( !OpenDirectoryEnabled )
			return;

		if ( !CanNavigate )
		{
			var entry = VirtualFileSystem.Instance.ResolvePath( path );
			LaunchExplorerWithPath( entry );
			return;
		}

		PlayDoubleClickSoundNoFirstClick();
		if ( _usingVirtualMode )
		{
			foreach ( var item in FileItems )
			{
				if ( item.FullPath == path && item.IsDirectory )
				{
					NavigateToShellPath( path );
					return;
				}
			}
		}
	}

	public virtual void HandleFileOpened( string path )
	{
		if ( !OpenFileEnabled )
			return;

		PlayDoubleClickSound();

		if ( _usingVirtualMode )
		{
			var shellItem = _shellManager.GetItems( _currentShellPath )
				.FirstOrDefault( i => i.Path == path );

			if ( shellItem != null )
			{
				if ( shellItem.Type == ShellFolderType.ControlPanelApplet )
				{
					Log.Info( $"Opening Control Panel applet: {shellItem.Name}" );
					return;
				}
				Shell.ShellExecute( shellItem.RealPath );
			}
		}
	}


	private void LaunchExplorerWithPath( string virtualPath )
	{
		// Launch the Explorer application with the specified virtual path
		ProcessManager.Instance.OpenExecutable( "C:\\Windows\\explorer.exe", new Win32LaunchOptions()
		{
			Arguments = $"{virtualPath}",
		} );
	}

	async void PlayDoubleClickSoundNoFirstClick()
	{
		await GameTask.DelaySeconds( 0.16f );
		PlaySingleClickSound();
	}
	async void PlayDoubleClickSound()
	{
		PlaySingleClickSound();
		await GameTask.DelaySeconds( 0.16f );
		PlaySingleClickSound();
	}

	async void PlaySingleClickSound()
	{
		var soundpath = XGUISoundSystem.GetSound( "START" );
		var soundfile = SoundFile.Load( soundpath );
		Sound.PlayFile( soundfile );
		await GameTask.DelaySeconds( 0.08f );
		Sound.PlayFile( soundfile );
	}

	public FlexDirection DefaultDirection = FlexDirection.Row;

	private async Task PopulateFromShellPath( string shellPath, CancellationToken cancellationToken = default )
	{
		this.GetOwnerXGUIPanel().Style.Cursor = "progress";
		LoadIconPositions( shellPath );

		var items = _shellManager.GetItems( shellPath );

		foreach ( var item in items.Where( i => i.IsFolder ) )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				this.GetOwnerXGUIPanel().Style.Cursor = null;
				return;
			}
			if ( ShouldFileBeVisible( item ) ) // Uses updated ShouldFileBeVisible
			{
				// Name for display is handled by AddDirectoryToView -> base.AddDirectoryToView -> new FileItem
				AddDirectoryToView( item.Path, true, item.Name );
				await UpdateItemIconAsync( item, true );
			}
		}
		foreach ( var item in items.Where( i => !i.IsFolder ) )
		{
			if ( cancellationToken.IsCancellationRequested )
			{
				this.GetOwnerXGUIPanel().Style.Cursor = null;
				return;
			}
			if ( ShouldFileBeVisible( item ) ) // Uses updated ShouldFileBeVisible
			{
				// Name for display is handled by AddFileToView -> base.AddFileToView -> new FileItem
				AddFileToView( item.Path, true, item.Name ); // Name override is item.Name from ShellItem
				await UpdateItemIconAsync( item, false );
			}
		}

		this.GetOwnerXGUIPanel().Style.Cursor = "wait";

		if ( ListView.ViewMode == ListView.ListViewMode.Icons && !AutoArrangeIcons )
		{
			var flexDirection = ListView.ItemContainer?.ComputedStyle?.FlexDirection ?? DefaultDirection;
			float iconWidth = 80f, iconHeight = 80f, spacingX = 2f, spacingY = 2f;
			float startX = 0f, startY = 0f;
			var availableWidth = ListView.Box.Rect.Width > 0 ? ListView.Box.Rect.Width : 800;
			int iconsPerRow = Math.Max( 1, (int)((availableWidth - startX) / (iconWidth + spacingX)) );

			int i = 0;
			if ( !_iconPositions.TryGetValue( shellPath, out var positions ) )
				positions = new Dictionary<string, Vector2>();

			var occupiedCells = new HashSet<(int, int)>();
			foreach ( var kvp in positions )
			{
				var pos = kvp.Value;
				int col = (int)Math.Round( (pos.x - startX) / (iconWidth + spacingX) );
				int row = (int)Math.Round( (pos.y - startY) / (iconHeight + spacingY) );
				occupiedCells.Add( (col, row) );
			}

			foreach ( var listViewItem in ListView.Items )
			{
				if ( cancellationToken.IsCancellationRequested ) return;
				if ( listViewItem.Data is FileItem fileItem )
				{
					Vector2 pos;
					if ( !positions.TryGetValue( fileItem.FullPath, out pos ) )
					{
						int col = 0, row = 0;
						bool found = false;
						for ( int searchIndex = 0; !found; searchIndex++ )
						{
							if ( flexDirection == FlexDirection.Row )
							{
								col = searchIndex % iconsPerRow;
								row = searchIndex / iconsPerRow;
							}
							else
							{
								row = searchIndex % iconsPerRow;
								col = searchIndex / iconsPerRow;
							}
							if ( !occupiedCells.Contains( (col, row) ) )
							{
								found = true;
								occupiedCells.Add( (col, row) );
							}
						}
						pos = new Vector2( startX + col * (iconWidth + spacingX), startY + row * (iconHeight + spacingY) );
						positions[fileItem.FullPath] = pos;
					}
					listViewItem.Style.Position = PositionMode.Absolute;
					listViewItem.Style.Left = pos.x;
					listViewItem.Style.Top = pos.y;
					i++;
					MakeItemGhostDraggable( listViewItem );
				}
			}
			_iconPositions[shellPath] = positions;
		}
		else
		{
			foreach ( var listViewItem in ListView.Items )
			{
				listViewItem.Style.Position = PositionMode.Relative;
				listViewItem.Style.Left = 0;
				listViewItem.Style.Top = 0;
				if ( listViewItem.Data is FileItem fileItem )
				{
					MakeItemGhostDraggable( listViewItem );
				}
			}
		}
		await GameTask.Delay( 10 );
		this.GetOwnerXGUIPanel().Style.Cursor = null;
	}

	// Override AddFileToView from FileBrowserView to handle HideFileExt
	public override void AddFileToView( string file, bool isFullPath = false, string nameOverride = "" )
	{
		string fullPath = isFullPath ? file : Path.Combine( CurrentPath ?? "", file );
		string displayName = nameOverride == "" ? System.IO.Path.GetFileName( fullPath ) : nameOverride;
		string extension = System.IO.Path.GetExtension( displayName );
		string nameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension( displayName );

		// Only hide extension if the type is known (has a FileAssociation)
		if ( _hideFileExtensions && !string.IsNullOrEmpty( extension ) )
		{
			var assoc = FileAssociationManager.Instance?.GetAssociation( extension );
			if ( assoc != null )
			{
				displayName = nameWithoutExtension;
			}
		}

		base.AddFileToView( file, isFullPath, displayName );
	}


	private async Task UpdateItemIconAsync( ShellItem item, bool isDirectory )
	{
		var path = item.Path;
		var realPath = item.RealPath;
		var iconName = item.IconName;
		var fileItem = FileItems.FirstOrDefault( fi => fi.FullPath == path ); // fi, not item
		if ( fileItem == null )
			return;

		foreach ( var listViewItem in ListView.Items )
		{
			if ( listViewItem.Data == fileItem && listViewItem.IconPanel != null )
			{
				var iconPanel = listViewItem.IconPanel;
				var size = ViewMode == FileBrowserViewMode.Icons ? 32 : 16;

				if ( item.Type == ShellFolderType.Shortcut || fileItem.FullPath.EndsWith( ".lnk" ) )
				{
					listViewItem.AddClass( "shortcut" );
				}

				string icon = "";
				if ( isDirectory )
				{
					icon = await GameTask.RunInThreadAsync( () => FileIconHelper.GetFolderIcon( realPath, size ) );
				}
				else
				{
					icon = await GameTask.RunInThreadAsync( () => FileIconHelper.GetFileIcon( realPath, size ) );
				}

				if ( icon == FileIconHelper.GetGenericFolderIcon( size ) || string.IsNullOrEmpty( icon ) ) // Added null/empty check
				{
					iconPanel.SetIcon( iconName, XGUIIconSystem.IconType.Folder, size ); // Fallback to ShellItem's icon
				}
				else
				{
					iconPanel.SetIcon( $"url:{icon}", iconSize: size );
				}
				break;
			}
		}
	}

	protected override void OnClick( MousePanelEvent e )
	{
		_currentContextMenu?.Delete();
	}
	ContextMenu _currentContextMenu;
	protected override void OnRightClick( MousePanelEvent e )
	{
		base.OnRightClick( e );
		bool clickedOnItem = false;
		foreach ( var item in ListView.Items )
		{
			if ( item.HasHovered )
			{
				clickedOnItem = true;
				ShowItemContextMenu( FileItems.FirstOrDefault( fi => fi == item.Data ), e );
				ListView.SelectItem( item );
				break;
			}
		}
		if ( !clickedOnItem )
		{
			ShowEmptySpaceContextMenu( e );
		}
	}

	private string EscapePathForRegistry( string path )
	{
		if ( string.IsNullOrEmpty( path ) ) return "_ROOT_";
		// Replace invalid key name characters. Be cautious with this.
		// A more robust way might be to hash the path or use a GUID mapping.
		return path.Replace( '\\', '_' ).Replace( '/', '_' ).Replace( ':', '_' ).Replace( '*', '_' )
				   .Replace( '?', '_' ).Replace( '"', '_' ).Replace( '<', '_' ).Replace( '>', '_' ).Replace( '|', '_' );
	}

	private void LoadFolderSpecificSettings( string shellPath )
	{
		if ( Registry.Instance == null || string.IsNullOrEmpty( shellPath ) ) return;

		string escapedPath = EscapePathForRegistry( shellPath );
		string regPath = Path.Combine( FolderSettingsBasePath, escapedPath );

		// Load ViewMode
		int? viewModeInt = Registry.Instance.GetValue<int?>( regPath, FolderViewModeValueName, null );
		if ( viewModeInt.HasValue && Enum.IsDefined( typeof( FileBrowserViewMode ), viewModeInt.Value ) )
		{
			this.ViewMode = (FileBrowserViewMode)viewModeInt.Value; // This should trigger ListView.UpdateViewMode via setter
		}
		// Else, it keeps its current value (which might be a global default or from previous folder)

		// Load AutoArrange
		int? autoArrangeInt = Registry.Instance.GetValue<int?>( regPath, FolderAutoArrangeValueName, null );
		if ( autoArrangeInt.HasValue )
		{
			this.AutoArrangeIcons = (autoArrangeInt.Value == 1);
		}
		// Else, it keeps its current value

		// This will trigger ArrangeIcons if needed, or apply icon positions if not auto arranging.
		// We need to ensure PopulateFromShellPath re-evaluates AutoArrangeIcons.
		// The call to ArrangeIcons() or icon positioning logic is already in PopulateFromShellPath.
	}

	private void SaveFolderSpecificSetting( string shellPath, string valueName, object value )
	{
		if ( Registry.Instance == null || string.IsNullOrEmpty( shellPath ) ) return;

		string escapedPath = EscapePathForRegistry( shellPath );
		string regPath = Path.Combine( FolderSettingsBasePath, escapedPath );
		Registry.Instance.SetValue( regPath, valueName, value );
	}


	private void ShowEmptySpaceContextMenu( MousePanelEvent e )
	{
		_currentContextMenu?.Delete();
		_currentContextMenu = new ContextMenu( this, ContextMenu.PositionMode.UnderMouse );

		_currentContextMenu.AddSubmenuItem( "View", submenu =>
		{
			submenu.AddRadioItem(
				"Large Icons",
				() => ViewMode == FileBrowserViewMode.Icons,
				() =>
				{
					ViewMode = FileBrowserViewMode.Icons;
					SaveFolderSpecificSetting( _currentShellPath, FolderViewModeValueName, (int)FileBrowserViewMode.Icons );
					Refresh();
				}
			);
			submenu.AddRadioItem(
				"List",
				() => ViewMode == FileBrowserViewMode.List,
				() =>
				{
					ViewMode = FileBrowserViewMode.List;
					SaveFolderSpecificSetting( _currentShellPath, FolderViewModeValueName, (int)FileBrowserViewMode.List );
					Refresh();
				}
			);
			submenu.AddRadioItem(
				"Details",
				() => ViewMode == FileBrowserViewMode.Details,
				() =>
				{
					ViewMode = FileBrowserViewMode.Details;
					SaveFolderSpecificSetting( _currentShellPath, FolderViewModeValueName, (int)FileBrowserViewMode.Details );
					Refresh();
				}
			);
		} );

		_currentContextMenu.AddSeparator();
		_currentContextMenu.AddMenuItem( "Customize this Folder...", () => { _currentContextMenu?.Delete(); } );
		_currentContextMenu.AddSeparator();

		_currentContextMenu.AddSubmenuItem( "Arrange Icons", submenu =>
		{
			submenu.AddMenuItem( "By Name", () => { ArrangeIcons(); } );
			submenu.AddMenuItem( "By Type", () => { } );
			submenu.AddMenuItem( "By Size", () => { } );
			submenu.AddMenuItem( "By Date", () => { } );
			submenu.AddSeparator();
			var autoArrangeItem = submenu.AddCheckItem(
				"Auto Arrange",
				() => AutoArrangeIcons,
				() =>
				{
					SetAutoArrangeIcons( !AutoArrangeIcons );
				}
			);
		} );
		_currentContextMenu.AddMenuItem( "Line Up Icons", () => { LineUpIconsToGrid(); } );
		_currentContextMenu.AddSeparator();
		_currentContextMenu.AddMenuItem( "Refresh", () => { Refresh(); _currentContextMenu?.Delete(); } );
		_currentContextMenu.AddSeparator();

		CreateFileNewMenu( _currentContextMenu );

		_currentContextMenu.AddSeparator();
		_currentContextMenu.AddMenuItem( "Properties", () =>
		{
			var folder = _shellManager.GetFolder( _currentShellPath );
			if ( folder != null )
			{
				if ( folder.HandlePropertiesClick != null ) folder.HandlePropertiesClick.Invoke();
				else
				{
					var dialog = new FilePropertiesDialog { Name = folder.Name, Path = folder.RealPath ?? _currentShellPath, IsDirectory = true, Size = 0 };
					XGUISystem.Instance.Panel.AddChild( dialog );
				}
			}
			_currentContextMenu?.Delete();
		} );
	}

	/// <summary>
	/// Creates a "New" submenu in the provided context menu for creating new files or folders.
	/// </summary>
	/// <param name="menu"></param>
	/// <returns>The created panel</returns>
	public Panel CreateFileNewMenu( ContextMenu menu )
	{
		if ( menu == null ) return null;
		var b = menu.AddSubmenuItem( "New", submenu =>
		{
			var associations = FileAssociationManager.Instance.GetAllAssociations();
			if ( associations != null )
			{
				submenu.AddMenuItem( "Folder", () =>
				{
					var currentFolder = _shellManager.GetFolder( _currentShellPath );
					if ( currentFolder?.RealPath != null )
					{
						string newFolderName = "New Folder";
						string newFolderPath = Path.Combine( currentFolder.RealPath, newFolderName );
						int count = 1;
						while ( _vfs.DirectoryExists( newFolderPath ) )
						{
							newFolderName = $"New Folder ({count})";
							newFolderPath = Path.Combine( currentFolder.RealPath, newFolderName );
							count++;
						}
						_vfs.CreateDirectory( newFolderPath );
						Refresh();
					}
					menu?.Delete();
				} );
				submenu.AddSeparator();
				foreach ( var assoc in associations.OrderBy( a => a.FriendlyName ) )
				{
					if ( !assoc.ShouldShowInShellCreateNew ) continue;
					string ext = assoc.Extension.TrimStart( '.' );
					string friendlyName = assoc.FriendlyName ?? ext.ToUpper() + " File";
					submenu.AddMenuItem( friendlyName, () =>
					{
						var currentFolder = _shellManager.GetFolder( _currentShellPath );
						if ( currentFolder?.RealPath != null )
						{
							string newFileName = $"New {friendlyName}.{ext}";
							string newFilePath = Path.Combine( currentFolder.RealPath, newFileName );
							int count = 1;
							while ( _vfs.FileExists( newFilePath ) )
							{
								newFileName = $"New {friendlyName} ({count}).{ext}";
								newFilePath = Path.Combine( currentFolder.RealPath, newFileName );
								count++;
							}
							_vfs.WriteAllText( newFilePath, "" ); // Create empty file
							Refresh();
						}
						menu?.Delete();
					} );
				}
			}
		} );
		return b; // Return the submenu item for further customization if needed
	}

	public void SetAutoArrangeIcons( bool autoArrange )
	{
		if ( AutoArrangeIcons == autoArrange ) return;
		AutoArrangeIcons = autoArrange;
		SaveFolderSpecificSetting( _currentShellPath, FolderAutoArrangeValueName, AutoArrangeIcons ? 1 : 0 );
		ArrangeIcons();
		Refresh();
	}

	private void ShowItemContextMenu( FileItem item, MousePanelEvent e )
	{
		if ( item == null ) return;
		_currentContextMenu?.Delete();
		_currentContextMenu = new ContextMenu( this, ContextMenu.PositionMode.UnderMouse );

		var shellItem = _shellManager.GetItems( _currentShellPath ).FirstOrDefault( i => i.Path == item.FullPath );
		string realPath = shellItem?.RealPath ?? item.FullPath; // Prefer realPath for operations

		// Get file association
		string extension = System.IO.Path.GetExtension( item.Name ); // Use item.Name which might have original extension
		if ( string.IsNullOrEmpty( extension ) && !item.IsDirectory && shellItem?.RealPath != null )
		{
			// If item.Name had extension hidden, try to get it from realPath
			extension = System.IO.Path.GetExtension( shellItem.RealPath );
		}

		FileAssociation association = null;
		if ( !item.IsDirectory && !string.IsNullOrEmpty( extension ) )
		{
			association = FileAssociationManager.Instance?.GetAssociation( extension );
		}

		bool hasAddedSpecificActions = false;

		// Populate actions from FileAssociation
		if ( association?.Actions != null && association.Actions.Any() )
		{
			// Make "Open" the first item if it exists and is the default program's action
			if ( association.Actions.TryGetValue( "open", out var openAction ) )
			{
				// Bold the default action (usually "Open")
				// Note: ContextMenu.AddMenuItem doesn't directly support bold.
				// This might require custom styling or a modified AddMenuItem.
				// For now, we'll just add it. A common approach is to make the default action the first one.
				_currentContextMenu.AddMenuItem( openAction.DisplayName ?? "Open", () =>
				{
					association.ExecuteAction( "open", realPath );
					_currentContextMenu?.Delete();
				} );
				hasAddedSpecificActions = true;
			}

			// Add other actions, sorted perhaps, or in a specific order
			foreach ( var actionEntry in association.Actions.OrderBy( a => a.Key == "open" ? 0 : 1 ).ThenBy( a => a.Value.DisplayName ) )
			{
				if ( actionEntry.Key.Equals( "open", StringComparison.OrdinalIgnoreCase ) && hasAddedSpecificActions )
				{
					// Already added "open" if it was the default.
					continue;
				}

				var fileAction = actionEntry.Value;
				_currentContextMenu.AddMenuItem( fileAction.DisplayName ?? actionEntry.Key, () =>
				{
					association.ExecuteAction( actionEntry.Key, realPath );
					_currentContextMenu?.Delete();
				} );
				hasAddedSpecificActions = true;
			}
		}
		else if ( item.IsDirectory ) // Default actions for directories
		{
			_currentContextMenu.AddMenuItem( "Open", () =>
			{
				HandleDirectoryOpened( item.FullPath );
				_currentContextMenu?.Delete();
			} );
			hasAddedSpecificActions = true;
		}
		else // Fallback for files with no specific associations or if "Open" wasn't primary
		{
			_currentContextMenu.AddMenuItem( "Open", () =>
			{
				HandleFileOpened( item.FullPath ); // Generic open
				_currentContextMenu?.Delete();
			} );
			hasAddedSpecificActions = true;
		}


		if ( !item.IsDirectory ) // Only show "Open with..." for files
		{
			_currentContextMenu.AddMenuItem( "Open with...", () =>
			{
				var openWithDialog = new OpenWithDialog { TargetFilePath = realPath };
				openWithDialog.OnProgramSelectedAndConfirmed = ( selectedProgram, setAsDefault ) =>
				{
					Log.Info( $"Open With: Selected '{selectedProgram}', Set as default: {setAsDefault}" );

					// 1. Execute the program
					ProcessManager.Instance.OpenExecutable( selectedProgram, new Win32LaunchOptions { Arguments = $"\"{realPath}\"" } );

					// 2. If "setAsDefault" is true, update the registry
					if ( setAsDefault && !string.IsNullOrEmpty( extension ) )
					{
						var currentAssoc = FileAssociationManager.Instance?.GetAssociation( extension );
						if ( currentAssoc != null )
						{
							// Update the DefaultProgram property of the association
							// This assumes FileAssociation has a DefaultProgram setter or a method to update it,
							// and RegisterAssociation will save the changes.
							currentAssoc.DefaultProgram = selectedProgram;

							// Also, ensure the "open" action points to this program.
							// The FileAssociationManager.RegisterAssociation should ideally handle creating/updating
							// the "open" action based on the DefaultProgram.
							// If not, you might need to manually adjust or add the "open" action here.
							currentAssoc.AddAction( "open", "Open", selectedProgram, "\"%1\"" ); // Ensure "open" action is correct

							FileAssociationManager.Instance.RegisterAssociation( currentAssoc );
							Log.Info( $"Set '{selectedProgram}' as default for '{extension}' files." );
						}
						else
						{
							// Create a new association if one doesn't exist
							// This is less common from "Open With" but possible
							string friendlyName = $"{extension.ToUpper()} File"; // Basic friendly name
							var newAssoc = new FileAssociation( extension, friendlyName, extension /*iconName*/, selectedProgram );
							newAssoc.AddAction( "open", "Open", selectedProgram, "\"%1\"" );
							FileAssociationManager.Instance.RegisterAssociation( newAssoc );
							Log.Info( $"Created new association and set '{selectedProgram}' as default for '{extension}' files." );
						}
					}
				};
				XGUISystem.Instance.Panel.AddChild( openWithDialog ); // Add dialog to the main UI panel
				_currentContextMenu?.Delete();
			} );
		}


		if ( hasAddedSpecificActions )
		{
			_currentContextMenu.AddSeparator();
		}

		// Standard items like Delete, Rename, Properties
		_currentContextMenu.AddMenuItem( "Delete", () =>
		{
			if ( shellItem?.RealPath != null )
			{
				if ( item.IsDirectory ) _vfs.DeleteDirectory( shellItem.RealPath, true );
				else _vfs.DeleteFile( shellItem.RealPath );
				Refresh();
			}
			_currentContextMenu?.Delete();
		} );
		_currentContextMenu.AddMenuItem( "Rename", () =>
		{
			var listViewItem = ListView.Items.FirstOrDefault( i => i.Data == item );
			if ( listViewItem != null )
			{
				listViewItem.BeginRename( newName =>
				{
					if ( !string.IsNullOrWhiteSpace( newName ) && newName != item.Name )
					{
						var currentShellItem = _shellManager.GetItems( _currentShellPath ).FirstOrDefault( i => i.Path == item.FullPath );
						if ( currentShellItem?.RealPath != null )
						{
							string parentDir = System.IO.Path.GetDirectoryName( currentShellItem.RealPath );
							string newFileName = newName;
							// Ensure the new name has an extension if the original did (and it's not a directory)
							if ( !item.IsDirectory )
							{
								string originalExtension = System.IO.Path.GetExtension( item.Name ); // Original name from FileItem
								if ( string.IsNullOrEmpty( System.IO.Path.GetExtension( newName ) ) && !string.IsNullOrEmpty( originalExtension ) )
								{
									newFileName += originalExtension;
								}
							}
							string newRealPath = Path.Combine( parentDir, newFileName );
							if ( item.IsDirectory ) _vfs.MoveDirectory( currentShellItem.RealPath, newRealPath );
							else _vfs.MoveFile( currentShellItem.RealPath, newRealPath );
							Refresh();
						}
					}
				} );
			}
			_currentContextMenu?.Delete();
		} );
		_currentContextMenu.AddSeparator();
		_currentContextMenu.AddMenuItem( "Properties", () =>
		{
			if ( shellItem != null )
			{
				var isDir = item.IsDirectory;
				long size = 0;
				if ( shellItem.RealPath != null )
				{
					size = isDir ? (_vfs.DirectoryExists( shellItem.RealPath ) ? VirtualFileSystem.Instance.RecursiveDirectorySize( shellItem.RealPath ) : 0)
								 : (_vfs.FileExists( shellItem.RealPath ) ? _vfs.FileSize( shellItem.RealPath ) : 0);
				}
				var dialog = new FilePropertiesDialog { Name = item.Name, Path = shellItem.RealPath ?? item.FullPath, IsDirectory = isDir, Size = size };
				XGUISystem.Instance.Panel.AddChild( dialog );
			}
			_currentContextMenu?.Delete();
		} );
	}


	public void MoveToRecycleBin( string path )
	{
		/*if ( !_vfs.FileExists( path ) && !_vfs.DirectoryExists( path ) )
		{
			Log.Warning( $"Path not found for moving to recycle bin: {path}" );
			return;
		}
		var recycleBinPath = Path.Combine( @"C:\Recycled", System.IO.Path.GetFileName( path ) ); // Use System.IO.Path
		if ( _vfs.DirectoryExists( path ) )
		{
			CopyDirectoryRecursive( path, recycleBinPath );
			_vfs.DeleteDirectory( path, true );
		}
		else
		{
			var content = _vfs.ReadAllBytes( path );
			_vfs.WriteAllBytes( recycleBinPath, content );
			_vfs.DeleteFile( path );
		}*/
	}



	public override void Refresh()
	{
		if ( _usingVirtualMode && !string.IsNullOrEmpty( _currentShellPath ) )
		{
			NavigateToShellPath( _currentShellPath, sound: false );
		}
		else
		{
			base.Refresh();
		}
	}

	public string IconPositionsFileName = ".iconpositions";
	private string GetPositionsFilePath( string folderPath )
	{
		var folder = _shellManager.GetFolder( folderPath );
		var realPath = folder?.RealPath;
		if ( string.IsNullOrEmpty( realPath ) ) return null;
		return Path.Combine( realPath, IconPositionsFileName );
	}
	private void LoadIconPositions( string folderPath )
	{
		var file = GetPositionsFilePath( folderPath );
		if ( file == null || !_vfs.FileExists( file ) )
		{
			_iconPositions[folderPath] = new();
			return;
		}
		try
		{
			var json = _vfs.ReadAllText( file );
			_iconPositions[folderPath] = JsonSerializer.Deserialize<Dictionary<string, Vector2>>( json ) ?? new();
		}
		catch { _iconPositions[folderPath] = new(); }
	}
	private void SaveIconPositions( string folderPath )
	{
		var file = GetPositionsFilePath( folderPath );
		if ( file == null || !_iconPositions.TryGetValue( folderPath, out var positions ) ) return; // Added check for positions
		var json = JsonSerializer.Serialize( positions ); // Use positions variable
		_vfs.WriteAllText( file, json );
	}

	private void MakeItemGhostDraggable( ListView.ListViewItem item )
	{
		Panel ghostRoot = null;
		Panel ghostFakeListView = null;
		Panel ghost = null;
		Vector2 grabOffset = default;
		item.Draggable = true;

		item.OnDragStartEvent += ( ItemDragEvent e ) =>
		{
			grabOffset = e.LocalGrabPosition;
			ghostRoot = new Panel();
			ghostRoot.AddClass( "Panel" );
			foreach ( var sheet in item.GetOwnerXGUIPanel().AllStyleSheets ?? Enumerable.Empty<StyleSheet>() )
			{
				ghostRoot.StyleSheet.Add( sheet );
			}
			ghostRoot.Style.Position = PositionMode.Absolute;
			ghostFakeListView = new Panel();
			ghostFakeListView.Classes = item.Parent?.Parent?.Classes ?? "listview";
			ghostFakeListView.AddClass( "ghost-listview" );
			ghostRoot.AddChild( ghostFakeListView );
			ghost = new Panel();
			ghostFakeListView.AddChild( ghost );
			ghost.Classes = item.Classes;
			ghost.RemoveClass( "selected" );
			ghost.Style.Position = PositionMode.Absolute;
			ghostRoot.Style.Left = e.ScreenPosition.x - grabOffset.x;
			ghostRoot.Style.Top = e.ScreenPosition.y - grabOffset.y;
			ghost.Style.Width = item.Box.Rect.Width;
			ghost.Style.Height = item.Box.Rect.Height;
			ghostRoot.Style.Width = item.Box.Rect.Width;
			ghostRoot.Style.Height = item.Box.Rect.Height;
			ghostFakeListView.Style.Width = item.Box.Rect.Width;
			ghostFakeListView.Style.Height = item.Box.Rect.Height;
			ghostRoot.Style.BackgroundColor = Color.Transparent;
			ghostRoot.Style.BorderColor = Color.Transparent;
			ghostRoot.Style.BorderWidth = 0;
			ghostFakeListView.Style.BackgroundColor = Color.Transparent;
			ghostFakeListView.Style.BorderColor = Color.Transparent;
			ghostFakeListView.Style.BorderWidth = 0;
			ghostFakeListView.Style.Padding = 0;
			ghostFakeListView.StyleSheet.Parse( @" .listview::before, .listview::after { border: none; }" );
			ghost.Style.Opacity = 0.79f;
			ghostRoot.Style.ZIndex = 100000;
			ghost.Style.PointerEvents = PointerEvents.None;
			ghostRoot.Style.PointerEvents = PointerEvents.None;

			var sourceChildren = (ListView.ViewMode == XGUI.ListView.ListViewMode.Icons) ? item.Children : item.Children.FirstOrDefault()?.Children;
			if ( sourceChildren != null )
			{
				foreach ( var panel in sourceChildren )
				{
					if ( panel is XGUIIconPanel iconPanel )
					{
						var iconClone = new XGUIIconPanel();
						iconClone.SetIcon( iconPanel.IconName, iconPanel.IconType, iconPanel.IconSize );
						iconClone.Classes = iconPanel.Classes;
						ghost.AddChild( iconClone );
					}
					else if ( panel is Label label )
					{
						var labelClone = new Label();
						labelClone.Text = label.Text;
						labelClone.Classes = label.Classes;
						ghost.AddChild( labelClone );
					}
					else if ( panel is Panel textPanel )
					{
						var textPanelClone = new Panel();
						// we need to also copy the label inside.
						foreach ( var child in textPanel.Children )
						{
							if ( child is Label textLabel )
							{
								var labelClone = new Label();
								labelClone.Text = textLabel.Text;
								labelClone.Classes = textLabel.Classes;
								textPanelClone.AddChild( labelClone );
							}
						}
						textPanelClone.Classes = textPanel.Classes;
						ghost.AddChild( textPanelClone );
					}
				}
			}
			XGUISystem.Instance.Panel.AddChild( ghostRoot );
			item.Style.Cursor = "grabbing";
		};

		item.OnDragEvent += ( ItemDragEvent e ) =>
		{
			if ( ghostRoot == null ) return; // Changed from ghost to ghostRoot
			ghostRoot.Style.Left = e.ScreenPosition.x - grabOffset.x;
			ghostRoot.Style.Top = e.ScreenPosition.y - grabOffset.y;
		};

		item.OnDragEndEvent += ( ItemDragEvent e ) =>
		{
			if ( ghostRoot != null ) // Changed from ghost to ghostRoot
			{
				VirtualFileBrowserView topmostView = null;
				var mousePos = Mouse.Position;
				var sorted = AllFileBrowsers.OrderByDescending( view => { var p = view.GetOwnerXGUIPanel(); return p?.Parent?.GetChildIndex( p ) ?? -1; } ).ToList();
				foreach ( var view in sorted ) { if ( view.Box.Rect.IsInside( mousePos ) ) topmostView = view; }

				if ( topmostView != null && topmostView != this && item.Data is FileItem crossDraggedItem )
				{
					var targetView = topmostView;
					var shellItem = _shellManager.GetItems( _currentShellPath ).FirstOrDefault( i => i.Path == crossDraggedItem.FullPath );
					if ( shellItem == null ) goto EndGhost;
					var targetShellFolder = targetView._shellManager.GetFolder( targetView._currentShellPath );
					if ( targetShellFolder?.RealPath == null ) goto EndGhost;
					var crossDraggedItemName = Path.GetFileName( crossDraggedItem.FullPath );
					string newPath = Path.Combine( targetShellFolder.RealPath, crossDraggedItemName );
					if ( crossDraggedItem.IsDirectory ) _vfs.MoveDirectory( shellItem.RealPath, newPath );
					else _vfs.MoveFile( shellItem.RealPath, newPath );
					if ( !targetView.AutoArrangeIcons && targetView.ListView.ViewMode == XGUI.ListView.ListViewMode.Icons )
					{
						var targetListViewRect = targetView.ListView.Box.Rect;
						var dropPos = e.ScreenPosition - new Vector2( targetListViewRect.Left, targetListViewRect.Top ) - grabOffset;
						// account for scroll offset
						dropPos.x += targetView.ListView.ItemContainer.ScrollOffset.x; dropPos.y += targetView.ListView.ItemContainer.ScrollOffset.y;
						dropPos.x = Math.Max( 0, dropPos.x ); dropPos.y = Math.Max( 0, dropPos.y );
						if ( !targetView._iconPositions.ContainsKey( targetView._currentShellPath ) ) targetView._iconPositions[targetView._currentShellPath] = new();
						string newShellPath = targetView._currentShellPath.TrimEnd( '/', '\\' ) + "/" + crossDraggedItemName;
						targetView._iconPositions[targetView._currentShellPath][newShellPath] = dropPos;
						targetView.SaveIconPositions( targetView._currentShellPath );
					}
					Refresh(); targetView.Refresh();
					goto EndGhost;
				}

				ListView.ListViewItem targetFolderItem = null;
				foreach ( var other in ListView.Items )
				{
					if ( other == item ) continue;
					if ( other.Data is FileItem fi && fi.IsDirectory && other.Box.Rect.IsInside( e.ScreenPosition ) ) { targetFolderItem = other; break; }
				}
				if ( targetFolderItem != null && item.Data is FileItem draggedItem )
				{
					var targetFolder = targetFolderItem.Data as FileItem;
					if ( targetFolder != null && targetFolder.IsDirectory && draggedItem.FullPath != targetFolder.FullPath && !draggedItem.FullPath.StartsWith( targetFolder.FullPath + Path.DirectorySeparatorChar ) )
					{
						var shellItem = _shellManager.GetItems( _currentShellPath ).FirstOrDefault( i => i.Path == draggedItem.FullPath );
						if ( shellItem == null ) return; // Early exit
						var targetShellItem = _shellManager.GetItems( _currentShellPath ).FirstOrDefault( i => i.Path == targetFolder.FullPath );
						if ( targetShellItem == null ) return; // Early exit
						string newPath = Path.Combine( targetShellItem.RealPath, Path.GetFileName( draggedItem.FullPath ) );
						if ( draggedItem.IsDirectory ) _vfs.MoveDirectory( shellItem.RealPath, newPath );
						else _vfs.MoveFile( shellItem.RealPath, newPath );
						Refresh();
						ghostRoot.Delete(); ghostRoot = null; // Use ghostRoot
						return;
					}
				}
				if ( !AutoArrangeIcons && ListView.ViewMode == XGUI.ListView.ListViewMode.Icons ) // Added ViewMode check
				{
					var parentRect = item.Parent?.Box.Rect ?? Box.Rect;
					float newLeft = e.ScreenPosition.x - parentRect.Left - grabOffset.x; // Use parentRect.Left
					float newTop = e.ScreenPosition.y - parentRect.Top - grabOffset.y;   // Use parentRect.Top

					// account for scroll offset
					newLeft += ListView.ItemContainer.ScrollOffset.x;
					newTop += ListView.ItemContainer.ScrollOffset.y;
					item.Style.Left = newLeft; item.Style.Top = newTop;
					if ( item.Data is FileItem fileItem )
					{
						if ( !_iconPositions.ContainsKey( _currentShellPath ) ) _iconPositions[_currentShellPath] = new();
						_iconPositions[_currentShellPath][fileItem.FullPath] = new Vector2( item.Style.Left?.Value ?? 0, item.Style.Top?.Value ?? 0 );
						SaveIconPositions( _currentShellPath );
					}
				}
				EndGhost:
				ghostRoot.Delete(); ghostRoot = null;
				ghostFakeListView = null; ghost = null; // Ensure ghost is also nulled
			}
			item.Style.Cursor = null; // Reset cursor
		};
	}
	public bool AutoArrangeIcons = true; // Default value, will be overridden by folder-specific settings
	public void ArrangeIcons()
	{
		if ( ListView.ViewMode != XGUI.ListView.ListViewMode.Icons )
			return;

		var shellPath = _currentShellPath;
		var items = ListView.Items.Where( i => i.Data is FileItem ).ToList();
		items.Sort( ( a, b ) =>
		{
			var fa = a.Data as FileItem; var fb = b.Data as FileItem;
			if ( fa.IsDirectory && !fb.IsDirectory ) return -1;
			if ( !fa.IsDirectory && fb.IsDirectory ) return 1;
			return string.Compare( fa.Name, fb.Name, StringComparison.OrdinalIgnoreCase );
		} );
		var flexDirection = ListView.ItemContainer?.ComputedStyle?.FlexDirection ?? DefaultDirection;
		float iconWidth = 80f, iconHeight = 80f, spacingX = 2f, spacingY = 2f;
		float startX = 0f, startY = 0f;
		var availableWidth = ListView.Box.Rect.Width > 0 ? ListView.Box.Rect.Width : 800;
		var availableHeight = ListView.Box.Rect.Height > 0 ? ListView.Box.Rect.Height : 600;
		int iconsPerRow = Math.Max( 1, (int)((availableWidth - startX) / (iconWidth + spacingX)) );
		int iconsPerCol = Math.Max( 1, (int)((availableHeight - startY) / (iconHeight + spacingY)) );
		var positions = new Dictionary<string, Vector2>();
		for ( int i = 0; i < items.Count; i++ )
		{
			var fileItem = items[i].Data as FileItem; int col, row; Vector2 pos;
			if ( flexDirection == FlexDirection.Row ) { col = i % iconsPerRow; row = i / iconsPerRow; }
			else { row = i % iconsPerCol; col = i / iconsPerCol; }
			pos = new Vector2( startX + col * (iconWidth + spacingX), startY + row * (iconHeight + spacingY) );
			items[i].Style.Position = PositionMode.Absolute; items[i].Style.Left = pos.x; items[i].Style.Top = pos.y;
			positions[fileItem.FullPath] = pos;
		}
		_iconPositions[shellPath] = positions;
		SaveIconPositions( shellPath );
	}

	public void LineUpIconsToGrid()
	{
		if ( ListView.ViewMode != XGUI.ListView.ListViewMode.Icons ) return;
		var shellPath = _currentShellPath;
		float iconWidth = 80f, iconHeight = 80f, spacingX = 2f, spacingY = 2f;
		float gridX = iconWidth + spacingX; float gridY = iconHeight + spacingY;
		if ( !_iconPositions.TryGetValue( shellPath, out var positions ) ) return;
		foreach ( var item in ListView.Items )
		{
			if ( item.Data is FileItem fileItem && positions.TryGetValue( fileItem.FullPath, out var pos ) )
			{
				float snappedX = (float)Math.Round( pos.x / gridX ) * gridX;
				float snappedY = (float)Math.Round( pos.y / gridY ) * gridY;
				item.Style.Left = snappedX; item.Style.Top = snappedY;
				positions[fileItem.FullPath] = new Vector2( snappedX, snappedY );
			}
		}
		SaveIconPositions( shellPath );
	}

	private bool IsSystemFile( ShellItem item )
	{
		if ( item == null || string.IsNullOrEmpty( item.Name ) ) return false;
		// Basic checks, VFS might provide attributes in the future
		string nameLower = item.Name.ToLowerInvariant();
		return nameLower == "desktop.ini" ||
			   nameLower == "thumbs.db" ||
			   nameLower == "ntuser.dat" || // Common system/hidden file
			   nameLower == ".iconpositions" || // Your own hidden file
			   (item.RealPath != null && _vfs.HasAttribute( item.RealPath, VirtualFileAttributes.System )); // Hypothetical VFS attribute
	}

	private bool IsHiddenFile( ShellItem item )
	{
		if ( item == null || string.IsNullOrEmpty( item.Name ) ) return false;
		// Basic checks, VFS might provide attributes in the future
		return item.Name.StartsWith( "." ) ||
			   (item.RealPath != null && _vfs.HasAttribute( item.RealPath, VirtualFileAttributes.Hidden )); // Hypothetical VFS attribute
	}

	private bool ShouldFileBeVisible( ShellItem item )
	{
		bool isSystem = IsSystemFile( item );
		bool isHidden = IsHiddenFile( item );

		if ( isSystem && !_showSystemFiles ) return false;
		if ( isHidden && !_showHiddenFiles ) return false;

		// Also hide the icon positions file itself, regardless of settings, if it's not already caught by IsSystemFile
		if ( item.Name.Equals( IconPositionsFileName, StringComparison.OrdinalIgnoreCase ) ) return false;

		return true;
	}
}
