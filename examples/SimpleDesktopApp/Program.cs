using System;

namespace SimpleDesktopApp;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Simple desktop app - runs the About window
        Avalazor.UI.AvalazorApplication.RunPanel<About>();
    }
}
