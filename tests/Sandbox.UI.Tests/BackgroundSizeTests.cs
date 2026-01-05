using Sandbox.UI;
using System;
using Xunit;

namespace Sandbox.UI.Tests;

/// <summary>
/// Test that background-size and background-position CSS properties are parsed and applied correctly
/// </summary>
public class BackgroundSizeTests
{
    [Fact]
    public void BackgroundSize_ParsesPixelValue()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "15px");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.NotNull(styles.BackgroundSizeY);
        Assert.Equal(15.0f, styles.BackgroundSizeX.Value.Value);
        Assert.Equal(LengthUnit.Pixels, styles.BackgroundSizeX.Value.Unit);
    }
    
    [Fact]
    public void BackgroundSize_ParsesTwoValues()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "15px 20px");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.NotNull(styles.BackgroundSizeY);
        Assert.Equal(15.0f, styles.BackgroundSizeX.Value.Value);
        Assert.Equal(20.0f, styles.BackgroundSizeY.Value.Value);
    }
    
    [Fact]
    public void BackgroundSize_ParsesCoverKeyword()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "cover");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.Equal(LengthUnit.Cover, styles.BackgroundSizeX.Value.Unit);
    }
    
    [Fact]
    public void BackgroundSize_ParsesContainKeyword()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "contain");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.Equal(LengthUnit.Contain, styles.BackgroundSizeX.Value.Unit);
    }
    
    [Fact]
    public void BackgroundPosition_ParsesPixelValues()
    {
        var styles = new Styles();
        var success = styles.Set("background-position", "10px 20px");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundPositionX);
        Assert.NotNull(styles.BackgroundPositionY);
        Assert.Equal(10.0f, styles.BackgroundPositionX.Value.Value);
        Assert.Equal(20.0f, styles.BackgroundPositionY.Value.Value);
    }
    
    [Fact]
    public void BackgroundPosition_ParsesSingleValue()
    {
        var styles = new Styles();
        var success = styles.Set("background-position", "5px");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundPositionX);
        Assert.NotNull(styles.BackgroundPositionY);
        // Both X and Y should be set to the same value
        Assert.Equal(5.0f, styles.BackgroundPositionX.Value.Value);
        Assert.Equal(5.0f, styles.BackgroundPositionY.Value.Value);
    }
    
    [Fact]
    public void BackgroundSize_ParsesPercentage()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "50%");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.NotNull(styles.BackgroundSizeY);
        Assert.Equal(50.0f, styles.BackgroundSizeX.Value.Value);
        Assert.Equal(LengthUnit.Percentage, styles.BackgroundSizeX.Value.Unit);
    }
    
    [Fact]
    public void BackgroundSize_ParsesAutoKeyword()
    {
        var styles = new Styles();
        var success = styles.Set("background-size", "auto");
        
        Assert.True(success);
        Assert.NotNull(styles.BackgroundSizeX);
        Assert.Equal(LengthUnit.Auto, styles.BackgroundSizeX.Value.Unit);
    }
}
