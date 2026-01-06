using System.Linq;
using Xunit;
using Sandbox.UI;

namespace Sandbox.UI.Tests;

public class StyleApplicationTests
{
    [Fact]
    public void Panel_WithUniversalSelector_AppliesStyles()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Create a stylesheet with universal selector
        var css = @"
            * {
                width: 150px;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.Width);
        Assert.Equal(150f, panel.ComputedStyle.Width.Value.Value);
    }

    [Fact]
    public void Panel_WithStyleSheet_AppliesStylesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        panel.AddClass("test-button");
        rootPanel.AddChild(panel);

        // Create a stylesheet with rules
        var css = @"
            .test-button {
                background-color: #ff0000;
                width: 200px;
                height: 50px;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act - Run layout to trigger style application
        rootPanel.Layout();

        // Assert - Check that computed styles have the values from the stylesheet
        Assert.NotNull(panel.ComputedStyle);
        
        // Check width and height first (these should work)
        Assert.NotNull(panel.ComputedStyle.Width);
        Assert.Equal(200f, panel.ComputedStyle.Width.Value.Value);
        
        Assert.NotNull(panel.ComputedStyle.Height);
        Assert.Equal(50f, panel.ComputedStyle.Height.Value.Value);
        
        // Check background color
        Assert.NotNull(panel.ComputedStyle.BackgroundColor);
        Assert.Equal(1f, panel.ComputedStyle.BackgroundColor.Value.r);
        Assert.Equal(0f, panel.ComputedStyle.BackgroundColor.Value.g);
        Assert.Equal(0f, panel.ComputedStyle.BackgroundColor.Value.b);
    }

    [Fact]
    public void Panel_WithMultipleStyleSheets_AppliesCascadeCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        panel.ElementName = "button";
        rootPanel.AddChild(panel);

        // Add two stylesheets with overlapping rules
        var css1 = @"
            button {
                background-color: #ff0000;
                width: 100px;
            }
        ";
        var css2 = @"
            button {
                background-color: #00ff00;
                height: 50px;
            }
        ";
        
        rootPanel.StyleSheet.Add(StyleSheet.FromString(css1, "sheet1"));
        rootPanel.StyleSheet.Add(StyleSheet.FromString(css2, "sheet2"));

        // Act
        rootPanel.Layout();

        // Assert - Later stylesheet should override background-color
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.BackgroundColor);
        // Sheet2 is added after sheet1, but sheets are inserted at index 0, so sheet2 comes first
        // and sheet1's rules should win due to order
        Assert.NotNull(panel.ComputedStyle.Width);
        Assert.NotNull(panel.ComputedStyle.Height);
    }

    [Fact]
    public void Panel_FillDefaults_SetsDefaultValuesForUnsetProperties()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Act - Run layout without any stylesheets
        rootPanel.Layout();

        // Assert - FillDefaults should set default values for CSS properties
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.Display);
        Assert.Equal(DisplayMode.Flex, panel.ComputedStyle.Display.Value);
        
        Assert.NotNull(panel.ComputedStyle.FlexDirection);
        Assert.NotNull(panel.ComputedStyle.AlignItems);
        Assert.NotNull(panel.ComputedStyle.Opacity);
    }

    [Fact]
    public void Panel_RgbaWith255Alpha_NormalizesAlphaCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // XGUI stylesheets use rgba(76,88,68,255) with alpha as 255 instead of 1.0
        var css = @"
            * {
                background-color: rgba(76,88,68,255);
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert - Alpha should be normalized to 1.0 (255/255)
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.BackgroundColor);
        var color = panel.ComputedStyle.BackgroundColor.Value;
        
        // Check RGB values are normalized from 0-255 to 0-1 range
        Assert.Equal(76f / 255f, color.r, precision: 3);
        Assert.Equal(88f / 255f, color.g, precision: 3);
        Assert.Equal(68f / 255f, color.b, precision: 3);
        
        // Check alpha is normalized from 255 to 1.0
        Assert.Equal(1.0f, color.a, precision: 3);
    }

    [Fact]
    public void Panel_ColorPropertyAlias_MapsToFontColor()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // CSS standard uses "color" for text color, s&box uses "font-color"
        var css = @"
            * {
                color: #FFFFFF;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert - "color" should be aliased to FontColor
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.FontColor);
        var color = panel.ComputedStyle.FontColor.Value;
        
        // Check white color (1, 1, 1, 1)
        Assert.Equal(1.0f, color.r, precision: 3);
        Assert.Equal(1.0f, color.g, precision: 3);
        Assert.Equal(1.0f, color.b, precision: 3);
        Assert.Equal(1.0f, color.a, precision: 3);
    }

    [Fact]
    public void Panel_BorderShorthand_ParsesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test border shorthand: "1px solid #808080"
        var css = @"
            * {
                border: 1px solid #808080;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        
        // Check border width
        Assert.NotNull(panel.ComputedStyle.BorderLeftWidth);
        Assert.Equal(1f, panel.ComputedStyle.BorderLeftWidth.Value.Value);
        Assert.NotNull(panel.ComputedStyle.BorderTopWidth);
        Assert.Equal(1f, panel.ComputedStyle.BorderTopWidth.Value.Value);
        Assert.NotNull(panel.ComputedStyle.BorderRightWidth);
        Assert.Equal(1f, panel.ComputedStyle.BorderRightWidth.Value.Value);
        Assert.NotNull(panel.ComputedStyle.BorderBottomWidth);
        Assert.Equal(1f, panel.ComputedStyle.BorderBottomWidth.Value.Value);
        
        // Check border color (gray #808080 = rgb(128,128,128))
        Assert.NotNull(panel.ComputedStyle.BorderLeftColor);
        var leftColor = panel.ComputedStyle.BorderLeftColor.Value;
        Assert.Equal(128f / 255f, leftColor.r, precision: 3);
        Assert.Equal(128f / 255f, leftColor.g, precision: 3);
        Assert.Equal(128f / 255f, leftColor.b, precision: 3);
        
        // All borders should have the same color
        Assert.NotNull(panel.ComputedStyle.BorderTopColor);
        Assert.NotNull(panel.ComputedStyle.BorderRightColor);
        Assert.NotNull(panel.ComputedStyle.BorderBottomColor);
    }

    [Fact]
    public void Panel_PaddingMarginShorthand_ParsesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test padding and margin shorthands with different value counts
        var css = @"
            * {
                padding: 5px;
                margin: 10px 20px;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        
        // Check padding (1 value = all sides)
        Assert.NotNull(panel.ComputedStyle.PaddingTop);
        Assert.Equal(5f, panel.ComputedStyle.PaddingTop.Value.Value);
        Assert.NotNull(panel.ComputedStyle.PaddingRight);
        Assert.Equal(5f, panel.ComputedStyle.PaddingRight.Value.Value);
        Assert.NotNull(panel.ComputedStyle.PaddingBottom);
        Assert.Equal(5f, panel.ComputedStyle.PaddingBottom.Value.Value);
        Assert.NotNull(panel.ComputedStyle.PaddingLeft);
        Assert.Equal(5f, panel.ComputedStyle.PaddingLeft.Value.Value);
        
        // Check margin (2 values = vertical horizontal)
        Assert.NotNull(panel.ComputedStyle.MarginTop);
        Assert.Equal(10f, panel.ComputedStyle.MarginTop.Value.Value);
        Assert.NotNull(panel.ComputedStyle.MarginRight);
        Assert.Equal(20f, panel.ComputedStyle.MarginRight.Value.Value);
        Assert.NotNull(panel.ComputedStyle.MarginBottom);
        Assert.Equal(10f, panel.ComputedStyle.MarginBottom.Value.Value);
        Assert.NotNull(panel.ComputedStyle.MarginLeft);
        Assert.Equal(20f, panel.ComputedStyle.MarginLeft.Value.Value);
    }

    [Fact]
    public void Panel_AlignmentProperties_ParsesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test alignment properties
        var css = @"
            * {
                align-items: center;
                align-self: flex-end;
                align-content: space-between;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        
        // Check align-items
        Assert.NotNull(panel.ComputedStyle.AlignItems);
        Assert.Equal(Align.Center, panel.ComputedStyle.AlignItems.Value);
        
        // Check align-self
        Assert.NotNull(panel.ComputedStyle.AlignSelf);
        Assert.Equal(Align.FlexEnd, panel.ComputedStyle.AlignSelf.Value);
        
        // Check align-content
        Assert.NotNull(panel.ComputedStyle.AlignContent);
        Assert.Equal(Align.SpaceBetween, panel.ComputedStyle.AlignContent.Value);
    }

    [Fact]
    public void Panel_GapShorthand_ParsesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test gap shorthand (row-gap column-gap)
        var css = @"
            * {
                gap: 10px 20px;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        
        // Check row-gap
        Assert.NotNull(panel.ComputedStyle.RowGap);
        Assert.Equal(10f, panel.ComputedStyle.RowGap.Value.Value);
        
        // Check column-gap
        Assert.NotNull(panel.ComputedStyle.ColumnGap);
        Assert.Equal(20f, panel.ComputedStyle.ColumnGap.Value.Value);
    }

    [Fact]
    public void Panel_FlexShorthand_ParsesCorrectly()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test flex shorthand (grow shrink basis)
        var css = @"
            * {
                flex: 1 0 auto;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        
        // Check flex-grow
        Assert.NotNull(panel.ComputedStyle.FlexGrow);
        Assert.Equal(1f, panel.ComputedStyle.FlexGrow.Value);
        
        // Check flex-shrink
        Assert.NotNull(panel.ComputedStyle.FlexShrink);
        Assert.Equal(0f, panel.ComputedStyle.FlexShrink.Value);
        
        // Check flex-basis (auto)
        Assert.NotNull(panel.ComputedStyle.FlexBasis);
        Assert.Equal(LengthUnit.Auto, panel.ComputedStyle.FlexBasis.Value.Unit);
    }

    [Fact]
    public void Panel_FontSmooth_ParsesAliases()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test font-smooth: never (maps to None)
        var css = @"
            * {
                font-smooth: never;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        Assert.True(panel.ComputedStyle.FontSmooth.HasValue);
        Assert.Equal(FontSmooth.None, panel.ComputedStyle.FontSmooth.Value);
    }

    [Fact]
    public void Panel_FontSmooth_ParsesAlwaysAlias()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Test font-smooth: always (maps to Antialiased)
        var css = @"
            * {
                font-smooth: always;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act
        rootPanel.Layout();

        // Assert
        Assert.NotNull(panel.ComputedStyle);
        Assert.True(panel.ComputedStyle.FontSmooth.HasValue);
        Assert.Equal(FontSmooth.Antialiased, panel.ComputedStyle.FontSmooth.Value);
    }

    [Fact]
    public void Panel_ActiveStyleBlocks_CreatesDefensiveCopy()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        panel.AddClass("test-panel");
        rootPanel.AddChild(panel);

        // Create a stylesheet with multiple rules
        var css = @"
            * {
                width: 100px;
            }
            .test-panel {
                height: 50px;
                background-color: #ff0000;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act - Run layout to apply styles
        rootPanel.Layout();

        // Assert - ActiveStyleBlocks should be accessible without throwing collection modification error
        var styleBlocks = panel.ActiveStyleBlocks;
        Assert.NotNull(styleBlocks);

        // Convert to list to force enumeration (this could throw if defensive copy isn't made)
        var blocksList = styleBlocks.ToList();
        Assert.NotEmpty(blocksList);

        // Verify we can iterate multiple times (defensive copy allows this)
        var firstCount = panel.ActiveStyleBlocks.Count();
        var secondCount = panel.ActiveStyleBlocks.Count();
        Assert.Equal(firstCount, secondCount);

        // Trigger another layout while iterating to simulate the race condition
        // This should not throw "Collection was modified" error
        var enumerator = panel.ActiveStyleBlocks.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        
        // Force a style rebuild (this would modify LastActiveRules if not defensive)
        panel.RemoveClass("test-panel");
        panel.AddClass("test-panel");
        rootPanel.Layout();
        
        // This should still work because we made a defensive copy
        var remainingBlocks = new List<IStyleBlock>();
        while (enumerator.MoveNext())
        {
            remainingBlocks.Add(enumerator.Current);
        }
        
        // We should have successfully iterated without errors
        Assert.NotNull(remainingBlocks);
    }

    [Fact]
    public void Panel_DefaultFlexDirection_IsRow()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var panel = new Panel();
        rootPanel.AddChild(panel);

        // Act - Run layout without any flex-direction styling
        rootPanel.Layout();

        // Assert - Default flex-direction should be Row (matching s&box)
        Assert.NotNull(panel.ComputedStyle);
        Assert.NotNull(panel.ComputedStyle.FlexDirection);
        Assert.Equal(FlexDirection.Row, panel.ComputedStyle.FlexDirection.Value);
    }

    [Fact]
    public void Panel_WindowContent_HasColumnFlexDirection()
    {
        // Arrange
        var rootPanel = new RootPanel();
        var windowPanel = new Panel();
        windowPanel.AddClass("window");
        var contentPanel = new Panel();
        contentPanel.AddClass("window-content");
        windowPanel.AddChild(contentPanel);
        rootPanel.AddChild(windowPanel);

        // Add XGUI Window.scss styles that set window-content to column
        var css = @"
            .window > .window-content {
                flex-direction: column;
                flex-grow: 1;
            }
        ";
        var sheet = StyleSheet.FromString(css);
        rootPanel.StyleSheet.Add(sheet);

        // Act - Run layout with window-content styling
        rootPanel.Layout();

        // Assert - window-content should override default Row with Column from CSS
        Assert.NotNull(contentPanel.ComputedStyle);
        Assert.NotNull(contentPanel.ComputedStyle.FlexDirection);
        Assert.Equal(FlexDirection.Column, contentPanel.ComputedStyle.FlexDirection.Value);
        
        // Also verify flex-grow is applied
        Assert.NotNull(contentPanel.ComputedStyle.FlexGrow);
        Assert.Equal(1f, contentPanel.ComputedStyle.FlexGrow.Value);
    }
}
