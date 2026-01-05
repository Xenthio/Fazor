using SkiaSharp;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using Sandbox.UI;

namespace Sandbox.UI.Skia;

/// <summary>
/// SkiaSharp-based panel renderer.
/// Implements IPanelRenderer to render Sandbox.UI panels using SkiaSharp.
/// </summary>
public class SkiaPanelRenderer : IPanelRenderer
{
    private SKCanvas? _canvas;
    
    // Tolerance for comparing border widths to determine if they're uniform
    private const float BorderWidthTolerance = 0.1f;
    
    // Cache typefaces to avoid recreation each frame (which can cause font rendering issues on resize).
    // This cache is bounded by the finite number of font family/style combinations used by the application.
    // Typefaces are long-lived resources and should not be frequently created/destroyed.
    // Use ConcurrentDictionary for thread safety when multiple windows are rendering simultaneously.
    private static readonly ConcurrentDictionary<(string family, SKFontStyle style), SKTypeface> _typefaceCache = new();
    
    // Directories to search for font files
    private static readonly List<string> _fontDirectories = new();
    
    // Cache for font file paths by family name (lowercase)
    private static readonly ConcurrentDictionary<string, string?> _fontFileCache = new();
    
    /// <summary>
    /// Add a directory to search for font files (.ttf, .otf)
    /// </summary>
    public static void AddFontDirectory(string directory)
    {
        if (!_fontDirectories.Contains(directory))
        {
            _fontDirectories.Add(directory);
            // Clear cache when directories change
            _fontFileCache.Clear();
        }
    }

    /// <summary>
    /// Static constructor to set up text measurement for Labels (backward compatibility)
    /// </summary>
    static SkiaPanelRenderer()
    {
        // Register text measurement function for Label layout calculations
        // This ensures measurement works even if RegisterAsActiveRenderer isn't called
        Label.TextMeasureFunc = (text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping) => 
            MeasureTextStatic(text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping);
        
        // Register extended text measurement function with overflow support (matches s&box)
        Label.TextMeasureFuncEx = (text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping, textOverflow, wordBreak, whiteSpace) =>
            MeasureTextStaticEx(text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping, textOverflow, wordBreak, whiteSpace);
        
        // Register texture size function for Image layout calculations
        Image.TextureSizeFunc = GetTextureSizeStatic;
    }

    /// <summary>
    /// Ensures the static constructor has run and text measurement is available.
    /// Call this before creating any panels to ensure accurate text layout.
    /// </summary>
    /// <returns>True when initialization is complete</returns>
    public static bool EnsureInitialized()
    {
        // Simply accessing this triggers the static constructor if not already run
        return Label.TextMeasureFunc != null;
    }

    /// <summary>
    /// Register this renderer as the active renderer for text measurement.
    /// Call this once when the renderer is created to enable accurate Label layout.
    /// </summary>
    public void RegisterAsActiveRenderer()
    {
        Label.TextMeasureFunc = (text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping) => 
            MeasureTextStatic(text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping);
        
        Label.TextMeasureFuncEx = (text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping, textOverflow, wordBreak, whiteSpace) =>
            MeasureTextStaticEx(text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping, textOverflow, wordBreak, whiteSpace);
    }

    /// <summary>
    /// Measure text using SkiaSharp for accurate layout calculations (IPanelRenderer implementation)
    /// </summary>
    public Vector2 MeasureText(string text, string? fontFamily, float fontSize, int fontWeight)
    {
        return MeasureTextStatic(text, fontFamily, fontSize, fontWeight, float.NaN, false);
    }

    /// <summary>
    /// Static text measurement for use before renderer instance is created.
    /// Now uses RichTextKit for proper text layout matching s&box.
    /// </summary>
    private static Vector2 MeasureTextStatic(string text, string? fontFamily, float fontSize, int fontWeight, float maxWidth, bool allowWrapping)
    {
        // Use defaults for overflow properties
        return MeasureTextStaticEx(text, fontFamily, fontSize, fontWeight, maxWidth, allowWrapping,
                                   TextOverflow.None, WordBreak.Normal, WhiteSpace.Normal);
    }
    
    /// <summary>
    /// Extended static text measurement with overflow property support (matches s&box).
    /// </summary>
    private static Vector2 MeasureTextStaticEx(string text, string? fontFamily, float fontSize, int fontWeight, 
                                               float maxWidth, bool allowWrapping,
                                               TextOverflow textOverflow, WordBreak wordBreak, WhiteSpace whiteSpace)
    {
        // Create a TextBlock wrapper for measurement with overflow properties
        var textBlock = new TextBlockWrapper();
        textBlock.Update(text, fontFamily, fontSize, fontWeight, FontSmooth.Auto, 
                        textOverflow, wordBreak, whiteSpace);
        
        // If wrapping is disabled or no width constraint, measure without width limit
        if (!allowWrapping || float.IsNaN(maxWidth) || maxWidth <= 0)
        {
            return textBlock.Measure(float.NaN, float.NaN);
        }
        
        // Measure with width constraint for wrapping
        return textBlock.Measure(maxWidth, float.NaN);
    }

