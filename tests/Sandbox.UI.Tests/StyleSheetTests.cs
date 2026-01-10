using Sandbox.UI;
using System.Reflection;
using Xunit;

namespace Sandbox.UI.Tests;

/// <summary>
/// Tests for the StyleSheet CSS parsing and selector matching system
/// </summary>
public class StyleSheetTests
{
    [Fact]
    public void StyleSheet_FromString_ParsesSimpleRule()
    {
        var css = ".button { width: 100px; height: 50px; }";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Single(sheet.Nodes);
        Assert.Equal(100, sheet.Nodes[0].Styles.Width?.Value);
        Assert.Equal(50, sheet.Nodes[0].Styles.Height?.Value);
    }

    [Fact]
    public void StyleSheet_FromString_ParsesMultipleRules()
    {
        var css = @"
            .button { width: 100px; }
            .label { height: 20px; }
        ";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Equal(2, sheet.Nodes.Count);
    }

    [Fact]
    public void StyleSheet_FromString_ParsesVariables()
    {
        var css = @"
            $primary-color: #ff0000;
            .button { background-color: $primary-color; }
        ";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Single(sheet.Nodes);
        // The variable should be replaced
        Assert.Equal(1f, sheet.Nodes[0].Styles.BackgroundColor?.r);
        Assert.Equal(0f, sheet.Nodes[0].Styles.BackgroundColor?.g);
        Assert.Equal(0f, sheet.Nodes[0].Styles.BackgroundColor?.b);
    }

    [Fact]
    public void StyleSheet_FromString_ParsesNestedRules()
    {
        var css = @"
            .button {
                width: 100px;
                &:hover {
                    width: 120px;
                }
            }
        ";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Equal(2, sheet.Nodes.Count);
    }

    [Fact]
    public void StyleSelector_MatchesClass()
    {
        var css = ".button { width: 100px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel();
        panel.AddClass("button");

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.NotNull(matchedSelector);
    }

    [Fact]
    public void StyleSelector_DoesNotMatchWrongClass()
    {
        var css = ".button { width: 100px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel();
        panel.AddClass("label");

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.Null(matchedSelector);
    }

    [Fact]
    public void StyleSelector_MatchesId()
    {
        var css = "#main { width: 100px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel { Id = "main" };

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.NotNull(matchedSelector);
    }

    [Fact]
    public void StyleSelector_MatchesElementName()
    {
        var css = "panel { width: 100px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel();
        // ElementName defaults to type name lowercased

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.NotNull(matchedSelector);
    }

    [Fact]
    public void StyleSelector_MatchesPseudoClass()
    {
        var css = ".button:hover { width: 120px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel();
        panel.AddClass("button");
        panel.Switch(PseudoClass.Hover, true);

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.NotNull(matchedSelector);
    }

    [Fact]
    public void StyleSelector_DoesNotMatchWithoutPseudoClass()
    {
        var css = ".button:hover { width: 120px; }";
        var sheet = StyleSheet.FromString(css);

        var panel = new Panel();
        panel.AddClass("button");
        // Not hovered

        var matchedSelector = sheet.Nodes[0].Test(panel);
        Assert.Null(matchedSelector);
    }

    [Fact]
    public void StyleSelector_Specificity_IdBeatsClass()
    {
        var css = @"
            .button { width: 100px; }
            #main { width: 200px; }
        ";
        var sheet = StyleSheet.FromString(css);

        // ID selector should have higher score
        var classSelector = sheet.Nodes[0].Selectors[0];
        var idSelector = sheet.Nodes[1].Selectors[0];

        Assert.True(idSelector.Score > classSelector.Score);
    }

    [Fact]
    public void Styles_Set_ParsesPixelValue()
    {
        var styles = new Styles();
        var result = styles.Set("width", "100px");

        Assert.True(result);
        Assert.Equal(100, styles.Width?.Value);
        Assert.Equal(LengthUnit.Pixels, styles.Width?.Unit);
    }

    [Fact]
    public void Styles_Set_ParsesPercentValue()
    {
        var styles = new Styles();
        var result = styles.Set("width", "50%");

        Assert.True(result);
        Assert.Equal(50, styles.Width?.Value);
        Assert.Equal(LengthUnit.Percentage, styles.Width?.Unit);
    }

