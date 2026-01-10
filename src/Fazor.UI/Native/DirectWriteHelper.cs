using System;
using System.Runtime.InteropServices;

namespace Fazor.UI.Native;

public static class DirectWriteHelper
{
    [DllImport("dwrite.dll", PreserveSig = false)]
    private static extern void DWriteCreateFactory(
        int factoryType,
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid,
        [MarshalAs(UnmanagedType.IUnknown)] out object factory
    );

    [Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDWriteFactory
    {
        void GetSystemFontCollection(out object collection, bool checkForUpdates);
        void CreateCustomFontCollection(object loader, IntPtr key, int keySize, out object collection);
        void RegisterFontCollectionLoader(object loader);
        void UnregisterFontCollectionLoader(object loader);
        void CreateFontFileReference([MarshalAs(UnmanagedType.LPWStr)] string filePath, IntPtr lastWriteTime, out object fontFile);
        void CreateCustomFontFileReference(object referenceKey, int keySize, object loader, out object fontFile);
        void CreateFontFace(int fontFaceType, int numberOfFiles, [MarshalAs(UnmanagedType.LPArray)] object[] fontFiles, int faceIndex, int fontFaceSimulationFlags, out object fontFace);
        void CreateRenderingParams(out IDWriteRenderingParams renderingParams);
        // ... other methods omitted
    }

    [Guid("2f0da53a-2add-47cd-82ee-d9ec34688e75")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDWriteRenderingParams
    {
        float GetGamma();
        float GetEnhancedContrast();
        float GetClearTypeLevel();
        int GetPixelGeometry();
        int GetRenderingMode();
    }

    public static void GetRenderingParams(out float gamma, out float contrast, out float clearTypeLevel, out int pixelGeometry, out int renderingMode)
    {
        // Defaults
        gamma = 2.2f;
        contrast = 1.0f;
        clearTypeLevel = 1.0f;
        pixelGeometry = 1; // RGB
        renderingMode = 0; // Default

        if (!OperatingSystem.IsWindows()) return;

        try
        {
            DWriteCreateFactory(0, typeof(IDWriteFactory).GUID, out var factoryObj);
            var factory = (IDWriteFactory)factoryObj;
            
            factory.CreateRenderingParams(out var paramsObj);
            
            gamma = paramsObj.GetGamma();
            contrast = paramsObj.GetEnhancedContrast();
            clearTypeLevel = paramsObj.GetClearTypeLevel();
            pixelGeometry = paramsObj.GetPixelGeometry();
            renderingMode = paramsObj.GetRenderingMode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get DirectWrite rendering params: {ex.Message}");
        }
    }
}
