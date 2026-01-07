using System;
using Avalazor.UI;
using Sandbox;
using XGUI;

namespace FakeOSApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize the file system with the application data path
        FileSystem.Initialize(AppContext.BaseDirectory);
        
        // Set up the XGUI system
        var scene = Scene.ActiveScene;
        var xguiSystem = scene.GetSystem<XGUISystem>();
        
        // Set the default theme
        xguiSystem.SetGlobalTheme("/XGUI/DefaultStyles/Computer95.scss");
        
        // Create the root panel with XGUI infrastructure
        var rootPanel = new Sandbox.UI.RootPanel();
        rootPanel.PanelBounds = new Sandbox.UI.Rect(0, 0, 1280, 720);
        
        // Create the XGUI root panel
        var xguiRootPanel = new XGUIRootPanel();
        xguiSystem.Panel = xguiRootPanel;
        rootPanel.AddChild(xguiRootPanel);
        
        // Load the FakeOS component
        // For now, we'll create a simple test window to verify the setup works
        var testWindow = new TestStartupWindow();
        xguiRootPanel.AddChild(testWindow);
        
        // Perform initial layout
        rootPanel.Layout();
        
        // Run the application
        AvalazorApplication.Run(rootPanel, 1280, 720, "FakeOS - XGUI Compatibility Demo");
    }
}

/// <summary>
/// A simple test window to verify the XGUI compatibility works.
/// Once FakeOS code is added, this can be replaced with FakeOSLoader.
/// </summary>
public class TestStartupWindow : XGUI.Window
{
    public TestStartupWindow()
    {
        Title = "XGUI Compatibility Test";
        Position = new Sandbox.UI.Vector2(100, 100);
        Size = new Sandbox.UI.Vector2(400, 300);
        HasMinimise = true;
        HasMaximise = true;
        HasClose = true;
        
        AddClass("window");
        
        // Create content
        var content = CreateWindowContentPanel();
        content.Style.Padding = 10;
        content.Style.FlexDirection = Sandbox.UI.FlexDirection.Column;
        content.Style.RowGap = 10;
        
        var title = new Sandbox.UI.Label();
        title.Text = "XGUI Compatibility Layer Test";
        title.Style.FontSize = 16;
        title.Style.FontWeight = 700;
        content.AddChild(title);
        
        var info = new Sandbox.UI.Label();
        info.Text = "This window demonstrates that the S&box/XGUI compatibility layer is working. " +
                   "The XGUI Window class, theming, and event system are all functional.";
        content.AddChild(info);
        
        var button = new Sandbox.UI.Button();
        button.Text = "Click Me!";
        button.AddEventListener("onclick", () => {
            Sandbox.Log.Info("Button clicked!");
        });
        content.AddChild(button);
    }
}
