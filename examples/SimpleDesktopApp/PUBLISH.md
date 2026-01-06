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
- ✅ ReadyToRun compilation for faster startup
- ✅ Assets folder extracted alongside the executable
- ⚠️ Native windowing libraries (SDL2, GLFW) extracted alongside the executable
- ⚠️ Native yoga layout library extracted alongside the executable
- ✅ Small size (~54MB exe + ~17MB native libs = ~71MB total)

**Note:** Native libraries for windowing (SDL2.dll, glfw3.dll, libSDL2-2.0.so, etc.) cannot be embedded in the single-file bundle due to Silk.NET P/Invoke requirements. They must remain on disk alongside the executable. See [Silk.NET Issue #2157](https://github.com/dotnet/Silk.NET/issues/2157) for details.

## Output

Published files are created in:
```
publish/
├── linux-x64/
│   ├── SimpleDesktopApp          # Single executable (~54MB)
│   ├── libSDL2-2.0.so           # SDL windowing library
│   ├── libglfw.so.3             # GLFW windowing library
│   ├── libyoga.so               # Yoga layout library
│   ├── Assets/                  # Themes, fonts, images
│   └── *.pdb                    # Debug symbols (optional)
├── osx-x64/
│   ├── SimpleDesktopApp
│   ├── libSDL2-2.0.dylib
│   ├── libglfw.3.dylib
│   └── libyoga.dylib
└── win-x64/
    ├── SimpleDesktopApp.exe
    ├── SDL2.dll
    ├── glfw3.dll
    └── yoga.dll
```

## Configuration Options

### Framework-Dependent (Default)
- Requires .NET 8 Runtime installed
- Smaller size (~54MB exe + ~17MB native libs = ~71MB total)
- Faster to distribute
- Default setting: `--self-contained false`

### Self-Contained
- Includes .NET Runtime
- Larger size (~90-100MB total)
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
  /p:IncludeNativeLibrariesForSelfExtract=false \
  /p:PublishReadyToRun=true

# Self-contained (includes .NET Runtime)
dotnet publish -c Release -r linux-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=false \
  /p:PublishReadyToRun=true
```

**Important:** `IncludeNativeLibrariesForSelfExtract` must be `false` for Silk.NET applications. Setting it to `true` causes `PlatformNotSupportedException` because SDL/GLFW libraries cannot be loaded from the extraction directory.

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
This error occurs when SDL2 or GLFW native libraries cannot be found or loaded. 

**Steps to fix:**

1. **Republish the application** with the latest code:
   ```bash
   ./publish-single-file.sh  # or publish-single-file.ps1 on Windows
   ```

2. **Verify native libraries are present** alongside the executable:
   - Windows: Check for `SDL2.dll`, `glfw3.dll`, and `yoga.dll` in the same folder as `SimpleDesktopApp.exe`
   - Linux: Check for `libSDL2-2.0.so`, `libglfw.so.3`, and `libyoga.so`
   - macOS: Check for `libSDL2-2.0.dylib`, `libglfw.3.dylib`, and `libyoga.dylib`

3. **Install Visual C++ Redistributables (Windows only)**:
   - SDL2.dll and glfw3.dll require the Microsoft Visual C++ Redistributables
   - Download from: https://aka.ms/vs/17/release/vc_redist.x64.exe
   - Install both x64 and x86 versions if unsure of your architecture

4. **Verify the configuration**: Ensure `IncludeNativeLibrariesForSelfExtract=false` in SimpleDesktopApp.csproj (this is already set in the latest code)

See: https://github.com/dotnet/Silk.NET/issues/2157

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
| Release single-file (framework-dependent) | ~71MB | .NET 8 Runtime |
| Release single-file (self-contained) | ~90-100MB | None |

## CI/CD Integration

For automated builds, use:

```yaml
- name: Publish SimpleDesktopApp
  run: dotnet publish examples/SimpleDesktopApp/SimpleDesktopApp.csproj -c Release -r linux-x64 -o dist/ --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=false
```
