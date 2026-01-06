#!/bin/bash
# Configurable publish script for SimpleDesktopApp
# 
# Usage:
#   ./publish.sh                           # Default: Assets on disk, framework-dependent
#   ./publish.sh --pack-assets             # Embed Assets in exe
#   ./publish.sh --self-contained --trimmed # Self-contained with trimming
#   ./publish.sh --pack-assets --self-contained --trimmed # Everything embedded

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT="$SCRIPT_DIR/SimpleDesktopApp.csproj"

# Default options
PACK_ASSETS=false
SELF_CONTAINED=false
TRIMMED=false
RUNTIME=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --pack-assets)
            PACK_ASSETS=true
            shift
            ;;
        --self-contained)
            SELF_CONTAINED=true
            shift
            ;;
        --trimmed)
            TRIMMED=true
            shift
            ;;
        --runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo ""
            echo "Options:"
            echo "  --pack-assets      Embed Assets in the executable"
            echo "  --self-contained   Include .NET runtime (no installation required)"
            echo "  --trimmed          Enable assembly trimming (requires --self-contained)"
            echo "  --runtime RID      Target runtime identifier (auto-detected by default)"
            echo "  --help             Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                                          # Default build"
            echo "  $0 --pack-assets                            # Embed assets"
            echo "  $0 --self-contained --trimmed               # Self-contained, trimmed"
            echo "  $0 --pack-assets --self-contained --trimmed # Truly single-file"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

echo "==========================================="
echo "  SimpleDesktopApp Configurable Publisher  "
echo "==========================================="
echo ""

# Determine runtime identifier if not specified
if [[ -z "$RUNTIME" ]]; then
    RUNTIME="linux-x64"
    if [[ "$OSTYPE" == "darwin"* ]]; then
        if [[ $(uname -m) == "arm64" ]]; then
            RUNTIME="osx-arm64"
        else
            RUNTIME="osx-x64"
        fi
    elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
        if [[ $(uname -m) == "aarch64" || $(uname -m) == "arm64" ]]; then
            RUNTIME="win-arm64"
        else
            RUNTIME="win-x64"
        fi
    else
        if [[ $(uname -m) == "aarch64" ]]; then
            RUNTIME="linux-arm64"
        else
            RUNTIME="linux-x64"
        fi
    fi
fi

OUTPUT_DIR="$SCRIPT_DIR/publish/$RUNTIME"

# Display configuration
echo "Configuration:"
echo "  Runtime:          $RUNTIME"
echo "  Pack Assets:      $PACK_ASSETS"
echo "  Self-Contained:   $SELF_CONTAINED"
echo "  Trimmed:          $TRIMMED"
echo "  Output Dir:       $OUTPUT_DIR"
echo ""

# Validate options
if [[ "$TRIMMED" == "true" && "$SELF_CONTAINED" == "false" ]]; then
    echo "⚠️  Warning: Trimming requires SelfContained. Enabling SelfContained."
    SELF_CONTAINED=true
fi

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build publish command
PUBLISH_ARGS=(
    "-c" "Release"
    "-r" "$RUNTIME"
    "-o" "$OUTPUT_DIR"
    "--self-contained" "$SELF_CONTAINED"
    "/p:PublishSingleFile=true"
    "/p:IncludeNativeLibrariesForSelfExtract=true"
    "/p:PublishReadyToRun=true"
)

if [[ "$TRIMMED" == "true" ]]; then
    PUBLISH_ARGS+=("/p:PublishTrimmed=true")
fi

if [[ "$PACK_ASSETS" == "false" ]]; then
    PUBLISH_ARGS+=("/p:PackAssets=false")
else
    PUBLISH_ARGS+=("/p:PackAssets=true")
fi

echo "Publishing..."
dotnet publish "$PROJECT" "${PUBLISH_ARGS[@]}"

if [ $? -ne 0 ]; then
    echo ""
    echo "❌ Publish failed!"
    exit 1
fi

# Show results
echo ""
echo "✅ Published successfully!"
echo ""

if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    EXE_PATH="$OUTPUT_DIR/SimpleDesktopApp.exe"
else
    EXE_PATH="$OUTPUT_DIR/SimpleDesktopApp"
fi

if [[ -f "$EXE_PATH" ]]; then
    EXE_SIZE=$(du -h "$EXE_PATH" | cut -f1)
    echo "Executable size:        $EXE_SIZE"
fi

DIR_SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
echo "Total directory size:   $DIR_SIZE"

echo ""
echo "Contents:"
ls -lh "$OUTPUT_DIR" | head -n 12

echo ""
echo "To run:"
echo "  $EXE_PATH"
echo ""

# Show deployment notes
echo "Deployment notes:"
if [[ "$PACK_ASSETS" == "true" ]]; then
    echo "  ✓ Assets embedded (extract to temp at runtime)"
else
    echo "  ⓘ Assets folder must be deployed alongside executable"
fi

if [[ "$SELF_CONTAINED" == "true" ]]; then
    echo "  ✓ .NET runtime included (no installation required)"
else
    echo "  ⓘ Requires .NET 8 runtime on target machine"
fi

echo ""