    private static SKFontStyle ToSKFontStyleStatic(int weight)
    {
        var skWeight = weight switch
        {
            <= 100 => SKFontStyleWeight.Thin,
            <= 200 => SKFontStyleWeight.ExtraLight,
            <= 300 => SKFontStyleWeight.Light,
            <= 400 => SKFontStyleWeight.Normal,
            <= 500 => SKFontStyleWeight.Medium,
            <= 600 => SKFontStyleWeight.SemiBold,
            <= 700 => SKFontStyleWeight.Bold,
            <= 800 => SKFontStyleWeight.ExtraBold,
            _ => SKFontStyleWeight.Black
        };

        return new SKFontStyle(skWeight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
    }

    private static SKTypeface GetCachedTypefaceStatic(string fontFamily, SKFontStyle fontStyle)
    {
        return _typefaceCache.GetOrAdd((fontFamily, fontStyle), key =>
        {
            // First try to load from font file
            var fontFile = FindFontFile(key.family);
            if (fontFile != null)
            {
                var fileTypeface = SKTypeface.FromFile(fontFile);
                if (fileTypeface != null)
                    return fileTypeface;
            }
            
            // Fall back to system font
            var typeface = SKTypeface.FromFamilyName(key.family, key.style);
            return typeface ?? SKTypeface.Default;
        });
    }
    
    /// <summary>
    /// Find a font file by family name in registered directories
    /// </summary>
    private static string? FindFontFile(string fontFamily)
    {
        var lowerFamily = fontFamily.ToLowerInvariant();
        
        return _fontFileCache.GetOrAdd(lowerFamily, family =>
        {
            // Common font file name patterns
            var patterns = new[]
            {
                $"{family}.ttf",
                $"{family}.otf",
                $"{fontFamily}.ttf",
                $"{fontFamily}.otf",
            };
            
            foreach (var dir in _fontDirectories)
            {
                if (!Directory.Exists(dir)) continue;
                
                foreach (var pattern in patterns)
                {
                    var path = Path.Combine(dir, pattern);
                    if (File.Exists(path))
                        return path;
                }
                
                // Also try searching for files containing the font name
                try
                {
                    foreach (var file in Directory.GetFiles(dir, "*.ttf"))
                    {
                        if (Path.GetFileNameWithoutExtension(file).ToLowerInvariant().Contains(family))
                            return file;
                    }
                    foreach (var file in Directory.GetFiles(dir, "*.otf"))
                    {
                        if (Path.GetFileNameWithoutExtension(file).ToLowerInvariant().Contains(family))
                            return file;
                    }
                }
                catch
                {
                    // Ignore directory access errors
                }
            }
            
            return null;
        });
    }

    public Rect Screen { get; private set; }

    /// <summary>
    /// The current SkiaSharp canvas being rendered to
    /// </summary>
    public SKCanvas? Canvas => _canvas;

    /// <summary>
    /// Render a root panel to the given canvas
    /// </summary>
    public void Render(SKCanvas canvas, RootPanel panel, float opacity = 1.0f)
    {
        _canvas = canvas;
        Screen = panel.PanelBounds;

        var state = new RenderState
        {
            X = Screen.Left,
            Y = Screen.Top,
            Width = Screen.Width,
            Height = Screen.Height,
            RenderOpacity = opacity
        };

        Render(panel, state);
    }

    public void Render(RootPanel panel, float opacity = 1.0f)
    {
        if (_canvas == null)
            throw new InvalidOperationException("Canvas not set. Call Render(SKCanvas, RootPanel) instead.");

        Screen = panel.PanelBounds;

        var state = new RenderState
        {
            X = Screen.Left,
            Y = Screen.Top,
            Width = Screen.Width,
            Height = Screen.Height,
            RenderOpacity = opacity
        };

        Render(panel, state);
    }

    public void Render(Panel panel, RenderState state)
    {
        if (_canvas == null) return;
        if (panel.ComputedStyle == null) return;
        if (!panel.IsVisible) return;

        _canvas.Save();

        // Apply transform matrix if present
        var hasTransform = ApplyPanelTransform(_canvas, panel);

        // Apply clipping for overflow:hidden (matches S&box behavior)
        bool hasClipping = false;
        if (panel.ComputedStyle.Overflow == OverflowMode.Hidden)
        {
            var clipRect = new SKRect(
                panel.Box.Rect.Left,
                panel.Box.Rect.Top,
                panel.Box.Rect.Right,
                panel.Box.Rect.Bottom
            );
            _canvas.ClipRect(clipRect);
            hasClipping = true;
        }

        // Draw background
        if (panel.HasBackground)
        {
            DrawBackground(_canvas, panel, ref state);
        }

        // Draw content (text, images, etc.)
        if (panel.HasContent)
        {
            DrawContent(_canvas, panel, ref state);
        }

        // Draw children
        if (panel.HasChildren)
        {
            panel.RenderChildren(this, ref state);
        }

        _canvas.Restore();
    }

    /// <summary>
    /// Apply the panel's transform matrix to the canvas.
    /// Matches S&box's approach: applies transform-origin by wrapping the transform matrix
    /// with translations to/from the origin point.
    /// Based on PanelRenderer.Matrix.cs from S&box engine (lines 39-49).
    /// </summary>
    /// <param name="canvas">The canvas to transform</param>
    /// <param name="panel">The panel whose transform to apply</param>
    /// <returns>True if a transform was applied</returns>
    private bool ApplyPanelTransform(SKCanvas canvas, Panel panel)
    {
        var style = panel.ComputedStyle;
        if (style == null) return false;
        
        // Check if transform is empty
        if (style.Transform?.IsEmpty() ?? true) return false;
        if (panel.TransformMatrix == System.Numerics.Matrix4x4.Identity) return false;
        
        // Calculate transform origin point (default is 0,0 - top-left of panel)
        // S&box: origin.x += style.TransformOriginX.Value.GetPixels( panel.Box.Rect.Width, 0.0f );
        float originX = panel.Box.Rect.Left + (style.TransformOriginX?.GetPixels(panel.Box.Rect.Width) ?? 0f);
        float originY = panel.Box.Rect.Top + (style.TransformOriginY?.GetPixels(panel.Box.Rect.Height) ?? 0f);
        
        // Apply transform with origin: translate(origin) * transform * translate(-origin)
        // This matches S&box's Matrix.CreateTranslation approach
        canvas.Translate(originX, originY);
        
        // Convert Matrix4x4 to SKMatrix (use 2D portion)
        var m = panel.TransformMatrix;
        var skMatrix = new SKMatrix(
            m.M11, m.M21, m.M41,  // First row (scaleX, skewY, translateX)
            m.M12, m.M22, m.M42,  // Second row (skewX, scaleY, translateY)
            m.M14, m.M24, m.M44   // Perspective row
        );
        canvas.Concat(ref skMatrix);
        
        canvas.Translate(-originX, -originY);
        
        // Update GlobalMatrix for child panels
        panel.LocalMatrix = panel.TransformMatrix;
        panel.GlobalMatrix = panel.TransformMatrix;
        
        return true;
    }

    private void DrawBackground(SKCanvas canvas, Panel panel, ref RenderState state)
    {
        var style = panel.ComputedStyle;
        if (style == null) return;

        var rect = panel.Box.Rect;
        var skRect = ToSKRect(rect);
        var opacity = panel.Opacity * state.RenderOpacity;

        // Border radius - each corner can have a different radius (matches s&box)
        // S&box uses the smallest dimension for percentage resolution to ensure circular corners
        // (deviates from CSS spec which uses width for X-radii and height for Y-radii for elliptical corners)
        var resolutionSize = Math.Min(rect.Width, rect.Height);
        var radiusTL = style.BorderTopLeftRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusTR = style.BorderTopRightRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusBL = style.BorderBottomLeftRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusBR = style.BorderBottomRightRadius?.GetPixels(resolutionSize) ?? 0;
        var hasRadius = radiusTL > 0 || radiusTR > 0 || radiusBL > 0 || radiusBR > 0;

        // Background gradient takes priority over solid color
        if (style.BackgroundGradient.HasValue && style.BackgroundGradient.Value.IsValid)
        {
            DrawGradientBackground(canvas, skRect, style.BackgroundGradient.Value, opacity, radiusTL, radiusTR, radiusBR, radiusBL);
        }
        // Background color
        else if (style.BackgroundColor.HasValue && style.BackgroundColor.Value.a > 0)
        {
            var color = ToSKColor(style.BackgroundColor.Value, opacity);

            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            if (hasRadius)
            {
                using var path = CreateRoundedRectPath(skRect, radiusTL, radiusTR, radiusBR, radiusBL);
                canvas.DrawPath(path, paint);
            }
            else
            {
                canvas.DrawRect(skRect, paint);
            }
        }
        
        // Background image
        DrawBackgroundImage(canvas, panel, skRect, opacity, radiusTL, radiusTR, radiusBR, radiusBL);

        // Border
        DrawBorder(canvas, panel, ref state);
    }
    
    /// <summary>
    /// Create a rounded rectangle path with per-corner radii (matches s&box behavior)
    /// Uses SKRoundRect for proper rounded corners
    /// </summary>
    private SKPath CreateRoundedRectPath(SKRect rect, float radiusTL, float radiusTR, float radiusBR, float radiusBL)
    {
        var path = new SKPath();
        
        // Create an SKRoundRect with individual corner radii
        // SKRoundRect expects radii in a specific order
        var roundRect = new SKRoundRect();
        
        // SetRectRadii takes an array of 4 SKPoint values, one for each corner
        // Order: top-left, top-right, bottom-right, bottom-left
        // Each SKPoint has X (horizontal radius) and Y (vertical radius)
        roundRect.SetRectRadii(rect, new SKPoint[]
        {
            new SKPoint(radiusTL, radiusTL),  // Top-left
            new SKPoint(radiusTR, radiusTR),  // Top-right
            new SKPoint(radiusBR, radiusBR),  // Bottom-right
            new SKPoint(radiusBL, radiusBL)   // Bottom-left
        });
        
        path.AddRoundRect(roundRect);
        return path;
    }
    
    private void DrawBackgroundImage(SKCanvas canvas, Panel panel, SKRect skRect, float opacity, float radiusTL, float radiusTR, float radiusBR, float radiusBL)
    {
        var style = panel.ComputedStyle;
        if (style?.BackgroundImage == null || string.IsNullOrEmpty(style.BackgroundImage.Path))
            return;
            
        var texture = style.BackgroundImage;
        
        // Load texture if not already loaded
        if (texture.NativeHandle == null)
        {
            texture.LoadData();
        }
        
        // Try to get SKImage from native handle
        SKImage? image = texture.NativeHandle as SKImage;
        if (image == null)
        {
            // Try to load from path using file system
            image = LoadTextureFromPath(texture.Path);
            if (image != null)
            {
                texture.NativeHandle = image;
                texture.Width = image.Width;
                texture.Height = image.Height;
            }
        }
        
        if (image == null) return;
        
        using var paint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, (byte)(255 * opacity)),
            IsAntialias = true
        };
        
