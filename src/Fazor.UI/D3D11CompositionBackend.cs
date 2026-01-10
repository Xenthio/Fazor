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

namespace Fazor.UI;

/// <summary>
/// DirectComposition interop APIs for transparent window support.
/// Uses raw COM vtable calls for reliable interop with DirectComposition.
/// </summary>
internal static partial class DCompApi
{
    /// <summary>
    /// Creates a DirectComposition device from a DXGI device.
    /// </summary>
    [LibraryImport("dcomp.dll")]
    public static partial int DCompositionCreateDevice(
        nint dxgiDevice,
        ref Guid iid,
        out nint dcompositionDevice);
    
    // IDCompositionDevice interface IID
    public static readonly Guid IID_IDCompositionDevice = new("C37EA93A-E7AA-450D-B16F-9746CB0407F3");
}

/// <summary>
/// Raw COM vtable wrapper for IDCompositionDevice.
/// Using raw vtable calls instead of managed COM interop for reliability.
/// </summary>
internal unsafe struct DCompDevice : IDisposable
{
    private nint _ptr;
    
    public DCompDevice(nint ptr) => _ptr = ptr;
    public bool IsValid => _ptr != 0;
    public nint Ptr => _ptr;
    
    // VTable layout for IDCompositionDevice:
    // 0: QueryInterface
    // 1: AddRef
    // 2: Release
    // 3: Commit
    // 4: WaitForCommitCompletion
    // 5: GetFrameStatistics
    // 6: CreateTargetForHwnd
    // 7: CreateVisual
    
    private nint* VTable => *(nint**)_ptr;
    
    public int Commit()
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, int>)VTable[3];
        return fn(_ptr);
    }
    
    public int CreateTargetForHwnd(nint hwnd, bool topmost, out DCompTarget target)
    {
        nint targetPtr = 0;
        var fn = (delegate* unmanaged[Stdcall]<nint, nint, int, nint*, int>)VTable[6];
        var hr = fn(_ptr, hwnd, topmost ? 1 : 0, &targetPtr);
        target = new DCompTarget(targetPtr);
        return hr;
    }
    
    public int CreateVisual(out DCompVisual visual)
    {
        nint visualPtr = 0;
        var fn = (delegate* unmanaged[Stdcall]<nint, nint*, int>)VTable[7];
        var hr = fn(_ptr, &visualPtr);
        visual = new DCompVisual(visualPtr);
        return hr;
    }
    
    public void Dispose()
    {
        if (_ptr != 0)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint>)VTable[2]; // Release
            fn(_ptr);
            _ptr = 0;
        }
    }
}

/// <summary>
/// Raw COM vtable wrapper for IDCompositionTarget.
/// </summary>
internal unsafe struct DCompTarget : IDisposable
{
    private nint _ptr;
    
    public DCompTarget(nint ptr) => _ptr = ptr;
    public bool IsValid => _ptr != 0;
    
    // VTable layout for IDCompositionTarget:
    // 0: QueryInterface
    // 1: AddRef
    // 2: Release
    // 3: SetRoot
    
    private nint* VTable => *(nint**)_ptr;
    
    public int SetRoot(DCompVisual visual)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, nint, int>)VTable[3];
        return fn(_ptr, visual.Ptr);
    }
    
    public void Dispose()
    {
        if (_ptr != 0)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint>)VTable[2]; // Release
            fn(_ptr);
            _ptr = 0;
        }
    }
}

/// <summary>
/// Raw COM vtable wrapper for IDCompositionVisual.
/// </summary>
internal unsafe struct DCompVisual : IDisposable
{
    private nint _ptr;
    
    public DCompVisual(nint ptr) => _ptr = ptr;
    public bool IsValid => _ptr != 0;
    public nint Ptr => _ptr;
    
    // VTable layout for IDCompositionVisual:
    // 0: QueryInterface
    // 1: AddRef
    // 2: Release
    // 3: SetOffsetX (IDCompositionTransform)
    // 4: SetOffsetX (float)
    // 5: SetOffsetY (IDCompositionTransform)
    // 6: SetOffsetY (float)
    // 7: SetTransform (IDCompositionTransform)
    // 8: SetTransform (D2D_MATRIX_3X2_F)
    // 9: SetTransformParent
    // 10: SetEffect
    // 11: SetBitmapInterpolationMode
    // 12: SetBorderMode
    // 13: SetClip (IDCompositionClip)
    // 14: SetClip (D2D_RECT_F)
    // 15: SetContent
    
