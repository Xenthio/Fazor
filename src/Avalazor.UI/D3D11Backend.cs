#if INCLUDE_D3D11_BACKEND
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SkiaSharp;
using Sandbox.UI;
using Sandbox.UI.Skia;
using System.Runtime.InteropServices;
using D3D11Box = Silk.NET.Direct3D11.Box;

namespace Avalazor.UI;

// DWM APIs for transparent windows
internal static partial class DwmApi
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmExtendFrameIntoClientArea(nint hwnd, ref MARGINS margins);
    
    [LibraryImport("dwmapi.dll")]
    public static partial int DwmIsCompositionEnabled(out int enabled);
}

/// <summary>
/// DirectX 11 graphics backend for Avalazor.
/// Provides hardware-accelerated rendering using Direct3D 11 and SkiaSharp.
/// Note: This backend is Windows-only and requires proper D3D11 setup.
/// </summary>
/// <remarks>
/// Direct3D support in SkiaSharp is limited. This implementation provides a working
/// D3D11 swapchain with a software-rendered SkiaSharp surface that is then blitted
/// to the D3D11 back buffer. For full hardware acceleration, use the OpenGL or Vulkan backends.
/// 
/// Resize Handling: This backend uses DXGI Scaling.None and dimension validation to prevent
/// stretched rendering during window resize. Frames with mismatched dimensions are skipped
/// to maintain visual quality. This approach works better than OpenGL on Windows due to
/// D3D11's more robust handling of swapchain resize operations.
/// </remarks>
public class D3D11Backend : IGraphicsBackend
{
    private D3D11? _d3d11;
    private DXGI? _dxgi;
    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _context;
    private ComPtr<IDXGISwapChain1> _swapChain;
    
    // Use raw pointers for resources that need to be released during resize
    // ComPtr doesn't reliably release references which causes DXGI_ERROR_INVALID_CALL
    private unsafe ID3D11RenderTargetView* _renderTargetView;
    private unsafe ID3D11Texture2D* _backBuffer;
    private unsafe ID3D11Texture2D* _stagingTexture;
    
    private GRContext? _grContext;
    private SKSurface? _surface;
    private SkiaPanelRenderer? _renderer;
    private IWindow? _window;
    private int _width;
    private int _height;
    
    // Reusable pixel buffer to avoid allocations per frame
    private byte[]? _pixelBuffer;

