using System;

namespace TestApp;

/// <summary>
/// Standalone test for the text squishing bug fix.
/// This test demonstrates that text now correctly unwraps when the window is resized larger.
/// </summary>
class TextResizeTestProgram
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Force AI mode for testing (since we're likely in a headless environment)
        Environment.SetEnvironmentVariable("FAZOR_AI_MODE", "1");
        
        Console.WriteLine("=== Text Resize Test ===");
        Console.WriteLine("Testing the fix for text staying squished after window resize");
        Console.WriteLine();
        
        // Run the text resize test
        Fazor.UI.FazorApplication.RunPanel<TextResizeTest>(
            title: "Fazor - Text Resize Bug Fix Test",
            width: 800,
            height: 600
        );
    }
}
