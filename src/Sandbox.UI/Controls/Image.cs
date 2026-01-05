namespace Sandbox.UI;

/// <summary>
/// A generic box that displays a given texture within itself.
/// Based on s&box's Image from engine/Sandbox.Engine/Systems/UI/Controls/Image.cs
/// </summary>
[Library("image"), Alias("img")]
public partial class Image : Panel
{
    /// <summary>
    /// The texture/image path being displayed by this panel
    /// </summary>
    public string? TexturePath { get; set; }

    public override bool HasContent => TexturePath != null;

    public Image()
    {
        YogaNode?.SetMeasureFunction(MeasureTexture);
    }

    /// <summary>
    /// Set the texture from a file path
    /// </summary>
    public virtual void SetTexture(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        TexturePath = name;
        YogaNode?.MarkDirty();
    }

    private float oldScaleToScreen = 1.0f;

    internal override void PreLayout(LayoutCascade cascade)
    {
        base.PreLayout(cascade);

        if (ScaleToScreen != oldScaleToScreen)
        {
            YogaNode?.MarkDirty();
        }
    }

    Vector2 MeasureTexture(YGNodeRef node, float width, YGMeasureMode widthMode, float height, YGMeasureMode heightMode)
    {
        // Default measurement - renderers should override this based on actual texture size
        // For now return a placeholder size
        if (string.IsNullOrEmpty(TexturePath))
            return new Vector2(0, 0);

        oldScaleToScreen = ScaleToScreen;

        // Try to get actual texture dimensions if available
        var textureSize = GetTextureSize(TexturePath);
        var defaultSize = textureSize.HasValue 
            ? new Vector2(textureSize.Value.width * ScaleToScreen, textureSize.Value.height * ScaleToScreen)
            : new Vector2(100 * ScaleToScreen, 100 * ScaleToScreen);

        var exact = YGMeasureMode.Exactly;
        var atMost = YGMeasureMode.AtMost;

        if (widthMode == exact && heightMode == exact)
            return new Vector2(width, height);

        if (widthMode == exact)
        {
            // Width fixed, scale height proportionally
            if (defaultSize.x > 0)
            {
                float aspectRatio = defaultSize.y / defaultSize.x;
                return new Vector2(width, width * aspectRatio);
            }
            return new Vector2(width, width); // Fallback to square if texture width is zero
        }

        if (heightMode == exact)
        {
            // Height fixed, scale width proportionally
            if (defaultSize.y > 0)
            {
                float aspectRatio = defaultSize.x / defaultSize.y;
                return new Vector2(height * aspectRatio, height);
            }
            return new Vector2(height, height); // Fallback to square if texture height is zero
        }

        if (widthMode == atMost && width < defaultSize.x)
        {
            if (defaultSize.x > 0)
            {
                float aspectRatio = defaultSize.y / defaultSize.x;
                return new Vector2(width, width * aspectRatio);
            }
            return new Vector2(width, width); // Fallback to square
        }

        if (heightMode == atMost && height < defaultSize.y)
        {
            if (defaultSize.y > 0)
            {
                float aspectRatio = defaultSize.x / defaultSize.y;
                return new Vector2(height * aspectRatio, height);
            }
            return new Vector2(height, height); // Fallback to square
        }

        return defaultSize;
    }

    /// <summary>
    /// Delegate to get texture dimensions - set by renderer
    /// </summary>
    public static Func<string, (int width, int height)?> TextureSizeFunc { get; set; }

    private (int width, int height)? GetTextureSize(string path)
    {
        if (TextureSizeFunc != null)
        {
            return TextureSizeFunc(path);
        }
        return null;
    }

    public override void DrawContent(ref RenderState state)
    {
        // Actual image drawing is handled by renderer implementation
    }

    public override void SetProperty(string name, string value)
    {
        if (name == "src")
            SetTexture(value);
    }
}
