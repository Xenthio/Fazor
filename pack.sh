#!/bin/bash
# Pack Fazor NuGet packages

set -e

CONFIGURATION="${1:-Release}"
OUTPUT_DIR="${2:-./nupkgs}"
VERSION="${3:-}"

echo "================================================"
echo "Fazor NuGet Package Builder"
echo "================================================"
echo ""

# Create output directory
if [ ! -d "$OUTPUT_DIR" ]; then
    mkdir -p "$OUTPUT_DIR"
    echo "Created output directory: $OUTPUT_DIR"
fi

# List of projects to pack
projects=(
    "src/Sandbox.Razor/Sandbox.Razor.csproj"
    "src/Sandbox.UI/Sandbox.UI.csproj"
    "src/Sandbox.UI.Skia/Sandbox.UI.Skia.csproj"
    "src/Sandbox.UI.AI/Sandbox.UI.AI.csproj"
    "src/Fazor.Build/Fazor.Build.csproj"
    "src/Fazor.UI/Fazor.UI.csproj"
)

echo "Building and packing projects..."
echo ""

VERSION_ARG=""
if [ -n "$VERSION" ]; then
    VERSION_ARG="/p:Version=$VERSION"
    echo "Using version: $VERSION"
    echo ""
fi

for project in "${projects[@]}"; do
    echo "Packing: $project"
    
    if [ -n "$VERSION_ARG" ]; then
        dotnet pack "$project" -c "$CONFIGURATION" -o "$OUTPUT_DIR" $VERSION_ARG --no-restore
    else
        dotnet pack "$project" -c "$CONFIGURATION" -o "$OUTPUT_DIR" --no-restore
    fi
    
    if [ $? -ne 0 ]; then
        echo "Failed to pack $project"
        exit 1
    fi
    echo ""
done

echo "================================================"
echo "Packaging complete!"
echo "Packages are in: $OUTPUT_DIR"
echo "================================================"
echo ""
echo "To install locally, run:"
echo "  dotnet nuget add source $(realpath $OUTPUT_DIR) --name fazor-local"
echo ""
