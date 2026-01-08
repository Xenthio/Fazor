#!/bin/bash
# Publish SimpleDesktopApp as a single-file executable
# This script creates an optimized single-file executable with all content packed in

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT="$SCRIPT_DIR/SimpleDesktopApp.csproj"

echo "Publishing SimpleDesktopApp as single-file executable..."

# Determine runtime identifier
RUNTIME="linux-x64"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # Detect macOS architecture
    if [[ $(uname -m) == "arm64" ]]; then
        RUNTIME="osx-arm64"
    else
        RUNTIME="osx-x64"
    fi
elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    # Detect Windows architecture
    if [[ $(uname -m) == "aarch64" || $(uname -m) == "arm64" ]]; then
        RUNTIME="win-arm64"
    else
        RUNTIME="win-x64"
    fi
else
    # Detect Linux architecture
    if [[ $(uname -m) == "aarch64" ]]; then
        RUNTIME="linux-arm64"
    else
        RUNTIME="linux-x64"
    fi
fi

OUTPUT_DIR="$SCRIPT_DIR/publish/$RUNTIME"

echo "Target runtime: $RUNTIME"
echo "Output directory: $OUTPUT_DIR"

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Publish with single-file settings
# - PublishSingleFile: Creates a single executable
# - IncludeNativeLibrariesForSelfExtract: true = embed native libs (PathResolver workaround enables this)
# - PublishReadyToRun: Pre-compiles for better startup performance
# - SelfContained: false = requires .NET 10 runtime (smaller), true = includes runtime (larger)
dotnet publish "$PROJECT" \
    -c Release \
    -r "$RUNTIME" \
    -o "$OUTPUT_DIR" \
    --self-contained false \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:PublishReadyToRun=true

# Show results
echo ""
echo "✅ Published successfully!"
echo ""
echo "Output directory: $OUTPUT_DIR"
echo "Executable size:"
if [[ "$OSTYPE" == "darwin"* ]]; then
    ls -lh "$OUTPUT_DIR/SimpleDesktopApp" | awk '{print $5, $9}'
else
    ls -lh "$OUTPUT_DIR/SimpleDesktopApp" 2>/dev/null | awk '{print $5, $9}' || ls -lh "$OUTPUT_DIR/SimpleDesktopApp.exe" | awk '{print $5, $9}'
fi

echo ""
echo "Contents:"
du -sh "$OUTPUT_DIR"
echo ""
echo "To run:"
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo "  $OUTPUT_DIR/SimpleDesktopApp.exe"
else
    echo "  $OUTPUT_DIR/SimpleDesktopApp"
fi
