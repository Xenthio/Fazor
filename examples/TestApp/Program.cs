using System;

namespace TestApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Generate test images if they don't exist
        Tools.TestImageGenerator.GenerateTestImages();
        
        // The framework now automatically detects if a display is available
        // and uses the AI renderer when running in headless environments (CI, SSH, etc.)
        // 
        // To force AI mode: set AVALAZOR_AI_MODE=1 environment variable
        // Or programmatically: Fazor.UI.FazorApplication.ForceAIMode = true;
        
        // Enable Panel Inspector in separate windows mode
        // Comment this line to use overlay mode instead
        Fazor.UI.PanelInspectorHelper.EnableSeparateWindows();
        
        // Or use overlay mode:
        // Fazor.UI.PanelInspectorHelper.EnableOverlayMode();
        
        // Run the Panel Selector to demonstrate all available test windows
        Fazor.UI.FazorApplication.RunPanel<PanelSelector>(title: "Fazor - Test Panel Selector");
        
        // Uncomment any of the following to run a specific test directly:
        
        // Run the Panel Inspector Test to demonstrate the new inspector feature
        //Fazor.UI.FazorApplication.RunPanel<PanelInspectorTest>(title: "Fazor - Panel Inspector Demo");
        
        // Run Flexbox Test:
        // Fazor.UI.FazorApplication.RunPanel<FlexboxTest>(title: "Fazor - Flexbox Layout Test");

        // Or use MainApp with text:
        //Fazor.UI.FazorApplication.RunPanel<XGUIPortTest>(title: "Fazor - Desktop Razor with XGUI Themes");

        //Fazor.UI.FazorApplication.RunPanel<RefOnClickDemo>();
        //Fazor.UI.FazorApplication.RunPanel<BorderImageTest>();
        //Fazor.UI.FazorApplication.RunPanel<About>(); // Transform-origin / Computer11 theme test window
        
        // Test custom chrome improvements
        //Fazor.UI.FazorApplication.RunPanel<CustomChromeTest>(title: "Custom Chrome Test");
        
        // Test custom chrome toggle bug
        //Fazor.UI.FazorApplication.RunPanel<CustomChromeToggleTest>(title: "Custom Chrome Toggle Test");
        
        // Run the Image Test to demonstrate texture rendering
        //Fazor.UI.FazorApplication.RunPanel<SimpleImageTest>(title: "Fazor - Image Rendering Test");
        
        // Run the Scrolling Demo
        //Fazor.UI.FazorApplication.RunPanel<ScrollingDemo>(title: "Fazor - Scrolling Demo");
    }
}