        // Use high-quality sampling for smooth image rendering
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        
        // Apply clipping if has border radius (per-corner)
        var hasRadius = radiusTL > 0 || radiusTR > 0 || radiusBR > 0 || radiusBL > 0;
        if (hasRadius)
        {
            canvas.Save();
            // Ensure we use the same radii as the background/border
            using var clipPath = CreateRoundedRectPath(skRect, radiusTL, radiusTR, radiusBR, radiusBL);
            canvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
        }
        
        // Calculate destination rect based on background-size and background-position CSS properties
        // Using ImageRect utility ported from s&box for proper size/position handling
        var panelRect = new Rect(skRect.Left, skRect.Top, skRect.Width, skRect.Height);
        var imageRectInput = new ImageRect.Input
        {
            Image = texture,
            PanelRect = panelRect,
            ImagePositionX = style.BackgroundPositionX,
            ImagePositionY = style.BackgroundPositionY,
            ImageSizeX = style.BackgroundSizeX,
            ImageSizeY = style.BackgroundSizeY,
            ScaleToScreen = 1.0f, // TODO: get actual DPI scale if needed
            DefaultSize = Length.Auto
        };
        
        var imageCalc = ImageRect.Calculate(imageRectInput);
        var bgRect = imageCalc.Rect;
        
        // Convert to SKRect - bgRect.X/Y are position, bgRect.Z/W are width/height
        var dstRect = new SKRect(
            skRect.Left + bgRect.X,
            skRect.Top + bgRect.Y,
            skRect.Left + bgRect.X + bgRect.Z,
            skRect.Top + bgRect.Y + bgRect.W
        );
        
        var srcRect = new SKRect(0, 0, image.Width, image.Height);
        canvas.DrawImage(image, srcRect, dstRect, sampling, paint);
        
