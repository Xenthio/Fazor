using Sandbox.UI;
using Fazor.Controls;
using Sandbox.UI.AI;
using Xunit;

namespace Sandbox.UI.AI.Tests;

/// <summary>
/// Tests for the AI-focused panel renderer
/// </summary>
public class AIPanelRendererTests
{
    [Fact]
    public void AIPanelRenderer_CreatesSuccessfully()
    {
        var renderer = new AIPanelRenderer();
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AIPanelRenderer_EnsureInitialized_ReturnsTrue()
    {
        var result = AIPanelRenderer.EnsureInitialized();
        Assert.True(result);
    }

    [Fact]
    public void AIPanelRenderer_MeasureText_ReturnsNonZero()
    {
        var renderer = new AIPanelRenderer();
        var size = renderer.MeasureText("Hello", "Arial", 16, 400);
        
        Assert.True(size.x > 0);
        Assert.True(size.y > 0);
    }

    [Fact]
    public void AIPanelRenderer_RenderEmptyRootPanel_ProducesOutput()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        renderer.Render(rootPanel);
        var output = renderer.LastOutput;

        Assert.NotNull(output);
        Assert.NotEmpty(output);
        Assert.Contains("UI STATE SNAPSHOT", output);
        Assert.Contains("PANEL TREE", output);
    }

    [Fact]
    public void AIPanelRenderer_RenderWithLabel_IncludesText()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var label = new Label("Test Label Text");
        rootPanel.AddChild(label);
        rootPanel.Layout();

        renderer.Render(rootPanel);
        var output = renderer.LastOutput;

        Assert.Contains("Test Label Text", output);
    }

    [Fact]
    public void AIPanelRenderer_RenderWithClasses_IncludesClasses()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        panel.AddClass("test-class");
        panel.AddClass("another-class");
        rootPanel.AddChild(panel);
        rootPanel.Layout();

        renderer.Render(rootPanel);
        var output = renderer.LastOutput;

        Assert.Contains("test-class", output);
        Assert.Contains("another-class", output);
    }

    [Fact]
    public void AIPanelRenderer_HitTest_FindsPanel()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        panel.Style.Width = 100;
        panel.Style.Height = 100;
        panel.Style.Left = 50;
        panel.Style.Top = 50;
        rootPanel.AddChild(panel);
        rootPanel.Layout();

        var hit = renderer.HitTest(rootPanel, 75, 75);

        Assert.NotNull(hit);
        Assert.Equal(panel, hit);
    }

    [Fact]
    public void AIPanelRenderer_HitTest_MissesOutsidePanel()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        panel.Style.Width = 100;
        panel.Style.Height = 100;
        panel.Style.Left = 50;
        panel.Style.Top = 50;
        panel.Style.Position = PositionMode.Absolute;
        rootPanel.AddChild(panel);
        rootPanel.Layout();

        var hit = renderer.HitTest(rootPanel, 10, 10);

        // Should hit the root panel, not the child
        Assert.Equal(rootPanel, hit);
    }

    [Fact]
    public void AIPanelRenderer_GetInteractiveElementsSummary_IncludesButtons()
    {
        var renderer = new AIPanelRenderer();
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var button = new Button("Click Me");
        rootPanel.AddChild(button);
        rootPanel.Layout();

        var summary = renderer.GetInteractiveElementsSummary(rootPanel);

        Assert.Contains("BUTTON", summary);
        Assert.Contains("INTERACTIVE ELEMENTS", summary);
    }

    [Fact]
    public void AIPanelRenderer_IncludeStyles_CanBeDisabled()
    {
        var renderer = new AIPanelRenderer();
        renderer.IncludeStyles = false;
        
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        renderer.Render(rootPanel);
        var output = renderer.LastOutput;

        // With styles disabled, shouldn't see style output
        Assert.DoesNotContain("Styles:", output);
    }

    [Fact]
    public void AIPanelRenderer_MaxDepth_LimitsOutput()
    {
        var renderer = new AIPanelRenderer();
        renderer.MaxDepth = 2; // Root, its children, and grandchildren
        
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var parent = new Panel();
        parent.AddClass("parent");
        var child = new Panel();
        child.AddClass("child");
        var grandchild = new Panel();
        grandchild.AddClass("grandchild");
        
        child.AddChild(grandchild);
        parent.AddChild(child);
        rootPanel.AddChild(parent);
        rootPanel.Layout();

        renderer.Render(rootPanel);
        var output = renderer.LastOutput;

        // Should have parent and child in the output
        Assert.Contains("parent", output);
        Assert.Contains("child", output);
    }
}