    private nint* VTable => *(nint**)_ptr;
    
    public int SetContent(nint content)
    {
        var fn = (delegate* unmanaged[Stdcall]<nint, nint, int>)VTable[15];
        return fn(_ptr, content);
    }
    
    public void Dispose()
    {
        if (_ptr != 0)
        {
            var fn = (delegate* unmanaged[Stdcall]<nint, uint>)VTable[2]; // Release
            fn(_ptr);
            _ptr = 0;
        }
    }
}

/// <summary>
/// DirectX 11 graphics backend with DirectComposition for full window transparency support.
/// This backend uses CreateSwapChainForComposition which properly supports per-pixel alpha blending.
/// </summary>
/// <remarks>
/// This backend provides true window transparency on Windows by using the DirectComposition API.
/// Unlike the standard D3D11 backend which uses CreateSwapChainForHwnd (limited transparency support),
/// this backend uses CreateSwapChainForComposition with a DirectComposition visual tree to properly
/// composite the window with the desktop using per-pixel alpha values.
/// 
/// Key differences from D3D11Backend:
/// - Uses DXGI_SWAP_CHAIN_DESC1 with AlphaMode.Premultiplied
/// - Creates swap chain with CreateSwapChainForComposition
/// - Binds swap chain to a DirectComposition visual
/// - Properly composites transparent areas with the desktop
/// 
/// Performance note: This backend uses software rendering (SkiaSharp raster) and blits to GPU.
/// For fully GPU-accelerated rendering, use the Vulkan or OpenGL backends.
/// </remarks>
public class D3D11CompositionBackend : IGraphicsBackend
{
    private D3D11? _d3d11;
    private DXGI? _dxgi;
    private ComPtr<ID3D11Device> _device;
    private ComPtr<ID3D11DeviceContext> _context;
    private ComPtr<IDXGISwapChain1> _swapChain;
    
    // DirectComposition objects - stored as raw COM vtable wrappers
    private DCompDevice _dcompDevice;
    private DCompTarget _dcompTarget;
    private DCompVisual _dcompVisual;
    
