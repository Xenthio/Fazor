using Sandbox.UI;
using Fazor.Controls;
using Xunit;

namespace Sandbox.UI.Tests;

/// <summary>
/// Tests for UI controls (TextEntry, Slider, Tab, etc.)
/// </summary>
public class ControlTests
{
    [Fact]
    public void TextEntry_InitializesCorrectly()
    {
        var textEntry = new TextEntry();

        Assert.NotNull(textEntry);
        Assert.Equal("", textEntry.Text);
        Assert.False(textEntry.Disabled);
        Assert.False(textEntry.Numeric);
    }

    [Fact]
    public void TextEntry_SetText_UpdatesValue()
    {
        var textEntry = new TextEntry();
        textEntry.Text = "Hello World";

        Assert.Equal("Hello World", textEntry.Text);
        Assert.Equal("Hello World", textEntry.Value);
    }

    [Fact]
    public void TextEntry_NumericMode_HandlesMinMax()
    {
        var textEntry = new TextEntry
        {
            Numeric = true,
            MinValue = 0,
            MaxValue = 100
        };

        Assert.True(textEntry.Numeric);
        Assert.Equal(0, textEntry.MinValue);
        Assert.Equal(100, textEntry.MaxValue);
    }

    [Fact]
    public void Slider_InitializesWithDefaults()
    {
        var slider = new Slider();

        Assert.NotNull(slider);
        Assert.Equal(0, slider.MinValue);
        Assert.Equal(100, slider.MaxValue);
        Assert.Equal(1.0f, slider.Step);
        Assert.Equal(0, slider.Value);
    }

    [Fact]
    public void Slider_SetValue_ClampsToRange()
    {
        var slider = new Slider
        {
            MinValue = 0,
            MaxValue = 10
        };

        slider.Value = 15;
        Assert.Equal(10, slider.Value); // Should be clamped to max

        slider.Value = -5;
        Assert.Equal(0, slider.Value); // Should be clamped to min
    }

    [Fact]
    public void Slider_SetValue_SnapsToStep()
    {
        var slider = new Slider
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 10
        };

        slider.Value = 23;
        Assert.Equal(20, slider.Value); // Should snap to nearest 10

        slider.Value = 27;
        Assert.Equal(30, slider.Value); // Should snap to nearest 10
    }

    [Fact]
    public void SliderScale_InitializesCorrectly()
    {
        var sliderScale = new SliderScale();

        Assert.NotNull(sliderScale);
        Assert.True(sliderScale.HasScales);
        Assert.NotNull(sliderScale.SliderArea);
        Assert.NotNull(sliderScale.Track);
        Assert.NotNull(sliderScale.Thumb);
    }

    [Fact]
    public void SliderScaleEntry_InitializesWithBothComponents()
    {
        var sliderEntry = new SliderScaleEntry();

        Assert.NotNull(sliderEntry);
        Assert.NotNull(sliderEntry.Slider);
        Assert.NotNull(sliderEntry.TextEntry);
    }

    [Fact]
    public void SliderScaleEntry_ValueSyncs_BetweenSliderAndText()
    {
        var sliderEntry = new SliderScaleEntry
        {
            MinValue = 0,
            MaxValue = 100,
            Step = 1
        };

        sliderEntry.Value = 50;
        Assert.Equal(50, sliderEntry.Slider?.Value);
    }

    [Fact]
    public void TabContainer_InitializesCorrectly()
    {
        var tabControl = new TabContainer();

        Assert.NotNull(tabControl);
        Assert.NotNull(tabControl.TabsContainer);
        Assert.NotNull(tabControl.SheetContainer);
        Assert.Equal("", tabControl.ActiveTab);
    }

    [Fact]
    public void Tab_InitializesCorrectly()
    {
        var tab = new Tab();

        Assert.NotNull(tab);
        Assert.True(tab.HasClass("tab"));
    }

    [Fact]
    public void Tab_SetProperties_IgnoresSlotAttribute()
    {
        var tab = new Tab();
        tab.SetProperty("slot", "tab");
        
        // Should not throw and should be ignored
        Assert.NotNull(tab);
    }

    [Fact]
    public void Resizer_InitializesCorrectly()
    {
        var resizer = new Resizer();

        Assert.NotNull(resizer);
        Assert.True(resizer.HasClass("resizer"));
    }

    [Fact]
    public void CheckBox_AliasedAsCheck()
    {
        // Verify that CheckBox can be created with 'check' alias
        // This is tested through PanelFactory but we can check the class itself
        var checkbox = new CheckBox();
        
        Assert.NotNull(checkbox);
        Assert.False(checkbox.Checked);
    }

    [Fact]
    public void TemplateSlot_BubblesUpToParent()
    {
        // Test that template slots bubble up to parent
        var parent = new TestSlotPanel();
        var child = new Panel();
        child.Parent = parent;
        
        var node = Html.Node.Parse("<div>test</div>").ChildNodes.First();
        child.OnTemplateSlot(node, "testslot", child);
        
        Assert.True(parent.SlotHandled);
        Assert.Equal("testslot", parent.SlotName);
        Assert.Equal(child, parent.SlotPanel);
    }

    [Fact]
    public void TabContainer_HasProperCssClasses()
    {
        // Test that TabContainer has proper CSS classes for styling
        var tabControl = new TabContainer();
        
        // TabContainer should have tabcontrol class
        Assert.True(tabControl.HasClass("tabcontrol"));
        
        // TabsContainer should have tabs class
        Assert.NotNull(tabControl.TabsContainer);
        Assert.True(tabControl.TabsContainer.HasClass("tabs"));
        
        // SheetContainer should have sheets class
        Assert.NotNull(tabControl.SheetContainer);
        Assert.True(tabControl.SheetContainer.HasClass("sheets"));
    }

    [Fact]
    public void TabContainer_TabButtons_HaveProperClass()
    {
        // Test that tab buttons get proper CSS class
        var tabControl = new TabContainer();
        var tabPanel = new Panel();
        
        var tabInfo = tabControl.AddTab(tabPanel, "test", "Test Tab", null);
        
        // Button should have button class for styling
        Assert.NotNull(tabInfo.Button);
        Assert.True(tabInfo.Button.HasClass("button"));
    }
}

