#!/bin/bash
# Configurable publish script for SimpleDesktopApp
# Usage: ./publish.sh [OPTIONS]

set -e

# Default values
PACK_ASSETS=false
SELF_CONTAINED=false
TRIMMED=false
NATIVE_AOT=false
RUNTIME=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --pack-assets|-PackAssets)
            PACK_ASSETS=true
            shift
            ;;
        --self-contained|-SelfContained)
            SELF_CONTAINED=true
            shift
            ;;
        --trimmed|-Trimmed)
            TRIMMED=true
            shift
            ;;
        --native-aot|-NativeAot|--nativeaot)
            NATIVE_AOT=true
            shift
            ;;
        --runtime|-Runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --help|-h|-\?)
            echo -e "\033[36mUsage: ./publish.sh [OPTIONS]\033[0m"
            echo ""
            echo -e "\033[33mOptions:\033[0m"
            echo "  --pack-assets              Embed Assets in the executable"
            echo "  --self-contained           Include .NET runtime (no installation required)"
            echo "  --trimmed                  Enable assembly trimming (requires --self-contained)"
            echo "  --native-aot               Enable Native AOT compilation (smallest binaries, ~7-10 MB)"
            echo "  --runtime <RID>            Target runtime identifier (auto-detected by default)"
            echo "  -h, --help                 Show this help message"
            echo ""
            echo -e "\033[33mExamples:\033[0m"
            echo "  ./publish.sh                                      # Default build"
            echo "  ./publish.sh --pack-assets                        # Embed assets"
            echo "  ./publish.sh --self-contained --trimmed           # Self-contained, trimmed"
            echo "  ./publish.sh --native-aot --pack-assets           # Native AOT (smallest)"
            exit 0
            ;;
        *)
            echo "Unknown argument: $1"
            echo "Use --help to see available options"
            exit 1
            ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/SimpleDesktopApp.csproj"

echo -e "\033[36m==========================================="
echo "  SimpleDesktopApp Configurable Publisher  "
echo "===========================================\033[0m"
echo ""

# Determine runtime identifier if not specified
if [ -z "$RUNTIME" ]; then
    OS=$(uname -s)
    ARCH=$(uname -m)
    
    case "$OS" in
        Linux)
            if [ "$ARCH" = "aarch64" ]; then
                RUNTIME="linux-arm64"
            else
                RUNTIME="linux-x64"
            fi
            ;;
        Darwin)
            if [ "$ARCH" = "arm64" ]; then
                RUNTIME="osx-arm64"
            else
                RUNTIME="osx-x64"
            fi
            ;;
        *)
            echo "Unsupported OS: $OS"
            exit 1
            ;;
    esac
fi

OUTPUT_DIR="$SCRIPT_DIR/publish/$RUNTIME"

# Display configuration
echo -e "\033[33mConfiguration:\033[0m"
echo "  Runtime:          $RUNTIME"
echo "  Pack Assets:      $PACK_ASSETS"
echo "  Self-Contained:   $SELF_CONTAINED"
echo "  Trimmed:          $TRIMMED"
echo "  Native AOT:       $NATIVE_AOT"
echo "  Output Dir:       $OUTPUT_DIR"
echo ""

# Validate options
if [ "$NATIVE_AOT" = true ]; then
    echo -e "\033[36mℹ️  Native AOT mode enabled - this produces the smallest binaries (~7-10 MB)\033[0m"
    echo -e "\033[90m   Note: Some features may have limited functionality (see warnings during build)\033[0m"
    SELF_CONTAINED=true
    TRIMMED=true
fi

if [ "$TRIMMED" = true ] && [ "$SELF_CONTAINED" = false ] && [ "$NATIVE_AOT" = false ]; then
    echo -e "\033[33m⚠️  Warning: Trimming requires SelfContained. Enabling SelfContained.\033[0m"
    SELF_CONTAINED=true
fi

# Clean output directory
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Build publish command
PUBLISH_ARGS=(
    "publish" "$PROJECT"
    "-c" "Release"
    "-r" "$RUNTIME"
    "-o" "$OUTPUT_DIR"
    "--self-contained" "$([ "$SELF_CONTAINED" = true ] && echo true || echo false)"
    "/p:PublishSingleFile=true"
    "/p:IncludeNativeLibrariesForSelfExtract=true"
    "/p:PublishReadyToRun=false"
)

if [ "$NATIVE_AOT" = true ]; then
    PUBLISH_ARGS+=("/p:PublishAot=true")
    PUBLISH_ARGS+=("/p:IlcOptimizationPreference=Size")
    PUBLISH_ARGS+=("/p:IlcGenerateStackTraceData=false")
    PUBLISH_ARGS+=("/p:StripSymbols=true")
    # Remove single-file settings for Native AOT
    PUBLISH_ARGS=("${PUBLISH_ARGS[@]/\/p:PublishSingleFile=true/}")
    PUBLISH_ARGS=("${PUBLISH_ARGS[@]/\/p:IncludeNativeLibrariesForSelfExtract=true/}")
fi

if [ "$TRIMMED" = true ]; then
    PUBLISH_ARGS+=("/p:PublishTrimmed=true")
    if [ "$NATIVE_AOT" = true ]; then
        PUBLISH_ARGS+=("/p:TrimMode=full")
    fi
fi

if [ "$PACK_ASSETS" = true ]; then
    PUBLISH_ARGS+=("/p:PackAssets=true")
else
    PUBLISH_ARGS+=("/p:PackAssets=false")
fi

echo -e "\033[36mPublishing...\033[0m"
dotnet "${PUBLISH_ARGS[@]}"

if [ $? -ne 0 ]; then
    echo ""
    echo -e "\033[31m❌ Publish failed!\033[0m"
    exit 1
fi

echo ""
echo -e "\033[32m✅ Published successfully!\033[0m"
echo ""

# Display size information
if [ -f "$OUTPUT_DIR/SimpleDesktopApp" ]; then
    EXE_SIZE=$(du -h "$OUTPUT_DIR/SimpleDesktopApp" | cut -f1)
    DIR_SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
    
    echo "Executable size:        $EXE_SIZE"
    echo "Total directory size:   $DIR_SIZE"
    echo ""
    
    echo "Contents:"
    ls -lh "$OUTPUT_DIR" | tail -n +2 | awk '{printf "  %s (%s)\n", $9, $5}'
    echo ""
    
    echo "To run:"
    echo "  $OUTPUT_DIR/SimpleDesktopApp"
    echo ""
    
    # Show deployment notes
    echo "Deployment notes:"
    if [ "$PACK_ASSETS" = true ]; then
        echo "  [OK] Assets embedded (extracts to temp at runtime)"
    else
        echo "  [!]  Assets are separate files - deploy the entire publish directory"
    fi
    
    if [ "$SELF_CONTAINED" = true ]; then
        echo "  [OK] .NET runtime included (no installation required)"
    else
        echo "  [!]  Requires .NET 8.0 runtime on target machine"
    fi
    
    if [ "$NATIVE_AOT" = true ]; then
        echo "  [OK] Native AOT compilation - maximum performance and smallest size"
    fi
else
    echo -e "\033[31mError: SimpleDesktopApp executable not found in output directory\033[0m"
    exit 1
fi
