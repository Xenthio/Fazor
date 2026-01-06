using System;

namespace SimpleDesktopApp;

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
        // Or programmatically: Avalazor.UI.AvalazorApplication.ForceAIMode = true;
        
        // Enable Panel Inspector in separate windows mode
        // Comment this line to use overlay mode instead
        Avalazor.UI.PanelInspectorHelper.EnableSeparateWindows();
        
        // Or use overlay mode:
        // Avalazor.UI.PanelInspectorHelper.EnableOverlayMode();
        
        // Run the Panel Inspector Test to demonstrate the new inspector feature
        //Avalazor.UI.AvalazorApplication.RunPanel<PanelInspectorTest>(title: "Avalazor - Panel Inspector Demo");
        
        // Run Flexbox Test:
        // Avalazor.UI.AvalazorApplication.RunPanel<FlexboxTest>(title: "Avalazor - Flexbox Layout Test");

        // Or use MainApp with text:
        //Avalazor.UI.AvalazorApplication.RunPanel<XGUIPortTest>(title: "Avalazor - Desktop Razor with XGUI Themes");


        //Avalazor.UI.AvalazorApplication.RunPanel<RefOnClickDemo>();
        //Avalazor.UI.AvalazorApplication.RunPanel<About>();
        //Avalazor.UI.AvalazorApplication.RunPanel<BorderImageTest>();
        
        // Test custom chrome improvements
        //Avalazor.UI.AvalazorApplication.RunPanel<CustomChromeTest>(title: "Custom Chrome Test");
        
        // Test custom chrome toggle bug
        Avalazor.UI.AvalazorApplication.RunPanel<CustomChromeToggleTest>(title: "Custom Chrome Toggle Test");
        
        // Run the Image Test to demonstrate texture rendering
        //Avalazor.UI.AvalazorApplication.RunPanel<SimpleImageTest>(title: "Avalazor - Image Rendering Test");
        
        // Run the Scrolling Demo
        //Avalazor.UI.AvalazorApplication.RunPanel<ScrollingDemo>(title: "Avalazor - Scrolling Demo");
    }
}
