using Sandbox.UI;
using Xunit;

namespace Fazor.Tests;

/// <summary>
/// Tests for the s&box Panel architecture
/// Validates Box and Panel functionality
/// </summary>
public class PanelArchitectureTests
{
    [Fact]
    public void Box_InitializesWithZeroRects()
    {
        // Arrange & Act
        var box = new Box();

        // Assert
        Assert.Equal(Rect.Zero, box.Rect);
        Assert.Equal(Rect.Zero, box.RectInner);
        Assert.Equal(Rect.Zero, box.RectOuter);
    }

    [Fact]
    public void Box_CanSetRectValues()
    {
        // Arrange
        var box = new Box();
        var testRect = new Rect(10, 20, 90, 180);

        // Act
        box.Rect = testRect;
        box.RectInner = new Rect(15, 25, 80, 170);
        box.RectOuter = new Rect(5, 15, 100, 190);

        // Assert
        Assert.Equal(testRect.Left, box.Rect.Left);
        Assert.Equal(15, box.RectInner.Left);
        Assert.Equal(5, box.RectOuter.Left);
    }

    [Fact]
    public void Panel_HasBoxProperty()
    {
        // Arrange & Act
        var panel = new Panel();

        // Assert
        Assert.NotNull(panel.Box);
        Assert.IsType<Box>(panel.Box);
    }

    [Fact]
    public void Panel_HasComputedStyle_Initially()
    {
        // Arrange & Act
        var panel = new Panel();

        // Assert
        // ComputedStyle is computed during PreLayout, initially null
        Assert.Null(panel.ComputedStyle);
    }

    [Fact]
    public void Panel_AddChild_AddsToChildren()
    {
        // Arrange
        var parent = new Panel();
        var child = new Panel();

        // Act
        parent.AddChild(child);

        // Assert
        Assert.Equal(1, parent.ChildrenCount);
        Assert.Equal(child, parent.Children.First());
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void Panel_AddClass_AddsToClasses()
    {
        // Arrange
        var panel = new Panel();

        // Act
        panel.AddClass("test-class");
        panel.AddClass("another-class");

        // Assert
        Assert.True(panel.HasClass("test-class"));
        Assert.True(panel.HasClass("another-class"));
    }

    [Fact]
    public void Panel_RemoveClass_RemovesFromClasses()
    {
        // Arrange
        var panel = new Panel();
        panel.AddClass("test-class");
        panel.AddClass("another-class");

        // Act
        panel.RemoveClass("test-class");

        // Assert
        Assert.False(panel.HasClass("test-class"));
        Assert.True(panel.HasClass("another-class"));
    }

    [Fact]
    public void Panel_HasClass_ChecksForClass()
    {
        // Arrange
        var panel = new Panel();
        panel.AddClass("test-class");

        // Act & Assert
        Assert.True(panel.HasClass("test-class"));
        Assert.False(panel.HasClass("nonexistent"));
    }

    [Fact]
    public void Panel_ToggleClass_TogglesClass()
    {
        // Arrange
        var panel = new Panel();

        // Act - add by toggle
        panel.ToggleClass("test-class");

        // Assert
        Assert.True(panel.HasClass("test-class"));

        // Act - remove by toggle
        panel.ToggleClass("test-class");

        // Assert
        Assert.False(panel.HasClass("test-class"));
    }

    [Fact]
    public void Panel_ComputedStyle_ReadOnly()
    {
        // Arrange
        var panel = new Panel();

        // Assert - ComputedStyle is initially null and is computed internally
        Assert.Null(panel.ComputedStyle);
        
        // ComputedStyle is read-only and computed during PreLayout
        // This test verifies the property exists and is accessible
        var style = panel.ComputedStyle;
        Assert.True(style == null || style is Styles);
    }

    [Fact]
    public void Panel_Children_InitiallyEmpty()
    {
        // Arrange & Act
        var panel = new Panel();

        // Assert
        Assert.False(panel.HasChildren);
        Assert.Equal(0, panel.ChildrenCount);
    }

    [Fact]
    public void Panel_ElementName_IsSetCorrectly()
    {
        // Arrange & Act
        var panel = new Panel { ElementName = "div" };

        // Assert
        Assert.Equal("div", panel.ElementName);
    }

    [Fact]
    public void Panel_IsVisible_DefaultsToTrue()
    {
        // Arrange & Act
        var panel = new Panel();

        // Assert
        Assert.True(panel.IsVisible);
    }

    [Fact]
    public void Panel_MultipleChildren_MaintainsHierarchy()
    {
        // Arrange
        var root = new Panel { ElementName = "div" };
        var header = new Panel { ElementName = "header" };
        var main = new Panel { ElementName = "main" };
        var footer = new Panel { ElementName = "footer" };

        // Act
        root.AddChild(header);
        root.AddChild(main);
        root.AddChild(footer);

        // Assert
        Assert.Equal(3, root.ChildrenCount);
        Assert.All(root.Children, child => Assert.Equal(root, child.Parent));
    }

    [Fact]
    public void Panel_DeepHierarchy_MaintainsParentChild()
    {
        // Arrange
        var grandparent = new Panel { ElementName = "div" };
        var parent = new Panel { ElementName = "section" };
        var child = new Panel { ElementName = "p" };

        // Act
        grandparent.AddChild(parent);
        parent.AddChild(child);

        // Assert
        Assert.Equal(parent, grandparent.Children.First());
        Assert.Equal(child, parent.Children.First());
        Assert.Equal(grandparent, parent.Parent);
        Assert.Equal(parent, child.Parent);
    }

    [Fact]
    public void Panel_Box_HasAllRectProperties()
    {
        // Arrange
        var panel = new Panel();

        // Assert - Box should have all three rect properties
        Assert.NotNull(panel.Box);
        Assert.Equal(Rect.Zero, panel.Box.Rect);
        Assert.Equal(Rect.Zero, panel.Box.RectInner);
        Assert.Equal(Rect.Zero, panel.Box.RectOuter);
        
        // Act - Set a rect value
        panel.Box.Rect = new Rect(0, 0, 100, 50);
        
        // Assert
        Assert.Equal(100, panel.Box.Rect.Width);
        Assert.Equal(50, panel.Box.Rect.Height);
    }
}
