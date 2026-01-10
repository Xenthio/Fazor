# Native Yoga Library

This directory contains the native Yoga layout engine library required by Fazor.UI.

## Setup

Fazor uses Facebook's Yoga layout engine via P/Invoke for maximum performance flexbox layout. You need to build the native library before running the project.

### Quick Start: Build from Source ✅

The build scripts use a custom CMakeLists.txt (inspired by YogaSharp) that properly builds Yoga as a **shared library** (.dll/.so/.dylib).

**Linux / macOS:**
```bash
cd src/Fazor.UI/Native
chmod +x build-yoga-libs.sh
./build-yoga-libs.sh
```

**Windows:**
```powershell
cd src/Fazor.UI/Native
.\build-yoga-libs.ps1
```

**Build Requirements:**
- **CMake** 3.15+ (`cmake --version`)
- **C++20 compiler**:
  - Linux: GCC 10+ or Clang 12+ (`gcc --version`)
  - macOS: Xcode Command Line Tools (`xcode-select --install`)
  - Windows: Visual Studio 2022 with C++ Desktop Development workload
- Internet connection to download Yoga source (1.2 MB)

### What the Scripts Do

1. Download Yoga v3.1.0 source from GitHub
2. Create a custom CMakeLists.txt that builds as SHARED library
3. Configure with CMake (C++20 standard)
4. Build the library
5. Copy to this directory as:
   - `yoga.dll` (Windows)
   - `libyoga.so` (Linux)
   - `libyoga.dylib` (macOS)

### Why a Custom CMakeLists.txt?

Yoga v3.1.0's default CMake configuration builds `yogacore` as a **static library**. The scripts replace it with a custom CMakeLists.txt that:
- Explicitly uses `add_library(yoga SHARED ...)` 
- Sets `CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=ON` for Windows
- Requires C++20 standard
- Includes all Yoga source files

This approach is based on [YogaSharp](https://github.com/VanderCat/YogaSharp)'s CMakeLists.txt.

### Alternative: Manual Download

If you prefer not to build from source:

**From React Native:**
1. Install React Native for your platform
2. Extract `yoga.dll` (Windows), `libyoga.so` (Linux), or `libyoga.dylib` (macOS) from the React Native installation
3. Place in this directory

**From Community Builds:**
1. Search GitHub for "yoga prebuilt binaries"
2. Download the appropriate file for your platform
3. Place in this directory

**Build from Source with Modifications:**
1. Download Yoga v3.1.0: https://github.com/facebook/yoga/releases/tag/v3.1.0
2. Modify `yoga/CMakeLists.txt`: Change `add_library(yogacore STATIC` to `SHARED`
3. Build with CMake
4. Copy resulting library here

### Required Files

After setup, you should have **one** of these files in this directory:
- `libyoga.so` - Linux shared library
- `libyoga.dylib` - macOS dynamic library
- `yoga.dll` - Windows dynamic-link library

The MSBuild configuration will automatically copy it to the output directory during build.

## Why NPM Packages Don't Have Binaries

The NPM packages (`yoga-layout`, `yoga-layout-prebuilt`) only contain:
- JavaScript/TypeScript wrapper code
- WASM binaries for browsers
- **No native .dll/.so/.dylib files**

This is why manual acquisition or building from source is required.

## Architecture

Fazor uses s&box's P/Invoke approach:
- Native Yoga C++ library (Facebook's official implementation v3.1.0)
- Direct P/Invoke bindings in C# (`Yoga.cs`, `YogaWrapper.cs`, `YGNodeRef.cs`)
- No managed wrapper dependencies
- Maximum performance with full Yoga feature support

This is the same architecture used by s&box for their UI system.

## Troubleshooting

**Build fails with "yogacore.lib" instead of DLL:**
- Yoga v3.1.0's CMake defaults to static library
- Solution: Modify Yoga's CMakeLists.txt to use SHARED, or use prebuilt binary

**"yoga library not found" runtime error:**
- Ensure the native library file is in this directory
- Check filename matches your platform
- On Linux: May need to add to LD_LIBRARY_PATH or use `ldconfig`

**Windows: Visual Studio not found:**
- Install Visual Studio 2022
- Include "Desktop development with C++" workload
- Ensure CMake can find it

**macOS: Command Line Tools not found:**
- Run: `xcode-select --install`
- Accept the license if prompted

**Linux: Compiler not found:**
- Debian/Ubuntu: `sudo apt install build-essential cmake`
- Fedora/RHEL: `sudo dnf install gcc-c++ cmake`
- Arch: `sudo pacman -S base-devel cmake`

## For More Information

- Yoga GitHub: https://github.com/facebook/yoga
- s&box Panel system reference
- Fazor documentation: See repository README
