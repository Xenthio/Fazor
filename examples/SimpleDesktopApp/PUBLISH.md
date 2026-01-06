# Publishing SimpleDesktopApp

This directory contains scripts to publish the SimpleDesktopApp as an optimized single-file executable.

## Quick Start

### Linux/macOS
```bash
./publish-single-file.sh
```

### Windows
```powershell
.\publish-single-file.ps1
```

## What Gets Published

The publish scripts create a **single-file executable** with:
- ✅ All .NET assemblies bundled into one file
- ✅ Native libraries (yoga, SkiaSharp, Silk.NET, etc.) embedded
- ✅ ReadyToRun compilation for faster startup
- ✅ Assets folder extracted alongside the executable
- ✅ Small size (~68MB framework-dependent, ~80MB self-contained)

## Output

Published files are created in:
```
publish/
├── linux-x64/
│   ├── SimpleDesktopApp      # Single executable
│   ├── Assets/               # Themes, fonts, images
│   └── *.pdb                 # Debug symbols (optional)
├── osx-x64/
└── win-x64/
```

## Configuration Options

### Framework-Dependent (Default)
- Requires .NET 8 Runtime installed
- Smaller size (~68MB)
- Faster to distribute
- Default setting: `--self-contained false`

### Self-Contained
- Includes .NET Runtime
- Larger size (~80-90MB)
- No runtime installation required
- To enable: Edit script and change `--self-contained false` to `--self-contained true`

### Trimming (Advanced)
- Can reduce size further by removing unused code
- Currently disabled to avoid issues with reflection-heavy code
- To enable: Add `/p:PublishTrimmed=true` to the publish command

## Manual Publishing

You can also publish manually with `dotnet publish`:

```bash
# Framework-dependent (requires .NET 8 Runtime)
dotnet publish -c Release -r linux-x64 --self-contained false \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishReadyToRun=true

# Self-contained (includes .NET Runtime)
dotnet publish -c Release -r linux-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishReadyToRun=true
```

## Runtime Identifiers

Common runtime identifiers (RIDs):
- `linux-x64` - 64-bit Linux
- `linux-arm64` - ARM64 Linux (Raspberry Pi, etc.)
- `osx-x64` - Intel macOS
- `osx-arm64` - Apple Silicon macOS (M1/M2/M3)
- `win-x64` - 64-bit Windows
- `win-arm64` - ARM64 Windows

## Troubleshooting

### "Could not copy ... The file is locked by MSBuild.exe"
This is fixed in the current version. The issue occurred when MSBuild loaded the Avalazor.Build.dll multiple times. The fix uses `GenerateDependencyFile` and proper isolated loading.

### Native library not found
Make sure the Avalazor.UI.csproj includes the native libraries with `CopyToPublishDirectory="PreserveNewest"`. This is already configured for yoga, SkiaSharp, and Silk.NET libraries.

### Assets not found at runtime
Assets are extracted alongside the executable. The app looks for them in the `Assets/` directory relative to the executable location.

## Size Comparison

| Configuration | Size | Requirements |
|---------------|------|--------------|
| Debug build (separate DLLs) | ~218MB | .NET 8 Runtime |
| Release single-file (framework-dependent) | ~68MB | .NET 8 Runtime |
| Release single-file (self-contained) | ~80-90MB | None |
| Release single-file + trimmed | ~50-60MB | None (risky) |

## CI/CD Integration

For automated builds, use:

```yaml
- name: Publish SimpleDesktopApp
  run: dotnet publish examples/SimpleDesktopApp/SimpleDesktopApp.csproj -c Release -r linux-x64 -o dist/ --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