        if (hasRadius)
        {
            canvas.Restore();
        }
    }
    
    // Cache for loaded texture images
    private static readonly ConcurrentDictionary<string, SKImage?> _textureCache = new();
    
    private SKImage? LoadTextureFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        // Check cache first
        if (_textureCache.TryGetValue(path, out var cached))
            return cached;
            
        SKImage? image = null;
        bool foundFile = false;
        bool hadException = false;
        
        try
        {
            // Try to load as file path
            if (File.Exists(path))
            {
                foundFile = true;
                using var stream = File.OpenRead(path);
                image = SKImage.FromEncodedData(stream);
            }
            // Try common asset paths
            else
            {
                var possiblePaths = new[]
                {
                    path,
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", path.TrimStart('/')),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path.TrimStart('/')),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", path.TrimStart('/')),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", path.TrimStart('/'))
                };
                
                foreach (var possiblePath in possiblePaths)
                {
                    if (File.Exists(possiblePath))
                    {
                        foundFile = true;
                        using var stream = File.OpenRead(possiblePath);
                        image = SKImage.FromEncodedData(stream);
                        if (image != null) break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            hadException = true;
            Log.Warning($"Failed to load texture '{path}': {ex.Message}");
        }
        
        // Only log more specific warnings if no exception occurred (to avoid duplicate logging)
        if (!hadException)
        {
            // Warn if image file was not found in any search path
            if (!foundFile)
            {
                Log.Warning($"Could not find texture file: {path}");
            }
            // Warn if file was found but failed to decode
            else if (image == null)
            {
                Log.Warning($"Found texture file but failed to decode: {path}");
            }
        }
        
        // Cache result (even if null to avoid repeated load attempts)
        _textureCache[path] = image;
        return image;
    }
    
    /// <summary>
    /// Get texture dimensions for layout calculations
    /// </summary>
    private static (int width, int height)? GetTextureSizeStatic(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        
        // Use the static cache directly - don't create unnecessary renderer instances
        if (_textureCache.TryGetValue(path, out var image) && image != null)
        {
            return (image.Width, image.Height);
        }
        
        return null;
    }

    private void DrawGradientBackground(SKCanvas canvas, SKRect rect, GradientInfo gradient, float opacity, float radiusTL, float radiusTR, float radiusBR, float radiusBL)
    {
        SKShader? shader = null;

        if (gradient.GradientType == GradientInfo.GradientTypes.Linear)
        {
            shader = CreateLinearGradientShader(rect, gradient, opacity);
        }
        else if (gradient.GradientType == GradientInfo.GradientTypes.Radial)
        {
            shader = CreateRadialGradientShader(rect, gradient, opacity);
        }

        if (shader == null) return;

        using (shader)
        using (var paint = new SKPaint
        {
            Shader = shader,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        })
        {
            var hasRadius = radiusTL > 0 || radiusTR > 0 || radiusBR > 0 || radiusBL > 0;
            if (hasRadius)
            {
                using var path = CreateRoundedRectPath(rect, radiusTL, radiusTR, radiusBR, radiusBL);
                canvas.DrawPath(path, paint);
            }
            else
            {
                canvas.DrawRect(rect, paint);
            }
        }
    }

    private SKShader? CreateLinearGradientShader(SKRect rect, GradientInfo gradient, float opacity)
    {
        var colorOffsets = gradient.ColorOffsets;
        if (colorOffsets.IsDefaultOrEmpty || colorOffsets.Length < 2)
            return null;

        // Calculate start and end points based on angle
        // CSS angles: 0deg = to top, 90deg = to right, 180deg = to bottom (default)
        var angle = gradient.Angle;
        var centerX = rect.MidX;
        var centerY = rect.MidY;
        var halfWidth = rect.Width / 2f;
        var halfHeight = rect.Height / 2f;

        // Calculate the length needed to cover the entire rect at this angle
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var length = MathF.Abs(halfWidth * sin) + MathF.Abs(halfHeight * cos);

        var startX = centerX - sin * length;
        var startY = centerY - cos * length;
        var endX = centerX + sin * length;
        var endY = centerY + cos * length;

        var colors = new SKColor[colorOffsets.Length];
        var positions = new float[colorOffsets.Length];

        for (int i = 0; i < colorOffsets.Length; i++)
        {
            colors[i] = ToSKColor(colorOffsets[i].color, opacity);
            positions[i] = colorOffsets[i].offset ?? (float)i / (colorOffsets.Length - 1);
        }

        return SKShader.CreateLinearGradient(
            new SKPoint(startX, startY),
            new SKPoint(endX, endY),
            colors,
            positions,
            SKShaderTileMode.Clamp
        );
    }

    private SKShader? CreateRadialGradientShader(SKRect rect, GradientInfo gradient, float opacity)
    {
        var colorOffsets = gradient.ColorOffsets;
        if (colorOffsets.IsDefaultOrEmpty || colorOffsets.Length < 2)
            return null;

        // GetPixels handles the percentage conversion correctly
        var centerX = rect.Left + gradient.OffsetX.GetPixels(rect.Width);
        var centerY = rect.Top + gradient.OffsetY.GetPixels(rect.Height);
        var radius = MathF.Max(rect.Width, rect.Height) / 2f;

        var colors = new SKColor[colorOffsets.Length];
        var positions = new float[colorOffsets.Length];

        for (int i = 0; i < colorOffsets.Length; i++)
        {
            colors[i] = ToSKColor(colorOffsets[i].color, opacity);
            positions[i] = colorOffsets[i].offset ?? (float)i / (colorOffsets.Length - 1);
        }

        return SKShader.CreateRadialGradient(
            new SKPoint(centerX, centerY),
            radius,
            colors,
            positions,
            SKShaderTileMode.Clamp
        );
    }

    private void DrawBorder(SKCanvas canvas, Panel panel, ref RenderState state)
    {
        var style = panel.ComputedStyle;
        if (style == null) return;

        var rect = panel.Box.Rect;
        var skRect = ToSKRect(rect);
        var opacity = panel.Opacity * state.RenderOpacity;

        // Check if we have a border-image to render
        if (style.BorderImageSource != null && !string.IsNullOrEmpty(style.BorderImageSource.Path) && style.BorderImageSource.Path != "invalid")
        {
            DrawBorderImage(canvas, panel, skRect, opacity);
            return; // Border-image replaces regular borders
        }

        // Get border widths with 1px clamp for non-zero values (matches s&box)
        var leftWidth = style.BorderLeftWidth?.GetPixels(1f) ?? 0;
        if (leftWidth > 0 && leftWidth < 1) leftWidth = 1;

        var topWidth = style.BorderTopWidth?.GetPixels(1f) ?? 0;
        if (topWidth > 0 && topWidth < 1) topWidth = 1;

        var rightWidth = style.BorderRightWidth?.GetPixels(1f) ?? 0;
        if (rightWidth > 0 && rightWidth < 1) rightWidth = 1;

        var bottomWidth = style.BorderBottomWidth?.GetPixels(1f) ?? 0;
        if (bottomWidth > 0 && bottomWidth < 1) bottomWidth = 1;
        
        // Check if any border exists
        var hasBorder = leftWidth > 0 || topWidth > 0 || rightWidth > 0 || bottomWidth > 0;
        if (!hasBorder) return;

        // Get border radius - each corner can be different (matches s&box)
        // S&box uses the smallest dimension for percentage resolution to ensure circular corners
        // (deviates from CSS spec which uses width for X-radii and height for Y-radii for elliptical corners)
        var resolutionSize = Math.Min(rect.Width, rect.Height);
        var radiusTL = style.BorderTopLeftRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusTR = style.BorderTopRightRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusBL = style.BorderBottomLeftRadius?.GetPixels(resolutionSize) ?? 0;
        var radiusBR = style.BorderBottomRightRadius?.GetPixels(resolutionSize) ?? 0;
        var hasRadius = radiusTL > 0 || radiusTR > 0 || radiusBL > 0 || radiusBR > 0;

        // Check if all borders are uniform (same color and width)
        var uniformColor = style.BorderLeftColor == style.BorderTopColor &&
                          style.BorderTopColor == style.BorderRightColor &&
                          style.BorderRightColor == style.BorderBottomColor;
        var uniformWidth = Math.Abs(leftWidth - topWidth) < BorderWidthTolerance &&
                          Math.Abs(topWidth - rightWidth) < BorderWidthTolerance &&
                          Math.Abs(rightWidth - bottomWidth) < BorderWidthTolerance;
        var borderWidth = (leftWidth + topWidth + rightWidth + bottomWidth) / 4f;

        // Check if we can use the simple path-based stroke rendering
        // This is preferred for uniform borders as it produces smoother results
        // We can only use it if:
        // 1. Borders are uniform in color and width
        // 2. For every corner with a radius, the radius is >= half the border width
        //    (Otherwise the stroke path would need a negative radius)
        bool IsRadiusValidForStroke(float r, float w) => r <= 0 || r >= w / 2f;
        
        var canUseStroke = uniformColor && uniformWidth && style.BorderLeftColor.HasValue &&
                          IsRadiusValidForStroke(radiusTL, borderWidth) &&
                          IsRadiusValidForStroke(radiusTR, borderWidth) &&
                          IsRadiusValidForStroke(radiusBR, borderWidth) &&
                          IsRadiusValidForStroke(radiusBL, borderWidth);

        if (canUseStroke)
        {
            var color = ToSKColor(style.BorderLeftColor.Value, opacity);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = borderWidth,
                IsAntialias = true
            };

            // Adjust rect to account for stroke being drawn on center of path
            var halfWidth = borderWidth / 2f;
            var adjustedRect = new SKRect(
                skRect.Left + halfWidth,
                skRect.Top + halfWidth,
                skRect.Right - halfWidth,
                skRect.Bottom - halfWidth
            );

            if (hasRadius)
            {
                // Adjust radii for the stroke inset
                // The path radius should be (OuterRadius - StrokeWidth/2)
                var rTL = Math.Max(0, radiusTL - halfWidth);
                var rTR = Math.Max(0, radiusTR - halfWidth);
                var rBR = Math.Max(0, radiusBR - halfWidth);
                var rBL = Math.Max(0, radiusBL - halfWidth);

                using var path = CreateRoundedRectPath(adjustedRect, rTL, rTR, rBR, rBL);
                canvas.DrawPath(path, paint);
            }
            else
            {
                canvas.DrawRect(adjustedRect, paint);
            }
        }
        else
        {
            // Non-uniform borders: draw each edge separately
            // Like s&box, we support border-radius even with non-uniform borders
            
            if (hasRadius)
            {
                // Draw non-uniform borders with rounded corners using path clipping
                DrawNonUniformBorderWithRadius(canvas, rect, style, opacity, 
                    leftWidth, topWidth, rightWidth, bottomWidth,
                    radiusTL, radiusTR, radiusBR, radiusBL);
            }
            else
            {
                // Simple rectangular borders without radius
                DrawNonUniformBorderSimple(canvas, rect, style, opacity,
                    leftWidth, topWidth, rightWidth, bottomWidth);
            }
        }
    }

    /// <summary>
    /// Draw non-uniform borders without radius (simple rectangles)
    /// </summary>
    private void DrawNonUniformBorderSimple(SKCanvas canvas, Rect rect, Styles style, float opacity,
        float leftWidth, float topWidth, float rightWidth, float bottomWidth)
    {
        // Left border
        if (leftWidth > 0 && style.BorderLeftColor.HasValue)
        {
            var color = ToSKColor(style.BorderLeftColor.Value, opacity);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(rect.Left, rect.Top, leftWidth, rect.Height, paint);
        }

        // Top border
        if (topWidth > 0 && style.BorderTopColor.HasValue)
        {
            var color = ToSKColor(style.BorderTopColor.Value, opacity);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(rect.Left, rect.Top, rect.Width, topWidth, paint);
        }

        // Right border
        if (rightWidth > 0 && style.BorderRightColor.HasValue)
        {
            var color = ToSKColor(style.BorderRightColor.Value, opacity);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(rect.Right - rightWidth, rect.Top, rightWidth, rect.Height, paint);
        }

        // Bottom border
        if (bottomWidth > 0 && style.BorderBottomColor.HasValue)
        {
            var color = ToSKColor(style.BorderBottomColor.Value, opacity);
            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };
            canvas.DrawRect(rect.Left, rect.Bottom - bottomWidth, rect.Width, bottomWidth, paint);
        }
    }

    /// <summary>
    /// Draw border using border-image (9-slice rendering)
    /// Based on s&box's border-image implementation
    /// </summary>
    private void DrawBorderImage(SKCanvas canvas, Panel panel, SKRect skRect, float opacity)
    {
        var style = panel.ComputedStyle;
        if (style == null || style.BorderImageSource == null) return;
        
        var texture = style.BorderImageSource;
        
        // Load texture if not already loaded
        if (texture.NativeHandle == null)
        {
            texture.LoadData();
        }
        
        // Try to get SKImage from native handle
        SKImage? image = texture.NativeHandle as SKImage;
        if (image == null)
        {
            // Try to load from path using file system
            image = LoadTextureFromPath(texture.Path);
            if (image != null)
            {
                texture.NativeHandle = image;
                texture.Width = image.Width;
                texture.Height = image.Height;
            }
        }
        
        if (image == null) return;
        
        // Get border image widths (slice sizes in the source image)
        var leftSlice = style.BorderImageWidthLeft?.GetPixels(1f) ?? (image.Width / 3f);
        var topSlice = style.BorderImageWidthTop?.GetPixels(1f) ?? (image.Height / 3f);
        var rightSlice = style.BorderImageWidthRight?.GetPixels(1f) ?? (image.Width / 3f);
        var bottomSlice = style.BorderImageWidthBottom?.GetPixels(1f) ?? (image.Height / 3f);
        
        // Get border widths (how wide to draw the borders in the destination)
        var leftWidth = style.BorderLeftWidth?.GetPixels(1f) ?? leftSlice;
        var topWidth = style.BorderTopWidth?.GetPixels(1f) ?? topSlice;
        var rightWidth = style.BorderRightWidth?.GetPixels(1f) ?? rightSlice;
        var bottomWidth = style.BorderBottomWidth?.GetPixels(1f) ?? bottomSlice;
        
        using var paint = new SKPaint
        {
            Color = style.BorderImageTint.HasValue 
                ? ToSKColor(style.BorderImageTint.Value, opacity) 
                : new SKColor(255, 255, 255, (byte)(255 * opacity)),
            IsAntialias = true
        };
        
        // Use high-quality sampling for smooth image rendering
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        
        // 9-slice rendering: divide image into 9 parts and draw each
        // Source rectangles (in image coordinates)
        var srcLeft = 0f;
        var srcTop = 0f;
        var srcRight = image.Width;
        var srcBottom = image.Height;
        var srcCenterLeft = leftSlice;
        var srcCenterTop = topSlice;
        var srcCenterRight = image.Width - rightSlice;
        var srcCenterBottom = image.Height - bottomSlice;
        
        // Destination rectangles (on canvas)
        var dstLeft = skRect.Left;
        var dstTop = skRect.Top;
        var dstRight = skRect.Right;
        var dstBottom = skRect.Bottom;
        var dstCenterLeft = skRect.Left + leftWidth;
        var dstCenterTop = skRect.Top + topWidth;
        var dstCenterRight = skRect.Right - rightWidth;
        var dstCenterBottom = skRect.Bottom - bottomWidth;
        
        // 1. Top-left corner
        canvas.DrawImage(image,
            new SKRect(srcLeft, srcTop, srcCenterLeft, srcCenterTop),
            new SKRect(dstLeft, dstTop, dstCenterLeft, dstCenterTop),
            sampling, paint);
        
        // 2. Top edge
        canvas.DrawImage(image,
            new SKRect(srcCenterLeft, srcTop, srcCenterRight, srcCenterTop),
            new SKRect(dstCenterLeft, dstTop, dstCenterRight, dstCenterTop),
            sampling, paint);
        
        // 3. Top-right corner
        canvas.DrawImage(image,
            new SKRect(srcCenterRight, srcTop, srcRight, srcCenterTop),
            new SKRect(dstCenterRight, dstTop, dstRight, dstCenterTop),
            sampling, paint);
        
        // 4. Left edge
        canvas.DrawImage(image,
            new SKRect(srcLeft, srcCenterTop, srcCenterLeft, srcCenterBottom),
            new SKRect(dstLeft, dstCenterTop, dstCenterLeft, dstCenterBottom),
            sampling, paint);
        
        // 5. Center (if fill is enabled)
        if (style.BorderImageFill == UI.BorderImageFill.Filled)
        {
            canvas.DrawImage(image,
                new SKRect(srcCenterLeft, srcCenterTop, srcCenterRight, srcCenterBottom),
                new SKRect(dstCenterLeft, dstCenterTop, dstCenterRight, dstCenterBottom),
                sampling, paint);
        }
        
        // 6. Right edge
        canvas.DrawImage(image,
            new SKRect(srcCenterRight, srcCenterTop, srcRight, srcCenterBottom),
            new SKRect(dstCenterRight, dstCenterTop, dstRight, dstCenterBottom),
            sampling, paint);
        
        // 7. Bottom-left corner
        canvas.DrawImage(image,
            new SKRect(srcLeft, srcCenterBottom, srcCenterLeft, srcBottom),
            new SKRect(dstLeft, dstCenterBottom, dstCenterLeft, dstBottom),
            sampling, paint);
        
        // 8. Bottom edge
        canvas.DrawImage(image,
            new SKRect(srcCenterLeft, srcCenterBottom, srcCenterRight, srcBottom),
            new SKRect(dstCenterLeft, dstCenterBottom, dstCenterRight, dstBottom),
            sampling, paint);
        
        // 9. Bottom-right corner
        canvas.DrawImage(image,
            new SKRect(srcCenterRight, srcCenterBottom, srcRight, srcBottom),
            new SKRect(dstCenterRight, dstCenterBottom, dstRight, dstBottom),
            sampling, paint);
    }

    /// <summary>
    /// Draw non-uniform borders with rounded corners
    /// Properly implements CSS border rendering with miter joins at corners
    /// </summary>
    private void DrawNonUniformBorderWithRadius(SKCanvas canvas, Rect rect, Styles style, float opacity,
        float leftWidth, float topWidth, float rightWidth, float bottomWidth,
        float radiusTL, float radiusTR, float radiusBR, float radiusBL)
    {
        var skRect = ToSKRect(rect);
        
        // Create outer path
        using var outerPath = CreateRoundedRectPath(skRect, radiusTL, radiusTR, radiusBR, radiusBL);
        
        // Calculate inner rect
        var innerRect = new SKRect(
            skRect.Left + leftWidth,
            skRect.Top + topWidth,
            skRect.Right - rightWidth,
            skRect.Bottom - bottomWidth
        );
        
        // Handle overlapping borders (clamp inner rect to non-negative)
        // This prevents "bowtie" artifacts when borders are thicker than the element
        if (innerRect.Width < 0)
        {
            float cx = (skRect.Left + leftWidth + skRect.Right - rightWidth) / 2;
            innerRect.Left = cx;
            innerRect.Right = cx;
        }
        if (innerRect.Height < 0)
        {
            float cy = (skRect.Top + topWidth + skRect.Bottom - bottomWidth) / 2;
            innerRect.Top = cy;
            innerRect.Bottom = cy;
        }
        
        // Calculate inner radii (circular, to match circular outer radii)
        // We use SKPoint array for SetRectRadii, but X and Y are equal per corner to maintain circular corners
        var innerRadii = new SKPoint[4];
        var innerRadiusTL = (float)Math.Max(0, radiusTL - Math.Max(leftWidth, topWidth));
        var innerRadiusTR = (float)Math.Max(0, radiusTR - Math.Max(rightWidth, topWidth));
        var innerRadiusBR = (float)Math.Max(0, radiusBR - Math.Max(rightWidth, bottomWidth));
        var innerRadiusBL = (float)Math.Max(0, radiusBL - Math.Max(leftWidth, bottomWidth));
        innerRadii[0] = new SKPoint(innerRadiusTL, innerRadiusTL);
        innerRadii[1] = new SKPoint(innerRadiusTR, innerRadiusTR);
        innerRadii[2] = new SKPoint(innerRadiusBR, innerRadiusBR);
        innerRadii[3] = new SKPoint(innerRadiusBL, innerRadiusBL);
        
        using var innerRoundRect = new SKRoundRect();
        innerRoundRect.SetRectRadii(innerRect, innerRadii);
        using var innerPath = new SKPath();
        innerPath.AddRoundRect(innerRoundRect);
        
        canvas.Save();
        
        // Clip to the outer rounded rect
        // This ensures the outer edge matches the "normal" uniform border rendering exactly
        canvas.ClipPath(outerPath, SKClipOperation.Intersect, true);
        
        // Clip OUT the inner rounded rect
        // This creates the hole in the middle
        canvas.ClipPath(innerPath, SKClipOperation.Difference, true);

        // Draw each side using a trapezoid defined by the sharp corners of the bounding boxes.
        // The miter line is the diagonal connecting the outer sharp corner to the inner sharp corner.
        // Since we are clipped to the rounded border path, these large trapezoids will be trimmed
        // to the correct rounded shape.
        
        // We add a small overlap to the inner coordinates to ensure the trapezoids overlap
        // at the miter lines, preventing hairline gaps due to anti-aliasing.
        float overlap = 0.5f;

        // Top
        if (topWidth > 0 && style.BorderTopColor.HasValue)
        {
            DrawTrapezoid(canvas, style.BorderTopColor.Value, opacity,
                skRect.Left, skRect.Top,          // Outer TL
                skRect.Right, skRect.Top,         // Outer TR
                innerRect.Right, innerRect.Top + overlap,   // Inner TR
                innerRect.Left, innerRect.Top + overlap);   // Inner TL
        }

        // Right
        if (rightWidth > 0 && style.BorderRightColor.HasValue)
        {
            DrawTrapezoid(canvas, style.BorderRightColor.Value, opacity,
                skRect.Right, skRect.Top,         // Outer TR
                skRect.Right, skRect.Bottom,      // Outer BR
                innerRect.Right - overlap, innerRect.Bottom,// Inner BR
                innerRect.Right - overlap, innerRect.Top);  // Inner TR
        }

        // Bottom
        if (bottomWidth > 0 && style.BorderBottomColor.HasValue)
        {
            DrawTrapezoid(canvas, style.BorderBottomColor.Value, opacity,
                skRect.Right, skRect.Bottom,      // Outer BR
                skRect.Left, skRect.Bottom,       // Outer BL
                innerRect.Left, innerRect.Bottom - overlap, // Inner BL
                innerRect.Right, innerRect.Bottom - overlap);// Inner BR
        }

        // Left
        if (leftWidth > 0 && style.BorderLeftColor.HasValue)
        {
            DrawTrapezoid(canvas, style.BorderLeftColor.Value, opacity,
                skRect.Left, skRect.Bottom,       // Outer BL
                skRect.Left, skRect.Top,          // Outer TL
                innerRect.Left + overlap, innerRect.Top,    // Inner TL
                innerRect.Left + overlap, innerRect.Bottom);// Inner BL
        }

        canvas.Restore();
    }

    private void DrawTrapezoid(SKCanvas canvas, Color color, float opacity,
        float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
    {
        using var path = new SKPath();
        path.MoveTo(x1, y1);
        path.LineTo(x2, y2);
        path.LineTo(x3, y3);
        path.LineTo(x4, y4);
        path.Close();

        using var paint = new SKPaint
        {
            Color = ToSKColor(color, opacity),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        
        canvas.DrawPath(path, paint);
    }

    private void DrawContent(SKCanvas canvas, Panel panel, ref RenderState state)
    {
        // Draw based on panel type
        if (panel is Label label)
        {
            DrawLabel(canvas, label, ref state);
        }
        else if (panel is Image image)
        {
            DrawImage(canvas, image, ref state);
        }
        else
        {
            // Allow custom content drawing
            panel.DrawContent(ref state);
        }

        // Draw caret for TextEntry if focused
        if (panel is TextEntry textEntry && textEntry.HasFocus && textEntry.Label != null)
        {
            DrawTextEntryCaret(canvas, textEntry, ref state);
        }
    }

    private void DrawTextEntryCaret(SKCanvas canvas, TextEntry textEntry, ref RenderState state)
    {
        // Don't draw caret if there's a selection
        if (textEntry.Label.HasSelection())
            return;

        var blinkRate = 0.8f;
        var timeSinceFocus = textEntry.GetType().GetField("TimeSinceNotInFocus", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(textEntry);

        if (timeSinceFocus is float time)
        {
            // Proper blink cycle: time % blinkRate creates repeating pattern from 0 to blinkRate
            var blink = (time % blinkRate) < (blinkRate * 0.5f);
            if (!blink) return; // Caret is in off phase

            var caret = textEntry.Label.GetCaretRect(textEntry.CaretPosition);
            
            // GetCaretRect returns coordinates that already include _textRect.Position.
            // Since _textRect is set from label.Box.RectInner (which is in parent/TextEntry space),
            // the caret coordinates are already in TextEntry's coordinate space. No offset needed.
            
            caret.Left = MathF.Floor(caret.Left); // avoid subpixel positions
            caret.Width = 1;

            var opacity = textEntry.Opacity * state.RenderOpacity;
            var style = textEntry.ComputedStyle;
            var caretColor = style?.CaretColor ?? style?.FontColor ?? Color.Black;

            using var paint = new SKPaint
            {
                Color = ToSKColor(caretColor, opacity),
                Style = SKPaintStyle.Fill,
                IsAntialias = false // Crisp 1px line
            };

            var skRect = new SKRect(caret.Left, caret.Top, caret.Left + caret.Width, caret.Bottom);
            canvas.DrawRect(skRect, paint);
        }
    }

    private void DrawLabel(SKCanvas canvas, Label label, ref RenderState state)
    {
        if (string.IsNullOrEmpty(label.Text)) return;

        var style = label.ComputedStyle;
        if (style == null) return;

        // Process text according to white-space style
        var processedText = label.ProcessWhiteSpace(label.Text);
        if (string.IsNullOrEmpty(processedText)) return;

        var opacity = label.Opacity * state.RenderOpacity;
        var textColor = style.FontColor ?? new Color(0, 0, 0, 1);
        var fontSize = style.FontSize?.GetPixels(16f) ?? 16f;
        var fontFamily = style.FontFamily ?? "Arial";
        var fontWeight = style.FontWeight ?? 400;
        var fontSmooth = style.FontSmooth ?? FontSmooth.Auto;
        
        // Get text rect
        var rect = label.Box.RectInner;

        // Get or create cached TextBlockWrapper
        TextBlockWrapper textBlock;
        if (label._textBlockWrapper is TextBlockWrapper cached)
        {
            textBlock = cached;
        }
        else
        {
            textBlock = new TextBlockWrapper();
            label._textBlockWrapper = textBlock;
        }
        
        // Get overflow-related style properties (matches s&box)
        var textOverflow = style.TextOverflow ?? TextOverflow.None;
        var wordBreak = style.WordBreak ?? WordBreak.Normal;
        var whiteSpace = style.WhiteSpace ?? WhiteSpace.Normal;
        
        // Update the text block with all style properties
        textBlock.Update(processedText, fontFamily, fontSize, fontWeight, fontSmooth, 
                        textOverflow, wordBreak, whiteSpace);
        
        // Measure with width constraint if wrapping is enabled
        var shouldWrap = style.WhiteSpace != WhiteSpace.NoWrap;
        if (shouldWrap && rect.Width > 0)
        {
            textBlock.Measure(rect.Width, rect.Height);
        }
        else
        {
            textBlock.Measure(float.NaN, float.NaN);
        }

        // Calculate starting position based on text alignment
        var x = rect.Left;
        var y = rect.Top;

        // Horizontal alignment
        if (style.TextAlign == TextAlign.Center)
        {
            x = rect.Left + (rect.Width - textBlock.MeasuredWidth) / 2;
        }
        else if (style.TextAlign == TextAlign.Right)
        {
            x = rect.Right - textBlock.MeasuredWidth;
        }

        // Vertical alignment (if needed, based on AlignItems)
        if (style.AlignItems == Align.Center)
        {
            y = rect.Top + (rect.Height - textBlock.MeasuredHeight) / 2;
        }
        else if (style.AlignItems == Align.FlexEnd)
        {
            y = rect.Bottom - textBlock.MeasuredHeight;
        }

        // Update label's _textRect to match the actual rendered position
        // This is needed for accurate hit testing in GetLetterAtScreenPosition
        label._textRect = new Rect(x, y, textBlock.MeasuredWidth, textBlock.MeasuredHeight);

        // Paint the text using RichTextKit with selection if enabled
        // RichTextKit handles selection rendering automatically like S&box
        var skColor = ToSKColor(textColor, opacity);
        if (label.ShouldDrawSelection && label.SelectionStart != label.SelectionEnd)
        {
            textBlock.Paint(canvas, x, y, skColor, label.SelectionStart, label.SelectionEnd, label.SelectionColor);
        }
        else
        {
            textBlock.Paint(canvas, x, y, skColor);
        }
    }

    private static SKTypeface GetCachedTypeface(string fontFamily, SKFontStyle fontStyle)
    {
        var key = (fontFamily, fontStyle);
        return _typefaceCache.GetOrAdd(key, k => SKTypeface.FromFamilyName(k.family, k.style));
    }

    private void DrawImage(SKCanvas canvas, Image image, ref RenderState state)
    {
        if (string.IsNullOrEmpty(image.TexturePath)) return;

        var rect = image.Box.Rect;
        var skRect = ToSKRect(rect);
        var opacity = image.Opacity * state.RenderOpacity;

        // Load the texture
        SKImage? skImage = LoadTextureFromPath(image.TexturePath);
        
        // If loading failed, use missing texture fallback
        if (skImage == null)
        {
            skImage = GetMissingTexture();
        }
        
        if (skImage == null) return; // Should never happen since we have fallback

        using var paint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, (byte)(255 * opacity)),
            IsAntialias = true
        };

        // Use high-quality sampling for smooth image rendering
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        // Get image rendering style (pixelated vs smooth)
        var style = image.ComputedStyle;
        if (style?.ImageRendering == ImageRendering.Pixelated || style?.ImageRendering == ImageRendering.Point)
        {
            // Use nearest-neighbor filtering for pixelated look
            sampling = new SKSamplingOptions(SKFilterMode.Nearest);
        }

        // Draw the image to fit the panel bounds
        // Use object-fit behavior if specified, otherwise default to fill
        var objectFit = style?.ObjectFit ?? ObjectFit.Fill;
        var destRect = CalculateObjectFitRect(skRect, skImage.Width, skImage.Height, objectFit);

        canvas.DrawImage(skImage, destRect, sampling, paint);
    }

    private SKRect CalculateObjectFitRect(SKRect container, int imageWidth, int imageHeight, ObjectFit objectFit)
    {
        // Guard against zero dimensions
        if (imageWidth <= 0 || imageHeight <= 0 || container.Width <= 0 || container.Height <= 0)
        {
            return container; // Fallback to container rect if dimensions are invalid
        }

        switch (objectFit)
        {
            case ObjectFit.Contain:
                {
                    // Scale to fit inside container, maintaining aspect ratio
                    float containerAspect = container.Width / container.Height;
                    float imageAspect = (float)imageWidth / imageHeight;
                    float scale = imageAspect > containerAspect 
                        ? container.Width / imageWidth 
                        : container.Height / imageHeight;
                    float width = imageWidth * scale;
                    float height = imageHeight * scale;
                    float x = container.Left + (container.Width - width) / 2;
                    float y = container.Top + (container.Height - height) / 2;
                    return new SKRect(x, y, x + width, y + height);
                }
            case ObjectFit.Cover:
                {
                    // Scale to cover container, maintaining aspect ratio (may crop)
                    float containerAspect = container.Width / container.Height;
                    float imageAspect = (float)imageWidth / imageHeight;
                    float scale = imageAspect > containerAspect 
                        ? container.Height / imageHeight 
                        : container.Width / imageWidth;
                    float width = imageWidth * scale;
                    float height = imageHeight * scale;
                    float x = container.Left + (container.Width - width) / 2;
                    float y = container.Top + (container.Height - height) / 2;
                    return new SKRect(x, y, x + width, y + height);
                }
            case ObjectFit.ScaleDown:
                {
                    // Like contain, but never scale up
                    if (imageWidth <= container.Width && imageHeight <= container.Height)
                    {
                        // Image fits, center it at original size
                        float x = container.Left + (container.Width - imageWidth) / 2;
                        float y = container.Top + (container.Height - imageHeight) / 2;
                        return new SKRect(x, y, x + imageWidth, y + imageHeight);
                    }
                    else
                    {
                        // Image too big, scale down using contain logic
                        return CalculateObjectFitRect(container, imageWidth, imageHeight, ObjectFit.Contain);
                    }
                }
            case ObjectFit.None:
                {
                    // No scaling, center at original size (may overflow)
                    float x = container.Left + (container.Width - imageWidth) / 2;
                    float y = container.Top + (container.Height - imageHeight) / 2;
                    return new SKRect(x, y, x + imageWidth, y + imageHeight);
                }
            case ObjectFit.Fill:
            default:
                // Stretch to fill container (default)
                return container;
        }
    }

    // Lazy-initialized missing texture
    private static SKImage? _missingTexture = null;
    private static readonly object _missingTextureLock = new();
    
    /// <summary>
    /// Get or create the pink and black checkerboard missing texture fallback
    /// </summary>
    private static SKImage? GetMissingTexture()
    {
        // Fast path without locking
        if (_missingTexture != null) return _missingTexture;
        
        lock (_missingTextureLock)
        {
            // Check again inside the lock in case another thread initialized it
            if (_missingTexture != null) return _missingTexture;
            
            try
            {
                // Create a 64x64 pink and black checkerboard pattern
                const int size = 64;
                const int checkSize = 8; // 8x8 pixel checks = 8x8 grid of checks
                
                using var bitmap = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bitmap);
                
                // Fill with pink
                canvas.Clear(new SKColor(255, 0, 255)); // Magenta/Pink
                
                // Draw black checks in checkerboard pattern
                using var blackPaint = new SKPaint { Color = SKColors.Black };
                for (int y = 0; y < size / checkSize; y++)
                {
                    for (int x = 0; x < size / checkSize; x++)
                    {
                        // Checkerboard: alternate based on (x + y) % 2
                        if ((x + y) % 2 == 1)
                        {
                            var rect = new SKRect(
                                x * checkSize, 
                                y * checkSize, 
                                (x + 1) * checkSize, 
                                (y + 1) * checkSize
                            );
                            canvas.DrawRect(rect, blackPaint);
                        }
                    }
                }
                
                _missingTexture = SKImage.FromBitmap(bitmap);
                return _missingTexture;
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to create missing texture: {ex.Message}");
                return null;
            }
        }
    }

    #region Conversion Helpers

    public static SKRect ToSKRect(Rect rect)
    {
        return new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
    }

    public static SKColor ToSKColor(Color color, float opacity = 1.0f)
    {
        return new SKColor(
            (byte)(color.r * 255),
            (byte)(color.g * 255),
            (byte)(color.b * 255),
            (byte)(color.a * opacity * 255)
        );
    }

    public static SKFontStyle ToSKFontStyle(int fontWeight)
    {
        var weight = fontWeight switch
        {
            100 => SKFontStyleWeight.Thin,
            200 => SKFontStyleWeight.ExtraLight,
            300 => SKFontStyleWeight.Light,
            400 => SKFontStyleWeight.Normal,
            500 => SKFontStyleWeight.Medium,
            600 => SKFontStyleWeight.SemiBold,
            700 => SKFontStyleWeight.Bold,
            800 => SKFontStyleWeight.ExtraBold,
            900 => SKFontStyleWeight.Black,
            _ => SKFontStyleWeight.Normal
        };

        return new SKFontStyle(weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
    }

    #endregion
}
