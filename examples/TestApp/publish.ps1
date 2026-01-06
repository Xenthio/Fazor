<#
.SYNOPSIS
    Configurable publish script for TestApp
.DESCRIPTION
    Publishes TestApp as a single-file executable with configurable options
.PARAMETER PackAssets
    Embed Assets in the executable (extracts to temp directory at runtime)
.PARAMETER SelfContained
    Include .NET runtime in the executable (no runtime installation required)
.PARAMETER Trimmed
    Enable assembly trimming to reduce size (only works with SelfContained)
.PARAMETER Runtime
    Target runtime identifier (auto-detected by default)
.EXAMPLE
    .\publish.ps1
    # Default: Assets on disk, framework-dependent
.EXAMPLE
    .\publish.ps1 -PackAssets
    # Embed Assets in exe (PowerShell style)
.EXAMPLE
    .\publish.ps1 --pack-assets
    # Embed Assets in exe (bash style)
.EXAMPLE
    .\publish.ps1 -SelfContained -Trimmed
    # Self-contained with trimming (~80-85MB)
.EXAMPLE
    .\publish.ps1 -PackAssets -SelfContained -Trimmed
    # Everything embedded, self-contained, trimmed (truly single-file)
#>

param(
    [switch]$PackAssets = $false,
    [switch]$SelfContained = $false,
    [switch]$Trimmed = $false,
    [string]$Runtime = "",
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"

# Process remaining arguments (bash-style flags)
if ($RemainingArgs) {
    for ($i = 0; $i -lt $RemainingArgs.Count; $i++) {
        $arg = $RemainingArgs[$i]
        
        switch -Regex ($arg) {
            "^(--pack-assets|--PackAssets)$" {
                $PackAssets = $true
            }
            "^(--self-contained|--SelfContained)$" {
                $SelfContained = $true
            }
            "^(--trimmed|--Trimmed)$" {
                $Trimmed = $true
            }
            "^(--runtime|--Runtime)$" {
                if ($i + 1 -lt $RemainingArgs.Count) {
                    $Runtime = $RemainingArgs[$i + 1]
                    $i++
                }
            }
            "^(--help|-h|-\?)$" {
                Write-Host "Usage: .\publish.ps1 [OPTIONS]" -ForegroundColor Cyan
                Write-Host ""
                Write-Host "Options:" -ForegroundColor Yellow
                Write-Host "  -PackAssets, --pack-assets       Embed Assets in the executable"
                Write-Host "  -SelfContained, --self-contained Include .NET runtime (no installation required)"
                Write-Host "  -Trimmed, --trimmed              Enable assembly trimming (requires -SelfContained)"
                Write-Host "  -Runtime <RID>, --runtime <RID>  Target runtime identifier (auto-detected by default)"
                Write-Host "  -?, -h, --help                   Show this help message"
                Write-Host ""
                Write-Host "Examples:" -ForegroundColor Yellow
                Write-Host "  .\publish.ps1                                              # Default build"
                Write-Host "  .\publish.ps1 -PackAssets                                  # Embed assets"
                Write-Host "  .\publish.ps1 --pack-assets                                # Embed assets (bash style)"
                Write-Host "  .\publish.ps1 -SelfContained -Trimmed                      # Self-contained, trimmed"
                Write-Host "  .\publish.ps1 -PackAssets -SelfContained -Trimmed          # Truly single-file"
                Write-Host "  .\publish.ps1 --pack-assets --self-contained --trimmed     # Truly single-file (bash style)"
                Write-Host ""
                exit 0
            }
            default {
                Write-Host "Unknown argument: $arg" -ForegroundColor Red
                Write-Host "Use --help or -? to see available options" -ForegroundColor Yellow
                exit 1
            }
        }
    }
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $ScriptDir "TestApp.csproj"

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  TestApp Configurable Publisher  " -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""

# Determine runtime identifier if not specified
if ([string]::IsNullOrEmpty($Runtime)) {
    $Runtime = "win-x64"
    if ($IsMacOS) {
        $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
        if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
            $Runtime = "osx-arm64"
        } else {
            $Runtime = "osx-x64"
        }
    } elseif ($IsLinux) {
        $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
        if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
            $Runtime = "linux-arm64"
        } else {
            $Runtime = "linux-x64"
        }
    } else {
        $Arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
        if ($Arch -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
            $Runtime = "win-arm64"
        } else {
            $Runtime = "win-x64"
        }
    }
}