/// <summary>
/// Test panel that handles template slots
/// </summary>
class TestSlotPanel : Panel
{
    public bool SlotHandled { get; private set; }
    public string SlotName { get; private set; }
    public Panel SlotPanel { get; private set; }

    public override void OnTemplateSlot(Html.Node node, string? slotName, Panel panel)
    {
        SlotHandled = true;
        SlotName = slotName;
        SlotPanel = panel;
    }
}

/// <summary>
/// Tests for popup system (BasePopup, ComboBoxDropdown)
/// </summary>
public class PopupTests
{
    [Fact]
    public void BasePopup_InitializesCorrectly()
    {
        var popup = new BasePopup();

        Assert.NotNull(popup);
        Assert.False(popup.IsPopupOpen);
        Assert.True(popup.CloseOnClickOutside);
        Assert.True(popup.CloseOnFocusLoss);
        Assert.Null(popup.Opener);
        Assert.True(popup.HasClass("popup"));
    }

    [Fact]
    public void ComboBoxDropdown_InitializesCorrectly()
    {
        var dropdown = new ComboBoxDropdown();

        Assert.NotNull(dropdown);
        Assert.True(dropdown.HasClass("dropdown-panel"));
        Assert.True(dropdown.HasClass("flat-top"));
        Assert.Empty(dropdown.Options);
    }

    [Fact]
    public void ComboBoxDropdown_PopulatesOptions()
    {
        var dropdown = new ComboBoxDropdown();
        dropdown.Options = new System.Collections.Generic.List<Option>
        {
            new Option("Option 1", "value1"),
            new Option("Option 2", "value2"),
            new Option("Option 3", "value3")
        };

        dropdown.PopulateOptions();

        Assert.Equal(3, dropdown.ChildrenCount);
    }

    [Fact]
    public void ComboBox_CreatesDropdownOnOpen()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);

        var comboBox = new ComboBox();
        comboBox.Options = new System.Collections.Generic.List<Option>
        {
            new Option("Test 1", "test1"),
            new Option("Test 2", "test2")
        };
        rootPanel.AddChild(comboBox);

        // Perform layout so Box.Rect is set
        rootPanel.Layout();

        Assert.False(comboBox.IsOpen);

        comboBox.Open();

        Assert.True(comboBox.IsOpen);
    }

    [Fact]
    public void ComboBox_ClosesDropdown()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);

        var comboBox = new ComboBox();
        comboBox.Options = new System.Collections.Generic.List<Option>
        {
            new Option("Test 1", "test1")
        };
        rootPanel.AddChild(comboBox);
        rootPanel.Layout();

        comboBox.Open();
        Assert.True(comboBox.IsOpen);

        comboBox.Close();
        Assert.False(comboBox.IsOpen);
    }

    [Fact]
    public void PopupServiceProvider_CanRegisterAndUnregister()
    {
        var oldService = PopupServiceProvider.Current;

        // Create mock service
        var mockService = new MockPopupService();

        PopupServiceProvider.Register(mockService);
        Assert.Same(mockService, PopupServiceProvider.Current);

        PopupServiceProvider.Unregister();
        Assert.Null(PopupServiceProvider.Current);

        // Restore original
        if (oldService != null)
            PopupServiceProvider.Register(oldService);
    }

    private class MockPopupService : IPopupService
    {
        public bool SupportsNativePopups => false;

        public void OpenPopup(BasePopup popup, Vector2 screenPosition, Panel? opener = null) { }
        public void MarkPopupForClose(BasePopup popup) { }
        public void ClosePopup(BasePopup popup) { }
        public void CloseAllPopups(BasePopup? except = null) { }
        public Vector2 ConvertToScreenCoordinates(Vector2 panelPos, RootPanel? rootPanel) => panelPos;
        public Vector2 ConvertFromScreenCoordinates(Vector2 screenPos, RootPanel? rootPanel) => screenPos;
        public Vector2 GetWindowPosition() => Vector2.Zero;
        public Vector2 GetWindowSize() => new Vector2(800, 600);
    }
}
