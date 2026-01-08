// FakeOSApp Global Usings
// This ensures XGUI types take precedence when there are ambiguities

// Use XGUI's Window instead of Sandbox.UI.Window for in-panel windows
global using Window = XGUI.Window;
global using ComboBox = XGUI.ComboBox;

// Common usings
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
