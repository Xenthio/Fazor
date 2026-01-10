# Graphics Backend Selection

Fazor supports multiple graphics backends for rendering the UI:

## Available Backends

### OpenGL (Default, Fully Functional)
- **Status**: ✅ Fully implemented and tested
- **Platform Support**: Linux, macOS, Windows
- **Use Case**: Default cross-platform rendering
- **Stability**: Production-ready

The OpenGL backend uses OpenGL 3.3 Core Profile and integrates with SkiaSharp's OpenGL backend. This is the most mature and well-tested backend.

### Vulkan (Functional)
- **Status**: ✅ Implemented with SkiaSharp GPU support
- **Platform Support**: Linux, Windows (with Vulkan drivers)
- **Use Case**: Modern low-level graphics API with better multi-threading support
- **Stability**: Functional - requires Vulkan-capable hardware and drivers

The Vulkan backend provides:
- Full Vulkan instance and device creation
- Window surface and swapchain management
- Integration with SkiaSharp's Vulkan backend via `GRContext.CreateVulkan`
- Proper synchronization with semaphores and fences
- Automatic swapchain recreation on window resize

**Requirements**:
- Vulkan-capable GPU with appropriate drivers
- Vulkan SDK installed (for development)

### DirectX 11 (Functional, Windows Only)
- **Status**: ✅ Implemented with software rendering + D3D11 blit
- **Platform Support**: Windows only
- **Use Case**: Native Windows DirectX rendering
- **Stability**: Functional - uses CPU rendering with D3D11 presentation

The DirectX 11 backend provides:
- D3D11 device and swap chain creation
- Hardware or WARP (software) driver support
- Software-rendered SkiaSharp surface blitted to D3D11 back buffer
- Proper resize and present operations

**Note**: Full GPU-accelerated SkiaSharp rendering via D3D11 requires custom SkiaSharp builds with `SK_Direct3D` enabled. The current implementation uses CPU rendering with efficient D3D11 presentation.

## Selecting a Backend

### In Code

```csharp
using Fazor.UI;

// OpenGL (default)
var window = new NativeWindow(
    width: 1280, 
    height: 720, 
    title: "My App"
);

// Explicitly specify OpenGL
var windowGL = new NativeWindow(
    width: 1280, 
    height: 720, 
    title: "My App",
    backendType: GraphicsBackendType.OpenGL
);

// Vulkan (requires Vulkan-capable hardware)
var windowVk = new NativeWindow(
    width: 1280, 
    height: 720, 
    title: "My App",
    backendType: GraphicsBackendType.Vulkan
);

// DirectX 11 (Windows only)
var windowD3D = new NativeWindow(
    width: 1280, 
    height: 720, 
    title: "My App",
    backendType: GraphicsBackendType.DirectX11
);
```

### Platform Restrictions

The DirectX11 backend can only be used on Windows. Attempting to use it on other platforms will throw a `PlatformNotSupportedException`.

The Vulkan backend requires Vulkan-capable hardware and drivers. On systems without Vulkan support, the backend will throw an exception during initialization.

## Implementation Status

| Backend | Device Init | Context Creation | Surface Creation | Rendering | Status |
|---------|-------------|------------------|------------------|-----------|--------|
| OpenGL | ✅ | ✅ | ✅ | ✅ | Production |
| Vulkan | ✅ | ✅ | ✅ | ✅ | Functional |
| DirectX 11 | ✅ | ✅ | ✅ | ✅ | Functional |

## Performance Characteristics

### OpenGL
- Best overall performance and compatibility
- GPU-accelerated rendering via SkiaSharp OpenGL backend
- Recommended for most use cases

### Vulkan
- Modern API with potential for better multi-threading
- GPU-accelerated rendering via SkiaSharp Vulkan backend
- May offer better performance for complex UIs

### DirectX 11
- Currently uses CPU rendering with D3D11 presentation
- Suitable for Windows-specific applications
- Full GPU acceleration requires custom SkiaSharp build

## Troubleshooting

### Vulkan Backend Issues
1. **"No Vulkan-capable devices found"**: Ensure your GPU has Vulkan drivers installed
2. **"Failed to create Vulkan instance"**: Check that required extensions are available
3. **Black screen**: Verify swapchain format is supported by your hardware

### DirectX 11 Backend Issues
1. **"DirectX11 backend is only available on Windows"**: This backend is Windows-only
2. **Hardware device creation failed**: Will automatically fallback to WARP (software) driver
3. **Performance issues**: Consider using OpenGL for better GPU acceleration

## References

- [SkiaSharp Documentation](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [Vulkan Tutorial](https://vulkan-tutorial.com/)
- [DirectX 11 Programming Guide](https://docs.microsoft.com/en-us/windows/win32/direct3d11/dx-graphics-overviews)
- [Silk.NET Documentation](https://github.com/dotnet/Silk.NET)