    [Fact]
    public void Styles_Set_ParsesColorHex()
    {
        var styles = new Styles();
        var result = styles.Set("color", "#ff0000");

        Assert.True(result);
        Assert.Equal(1f, styles.FontColor?.r);
        Assert.Equal(0f, styles.FontColor?.g);
        Assert.Equal(0f, styles.FontColor?.b);
    }

    [Fact]
    public void Styles_Set_ParsesColorName()
    {
        var styles = new Styles();
        var result = styles.Set("color", "red");

        Assert.True(result);
        Assert.Equal(Color.Red, styles.FontColor);
    }

    [Fact]
    public void Styles_Set_ParsesFlexDirection()
    {
        var styles = new Styles();
        var result = styles.Set("flex-direction", "column");

        Assert.True(result);
        Assert.Equal(FlexDirection.Column, styles.FlexDirection);
    }

    [Fact]
    public void Styles_Set_ParsesPadding()
    {
        var styles = new Styles();
        var result = styles.Set("padding", "10px");

        Assert.True(result);
        Assert.Equal(10, styles.PaddingTop?.Value);
        Assert.Equal(10, styles.PaddingRight?.Value);
        Assert.Equal(10, styles.PaddingBottom?.Value);
        Assert.Equal(10, styles.PaddingLeft?.Value);
    }

    [Fact]
    public void Color_Parse_HandlesShortHex()
    {
        var result = Color.Parse("#f00");

        Assert.NotNull(result);
        Assert.Equal(1f, result.Value.r);
        Assert.Equal(0f, result.Value.g);
        Assert.Equal(0f, result.Value.b);
    }

    [Fact]
    public void StyleSheetCollection_Parse_AppliesStyles()
    {
        var panel = new Panel();
        panel.StyleSheet.Parse(".panel { width: 100px; }");

        // The stylesheet should be added to the collection
        Assert.True(panel.AllStyleSheets.Any());
    }

    [Fact]
    public void Panel_WithStyleSheetAttribute_LoadsStylesheet()
    {
        // Test that StyleSheetAttribute can be read from a panel
        var type = typeof(TestPanelWithStyleSheet);
        var attrs = type.GetCustomAttributes(typeof(StyleSheetAttribute), false);
        
        Assert.NotEmpty(attrs);
        var styleAttr = (StyleSheetAttribute)attrs[0];
        Assert.Equal("test.scss", styleAttr.Name);
    }

    [Fact]
    public void Panel_WithSourceLocationAttribute_ReturnsPath()
    {
        var type = typeof(TestPanelWithSourceLocation);
        var attr = type.GetCustomAttribute(typeof(SourceLocationAttribute), false) as SourceLocationAttribute;
        
        Assert.NotNull(attr);
        Assert.Equal("/test/path/TestPanel.razor", attr.FilePath);
    }

    [Fact]
    public void Styles_Set_ParsesLinearGradient()
    {
        var styles = new Styles();
        var result = styles.Set("background", "linear-gradient(135deg, #007acc 0%, #005a9e 100%)");

        Assert.True(result);
        Assert.NotNull(styles.BackgroundGradient);
        Assert.True(styles.BackgroundGradient.Value.IsValid);
        Assert.Equal(GradientInfo.GradientTypes.Linear, styles.BackgroundGradient.Value.GradientType);
        Assert.Equal(2, styles.BackgroundGradient.Value.ColorOffsets.Length);
    }

    [Fact]
    public void Styles_Set_ParsesLinearGradientWithoutAngle()
    {
        var styles = new Styles();
        var result = styles.Set("background", "linear-gradient(#ff0000, #0000ff)");

        Assert.True(result);
        Assert.NotNull(styles.BackgroundGradient);
        Assert.True(styles.BackgroundGradient.Value.IsValid);
        Assert.Equal(2, styles.BackgroundGradient.Value.ColorOffsets.Length);
        // First color should be red
        Assert.Equal(1f, styles.BackgroundGradient.Value.ColorOffsets[0].color.r);
        Assert.Equal(0f, styles.BackgroundGradient.Value.ColorOffsets[0].color.g);
        // Second color should be blue
        Assert.Equal(0f, styles.BackgroundGradient.Value.ColorOffsets[1].color.r);
        Assert.Equal(0f, styles.BackgroundGradient.Value.ColorOffsets[1].color.g);
        Assert.Equal(1f, styles.BackgroundGradient.Value.ColorOffsets[1].color.b);
    }

