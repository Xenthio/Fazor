# Optimizing Binary Size

This document explains how to reduce the size of published Fazor/Avalazor binaries.

## Default Configuration

By default, the example projects are configured for **optimal size** while maintaining cross-platform compatibility:

- **OpenGL backend only**: The most portable and efficient backend
- **No AI renderer**: AI debugging tools are excluded
- **No ReadyToRun**: Smaller binaries, slightly slower startup
- **No debug symbols**: Release builds exclude .pdb files

## Size Comparison

| Configuration | Size (Linux x64) | Notes |
|--------------|------------------|-------|
| All backends + R2R | ~98 MB | Original (all features) |
| OpenGL only + R2R | ~61 MB | 38% smaller |
| **OpenGL only, no R2R** | **~37 MB** | **62% smaller (default)** |

## Build Configurations

### Standard Build (Recommended)

```bash
./publish.ps1 -PackAssets -SelfContained -Trimmed
```

This produces the smallest binaries (~37 MB on Linux) with:
- Single executable file
- All dependencies included
- Assets embedded
- OpenGL backend only
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

#### Enable DirectX 11 Backend (Windows only)

Add to your `.csproj`:
```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);INCLUDE_D3D11_BACKEND</DefineConstants>
</PropertyGroup>
```

Size impact: +15-20 MB

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

For the best Windows experience with transparency support, you can enable D3D11:

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' And '$(RuntimeIdentifier)'.Contains('win')">
  <DefineConstants>$(DefineConstants);INCLUDE_D3D11_BACKEND</DefineConstants>
</PropertyGroup>
```

The application will auto-select D3D11 Composition backend on Windows when available.

## Further Size Reduction

If you need even smaller binaries:

1. **Remove unused controls**: Comment out unused controls in `Sandbox.UI/Controls/`
2. **Minimize Assets**: Reduce the size of fonts, images, and stylesheets
3. **Profile trimming**: Use `dotnet-illink` analyzer to identify what can be safely trimmed
4. **Consider framework-dependent deployment**: Requires .NET runtime on target machine but much smaller (~5-10 MB)

## Troubleshooting

### Trimming Warnings

The build may produce trim analysis warnings. These are mostly safe to ignore as long as the application works correctly. Critical paths have been annotated to preserve necessary code.

### Missing Backend Errors

If you see errors about missing backend types (e.g., `D3D11Backend`), ensure you've added the appropriate define constant to enable that backend.

### Runtime Errors

If the application fails to start after trimming, try:
1. Set `<TrimMode>partial</TrimMode>` instead of `full`
2. Add `<PublishTrimmed>false</PublishTrimmed>` to disable trimming
3. Check the trim warnings for hints about what might be missing
