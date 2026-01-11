using System.Linq;
using Sandbox.UI;
namespace Fazor.Controls;
/// <summary>
/// A container with tabs, allowing you to switch between different sheets.
/// You can position the tabs by adding the class tabs-bottom, tabs-left, tabs-right (default is tabs-top)
/// Based on XGUI-3 TabContainer.
/// </summary>
public class TabContainer : Panel
{
    /// <summary>
    /// A control housing the tabs
    /// </summary>
    public Panel TabsContainer { get; protected set; }
    /// <summary>
    /// A control housing the sheets
    /// </summary>
    public Panel SheetContainer { get; protected set; }
    /// <summary>
    /// Access to the tabs on this control
    /// </summary>
    public List<Tab> Tabs = new();
    /// <summary>
    /// If a cookie is set then the selected tab will be saved and restored.
    /// TODO: Implement cookie support when available
    /// </summary>
    public string TabCookie { get; set; } = "";
    /// <summary>
    /// If true we will act as a tab bar and have no body.
    /// </summary>
    public bool NoBody
    {
        get => SheetContainer.Style.Display == DisplayMode.None;
        set
        {
            SheetContainer.Style.Display = value ? DisplayMode.None : DisplayMode.Flex;
        }
    }
    string _activeTab = "";
    /// <summary>
    /// The tab that is active
    /// </summary>
    public string ActiveTab
    {
        get => _activeTab;
        set
        {
            if (_activeTab == value) return;
            _activeTab = value;
            var t = Tabs.FirstOrDefault(x => x.TabName == _activeTab);
            SwitchTab(t);
        }
    }
    public TabContainer()
    {
        // Manually add the tabcontainer class to match XGUI-3's TabContainer pattern
        // Note: XGUI-3's CSS uses PascalCase (.TabContainer) but their C# uses lowercase
        // We add both for compatibility
        AddClass("tabcontainer");
        TabsContainer = AddChild(new Panel(this, "tabs"));
        TabsContainer.AddClass("tabs");
        SheetContainer = AddChild(new Panel(this, "sheets"));
        SheetContainer.AddClass("sheets");
    }
    public override void SetProperty(string name, string value)
    {
        if (name == "cookie")
        {
            TabCookie = value;
            return;
        }
        if (name == "nobody")
        {
            NoBody = value == "true" || value == "1";
            return;
        }
        if (name == "activetab")
        {
            ActiveTab = value;
            return;
        }
        base.SetProperty(name, value);
    }
    /// <summary>
    /// Add a tab to the sheet.
    /// </summary>
    public Tab AddTab(Panel panel, string tabName, string title, string icon = null)
    {
        var index = Tabs.Count;
        var tab = new Tab(this, title, icon, panel);
        tab.TabName = tabName;
        Tabs.Add(tab);
        // TODO: Cookie support when implemented
        // var cookieIndex = string.IsNullOrWhiteSpace(TabCookie) ? -1 : GetCookie($"dropdown.{TabCookie}", -1);
        panel.Parent = SheetContainer;
        if (index == 0) // || cookieIndex == index
        {
            SwitchTab(tab, false);
        }
        else
        {
            tab.Active = false;
        }
        return tab;
    }
    public override void OnTemplateSlot(Sandbox.Html.Node element, string slotName, Panel panel)
    {
        if (slotName == "tab")
        {
            AddTab(panel, 
                element.GetAttribute("tabname", null), 
                element.GetAttribute("tabtext", null), 
                element.GetAttribute("tabicon", null));
            return;
        }
        base.OnTemplateSlot(element, slotName, panel);
    }
    /// <summary>
    /// Switch to a specific tab.
    /// </summary>
    public void SwitchTab(Tab tab, bool setCookie = true)
    {
        ActiveTab = tab.TabName;
        foreach (var page in Tabs)
        {
            page.Active = page == tab;
        }
        // TODO: Cookie support when implemented
        // if (setCookie && !string.IsNullOrEmpty(TabCookie))
        // {
        //     SetCookie($"dropdown.{TabCookie}", Tabs.IndexOf(tab));
        // }
    }
    /// <summary>
    /// Holds a Tab button and a Page for each sheet on the TabControl.
    /// </summary>
    public class Tab
    {
        private TabContainer Parent;
        public Button Button { get; protected set; }
        public Panel Page { get; protected set; }
        public string TabName { get; set; } = "";
        public Tab(TabContainer tabContainer, string title, string icon, Panel panel)
        {
            Parent = tabContainer;
            Page = panel;
            Button = new Button(title, icon, () => Parent?.SwitchTab(this, true));
            Button.Parent = tabContainer.TabsContainer;
        }
        private bool active;
        /// <summary>
        /// Change appearance based on active status
        /// </summary>
        public bool Active
        {
            get => active;
            set
            {
                active = value;
                Button.Active = value;
                Page.SetClass("active", value);
            }
        }
    }
}
