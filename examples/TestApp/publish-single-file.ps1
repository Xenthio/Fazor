# Publish TestApp as a single-file executable
# This script creates an optimized single-file executable with all content packed in

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $ScriptDir "TestApp.csproj"

Write-Host "Publishing TestApp as single-file executable..." -ForegroundColor Cyan

# Determine runtime identifier
$Runtime = "win-x64"
if ($IsMacOS) {
    # Detect macOS architecture
    $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
    if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
        $Runtime = "osx-arm64"
    } else {
        $Runtime = "osx-x64"
    }
} elseif ($IsLinux) {
    # Detect Linux architecture
    $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
    if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
        $Runtime = "linux-arm64"
    } else {
        $Runtime = "linux-x64"
    }
} else {
    # Detect Windows architecture
    $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
    if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
        $Runtime = "win-arm64"
    } else {
        $Runtime = "win-x64"
    }
}

$OutputDir = Join-Path $ScriptDir "publish\$Runtime"

Write-Host "Target runtime: $Runtime" -ForegroundColor Green
Write-Host "Output directory: $OutputDir" -ForegroundColor Green

# Clean output directory
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Publish with single-file settings
# - PublishSingleFile: Creates a single executable
# - IncludeNativeLibrariesForSelfExtract: true = embed native libs (PathResolver workaround enables this)
# - PublishReadyToRun: Pre-compiles for better startup performance
# - SelfContained: false = requires .NET 8 runtime (smaller), true = includes runtime (larger)
dotnet publish $Project `
    -c Release `
    -r $Runtime `
    -o $OutputDir `
    --self-contained false `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=true

# Show results
Write-Host ""
Write-Host "✅ Published successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Output directory: $OutputDir" -ForegroundColor Yellow

$ExePath = Join-Path $OutputDir "TestApp.exe"
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $OutputDir "TestApp"
}

if (Test-Path $ExePath) {
    $FileSize = (Get-Item $ExePath).Length
    $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
    Write-Host "Executable size: $FileSizeMB MB" -ForegroundColor Cyan
}

$DirSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$DirSizeMB = [math]::Round($DirSize / 1MB, 2)
Write-Host "Total directory size: $DirSizeMB MB" -ForegroundColor Cyan

Write-Host ""
Write-Host "To run:" -ForegroundColor Yellow
if ($Runtime -eq "win-x64") {
    Write-Host "  $ExePath" -ForegroundColor White
} else {
    Write-Host "  $ExePath" -ForegroundColor White
}
