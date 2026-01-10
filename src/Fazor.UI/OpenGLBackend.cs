using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SkiaSharp;
using Sandbox.UI;
using Sandbox.UI.Skia;

namespace Fazor.UI;

/// <summary>
/// OpenGL graphics backend for Fazor.
/// Provides hardware-accelerated rendering using OpenGL and SkiaSharp.
/// </summary>
/// <remarks>
/// This backend works well on Linux and macOS. On Windows, DirectX11 backend is
/// recommended for better resize behavior due to platform-specific limitations.
/// </remarks>
public class OpenGLBackend : IGraphicsBackend
{
    private GL? _gl;
    private GRContext? _grContext;
    private GRGlInterface? _grGlInterface;
    private SKSurface? _surface;
    private SkiaPanelRenderer? _renderer;
    private IWindow? _window;

    public void Initialize(IWindow window)
    {
        _window = window;
        _gl = window.CreateOpenGL();

        _grGlInterface = GRGlInterface.Create((name) =>
            _gl.Context.TryGetProcAddress(name, out var addr) ? addr : IntPtr.Zero);

        _grContext = GRContext.CreateGl(_grGlInterface);
        _renderer = new SkiaPanelRenderer();

        CreateSurface(window.FramebufferSize);
    }

    public void Resize(Vector2D<int> size)
    {
        if (_gl == null) return;

        _gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);

        _surface?.Dispose();
        _surface = null;
        CreateSurface(size);
    }

    public void Render(RootPanel panel)
    {
        if (_gl == null || _surface == null || _renderer == null || _grContext == null) return;

        // Reset Skia's GL state tracking since Silk.NET may have changed GL state
        _grContext.ResetContext();

        // Enable alpha blending for transparent windows
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Clear with transparent color to support window transparency (e.g., ThinGrey theme)
        _gl.ClearColor(0, 0, 0, 0);
        _gl.Clear(ClearBufferMask.ColorBufferBit);
        _surface.Canvas.Clear(SKColors.Transparent);

        _renderer.Render(_surface.Canvas, panel);

        _grContext.Flush();
        
        // Don't manually swap - Silk.NET handles this automatically after the Render callback
    }

    private void CreateSurface(Vector2D<int> size)
    {
        if (_grContext == null) return;
        var glInfo = new GRGlFramebufferInfo(0, 0x8058); // Rgba8
        var target = new GRBackendRenderTarget(size.X, size.Y, 0, 8, glInfo);

        // Configure surface properties for RGB subpixel rendering
        // Note: RgbHorizontal is the most common pixel layout on modern LCD displays.
        // Some displays may use different layouts (RgbVertical, BgrHorizontal) but there's
        // no reliable cross-platform way to detect this. RgbHorizontal is a reasonable default.
        var surfProps = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);
        
        _surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888, surfProps);
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _grContext?.Dispose();
        _grGlInterface?.Dispose();
        _gl?.Dispose();
    }
}