    [Fact]
    public void Styles_Set_ParsesLinearGradientWithToDirection()
    {
        var styles = new Styles();
        var result = styles.Set("background", "linear-gradient(to right, red, blue)");

        Assert.True(result);
        Assert.NotNull(styles.BackgroundGradient);
        Assert.True(styles.BackgroundGradient.Value.IsValid);
        // "to right" should be 90 degrees = PI/2 radians
        Assert.True(Math.Abs(styles.BackgroundGradient.Value.Angle - (MathF.PI / 2f)) < 0.01f);
    }

    [Fact]
    public void Styles_Set_ParsesRadialGradient()
    {
        var styles = new Styles();
        var result = styles.Set("background", "radial-gradient(#ff0000, #0000ff)");

        Assert.True(result);
        Assert.NotNull(styles.BackgroundGradient);
        Assert.True(styles.BackgroundGradient.Value.IsValid);
        Assert.Equal(GradientInfo.GradientTypes.Radial, styles.BackgroundGradient.Value.GradientType);
    }

    [Fact]
    public void Styles_Set_BackgroundColor_ClearsGradient()
    {
        var styles = new Styles();
        
        // First set a gradient
        styles.Set("background", "linear-gradient(#ff0000, #0000ff)");
        Assert.NotNull(styles.BackgroundGradient);
        
        // Then set a solid color
        styles.Set("background", "#00ff00");
        Assert.Null(styles.BackgroundGradient);
        Assert.NotNull(styles.BackgroundColor);
        Assert.Equal(0f, styles.BackgroundColor.Value.r);
        Assert.Equal(1f, styles.BackgroundColor.Value.g);
        Assert.Equal(0f, styles.BackgroundColor.Value.b);
    }

    [Fact]
    public void StyleSheet_FromString_ParsesGradientRule()
    {
        var css = ".button { background: linear-gradient(135deg, #007acc 0%, #005a9e 100%); }";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Single(sheet.Nodes);
        Assert.NotNull(sheet.Nodes[0].Styles.BackgroundGradient);
        Assert.True(sheet.Nodes[0].Styles.BackgroundGradient.Value.IsValid);
    }

    [Fact]
    public void Styles_Set_ParsesTransition()
    {
        var styles = new Styles();
        var result = styles.Set("transition", "all 0.3s ease");
        
        Assert.True(result);
        Assert.NotNull(styles.Transitions);
        Assert.Single(styles.Transitions.List);
        Assert.Equal("all", styles.Transitions.List[0].Property);
        Assert.Equal(300, styles.Transitions.List[0].Duration); // 0.3s = 300ms
        Assert.Equal("ease", styles.Transitions.List[0].TimingFunction);
    }

    [Fact]
    public void Styles_Set_ParsesTransitionWithDelay()
    {
        var styles = new Styles();
        var result = styles.Set("transition", "opacity 0.5s 100ms linear");
        
        Assert.True(result);
        Assert.NotNull(styles.Transitions);
        Assert.Single(styles.Transitions.List);
        Assert.Equal("opacity", styles.Transitions.List[0].Property);
        Assert.Equal(500, styles.Transitions.List[0].Duration); // 0.5s = 500ms
        Assert.Equal(100, styles.Transitions.List[0].Delay); // 100ms
        Assert.Equal("linear", styles.Transitions.List[0].TimingFunction);
    }

    [Fact]
    public void Styles_Set_ParsesMultipleTransitions()
    {
        var styles = new Styles();
        var result = styles.Set("transition", "opacity 0.3s ease, transform 0.5s ease-in-out");
        
        Assert.True(result);
        Assert.NotNull(styles.Transitions);
        Assert.Equal(2, styles.Transitions.List.Count);
        Assert.Equal("opacity", styles.Transitions.List[0].Property);
        Assert.Equal("transform", styles.Transitions.List[1].Property);
    }

