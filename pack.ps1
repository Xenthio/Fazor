#!/usr/bin/env pwsh
# Pack Fazor NuGet packages

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "./nupkgs",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Fazor NuGet Package Builder" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Create output directory
if (!(Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
    Write-Host "Created output directory: $OutputDir" -ForegroundColor Green
}

# List of projects to pack
$projects = @(
    "src/Sandbox.Razor/Sandbox.Razor.csproj",
    "src/Sandbox.UI/Sandbox.UI.csproj",
    "src/Sandbox.UI.Skia/Sandbox.UI.Skia.csproj",
    "src/Sandbox.UI.AI/Sandbox.UI.AI.csproj",
    "src/Fazor.Build/Fazor.Build.csproj",
    "src/Fazor.UI/Fazor.UI.csproj"
)

Write-Host "Building and packing projects..." -ForegroundColor Yellow
Write-Host ""

$versionArg = ""
if ($Version) {
    $versionArg = "/p:Version=$Version"
    Write-Host "Using version: $Version" -ForegroundColor Cyan
    Write-Host ""
}

foreach ($project in $projects) {
    Write-Host "Packing: $project" -ForegroundColor Green
    
    if ($versionArg) {
        dotnet pack $project -c $Configuration -o $OutputDir $versionArg --no-restore
    } else {
        dotnet pack $project -c $Configuration -o $OutputDir --no-restore
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to pack $project" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Packaging complete!" -ForegroundColor Green
Write-Host "Packages are in: $OutputDir" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To install locally, run:" -ForegroundColor Yellow
Write-Host "  dotnet nuget add source $(Resolve-Path $OutputDir) --name fazor-local" -ForegroundColor White
Write-Host ""