$OutputDir = Join-Path $ScriptDir "publish\$Runtime"

# Display configuration
Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Runtime:          $Runtime" -ForegroundColor White
Write-Host "  Pack Assets:      $PackAssets" -ForegroundColor White
Write-Host "  Self-Contained:   $SelfContained" -ForegroundColor White
Write-Host "  Trimmed:          $Trimmed" -ForegroundColor White
Write-Host "  Output Dir:       $OutputDir" -ForegroundColor White
Write-Host ""

# Validate options
if ($Trimmed -and -not $SelfContained) {
    Write-Host "⚠️  Warning: Trimming requires SelfContained. Enabling SelfContained." -ForegroundColor Yellow
    $SelfContained = $true
}

# Clean output directory
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Build publish command
$publishArgs = @(
    "publish", $Project,
    "-c", "Release",
    "-r", $Runtime,
    "-o", $OutputDir,
    "--self-contained", $SelfContained.ToString().ToLower(),
    "/p:PublishSingleFile=true",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:PublishReadyToRun=true"
)

if ($Trimmed) {
    $publishArgs += "/p:PublishTrimmed=true"
}

# Add property to control asset packaging
if (-not $PackAssets) {
    # Keep current behavior - Assets excluded from single file
    $publishArgs += "/p:PackAssets=false"
} else {
    # Embed Assets in the executable
    $publishArgs += "/p:PackAssets=true"
}

Write-Host "Publishing..." -ForegroundColor Cyan
& dotnet $publishArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ Publish failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

# Show results
Write-Host ""
Write-Host "✅ Published successfully!" -ForegroundColor Green
Write-Host ""

$ExePath = Join-Path $OutputDir "TestApp.exe"
if (-not (Test-Path $ExePath)) {
    $ExePath = Join-Path $OutputDir "TestApp"
}

if (Test-Path $ExePath) {
    $FileSize = (Get-Item $ExePath).Length
    $FileSizeMB = [math]::Round($FileSize / 1MB, 2)
    Write-Host "Executable size:        $FileSizeMB MB" -ForegroundColor Cyan
}

$DirSize = (Get-ChildItem -Path $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum
$DirSizeMB = [math]::Round($DirSize / 1MB, 2)
Write-Host "Total directory size:   $DirSizeMB MB" -ForegroundColor Cyan

Write-Host ""
Write-Host "Contents:" -ForegroundColor Yellow
Get-ChildItem -Path $OutputDir -Recurse -File | Select-Object -First 10 | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  $($_.Name) ($size MB)"
}
if ((Get-ChildItem -Path $OutputDir -Recurse -File).Count -gt 10) {
    Write-Host "  ... and more" -ForegroundColor Gray
}

Write-Host ""
Write-Host "To run:" -ForegroundColor Yellow
Write-Host "  $ExePath" -ForegroundColor White
Write-Host ""

# Show deployment notes
Write-Host "Deployment notes:" -ForegroundColor Yellow
if ($PackAssets) {
    Write-Host "  [OK] Assets embedded (extracts to temp at runtime)" -ForegroundColor Green
} else {
    Write-Host "  [INFO] Assets folder must be deployed alongside executable" -ForegroundColor Cyan
}

if ($SelfContained) {
    Write-Host "  [OK] .NET runtime included (no installation required)" -ForegroundColor Green
} else {
    Write-Host "  [INFO] Requires .NET 8 runtime on target machine" -ForegroundColor Cyan
}

Write-Host ""
