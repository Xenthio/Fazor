# MSBuild File Locking - Prevention and Workarounds

## The Problem

When building .NET projects, you might encounter file locking errors like:

```
Could not copy "obj\Release\net10.0\Avalazor.Build.dll" to "bin\Release\net10.0\Avalazor.Build.dll". 
Exceeded retry count of 10. Failed. 
The file is locked by: "MSBuild.exe (46260), MSBuild.exe (8000), MSBuild.exe (42604)"
```

This happens when:
1. **Multiple concurrent builds** of the same project try to copy files simultaneously
2. **MSBuild task assemblies** are loaded and then MSBuild tries to overwrite them
3. **Anti-virus or file indexing** services lock files during the build

## Solutions Implemented

### 1. Isolated Assembly Loading (Avalazor.Build.csproj)

```xml
<PropertyGroup>
  <!-- Generates .deps.json for isolated loading contexts -->
  <GenerateDependencyFile>true</GenerateDependencyFile>
  <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
</PropertyGroup>

<ItemGroup>
  <!-- Prevents transitive dependencies from being loaded -->
  <ProjectReference Include="..\Sandbox.Razor\Sandbox.Razor.csproj" PrivateAssets="all" />
</ItemGroup>
```

**How it works:**
- MSBuild 16.0+ uses isolated AssemblyLoadContext when a `.deps.json` file exists
- This prevents the DLL from being locked when MSBuild loads it for task execution
- `PrivateAssets="all"` ensures dependencies are properly isolated

### 2. Build-Only Project References

In consuming projects (like SimpleDesktopApp):

```xml
<ProjectReference Include="..\..\src\Avalazor.Build\Avalazor.Build.csproj" 
                  ReferenceOutputAssembly="false" 
                  OutputItemType="AvalazorBuildReference" />
```

**How it works:**
- `ReferenceOutputAssembly="false"` tells MSBuild not to reference the assembly in the consuming project
- Ensures build order while avoiding unnecessary assembly references

### 3. Native Library Inclusion (Avalazor.UI.csproj)

```xml
<ItemGroup>
  <None Include="Native/yoga.dll" 
        CopyToOutputDirectory="PreserveNewest" 
        CopyToPublishDirectory="PreserveNewest" />
  <None Include="Native/libyoga.so" 
        CopyToOutputDirectory="PreserveNewest" 
        CopyToPublishDirectory="PreserveNewest" />
  <None Include="Native/libyoga.dylib" 
        CopyToOutputDirectory="PreserveNewest" 
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

**Why this matters:**
- Explicit `<None Include>` ensures native libraries are copied to publish output
- Without this, single-file publish would fail to include platform-specific native libraries

## Best Practices

### ✅ Do's

1. **Build sequentially in CI/CD** - Avoid parallel solution builds
   ```bash
   dotnet build -c Release
   # Wait for completion before next build
   ```

2. **Use separate output directories** for different configurations
   ```xml
   <BaseOutputPath>bin\$(Configuration)\</BaseOutputPath>
   ```

3. **Clean between builds** if you suspect file locks
   ```bash
   dotnet clean && dotnet build
   ```

4. **Disable anti-virus scanning** of build output directories (if safe to do so)

5. **Use MSBuild binary logging** to diagnose issues
   ```bash
   dotnet build -bl:build.binlog
   # Analyze with: https://msbuildlog.com/
   ```

### ❌ Don'ts

1. **Don't run concurrent builds** of the same solution/project
   - Running `dotnet build & dotnet build &` will cause file locks
   - Let one build complete before starting another

2. **Don't manually delete bin/obj during build**
   - Use `dotnet clean` instead
   - Manual deletion can race with MSBuild

3. **Don't share build output directories** across different projects
   - Keep each project's output separate

## Troubleshooting

### Still getting file lock errors?

1. **Check for hanging MSBuild processes:**
   ```bash
   # Windows
   tasklist | findstr MSBuild
   taskkill /IM MSBuild.exe /F
   
   # Linux/macOS
   ps aux | grep -i msbuild
   pkill -9 -f msbuild
   ```

2. **Check for anti-virus/indexing services:**
   - Windows Defender, Dropbox, OneDrive can lock files
   - Add build output directories to exclusion lists

3. **Use Process Monitor (Windows):**
   - Download: https://learn.microsoft.com/sysinternals/downloads/procmon
   - Filter for file operations on the locked file
   - Identify which process is holding the lock

4. **Check file permissions:**
   ```bash
   # Linux/macOS
   ls -la bin/Release/net10.0/
   # Ensure your user owns the files
   ```

5. **Try building with MSBuild directly:**
   ```bash
   # Sometimes `msbuild` behaves differently than `dotnet build`
   msbuild Avalazor.sln -t:Rebuild -p:Configuration=Release
   ```

## CI/CD Configuration

### GitHub Actions

```yaml
- name: Build
  run: dotnet build -c Release
  # Avoid parallel builds in the same job

- name: Publish
  run: dotnet publish examples/SimpleDesktopApp/SimpleDesktopApp.csproj -c Release -o dist/
  # Build artifacts from previous step are reused
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'build'
    projects: '**/*.csproj'
    arguments: '-c Release'
  # Set MaxCpuCount=1 to avoid parallel project builds
```

## References

- [MSBuild Task Isolation](https://github.com/dotnet/msbuild/blob/main/documentation/specs/task-isolation.md)
- [.NET SDK Project Files](https://learn.microsoft.com/dotnet/core/project-sdk/overview)
- [AssemblyLoadContext](https://learn.microsoft.com/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
