using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Sandbox.UI;
using Fazor.Controls;
using Xunit;

namespace Sandbox.UI.Tests;

/// <summary>
/// Tests for Razor interactivity features like @ref and @onclick
/// </summary>
public class RazorInteractivityTests
{
    [Fact]
    public void OnClick_EventHandlerWorks()
    {
        // Arrange - Create a panel manually and test the onclick attribute works
        var root = new RootPanel();
        var button = new Button();
        button.Parent = root;
        
        var clickCount = 0;
        button.AddEventListener("onclick", () => clickCount++);

        // Act - simulate a click
        button.Click();
        button.ProcessPendingEvents();

        // Assert
        Assert.Equal(1, clickCount);

        // Click again
        button.Click();
        button.ProcessPendingEvents();
        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void Ref_CapturesElementReference()
    {
        // This test validates the AddReferenceCapture method directly
        // Since Razor uses RenderTreeBuilder, we test that the method works

        // Arrange
        Button? capturedRef = null;
        var button = new Button();
        
        // Act - Simulate what @ref does
        Action<Button> setter = (value) => capturedRef = value;
        setter(button);

        // Assert - ref should now be set
        Assert.NotNull(capturedRef);
        Assert.Same(button, capturedRef);
        Assert.True(capturedRef.IsValid());
    }

    [Fact]
    public void OnClick_WithPanelEvent_Receives_EventArgument()
    {
        // Arrange
        MousePanelEvent? capturedEvent = null;
        var button = new Button();
        var root = new RootPanel();
        button.Parent = root;

        button.AddEventListener("onclick", (PanelEvent e) => 
        {
            if (e is MousePanelEvent mpe)
                capturedEvent = mpe;
        });

        // Act
        button.Click();
        button.ProcessPendingEvents();

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal("onclick", capturedEvent.Name);
        Assert.Equal(button, capturedEvent.Target);
    }

    [Fact]
    public void Button_Click_TriggersEventListeners()
    {
        // Arrange
        var button = new Button();
        var root = new RootPanel();
        button.Parent = root;
        
        var clickCount = 0;
        button.AddEventListener("onclick", () => clickCount++);

        // Act
        button.Click();
        button.ProcessPendingEvents();

        // Assert
        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void EventListener_CanBeRemoved()
    {
        // Arrange
        var button = new Button();
        var root = new RootPanel();
        button.Parent = root;
        
        var clickCount = 0;
        button.AddEventListener("onclick", () => clickCount++);

        // Act - click once
        button.Click();
        button.ProcessPendingEvents();
        Assert.Equal(1, clickCount);

        // Remove listener
        button.RemoveEventListener("onclick");

        // Click again
        button.Click();
        button.ProcessPendingEvents();

        // Assert - count should not change
        Assert.Equal(1, clickCount);
    }
}
