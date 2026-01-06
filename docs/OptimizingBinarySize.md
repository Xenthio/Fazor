# Optimizing Binary Size

This document explains how to reduce the size of published Avalazor binaries.

## Default Configuration

By default, the example projects are configured for **cross-platform compatibility and proper functionality**:

- **OpenGL backend**: Used on Linux/macOS
- **Direct3D11 backend**: Used on Windows (OpenGL is broken on Windows)
- **No AI renderer**: AI debugging tools are excluded
- **No ReadyToRun**: Smaller binaries, slightly slower startup
- **No debug symbols**: Release builds exclude .pdb files

## Size Comparison

| Configuration | Size (Linux x64) | Size (Windows x64) | Notes |
|--------------|------------------|-------------------|-------|
| All backends + R2R | ~98 MB | ~98 MB | Original (all features) |
| OpenGL + D3D11, no R2R | ~54 MB | ~54 MB | **Default (reliable)** |
| OpenGL only, no R2R | ~37 MB | ~37 MB | Smaller but broken on Windows |

**Note:** D3D11 backend is required for Windows as OpenGL support is unreliable on that platform.

## Build Configurations

### Standard Build (Recommended)

```bash
./publish.ps1 -PackAssets -SelfContained -Trimmed
```

This produces reliable binaries (~54 MB) with:
- Single executable file
- All dependencies included
- Assets embedded
- OpenGL backend (Linux/macOS) + D3D11 backend (Windows)
- Maximum trimming

### Including Optional Features

To enable additional backends or features, you can define compile-time constants:

#### Enable AI Renderer (for headless debugging)

Add to your `.csproj`:
```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);INCLUDE_AI_RENDERER</DefineConstants>
</PropertyGroup>
```

Then reference the AI project:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Sandbox.UI.AI\Sandbox.UI.AI.csproj" />
</ItemGroup>
```

Size impact: +5-10 MB

#### Enable Vulkan Backend

Add to your `.csproj`:
```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);INCLUDE_VULKAN_BACKEND</DefineConstants>
</PropertyGroup>
```

Size impact: +15-20 MB

**Note:** Direct3D11 backend is always included as it's required for Windows support.

### Enable ReadyToRun (Faster Startup)

To enable R2R for faster startup at the cost of larger binaries, modify your `.csproj`:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

Or pass `/p:PublishReadyToRun=true` to the publish command.

Size impact: +20-25 MB

## Platform-Specific Optimizations

### Linux/macOS

The default OpenGL backend is optimal for these platforms. No additional configuration needed.

### Windows

The Direct3D11 backend is **always included** and automatically used on Windows because OpenGL support is unreliable on that platform. The D3D11Composition backend provides proper transparency support.

## Further Size Reduction

If you need even smaller binaries:

1. **Remove unused controls**: Comment out unused controls in `Sandbox.UI/Controls/`
2. **Minimize Assets**: Reduce the size of fonts, images, and stylesheets
3. **Profile trimming**: Use `dotnet-illink` analyzer to identify what can be safely trimmed
4. **Consider framework-dependent deployment**: Requires .NET runtime on target machine but much smaller (~5-10 MB)

## Troubleshooting

### Trimming Warnings

The build may produce trim analysis warnings. These are mostly safe to ignore as long as the application works correctly. Critical paths have been annotated to preserve necessary code.

### OpenGL on Windows

OpenGL is not used on Windows by default because it has known issues. The framework automatically uses Direct3D11 instead, which is more reliable and provides better transparency support.

### Runtime Errors

If the application fails to start after trimming, try:
1. Set `<TrimMode>partial</TrimMode>` instead of `full`
2. Add `<PublishTrimmed>false</PublishTrimmed>` to disable trimming
3. Check the trim warnings for hints about what might be missing
