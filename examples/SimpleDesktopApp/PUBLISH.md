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
- ✅ Native windowing libraries (SDL2, GLFW) embedded in the executable
- ✅ ReadyToRun compilation for faster startup
- ✅ Assets folder extracted alongside the executable (for file system access)
- ✅ Small size (~69MB exe + ~2MB assets = ~71MB total)

**Note:** A PathResolver workaround enables SDL2/GLFW DLLs to be embedded by helping Silk.NET find them in the extraction directory. Assets remain on disk for file system access. See [Silk.NET Issue #2157](https://github.com/dotnet/Silk.NET/issues/2157) for technical details.

## Output

Published files are created in:
```
publish/
├── linux-x64/
│   ├── SimpleDesktopApp          # Single executable (~69MB, includes SDL/GLFW)
│   ├── Assets/                  # Themes, fonts, images
│   └── *.pdb                    # Debug symbols (optional)
├── osx-x64/
│   ├── SimpleDesktopApp
│   └── Assets/
└── win-x64/
    ├── SimpleDesktopApp.exe
    └── Assets/
```
```

## Configuration Options

### Framework-Dependent (Default)
- Requires .NET 8 Runtime installed
- Smaller size (~69MB exe + ~2MB assets = ~71MB total)
- Faster to distribute
- Default setting: `--self-contained false`

### Self-Contained
- Includes .NET Runtime
- Larger size (~85-90MB total)
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

**Note:** `IncludeNativeLibrariesForSelfExtract=true` is now supported thanks to a PathResolver workaround that helps Silk.NET find extracted DLLs. See NativeWindow.cs for implementation details.

## Runtime Identifiers

Common runtime identifiers (RIDs):
- `linux-x64` - 64-bit Linux
- `linux-arm64` - ARM64 Linux (Raspberry Pi, etc.)
- `osx-x64` - Intel macOS
- `osx-arm64` - Apple Silicon macOS (M1/M2/M3)
- `win-x64` - 64-bit Windows
- `win-arm64` - ARM64 Windows

## Troubleshooting

### "PlatformNotSupportedException: Couldn't find a suitable window platform"
This error should no longer occur with the latest code. Native libraries are now embedded in the executable using a PathResolver workaround.

**If you still see this error:**

1. **Republish the application** with the latest code:
   ```bash
   ./publish-single-file.sh  # or publish-single-file.ps1 on Windows
   ```

2. **Install Visual C++ Redistributables (Windows only)**:
   - SDL2.dll and glfw3.dll require the Microsoft Visual C++ Redistributables
   - Download from: https://aka.ms/vs/17/release/vc_redist.x64.exe
   - Install both x64 and x86 versions if unsure of your architecture

3. **Verify the configuration**: Ensure `IncludeNativeLibrariesForSelfExtract=true` in SimpleDesktopApp.csproj (this is now set to true with PathResolver support)

See: https://github.com/dotnet/Silk.NET/issues/2157

### "Could not copy ... The file is locked by MSBuild.exe"
This is fixed in the current version. The issue occurred when MSBuild loaded the Avalazor.Build.dll multiple times. The fix uses `GenerateDependencyFile` and proper isolated loading.

### Native library not found
Native libraries are now embedded in the single-file executable. Only Assets remain on disk for file system access.

### Assets not found at runtime
Assets are extracted alongside the executable. The app looks for them in the `Assets/` directory relative to the executable location.

## Size Comparison

| Configuration | Size | Requirements |
|---------------|------|--------------|
| Debug build (separate DLLs) | ~218MB | .NET 8 Runtime |
| Release single-file (framework-dependent) | ~71MB (69MB exe + 2MB assets) | .NET 8 Runtime |
| Release single-file (self-contained) | ~85-90MB | None |

## CI/CD Integration

For automated builds, use:

```yaml
- name: Publish SimpleDesktopApp
  run: dotnet publish examples/SimpleDesktopApp/SimpleDesktopApp.csproj -c Release -r linux-x64 -o dist/ --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