    [Fact]
    public void StyleSheet_FromString_ParsesTransitionRule()
    {
        var css = ".button { transition: background-color 0.2s ease; }";
        var sheet = StyleSheet.FromString(css);

        Assert.NotNull(sheet);
        Assert.Single(sheet.Nodes);
        Assert.True(sheet.Nodes[0].Styles.HasTransitions);
        Assert.Equal("background-color", sheet.Nodes[0].Styles.Transitions!.List[0].Property);
        Assert.Equal(200, sheet.Nodes[0].Styles.Transitions.List[0].Duration);
    }

    [Fact]
    public void Transitions_LerpsBackgroundColorCorrectly()
    {
        // Set up the time
        PanelRealTime.TimeNow = 1.0;
        PanelRealTime.TimeDelta = 0.016;

        // Create a panel with transitions
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);
        panel.AddClass("button");

        // Create stylesheet with normal and hover states
        var css = @"
            .button { 
                background-color: #FF0000; 
                transition: all 0.3s linear; 
            }
            .button:hover { 
                background-color: #00FF00; 
            }
        ";
        var sheet = StyleSheet.FromString(css);
        panel.StyleSheet.Add(sheet);

        // Initial layout - should have red background
        rootPanel.Layout();
        
        var initialColor = panel.ComputedStyle!.BackgroundColor;
        Assert.NotNull(initialColor);
        Assert.Equal(1.0f, initialColor.Value.r, 0.01);  // Red
        Assert.Equal(0.0f, initialColor.Value.g, 0.01);
        Assert.Equal(0.0f, initialColor.Value.b, 0.01);

        // Capture computed style before hover
        var beforeHoverColor = panel.ComputedStyle!.BackgroundColor;

        // Trigger hover
        PanelRealTime.Update(0.016);  // Advance time
        panel.Switch(PseudoClass.Hover, true);
        
        rootPanel.Layout();

        // On first frame after hover, should be interpolating (not black)
        var hoverColor = panel.ComputedStyle!.BackgroundColor;
        Assert.NotNull(hoverColor);
        
        // On this first frame after hover, the transition started at TimeNow - TimeDelta (1.0) and is evaluated at 1.016,
        // so the elapsed fraction is about 5% (0.016 / 0.3) and the color should be around (0.95, 0.05, 0)
        var debugInfo =
            $"Before hover: {beforeHoverColor?.ToString() ?? "null"}\n" +
            $"Hover frame 1: R={hoverColor.Value.r}, G={hoverColor.Value.g}, B={hoverColor.Value.b}, A={hoverColor.Value.a}\n" +
            $"TimeNow={PanelRealTime.TimeNow}, TimeDelta={PanelRealTime.TimeDelta}\n" +
            $"Transitions entries count: {panel.Transitions?.Entries?.Count ?? 0}";
        
        Assert.True(hoverColor.Value.r > 0.5f, $"Expected red > 0.5 but got {hoverColor.Value.r}. Debug: {debugInfo}");
        Assert.True(hoverColor.Value.a > 0.9f, $"Expected alpha > 0.9 but got {hoverColor.Value.a}");  // Should be mostly opaque
    }
}

/// <summary>
/// Test panel with StyleSheet attribute for testing
/// </summary>
[StyleSheet("test.scss")]
internal class TestPanelWithStyleSheet : Panel { }

/// <summary>
/// Test panel with empty StyleSheet attribute (auto-load behavior)
/// </summary>
[StyleSheet]
internal class TestPanelWithAutoStyleSheet : Panel { }

/// <summary>
/// Test panel with SourceLocation attribute for testing
/// </summary>
[SourceLocation("/test/path/TestPanel.razor", 1)]
internal class TestPanelWithSourceLocation : Panel { }

