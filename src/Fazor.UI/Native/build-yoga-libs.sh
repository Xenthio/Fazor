#!/bin/bash
# Script to build Facebook Yoga native library from source
# Requires CMake and C++ compiler (GCC or Clang)

set -e

echo "Fazor Yoga Native Library Setup"
echo "===================================="
echo ""

# Determine platform
PLATFORM=$(uname -s)
ARCH=$(uname -m)

echo "Platform: $PLATFORM"
echo "Architecture: $ARCH"
echo ""
echo "Building Yoga as a shared library..."
echo "Required: CMake 3.15+ and C++ compiler (GCC/Clang)"
echo ""

# Get script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Determine target library name
case "$PLATFORM" in
    Linux*)
        TARGET="libyoga.so"
        ;;
    Darwin*)
        TARGET="libyoga.dylib"
        ;;
    *)
        echo "Unsupported platform: $PLATFORM"
        exit 1
        ;;
esac

# Create temp directory
TEMP_DIR=$(mktemp -d)
cd "$TEMP_DIR"

# Download Yoga source
echo "Downloading Yoga v3.1.0 source..."
curl -L "https://github.com/facebook/yoga/archive/refs/tags/v3.1.0.tar.gz" -o yoga.tar.gz

# Extract
echo "Extracting..."
tar -xzf yoga.tar.gz

# Create custom CMakeLists.txt for building as shared library
echo "Creating custom CMakeLists.txt for shared library build..."
cat > yoga-3.1.0/CMakeLists.txt << 'EOF'
cmake_minimum_required(VERSION 3.15)

project(yoga-shared)

# Yoga requires C++20
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(YOGA_ROOT ${CMAKE_CURRENT_SOURCE_DIR})

# Collect all Yoga source files
file(GLOB_RECURSE SOURCES 
    ${YOGA_ROOT}/yoga/*.cpp
)

# Create shared library
add_library(yoga SHARED ${SOURCES})

# Include directories
target_include_directories(yoga
    PUBLIC
    $<BUILD_INTERFACE:${YOGA_ROOT}>
    $<INSTALL_INTERFACE:${CMAKE_INSTALL_PREFIX}/include/yoga>
)

# Debug definitions
target_compile_definitions(yoga PRIVATE
    $<$<CONFIG:Debug>:DEBUG=1>
)

# Set output name (removes "lib" prefix on some platforms if needed)
set_target_properties(yoga PROPERTIES OUTPUT_NAME "yoga")
EOF

# Build
echo "Building with CMake..."
cd yoga-3.1.0
mkdir -p build
cd build

cmake .. -DCMAKE_BUILD_TYPE=Release
if [ $? -ne 0 ]; then
    echo "CMake configuration failed."
    echo "Make sure CMake and a C++ compiler are installed."
    exit 1
fi

cmake --build . -- -j$(nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 4)
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

# Find and copy the library
echo "Searching for built library..."
LIB_FOUND=false

# Search for the built library
for location in \
    "libyoga.so" \
    "libyoga.dylib" \
    "yoga.so" \
    "yoga.dylib" \
    "lib/libyoga.so" \
    "lib/libyoga.dylib"
do
    if [ -f "$location" ]; then
        echo "Found at: $location"
        cp "$location" "$SCRIPT_DIR/$TARGET"
        LIB_FOUND=true
        break
    fi
done

if [ "$LIB_FOUND" = false ]; then
    echo ""
    echo "ERROR: Could not find shared library after build"
    echo ""
    echo "Searching for any built files:"
    find . -name "*.so" -o -name "*.dylib" -o -name "*.a" | head -10
    echo ""
    exit 1
fi

# Cleanup
cd "$SCRIPT_DIR"
rm -rf "$TEMP_DIR"

echo ""
echo "Successfully built and installed $TARGET!"
echo "Location: $SCRIPT_DIR/$TARGET"
echo ""
echo "The native Yoga library is now ready for P/Invoke."
