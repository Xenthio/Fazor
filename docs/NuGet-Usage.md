# Using Fazor as a NuGet Library

This guide explains how to use Fazor packages in your own projects (like RTXLauncher).

## 📦 Available Packages

Fazor is distributed as multiple NuGet packages:

| Package | Description | Size |
|---------|-------------|------|
| **Fazor.UI** | Main runtime with window management, input, and rendering | 271KB |
| **Fazor.Sandbox.UI** | Core UI framework (Panel system, styling, layout) | 190KB |
| **Fazor.Sandbox.UI.Skia** | SkiaSharp rendering backend | 21KB |
| **Fazor.Razor** | Razor file transpilation engine | 348KB |
| **Fazor.Build** | MSBuild integration for automatic Razor/SCSS compilation | 13KB |
| **Fazor.Sandbox.UI.AI** *(optional)* | AI debugging renderer for headless mode | 16KB |

## 🚀 Quick Start

### 1. Create or Open Your Project

```bash
# Create a new console app (or use existing project like RTXLauncher)
dotnet new console -n MyFazorApp
cd MyFazorApp
```

### 2. Add Fazor Packages

For most projects, you need these packages:

```bash
# Core packages (required)
dotnet add package Fazor.UI
dotnet add package Fazor.Build

# Optional: AI debugging support
dotnet add package Fazor.Sandbox.UI.AI
```

**Note:** `Fazor.UI` automatically includes `Fazor.Sandbox.UI` and `Fazor.Sandbox.UI.Skia` as dependencies.

### 3. Configure Your Project File

Add the Fazor build targets to your `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Disable console window on Windows -->
  <PropertyGroup Condition="'$(OS)' == 'Windows_NT'">
    <OutputType>WinExe</OutputType>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core Fazor packages -->
    <PackageReference Include="Fazor.UI" Version="0.1.0" />
    
    <!-- Build-time integration (required for Razor transpilation) -->
    <PackageReference Include="Fazor.Build" Version="0.1.0" />
  </ItemGroup>

  <!-- Import Fazor build targets for Razor and SCSS compilation -->
  <Import Project="$(MSBuildThisFileDirectory)..\..\..\.nuget\packages\fazor.build\0.1.0\build\Fazor.Build.targets" />
  
  <!-- Or use NuGet's automatic import (if packages are properly configured) -->
  
</Project>
```

### 4. Create Your First Component

Create `MainApp.razor`:

```razor
@using Fazor.UI
@using Sandbox.UI
@inherits Panel

<root class="main-app">
    <div class="header">
        <h1>Hello Fazor!</h1>
    </div>
    <div class="content">
        <button @onclick="HandleClick">
            Clicks: @count
        </button>
    </div>
</root>

@code {
    private int count = 0;

    private void HandleClick()
    {
        count++;
        StateHasChanged();
    }
}
```

### 5. Set Up Program.cs

```csharp
using Fazor.UI;

// Run your Fazor app
FazorApplication.RunPanel<MainApp>();
```

### 6. Build and Run

```bash
dotnet build
dotnet run
```

## 📝 Advanced Configuration

### Assets and Themes

Fazor includes themes and assets. To use them:

1. Copy the `Assets` folder from the Fazor repository to your project
2. Configure content files in your `.csproj`:

```xml
<ItemGroup>
  <Content Include="Assets\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### SCSS Styling

Create a `.scss` file next to your Razor component:

**MainApp.scss:**
```scss
$primary-color: #007acc;

.main-app {
    width: 100%;
    height: 100%;
    background-color: #1e1e1e;
    
    .header {
        padding: 20px;
        background-color: $primary-color;
        
        h1 {
            color: white;
            font-size: 24px;
        }
    }
    
    button {
        padding: 10px 20px;
        background-color: $primary-color;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        
        &:hover {
            opacity: 0.8;
        }
    }
}
```

Apply the stylesheet to your component:

```razor
@attribute [StyleSheet("MainApp.scss")]
```

### Using XGUI Themes

If you copied the Assets folder, you can use XGUI themes:

```razor
@attribute [StyleSheet("/Assets/themes/Fazor.Defaults.scss")]
@attribute [StyleSheet("/Assets/themes/XGUI/DefaultStyles/OliveGreen.scss")]
```

### Publishing Your App

```bash
# Self-contained build
dotnet publish -c Release --self-contained true -r win-x64

# Framework-dependent build (smaller, requires .NET runtime)
dotnet publish -c Release --self-contained false
```

## 🛠️ Building Fazor Packages Locally

If you want to build the NuGet packages yourself:

```bash
# Clone and build Fazor
git clone https://github.com/Xenthio/Fazor.git
cd Fazor
git submodule update --init --recursive

# Build and pack
dotnet restore
dotnet build -c Release

# Create packages (Windows)
./pack.ps1 Release ./nupkgs 0.1.0

# Create packages (Linux/macOS)
./pack.sh Release ./nupkgs 0.1.0
```

### Add Local Package Source

```bash
# Add local NuGet source
dotnet nuget add source /path/to/Fazor/nupkgs --name fazor-local

# Install from local source
dotnet add package Fazor.UI --version 0.1.0 --source fazor-local
```

## 📚 Examples

### Creating a Window

```razor
@using Fazor.UI
@using Sandbox.UI
@using Sandbox.UI.Controls
@inherits Window

<root title="My Window" hasminimise="true" hasmaximise="true" hasclose="true" 
      windowwidth="800" windowheight="600">
    <div class="window-content">
        <h1>Window Content</h1>
        <p>This is a Fazor window!</p>
    </div>
</root>
```

### Adding UI Controls

```razor
@using Sandbox.UI.Controls

<root>
    <Label Text="Enter your name:" />
    <TextEntry @bind-Value="name" />
    
    <Button Text="Submit" @onclick="OnSubmit" />
    
    <Label Text="@greeting" />
</root>

@code {
    private string name = "";
    private string greeting = "";
    
    void OnSubmit() {
        greeting = $"Hello, {name}!";
        StateHasChanged();
    }
}
```

## 🔧 Troubleshooting

### Razor Files Not Compiling

Make sure:
1. `Fazor.Build` package is referenced
2. Build targets are imported in your `.csproj`
3. Razor files have `.razor` extension
4. Clean and rebuild: `dotnet clean && dotnet build`

### Native Library Not Found

The Yoga layout library should be automatically included. If you get errors:
1. Check that `Fazor.UI` package is properly restored
2. Ensure native libraries are copied to output:
   - `yoga.dll` (Windows)
   - `libyoga.so` (Linux)
   - `libyoga.dylib` (macOS)

### Themes Not Loading

1. Ensure Assets folder is copied to output directory
2. Check file paths are correct (use forward slashes `/`)
3. Use absolute paths starting with `/Assets/`

## 📖 Documentation

- [Main README](../README.md) - Full Fazor documentation
- [XGUI Themes](../Assets/themes/XGUI/README.md) - Theme system documentation
- [s&box UI Reference](https://docs.facepunch.com/s/sbox-dev/doc/ui-C5Np9rxPJm) - API compatibility reference

## 💬 Support

- **GitHub Issues**: https://github.com/Xenthio/Fazor/issues
- **Discussions**: https://github.com/Xenthio/Fazor/discussions

## 📄 License

MIT License - See [LICENSE](../LICENSE) for details