/// <summary>
/// Tests for SetTheme not overriding StyleSheet attributes
/// </summary>
public class SetThemeTests
{
    [Fact]
    public void IsStyleSheetFromAttribute_RecognizesAttributeLoadedSheets()
    {
        // Create a test stylesheet with a full path
        var testPath = "/home/test/test-attribute.scss";
        var testSheet = StyleSheet.FromString(".test { color: red; }", testPath);
        
        // Manually set the FileName since FromString doesn't set it
        var fileNameProp = typeof(StyleSheet).GetProperty("FileName");
        fileNameProp?.SetValue(testSheet, testPath);
        
        // Create a window and manually set up the loaded template stylesheets
        var window = new TestWindowForStyleSheetTracking();
        
        // Use reflection to set up the _loadedTemplateStylesheets list
        var templateListField = typeof(Panel).GetField("_loadedTemplateStylesheets",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = new List<string> { testPath };
        templateListField?.SetValue(window, list);
        
        // Verify the list was set
        var retrievedList = (List<string>?)templateListField?.GetValue(window);
        Assert.NotNull(retrievedList);
        Assert.Single(retrievedList);
        
        // Verify the stylesheet has the correct filename
        Assert.Equal(testPath, testSheet.FileName);
        
        // Test that IsStyleSheetFromAttribute recognizes this sheet
        var isFromAttrMethod = typeof(Panel).GetMethod("IsStyleSheetFromAttribute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)(isFromAttrMethod?.Invoke(window, new object[] { testSheet }) ?? false);
        
        Assert.True(result, $"Stylesheet should be recognized as loaded from attribute. Sheet filename: {testSheet.FileName}, Tracked list: {string.Join(", ", retrievedList)}");
    }
    
    [Fact]
    public void IsStyleSheetFromAttribute_RejectsNonAttributeSheets()
    {
        // Create a test stylesheet
        var testSheet = StyleSheet.FromString(".test { color: red; }", "theme.scss");
        
        // Create a window with no attribute stylesheets
        var window = new TestWindowForStyleSheetTracking();
        
        // Test that IsStyleSheetFromAttribute does NOT recognize this sheet
        var isFromAttrMethod = typeof(Panel).GetMethod("IsStyleSheetFromAttribute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (bool)(isFromAttrMethod?.Invoke(window, new object[] { testSheet }) ?? false);
        
        Assert.False(result, "Stylesheet should NOT be recognized as loaded from attribute");
    }
    
    [Fact]
    public void IsStyleSheetFromAttribute_DistinguishesSimilarPaths()
    {
        // Test that stylesheets with similar names but different paths are correctly distinguished
        var componentPath = "/project/component/MainWindow.scss";
        var themePath = "/project/theme/MainWindow.scss";
        
        // Create stylesheets with similar names but different paths
        var componentSheet = StyleSheet.FromString(".component { color: blue; }", componentPath);
        var themeSheet = StyleSheet.FromString(".theme { color: red; }", themePath);
        
        // Manually set the FileNames
        var fileNameProp = typeof(StyleSheet).GetProperty("FileName");
        fileNameProp?.SetValue(componentSheet, componentPath);
        fileNameProp?.SetValue(themeSheet, themePath);
        
        // Create a window and track only the component stylesheet
        var window = new TestWindowForStyleSheetTracking();
        var templateListField = typeof(Panel).GetField("_loadedTemplateStylesheets",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = new List<string> { componentPath };
        templateListField?.SetValue(window, list);
        
        var isFromAttrMethod = typeof(Panel).GetMethod("IsStyleSheetFromAttribute",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Component stylesheet should be recognized
        var componentResult = (bool)(isFromAttrMethod?.Invoke(window, new object[] { componentSheet }) ?? false);
        Assert.True(componentResult, 
            $"Component stylesheet should be recognized. Path: {componentPath}");
        
        // Theme stylesheet should NOT be recognized (different path, same filename)
        var themeResult = (bool)(isFromAttrMethod?.Invoke(window, new object[] { themeSheet }) ?? false);
        Assert.False(themeResult, 
            $"Theme stylesheet should NOT be recognized as it has a different path. Path: {themePath}");
    }
    
    [Fact]
    public void StyleSheetAttribute_WithoutName_UsesTypeName()
    {
        // Test that when StyleSheet attribute has no name, it should try to load TypeName.scss
        var type = typeof(TestPanelWithAutoStyleSheet);
        var attrs = type.GetCustomAttributes<StyleSheetAttribute>(false).ToList();
        
        Assert.NotEmpty(attrs);
        
        // The attribute should have an empty name (which triggers auto-loading)
        var attr = attrs[0];
        Assert.True(string.IsNullOrEmpty(attr.Name) || attr.Name == "",
            "StyleSheet attribute without parameter should have empty Name");
    }
}

/// <summary>
/// Test window for stylesheet tracking tests
/// </summary>
internal class TestWindowForStyleSheetTracking : Window { }
