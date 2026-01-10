# Fazor NuGet Package Example

This is a minimal example showing how to use Fazor NuGet packages in your own project.

## Setup

1. **Create a new console project:**
```bash
dotnet new console -n MyFazorApp
cd MyFazorApp
```

2. **Add Fazor packages from local source:**
```bash
# Add local package source (replace path with your Fazor repo path)
dotnet nuget add source /path/to/Fazor/nupkgs --name fazor-local

# Add packages
dotnet add package Fazor.UI --version 0.1.0 --source fazor-local
dotnet add package Fazor.Build --version 0.1.0 --source fazor-local
```

3. **Update your `.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Fazor.UI" Version="0.1.0" />
    <PackageReference Include="Fazor.Build" Version="0.1.0" />
  </ItemGroup>

</Project>
```

4. **Create `MainApp.razor`:**
```razor
@using Sandbox.UI
@inherits Panel

<root class="main-app">
    <div class="header">
        <h1>Hello from NuGet Fazor!</h1>
    </div>
    <div class="content">
        <button @onclick="HandleClick">
            Clicks: @count
        </button>
    </div>
</root>

<style>
.main-app {
    width: 100%;
    height: 100%;
    background-color: #1e1e1e;
    flex-direction: column;
    align-items: center;
    justify-content: center;
}

.header {
    padding: 20px;
    background-color: #007acc;
}

.header h1 {
    color: white;
    font-size: 32px;
}

.content {
    padding: 20px;
}

button {
    padding: 10px 20px;
    background-color: #007acc;
    color: white;
    border: none;
    border-radius: 4px;
    cursor: pointer;
    font-size: 16px;
}

button:hover {
    opacity: 0.8;
}
</style>

@code {
    private int count = 0;

    private void HandleClick()
    {
        count++;
        StateHasChanged();
    }
}
```

5. **Update `Program.cs`:**
```csharp
using Fazor.UI;

FazorApplication.RunPanel<MainApp>();
```

6. **Build and run:**
```bash
dotnet build
dotnet run
```

You should see a window with "Hello from NuGet Fazor!" and a clickable button!

## Notes

- The Fazor.Build package automatically handles Razor transpilation
- Native libraries (Yoga) are automatically included
- For XGUI themes, copy the Assets folder from the Fazor repository

## Troubleshooting

If Razor files don't compile:
1. Clean and rebuild: `dotnet clean && dotnet build`
2. Check that both Fazor.UI and Fazor.Build packages are installed
3. Ensure .razor files are in your project root or subdirectories

For more information, see [docs/NuGet-Usage.md](../../docs/NuGet-Usage.md)
