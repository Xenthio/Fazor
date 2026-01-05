# Assets Folder

This folder contains all static assets (images, themes, resources) for the application.

## Structure

```
Assets/
├── XGUI/            # XGUI theme system (XGUI-3 compatible structure)
│   ├── DefaultStyles/   # Complete theme definitions
│   ├── FunctionStyles/  # Base component styles
│   └── Resources/       # Theme images and icons
├── themes/          # Avalazor-specific styles
│   └── *.scss      # Individual theme files
└── images/         # Image assets for UI elements
```

## How It Works

### Build & Publish Behavior

All files in the `Assets/` folder are:
- Copied to the build output directory (bin/Debug or bin/Release)
- Copied to the publish directory for distribution
- Embedded in single-file publish executables
- Files appear at the root of the output (e.g., `Assets/XGUI/` becomes `XGUI/`)

This is configured in the `.csproj` file:

```xml
<ItemGroup>
  <Content Include="Assets\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
    <Link>%(RecursiveDir)%(Filename)%(Extension)</Link>
  </Content>
</ItemGroup>
```

### StyleSheet Resolution

The `[StyleSheet]` attribute searches for files in the following order:
1. `{BaseDirectory}/Assets/{path}` - Assets subdirectory (primary location)
2. `{BaseDirectory}/{path}` - Direct path (backward compatibility)
3. Assembly location
4. Current working directory

Example usage:
```csharp
// Resolves to Assets/XGUI/DefaultStyles/ComputerXP.scss
[StyleSheet("/XGUI/DefaultStyles/ComputerXP.scss")]

// Relative to component location  
[StyleSheet("MyComponent.scss")]
```

### Image/Texture Resolution

Images referenced in CSS (via `url()`) or code are searched in:
1. Exact path if it exists
2. `{BaseDirectory}/Assets/{path}` - Assets subdirectory (primary location)
3. `{BaseDirectory}/{path}` - Direct path
4. `{BaseDirectory}/assets/{path}` (legacy, lowercase)
5. `{BaseDirectory}/wwwroot/{path}` (legacy)

Example in SCSS:
```scss
.button {
    background-image: url("XGUI/Resources/button_bg.png");
    border-image: url("XGUI/Resources/button_border.png") 5 / 5px;
}
```

## Adding New Assets

### Adding Themes

1. Place theme `.scss` files in `Assets/XGUI/DefaultStyles/` (XGUI-3 compatible) or `Assets/themes/`
2. Reference in Razor components:
   ```csharp
   @attribute [StyleSheet("/XGUI/DefaultStyles/MyTheme.scss")]
   ```

### Adding Images

1. Place image files in `Assets/images/` (or any subfolder)
2. Reference in CSS:
   ```scss
   background-image: url("images/my-image.png");
   ```

### Adding Resources

For theme-specific resources (icons, borders, etc.):
1. Create a subfolder under `Assets/XGUI/`
   - Example: `Assets/XGUI/Resources/`
2. Place image files there
3. Reference in theme SCSS files:
   ```scss
   $icon-close: url("XGUI/Resources/icon_close.png");
   ```

## Best Practices

1. **Organize by type**: Keep themes, images, and other resource types in separate subfolders
2. **Use relative paths**: Reference assets relative to the output root (e.g., `XGUI/...`)
3. **Lowercase names**: Use lowercase filenames for cross-platform compatibility
4. **Small file sizes**: Optimize images before adding to keep build size small
5. **Version control**: Commit all assets to the repository for team collaboration
6. **XGUI-3 compatibility**: Use `/XGUI/...` paths to match XGUI-3 structure

## Troubleshooting

### Stylesheet not found
- Verify the file exists in `Assets/XGUI/` or `Assets/themes/`
- Check the path in `[StyleSheet]` matches the output location
- Run `dotnet build` to ensure assets are copied

### Image not loading
- Check console for "Failed to load texture" messages
- Verify file path in CSS `url()` is correct
- Ensure image file is in `Assets/` and copied to output

### Build not copying assets
- Verify `.csproj` has the `<Content Include="Assets\**\*">` section
- Clean and rebuild: `dotnet clean && dotnet build`