    // Use raw pointers for resources that need to be released during resize
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
            throw new PlatformNotSupportedException("DirectX11 Composition backend is only available on Windows");
        }
        
        _window = window;
        _width = window.FramebufferSize.X;
        _height = window.FramebufferSize.Y;
        
        Console.WriteLine("[D3D11CompositionBackend] Initializing DirectX 11 with DirectComposition backend...");
        
        // Create D3D11 device, DXGI swap chain, and DirectComposition objects
        CreateDeviceAndSwapChain(window);
        Console.WriteLine("[D3D11CompositionBackend] Device, swap chain, and DirectComposition created");

        // Create Skia GRContext - use CPU rendering and blit to D3D11
        CreateGRContext();
        Console.WriteLine("[D3D11CompositionBackend] SkiaSharp context created (CPU with D3D11 blit)");

        _renderer = new SkiaPanelRenderer();

        CreateSurface(window.FramebufferSize);
        Console.WriteLine("[D3D11CompositionBackend] DirectX 11 Composition backend initialized successfully!");
        Console.WriteLine("[D3D11CompositionBackend] Full per-pixel transparency is now supported.");
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
                Console.WriteLine("[D3D11CompositionBackend] Hardware device creation failed, trying WARP...");
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
        
        Console.WriteLine($"[D3D11CompositionBackend] Using feature level: {actualFeatureLevel}");
        
        // Get DXGI device from D3D11 device
        IDXGIDevice* dxgiDevice;
        var dxgiDeviceGuid = typeof(IDXGIDevice).GUID;
        _device.Handle->QueryInterface(&dxgiDeviceGuid, (void**)&dxgiDevice);
        
        // Create DirectComposition device from DXGI device
        CreateDirectCompositionDevice((nint)dxgiDevice);
        
        // Get DXGI factory from adapter
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
        
        // Create swap chain for composition - this is the key difference!
        // This swap chain supports AlphaMode.Premultiplied for true transparency
        var swapChainDesc = new SwapChainDesc1
        {
            Width = (uint)_width,
            Height = (uint)_height,
            Format = Silk.NET.DXGI.Format.FormatB8G8R8A8Unorm,
            Stereo = false,
            SampleDesc = new SampleDesc { Count = 1, Quality = 0 },
            BufferUsage = DXGI.UsageRenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch, // Stretch for composition
            SwapEffect = SwapEffect.FlipSequential, // Required for composition
            AlphaMode = AlphaMode.Premultiplied, // The key for transparency!
            Flags = 0
        };
        
        IDXGISwapChain1* swapChainPtr;
        
        // CreateSwapChainForComposition - supports per-pixel alpha
        var hr = factory->CreateSwapChainForComposition(
            (IUnknown*)_device.Handle,
            &swapChainDesc,
            null, // No restrict to output
            &swapChainPtr
        );
        
        if (hr < 0)
        {
            throw new Exception($"Failed to create swap chain for composition: HRESULT 0x{hr:X8}");
        }
        
        _swapChain = new ComPtr<IDXGISwapChain1>(swapChainPtr);
        Console.WriteLine("[D3D11CompositionBackend] Swap chain for composition created with premultiplied alpha");
        
        // Bind swap chain to DirectComposition visual
        BindSwapChainToComposition(hwnd);
        
        // Clean up DXGI objects
        factory->Release();
        adapter->Release();
        dxgiDevice->Release();
        
        // Create render target view
        CreateRenderTargetView();
        
        // Create staging texture for CPU-GPU transfer
        CreateStagingTexture();
    }

    private unsafe void CreateDirectCompositionDevice(nint dxgiDevice)
    {
        // Create DirectComposition device using DCompositionCreateDevice
        var iid = DCompApi.IID_IDCompositionDevice;
        var hr = DCompApi.DCompositionCreateDevice(dxgiDevice, ref iid, out var dcompDevicePtr);
        
        if (hr < 0)
        {
            throw new Exception($"Failed to create DirectComposition device: HRESULT 0x{hr:X8}");
        }
        
        _dcompDevice = new DCompDevice(dcompDevicePtr);
        Console.WriteLine("[D3D11CompositionBackend] DirectComposition device created");
    }

    private unsafe void BindSwapChainToComposition(nint hwnd)
    {
        if (!_dcompDevice.IsValid)
        {
            throw new InvalidOperationException("DirectComposition device not created");
        }
        
        // Create composition target for the window
        var hr = _dcompDevice.CreateTargetForHwnd(hwnd, true, out _dcompTarget);
        if (hr < 0)
        {
            throw new Exception($"Failed to create composition target: HRESULT 0x{hr:X8}");
        }
        Console.WriteLine("[D3D11CompositionBackend] Composition target created");
        
        // Create a visual for the swap chain content
        hr = _dcompDevice.CreateVisual(out _dcompVisual);
        if (hr < 0)
        {
            throw new Exception($"Failed to create composition visual: HRESULT 0x{hr:X8}");
        }
        Console.WriteLine("[D3D11CompositionBackend] Composition visual created");
        
        // Set the swap chain as the content of the visual
        // IDCompositionVisual.SetContent takes an IUnknown pointer to the swap chain
        // The swap chain handle is already a COM interface pointer
        hr = _dcompVisual.SetContent((nint)_swapChain.Handle);
        if (hr < 0)
        {
            throw new Exception($"Failed to set visual content: HRESULT 0x{hr:X8}");
        }
        Console.WriteLine("[D3D11CompositionBackend] Swap chain bound to visual");
        
        // Set the visual as the root of the composition target
        hr = _dcompTarget.SetRoot(_dcompVisual);
        if (hr < 0)
        {
            throw new Exception($"Failed to set composition root: HRESULT 0x{hr:X8}");
        }
        Console.WriteLine("[D3D11CompositionBackend] Visual set as composition root");
        
        // Commit the composition
        hr = _dcompDevice.Commit();
        if (hr < 0)
        {
            throw new Exception($"Failed to commit composition: HRESULT 0x{hr:X8}");
        }
        Console.WriteLine("[D3D11CompositionBackend] Composition committed");
    }

    private unsafe void CreateRenderTargetView()
    {
        // Get back buffer
        ID3D11Texture2D* backBufferPtr;
        _swapChain.GetBuffer(0, SilkMarshal.GuidPtrOf<ID3D11Texture2D>(), (void**)&backBufferPtr);
        _backBuffer = backBufferPtr;
        
        // Create render target view
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
        // Using null GRContext means we'll use software rendering
        // For full hardware acceleration, OpenGL or Vulkan backends are recommended
        _grContext = null;
    }

    public unsafe void Resize(Vector2D<int> size)
    {
        if (_device.Handle == null || size.X <= 0 || size.Y <= 0) return;

        // Don't resize if dimensions haven't changed
        if (size.X == _width && size.Y == _height)
        {
            return;
        }

        _width = size.X;
        _height = size.Y;
        
        // Dispose old surface first
        _surface?.Dispose();
        _surface = null;
        
        // Clear all device context state
        _context.Handle->ClearState();
        _context.Handle->Flush();
        
        // Release all references to back buffer
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
        
        if (_stagingTexture != null)
        {
            _stagingTexture->Release();
            _stagingTexture = null;
        }
        
        // Resize swap chain buffers
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
        
        // Commit the composition after resize
        if (_dcompDevice.IsValid)
        {
            _dcompDevice.Commit();
        }
        
        Console.WriteLine($"[D3D11CompositionBackend] Resize complete: {_width}x{_height}");
    }

    public unsafe void Render(RootPanel panel)
    {
        if (_surface == null || _renderer == null || _renderTargetView == null) return;

        // Verify surface dimensions match backend dimensions
        var surfaceWidth = _surface.Canvas.DeviceClipBounds.Width;
        var surfaceHeight = _surface.Canvas.DeviceClipBounds.Height;
        
        if (surfaceWidth != _width || surfaceHeight != _height)
        {
            Console.WriteLine($"[D3D11CompositionBackend] Dimension mismatch! Surface: {surfaceWidth}x{surfaceHeight}, Backend: {_width}x{_height}");
            return;
        }

        // Clear the back buffer with transparent color
        // This is critical for proper transparency!
        float* clearColor = stackalloc float[] { 0.0f, 0.0f, 0.0f, 0.0f };
        _context.ClearRenderTargetView(_renderTargetView, clearColor);

        // Render UI to Skia surface with transparent background
        _surface.Canvas.Clear(SKColors.Transparent);
        _renderer.Render(_surface.Canvas, panel);
        _surface.Canvas.Flush();

        // Copy Skia bitmap data to D3D11 texture
        CopyToBackBuffer();

        // Present - for composition swap chains, we use SyncInterval=1 for VSync
        var hr = _swapChain.Present(1, 0);
        if (hr < 0)
        {
            Console.WriteLine($"[D3D11CompositionBackend] Present failed: HRESULT 0x{hr:X8}");
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
        
        // Get DirectWrite rendering parameters for proper text rendering on Windows
        float gamma = 2.2f;
        float contrast = 1.0f;
        float clearTypeLevel = 1.0f;
        int pixelGeometry = 1;
        int renderingMode = 0;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                Fazor.UI.Native.DirectWriteHelper.GetRenderingParams(out gamma, out contrast, out clearTypeLevel, out pixelGeometry, out renderingMode);
                Console.WriteLine($"[D3D11CompositionBackend] DirectWrite Params: Gamma={gamma}, Contrast={contrast}, ClearType={clearTypeLevel}, Geom={pixelGeometry}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[D3D11CompositionBackend] Failed to get DirectWrite params: {ex.Message}");
            }
        }

        var imageInfo = new SKImageInfo(size.X, size.Y, SKColorType.Bgra8888, SKAlphaType.Premul);
        
        // Configure surface properties for RGB subpixel rendering
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
        
        // Release DirectComposition objects using struct Dispose methods
        _dcompVisual.Dispose();
        _dcompTarget.Dispose();
        _dcompDevice.Dispose();
        
        // Release D3D11 resources
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
