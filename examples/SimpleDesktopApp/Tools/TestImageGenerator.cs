using SkiaSharp;
using System;
using System.IO;

namespace SimpleDesktopApp.Tools;

/// <summary>
/// Simple utility to create test images for the Image rendering test
/// </summary>
public static class TestImageGenerator
{
    public static void GenerateTestImages()
    {
        var imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images");
        Directory.CreateDirectory(imagesDir);
        
        var testImagePath = Path.Combine(imagesDir, "test-image.png");
        
        // Only create if it doesn't exist
        if (!File.Exists(testImagePath))
        {
            Console.WriteLine($"Creating test image at: {testImagePath}");
            CreateTestImage(testImagePath);
        }
        else
        {
            Console.WriteLine($"Test image already exists at: {testImagePath}");
        }
    }
    
    private static void CreateTestImage(string path)
    {
        // Create a 200x200 test image
        using var bitmap = new SKBitmap(200, 200, SKColorType.Rgba8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(bitmap))
        {
            // Fill background with blue
            canvas.Clear(new SKColor(74, 144, 226)); // #4a90e2
            
            // Draw white circle
            using (var paint = new SKPaint 
            { 
                Color = SKColors.White, 
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawCircle(100, 100, 60, paint);
            }
            
            // Draw text "TEST"
            using (var font = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 32))
            using (var paint = new SKPaint 
            { 
                Color = new SKColor(74, 144, 226), // Match background color
                IsAntialias = true
            })
            {
                // Center vertically by adjusting y (text baseline)
                canvas.DrawText("TEST", 100, 115, SKTextAlign.Center, font, paint);
            }
        }

        // Save as PNG
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
        
        Console.WriteLine($"✓ Created test image: {path}");
    }
}