/// <summary>
/// Tests for the AIHelper static helper class
/// </summary>
public class AIHelperTests
{
    [Fact]
    public void AIHelper_Renderer_CreatesInstance()
    {
        var renderer = AIHelper.Renderer;
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AIHelper_Snapshot_ProducesOutput()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        var snapshot = AIHelper.Snapshot(rootPanel);

        Assert.NotNull(snapshot);
        Assert.NotEmpty(snapshot);
        Assert.Contains("UI STATE SNAPSHOT", snapshot);
    }

    [Fact]
    public void AIHelper_SnapshotStructure_OmitsStylesAndLayout()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        var snapshot = AIHelper.SnapshotStructure(rootPanel);

        Assert.NotNull(snapshot);
        Assert.DoesNotContain("Styles:", snapshot);
        Assert.DoesNotContain("Layout:", snapshot);
    }

    [Fact]
    public void AIHelper_GetInteractiveElements_ReturnsInfo()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var button = new Button("Test Button");
        rootPanel.AddChild(button);
        rootPanel.Layout();

        var interactive = AIHelper.GetInteractiveElements(rootPanel);

        Assert.Contains("INTERACTIVE ELEMENTS", interactive);
        Assert.Contains("BUTTON", interactive);
    }

    [Fact]
    public void AIHelper_WhatIsAt_ReturnsInfo()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        var info = AIHelper.WhatIsAt(rootPanel, 100, 100);

        Assert.NotNull(info);
        Assert.NotEmpty(info);
    }

    [Fact]
    public void AIHelper_FindByClass_FindsPanel()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        panel.AddClass("my-test-class");
        rootPanel.AddChild(panel);
        rootPanel.Layout();

        var results = AIHelper.FindByClass(rootPanel, "my-test-class");

        Assert.Single(results);
        Assert.Equal(panel, results[0].Panel);
    }

    [Fact]
    public void AIHelper_FindByElement_FindsPanel()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var label = new Label("Test");
        rootPanel.AddChild(label);
        rootPanel.Layout();

        var results = AIHelper.FindByElement(rootPanel, "label");

        Assert.NotEmpty(results);
    }

    [Fact]
    public void AIHelper_FindById_FindsPanel()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        panel.Id = "my-unique-id";
        rootPanel.AddChild(panel);
        rootPanel.Layout();

        var result = AIHelper.FindById(rootPanel, "my-unique-id");

        Assert.NotNull(result);
        Assert.Equal(panel, result.Panel);
    }

    [Fact]
    public void AIHelper_QuickSummary_ProducesOverview()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var panel = new Panel();
        var label = new Label("Test");
        rootPanel.AddChild(panel);
        rootPanel.AddChild(label);
        rootPanel.Layout();

        var summary = AIHelper.QuickSummary(rootPanel);

        Assert.Contains("QUICK UI SUMMARY", summary);
        Assert.Contains("Viewport:", summary);
        Assert.Contains("Total panels:", summary);
    }

    [Fact]
    public void AIHelper_DescribeClick_ReturnsInfo()
    {
        var rootPanel = new RootPanel();
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        
        var button = new Button("Test Button");
        button.Style.Width = 100;
        button.Style.Height = 50;
        rootPanel.AddChild(button);
        rootPanel.Layout();

        var info = AIHelper.DescribeClick(rootPanel, 50, 25);

        Assert.Contains("Click at", info);
    }

    [Fact]
    public void PanelInfo_ToString_FormatsCorrectly()
    {
        var panel = new Panel();
        panel.AddClass("test-class");
        panel.Style.Width = 100;
        panel.Style.Height = 50;

        var rootPanel = new RootPanel();
        rootPanel.AddChild(panel);
        rootPanel.PanelBounds = new Rect(0, 0, 800, 600);
        rootPanel.Layout();

        var info = new PanelInfo(panel);
        var str = info.ToString();

        Assert.Contains("panel", str);
        Assert.Contains("test-class", str);
    }
}
