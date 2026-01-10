# PowerShell script to build Yoga native library for Windows from source
# Requires Visual Studio 2022 with C++ tools and CMake

Write-Host "Fazor Yoga Native Library Setup for Windows" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Building Yoga as a shared library (DLL)..." -ForegroundColor Yellow
Write-Host "Required: Visual Studio 2022 with C++ tools and CMake" -ForegroundColor Yellow
Write-Host ""

# Download Yoga source
Write-Host "Downloading Yoga v3.1.0 source..." -ForegroundColor Yellow
$yogaUrl = "https://github.com/facebook/yoga/archive/refs/tags/v3.1.0.tar.gz"
Invoke-WebRequest -Uri $yogaUrl -OutFile "yoga.tar.gz"

# Extract
Write-Host "Extracting..." -ForegroundColor Yellow
tar -xzf yoga.tar.gz

# Create custom CMakeLists.txt for building as shared library
Write-Host "Creating custom CMakeLists.txt for shared library build..." -ForegroundColor Yellow
$customCMake = @"
cmake_minimum_required(VERSION 3.15)

# Enable symbol export for Windows DLL
set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS ON)

project(yoga-sharp)

# Yoga requires C++20
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

set(YOGA_ROOT `${CMAKE_CURRENT_SOURCE_DIR})

# Collect all Yoga source files
file(GLOB_RECURSE SOURCES 
    `${YOGA_ROOT}/yoga/*.cpp
)

# Create shared library
add_library(yoga SHARED `${SOURCES})

# Include directories
target_include_directories(yoga
    PUBLIC
    `$<BUILD_INTERFACE:`${YOGA_ROOT}>
    `$<INSTALL_INTERFACE:`${CMAKE_INSTALL_PREFIX}/include/yoga>
)

# Remove "lib" prefix on Windows (yoga.dll instead of libyoga.dll)
set_target_properties(yoga PROPERTIES PREFIX "")

# Debug definitions
target_compile_definitions(yoga PRIVATE
    `$<`$<CONFIG:Debug>:DEBUG=1>
)
"@

Set-Content -Path "yoga-3.1.0\CMakeLists.txt" -Value $customCMake

# Build
Write-Host "Building with CMake..." -ForegroundColor Yellow
Set-Location yoga-3.1.0
New-Item -ItemType Directory -Force -Path "build" | Out-Null
Set-Location build

cmake .. -G "Visual Studio 18 2026" -A x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "CMake configuration failed. Make sure Visual Studio 2022 with C++ tools is installed." -ForegroundColor Red
    exit 1
}

cmake --build . --config Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}

# Copy DLL (check multiple possible locations)
Write-Host "Copying yoga.dll..." -ForegroundColor Yellow
$dllLocations = @(
    "Release\yoga.dll",
    "bin\Release\yoga.dll",
    "yoga\Release\yoga.dll",
    "lib\Release\yoga.dll"
)

$copied = $false
foreach ($location in $dllLocations) {
    if (Test-Path $location) {
        Write-Host "Found at $location" -ForegroundColor Cyan
        Copy-Item $location "..\..\yoga.dll" -Force
        $copied = $true
        break
    }
}

if (-not $copied) {
    Write-Host ""
    Write-Host "ERROR: Could not find yoga.dll after build" -ForegroundColor Red
    Write-Host ""
    Write-Host "Searching for any built files:" -ForegroundColor Yellow
    Get-ChildItem -Recurse -Filter "yoga.*" | Select-Object -First 10 | ForEach-Object { 
        Write-Host "  $($_.FullName)" -ForegroundColor Cyan 
    }
    Write-Host ""
    exit 1
}

# Cleanup
Set-Location ..\..
Remove-Item -Recurse -Force yoga-3.1.0 -ErrorAction SilentlyContinue
Remove-Item yoga.tar.gz -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Successfully built and installed yoga.dll!" -ForegroundColor Green
Write-Host "Location: $(Get-Location)\yoga.dll" -ForegroundColor Cyan
Write-Host ""
Write-Host "The native Yoga library is now ready for P/Invoke." -ForegroundColor Green
