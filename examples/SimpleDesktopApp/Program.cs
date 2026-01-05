using System;

namespace SimpleDesktopApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Generate test images if they don't exist
        Tools.TestImageGenerator.GenerateTestImages();
        
        // Test custom chrome on VGUI theme (OliveGreen)
        Console.WriteLine("=== Testing VGUI Custom Chrome ===");
        Avalazor.UI.AvalazorApplication.RunPanel<VGUIChromeTester>(title: "VGUI Custom Chrome Test");
        
        // Uncomment to test ThinGrey transparency:
        // Console.WriteLine("=== Testing ThinGrey Transparency ===");
        // Avalazor.UI.AvalazorApplication.RunPanel<ThinGreyTester>(title: "ThinGrey Transparency Test");
    }
}
