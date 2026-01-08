# Publishing SimpleDesktopApp

This directory contains configurable scripts to publish SimpleDesktopApp as an optimized single-file executable with optional features.

## Quick Start

### Default Build (Assets on disk, framework-dependent)

**Linux/macOS:**
```bash
./publish.sh
```

**Windows:**
```powershell
.\publish.ps1
```

### Advanced Options

**Embed Assets (truly single-file):**
```bash
./publish.sh --pack-assets
```

**Self-Contained with Trimming (no runtime required):**
```bash
./publish.sh --self-contained --trimmed
```

**Everything Embedded (truly single-file, self-contained):**
```bash
./publish.sh --pack-assets --self-contained --trimmed
```

## Configuration Options

The new `publish.sh` and `publish.ps1` scripts support the following options:

| Option | Description | Impact |
|--------|-------------|--------|
| `--pack-assets` | Embed Assets in the executable | Assets extract to temp directory at runtime |
| `--self-contained` | Include .NET runtime | +15-20MB, no runtime installation needed |
| `--trimmed` | Enable assembly trimming | -5-10MB (requires `--self-contained`) |
| `--runtime <RID>` | Target specific runtime | Override auto-detection |

### Examples

```bash
# Default: Assets on disk, framework-dependent (~69MB exe + 2MB assets)
./publish.sh

# Embed assets: Truly single-file (~71MB exe only)
./publish.sh --pack-assets

# Self-contained: Includes .NET runtime (~85MB total)
./publish.sh --self-contained --trimmed

# Maximum optimization: Everything embedded, self-contained, trimmed (~80MB exe only)
./publish.sh --pack-assets --self-contained --trimmed
```

## What Gets Published

The publish scripts create a **single-file executable** with:
- ✅ All .NET assemblies bundled into one file
- ✅ Native windowing libraries (SDL2, GLFW) embedded in the executable
- ✅ ReadyToRun compilation for faster startup
- ✅ Configurable: Assets on disk OR embedded in exe
- ✅ Configurable: Framework-dependent OR self-contained

**Note:** A PathResolver workaround enables SDL2/GLFW DLLs to be embedded by helping Silk.NET find them in the extraction directory. See [Silk.NET Issue #2157](https://github.com/dotnet/Silk.NET/issues/2157) for technical details.

## Output Structure

### Default Build (Assets on disk)
```
publish/
├── linux-x64/
│   ├── SimpleDesktopApp          # Single executable (~69MB, includes SDL/GLFW)
│   ├── Assets/                  # Themes, fonts, images (~2MB)
│   └── *.pdb                    # Debug symbols (optional)
├── osx-x64/
│   ├── SimpleDesktopApp
│   └── Assets/
└── win-x64/
    ├── SimpleDesktopApp.exe
    └── Assets/
```

### With --pack-assets (Truly single-file)
```
publish/
├── linux-x64/
│   ├── SimpleDesktopApp          # Single executable (~71MB, includes everything)
│   └── *.pdb                    # Debug symbols (optional)
├── osx-x64/
│   └── SimpleDesktopApp
└── win-x64/
    └── SimpleDesktopApp.exe
```

## Size Comparison

| Configuration | Exe Size | Total Size | Runtime Required |
|---------------|----------|------------|------------------|
| Default | 69MB | 71MB (exe + assets) | .NET 10 |
| --pack-assets | 71MB | 71MB (exe only) | .NET 10 |
| --self-contained | 85MB | 87MB (exe + assets) | None |
| --self-contained --trimmed | 80MB | 82MB (exe + assets) | None |
| --pack-assets --self-contained --trimmed | 80MB | 80MB (exe only) | None |

## Legacy Scripts

The original `publish-single-file.sh` and `publish-single-file.ps1` scripts are still available for compatibility but are deprecated in favor of the new configurable `publish.sh` and `publish.ps1` scripts.
## Manual Publishing

You can also publish manually with `dotnet publish`:

```bash
# Default: Framework-dependent, Assets on disk
dotnet publish -c Release -r linux-x64 --self-contained false \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishReadyToRun=true \
  /p:PackAssets=false

# With packed assets
dotnet publish -c Release -r linux-x64 --self-contained false \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishReadyToRun=true \
  /p:PackAssets=true

# Self-contained with trimming
dotnet publish -c Release -r linux-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true \
  /p:PublishReadyToRun=true \
  /p:PublishTrimmed=true \
  /p:PackAssets=false
```

**Note:** `IncludeNativeLibrariesForSelfExtract=true` is supported thanks to a PathResolver workaround that helps Silk.NET find extracted DLLs. See NativeWindow.cs for implementation details.

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
| Debug build (separate DLLs) | ~218MB | .NET 10 Runtime |
| Release single-file (framework-dependent) | ~71MB (69MB exe + 2MB assets) | .NET 10 Runtime |
| Release single-file (self-contained) | ~85-90MB | None |

## CI/CD Integration

For automated builds, use:

```yaml
- name: Publish SimpleDesktopApp
  run: dotnet publish examples/SimpleDesktopApp/SimpleDesktopApp.csproj -c Release -r linux-x64 -o dist/ --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```