    public unsafe void Initialize(IWindow window)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DirectX11 backend is only available on Windows");
        }
        
        _window = window;
        _width = window.FramebufferSize.X;
        _height = window.FramebufferSize.Y;
        
        Console.WriteLine("[D3D11Backend] Initializing DirectX 11 backend...");
        
        // Create D3D11 device and context
        CreateDeviceAndSwapChain(window);
        Console.WriteLine("[D3D11Backend] Device and swap chain created");

        // Create Skia GRContext - use CPU rendering and blit to D3D11
        // Note: Full D3D11 GPU support requires custom SkiaSharp builds
        CreateGRContext();
        Console.WriteLine("[D3D11Backend] SkiaSharp context created (CPU with D3D11 blit)");

        _renderer = new SkiaPanelRenderer();

        CreateSurface(window.FramebufferSize);
        Console.WriteLine("[D3D11Backend] DirectX 11 backend initialized successfully!");
    }

    private unsafe void CreateDeviceAndSwapChain(IWindow window)
    {
        _d3d11 = D3D11.GetApi(window);
        _dxgi = DXGI.GetApi(window);
        
        // Create D3D11 device with appropriate feature level
        D3DFeatureLevel[] featureLevels = new[]
        {
            D3DFeatureLevel.Level111,
            D3DFeatureLevel.Level110,
            D3DFeatureLevel.Level101,
            D3DFeatureLevel.Level100
        };
        
        D3DFeatureLevel actualFeatureLevel;
        ID3D11Device* devicePtr;
        ID3D11DeviceContext* contextPtr;
        
        fixed (D3DFeatureLevel* featureLevelsPtr = featureLevels)
        {
            var result = _d3d11.CreateDevice(
                null, // Use default adapter
                D3DDriverType.Hardware,
                default,
                (uint)CreateDeviceFlag.BgraSupport, // Required for D2D interop
                featureLevelsPtr,
                (uint)featureLevels.Length,
                D3D11.SdkVersion,
                &devicePtr,
                &actualFeatureLevel,
                &contextPtr
            );
            
            if (result < 0) // FAILED
            {
                // Try WARP (software) driver
                Console.WriteLine("[D3D11Backend] Hardware device creation failed, trying WARP...");
                result = _d3d11.CreateDevice(
                    null,
                    D3DDriverType.Warp,
                    default,
                    (uint)CreateDeviceFlag.BgraSupport,
                    featureLevelsPtr,
                    (uint)featureLevels.Length,
                    D3D11.SdkVersion,
                    &devicePtr,
                    &actualFeatureLevel,
                    &contextPtr
                );
                
                if (result < 0)
                {
                    throw new Exception($"Failed to create D3D11 device: HRESULT 0x{result:X8}");
                }
            }
        }
        
        _device = new ComPtr<ID3D11Device>(devicePtr);
        _context = new ComPtr<ID3D11DeviceContext>(contextPtr);
        
        Console.WriteLine($"[D3D11Backend] Using feature level: {actualFeatureLevel}");
        
        // Get DXGI factory from device
        IDXGIDevice* dxgiDevice;
        var dxgiDeviceGuid = typeof(IDXGIDevice).GUID;
        _device.Handle->QueryInterface(&dxgiDeviceGuid, (void**)&dxgiDevice);
        
        IDXGIAdapter* adapter;
        dxgiDevice->GetAdapter(&adapter);
        
        IDXGIFactory2* factory;
        adapter->GetParent(SilkMarshal.GuidPtrOf<IDXGIFactory2>(), (void**)&factory);
        
        // Get native window handle
        var nativeWindow = window.Native!.Win32;
        if (!nativeWindow.HasValue)
        {
            throw new Exception("Could not get Win32 window handle");
        }
        var hwnd = (nint)nativeWindow.Value.Hwnd;
        
        // Enable DWM composition for transparent windows
        // Note: Even with DWM enabled, CreateSwapChainForHwnd does NOT support AlphaMode.Premultiplied
        // Premultiplied alpha requires CreateSwapChainForComposition or CreateSwapChainForCoreWindow
        // We still enable DWM composition as it may help with some transparency scenarios
        if (DwmApi.DwmIsCompositionEnabled(out int compositionEnabled) >= 0 && compositionEnabled != 0)
        {
            // Extend DWM frame into entire client area for transparency
            var margins = new DwmApi.MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            if (DwmApi.DwmExtendFrameIntoClientArea(hwnd, ref margins) >= 0)
            {
                Console.WriteLine("[D3D11Backend] DWM composition enabled");
            }
        }
        
        // Create swap chain
        // Note: AlphaMode.Premultiplied is NOT supported with CreateSwapChainForHwnd
        // Using Unspecified which is the only reliable option for HWND-based windows
        var swapChainDesc = new SwapChainDesc1
        {
            Width = (uint)_width,
            Height = (uint)_height,
            Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
            Stereo = false,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            BufferUsage = DXGI.UsageRenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.None, // Don't stretch - maintain 1:1 pixel mapping
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Unspecified, // Premultiplied requires CreateSwapChainForComposition
            Flags = 0
        };
        
        IDXGISwapChain1* swapChainPtr;
        var hr = factory->CreateSwapChainForHwnd(
            (IUnknown*)_device.Handle,
            hwnd,
            &swapChainDesc,
            null,
            null,
            &swapChainPtr
        );
        
        if (hr < 0)
        {
            throw new Exception($"Failed to create swap chain: HRESULT 0x{hr:X8}");
        }
        
        _swapChain = new ComPtr<IDXGISwapChain1>(swapChainPtr);
        
        // Clean up DXGI objects
        factory->Release();
        adapter->Release();
        dxgiDevice->Release();
        
        // Create render target view
        CreateRenderTargetView();
        
        // Create staging texture for CPU-GPU transfer
        CreateStagingTexture();
    }

    private unsafe void CreateRenderTargetView()
    {
        // Get back buffer - this adds a reference to the back buffer
        ID3D11Texture2D* backBufferPtr;
        _swapChain.GetBuffer(0, SilkMarshal.GuidPtrOf<ID3D11Texture2D>(), (void**)&backBufferPtr);
        _backBuffer = backBufferPtr;
        
        // Create render target view
        // Using null for render target view description to use defaults based on back buffer format.
        // This is the standard approach when the back buffer is already in the desired format (BGRA8).
        ID3D11RenderTargetView* rtvPtr;
        RenderTargetViewDesc* rtvDescPtr = null;
        var hr = _device.Handle->CreateRenderTargetView((ID3D11Resource*)_backBuffer, rtvDescPtr, &rtvPtr);
        if (hr < 0)
        {
            throw new Exception($"Failed to create render target view: HRESULT 0x{hr:X8}");
        }
        _renderTargetView = rtvPtr;
        
        // Set render target
        ID3D11DepthStencilView* dsv = null;
        _context.Handle->OMSetRenderTargets(1, &rtvPtr, dsv);
        
        // Set viewport
        var viewport = new Viewport
        {
            TopLeftX = 0,
            TopLeftY = 0,
            Width = _width,
            Height = _height,
            MinDepth = 0,
            MaxDepth = 1
        };
        _context.RSSetViewports(1, &viewport);
    }

    private unsafe void CreateStagingTexture()
    {
        var textureDesc = new Texture2DDesc
        {
            Width = (uint)_width,
            Height = (uint)_height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            Usage = Usage.Default,
            BindFlags = (uint)BindFlag.ShaderResource,
            CPUAccessFlags = 0,
            MiscFlags = 0
        };
        
        // Create texture without initial data - we'll upload pixel data each frame via UpdateSubresource
        ID3D11Texture2D* stagingPtr;
        SubresourceData* subresData = null;
        var hr = _device.Handle->CreateTexture2D(&textureDesc, subresData, &stagingPtr);
        if (hr < 0)
        {
            throw new Exception($"Failed to create staging texture: HRESULT 0x{hr:X8}");
        }
        _stagingTexture = stagingPtr;
    }

    private void CreateGRContext()
    {
        // Note: SkiaSharp's Direct3D support is limited in standard builds.
        // We use a software GRContext and blit to D3D11 for display.
        // For full hardware acceleration, use OpenGL or Vulkan backends.
        
        // For now, we'll use raster rendering and blit to D3D11
        // In the future, if SkiaSharp adds full D3D11 support, we can use:
        // _grContext = GRContext.CreateDirect3D(new GRD3DBackendContext { ... });
        
        // Using null GRContext means we'll use software rendering
        _grContext = null;
    }

    public unsafe void Resize(Vector2D<int> size)
    {
        if (_device.Handle == null || size.X <= 0 || size.Y <= 0) return;

        //Console.WriteLine($"[D3D11Backend] Resize called: {size.X}x{size.Y}, current: {_width}x{_height}");

        // Don't resize if dimensions haven't changed
        if (size.X == _width && size.Y == _height)
        {
            //Console.WriteLine($"[D3D11Backend] Skipping resize - dimensions unchanged");
            return;
        }

        _width = size.X;
        _height = size.Y;
        
        // Dispose old surface first - this must happen before we resize the swap chain
        _surface?.Dispose();
        _surface = null;
        
        // IMPORTANT: Before resizing the swap chain, we must release ALL references
        // to the back buffer. DXGI_ERROR_INVALID_CALL (0x887A0001) occurs
        // if there are ANY outstanding references to the back buffer.
        
        // Step 1: Clear all device context state - this unbinds all views, shaders, etc.
        _context.Handle->ClearState();
        
        // Step 2: Flush any pending commands to ensure GPU has finished with resources
        _context.Handle->Flush();
        
        // Step 3: Release the render target view (holds reference to back buffer)
        if (_renderTargetView != null)
        {
            _renderTargetView->Release();
            _renderTargetView = null;
        }
        
        // Step 4: Release the back buffer texture
        if (_backBuffer != null)
        {
            _backBuffer->Release();
            _backBuffer = null;
        }
        
        // Step 5: Release the staging texture
        if (_stagingTexture != null)
        {
            _stagingTexture->Release();
            _stagingTexture = null;
        }
        
        // Step 6: Now resize the swap chain buffers
        var hr = _swapChain.ResizeBuffers(2, (uint)_width, (uint)_height, 
            Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm, 0);
        if (hr < 0)
        {
            throw new Exception($"Failed to resize swap chain: HRESULT 0x{hr:X8}");
        }
        
        // Recreate render target view and staging texture
        CreateRenderTargetView();
        CreateStagingTexture();
        
        // Create new surface with correct dimensions
        CreateSurface(new Vector2D<int>(_width, _height));
        
        Console.WriteLine($"[D3D11Backend] Resize complete: {_width}x{_height}");
    }

    public unsafe void Render(RootPanel panel)
    {
        if (_surface == null || _renderer == null || _renderTargetView == null) return;

        // Verify surface dimensions match backend dimensions
        var surfaceWidth = _surface.Canvas.DeviceClipBounds.Width;
        var surfaceHeight = _surface.Canvas.DeviceClipBounds.Height;
        
        if (surfaceWidth != _width || surfaceHeight != _height)
        {
            Console.WriteLine($"[D3D11Backend] Dimension mismatch! Surface: {surfaceWidth}x{surfaceHeight}, Backend: {_width}x{_height}");
            // Don't render with mismatched dimensions - this causes stretching
            return;
        }

        // Clear the back buffer with transparent color to support window transparency
        float* clearColor = stackalloc float[] { 0.0f, 0.0f, 0.0f, 0.0f }; // Transparent
        _context.ClearRenderTargetView(_renderTargetView, clearColor);

        // Render UI to Skia surface - use transparent clear for window transparency support
        _surface.Canvas.Clear(SKColors.Transparent);
        _renderer.Render(_surface.Canvas, panel);
        _surface.Canvas.Flush();

        // Copy Skia bitmap data to D3D11 texture
        CopyToBackBuffer();

        // Present
        var hr = _swapChain.Present(1, 0); // VSync enabled
        if (hr < 0)
        {
            Console.WriteLine($"[D3D11Backend] Present failed: HRESULT 0x{hr:X8}");
        }
    }

    private unsafe void CopyToBackBuffer()
    {
        if (_surface == null || _stagingTexture == null || _backBuffer == null) return;
        
        // Reuse pixel buffer to avoid allocations per frame
        int requiredSize = _width * _height * 4;
        if (_pixelBuffer == null || _pixelBuffer.Length < requiredSize)
        {
            _pixelBuffer = new byte[requiredSize];
        }
        
        // Explicitly request sRGB color space for the destination to ensure conversion from Linear sRGB
        // using var srgb = SKColorSpace.CreateSrgb();
        // var info = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul, srgb);
        var info = new SKImageInfo(_width, _height, SKColorType.Bgra8888, SKAlphaType.Premul);
        
        fixed (byte* pixelsPtr = _pixelBuffer)
        {
            _surface.ReadPixels(info, (IntPtr)pixelsPtr, _width * 4, 0, 0);
            
            // Update the staging texture
            var box = new D3D11Box
            {
                Left = 0,
                Top = 0,
                Front = 0,
                Right = (uint)_width,
                Bottom = (uint)_height,
                Back = 1
            };
            
            _context.UpdateSubresource(
                (ID3D11Resource*)_stagingTexture,
                0,
                &box,
                pixelsPtr,
                (uint)(_width * 4),
                (uint)(_width * _height * 4)
            );
        }
        
        // Copy staging texture to back buffer
        _context.CopyResource(
            (ID3D11Resource*)_backBuffer,
            (ID3D11Resource*)_stagingTexture
        );
    }

    private void CreateSurface(Vector2D<int> size)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        
        // Create a raster surface for software rendering with ClearType-style subpixel rendering
        // This will be blitted to D3D11 for display
        
        // Get DirectWrite rendering parameters for proper text rendering on Windows
        float gamma = 2.2f;
        float contrast = 1.0f;
        float clearTypeLevel = 1.0f;
        int pixelGeometry = 1; // RGB
        int renderingMode = 0;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                Avalazor.UI.Native.DirectWriteHelper.GetRenderingParams(out gamma, out contrast, out clearTypeLevel, out pixelGeometry, out renderingMode);
                Console.WriteLine($"[D3D11Backend] DirectWrite Params: Gamma={gamma}, Contrast={contrast}, ClearType={clearTypeLevel}, Geom={pixelGeometry}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[D3D11Backend] Failed to get DirectWrite params: {ex.Message}");
            }
        }

        // Use sRGB Linear color space for correct blending and gamma correction
        // This matches Chromium's behavior for text rendering
        // var colorSpace = SKColorSpace.CreateSrgbLinear();
        // var imageInfo = new SKImageInfo(size.X, size.Y, SKColorType.Bgra8888, SKAlphaType.Premul, colorSpace);
        var imageInfo = new SKImageInfo(size.X, size.Y, SKColorType.Bgra8888, SKAlphaType.Premul);
        
        // Configure surface properties for RGB subpixel rendering (ClearType on Windows)
        // This makes text appear sharper and more like native Windows text rendering
        // RgbHorizontal is the most common pixel layout on modern LCD displays
        // Note: SkiaSharp currently doesn't expose the constructor to set gamma/contrast directly in SKSurfaceProperties
        var surfProps = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);
        
        _surface = SKSurface.Create(imageInfo, surfProps);
        
        if (_surface == null)
        {
            throw new Exception("Failed to create Skia raster surface");
        }
    }

    public unsafe void Dispose()
    {
        _surface?.Dispose();
        _grContext?.Dispose();
        
        // Use explicit Release calls to ensure COM references are properly decremented
        if (_stagingTexture != null)
        {
            _stagingTexture->Release();
            _stagingTexture = null;
        }
        if (_renderTargetView != null)
        {
            _renderTargetView->Release();
            _renderTargetView = null;
        }
        if (_backBuffer != null)
        {
            _backBuffer->Release();
            _backBuffer = null;
        }
        if (_swapChain.Handle != null)
        {
            _swapChain.Handle->Release();
            _swapChain = default;
        }
        if (_context.Handle != null)
        {
            _context.Handle->Release();
            _context = default;
        }
        if (_device.Handle != null)
        {
            _device.Handle->Release();
            _device = default;
        }
        _dxgi?.Dispose();
        _d3d11?.Dispose();
    }
}
#endif
