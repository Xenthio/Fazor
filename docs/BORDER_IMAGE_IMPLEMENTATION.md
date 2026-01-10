# Border-Image and Background-Image Implementation

This document describes the implementation of CSS `border-image` and `background-image` properties in Fazor/Fazor, ported from s&box's UI system.

## Overview

The implementation adds full support for:
- `background-image: url("path/to/image.png")` - Background textures
- `border-image: url("path") slice / width repeat fill` - 9-slice border rendering

## CSS Properties Supported

### background-image
```scss
.element {
    background-image: url("path/to/texture.png");
}
```

Sets a background texture for the element. The existing `DrawBackgroundImage()` renderer method handles the rendering.

### border-image
```scss
.button {
    /* Full shorthand syntax */
    border-image: url("border.png") 10 / 5px stretch;
    
    /* Or set properties individually */
    border-image-width-left: 5px;
    border-image-width-right: 5px;
    border-image-width-top: 5px;
    border-image-width-bottom: 5px;
    border-image-fill: filled;  /* or unfilled */
    border-image-repeat: stretch;  /* or round */
    border-image-tint: rgba(255, 255, 255, 1);
}
```

## How Border-Image Works (9-Slice Rendering)

Border-image divides a source image into 9 sections:

```
┌─────┬─────────┬─────┐
│ TL  │   Top   │ TR  │  Corner sections
├─────┼─────────┼─────┤
│     │         │     │
│Left │ Center  │Right│  Edge sections  
│     │         │     │
├─────┼─────────┼─────┤
│ BL  │ Bottom  │ BR  │  Corner sections
└─────┴─────────┴─────┘
```

### Slice Parameters

The slice values define where to cut the source image:
- `border-image: url("img.png") 10 15 10 15` means:
  - Top slice: 10px from top
  - Right slice: 15px from right
  - Bottom slice: 10px from bottom  
  - Left slice: 15px from left

### Width Parameters

The width values (after `/`) define how wide to draw the borders:
- `border-image: url("img.png") 10 / 5px` means:
  - Slice the image at 10px from each edge
  - Draw the borders 5px wide on the element

### Rendering Algorithm

1. **Source Rectangles** (from image):
   - Top-left corner: `(0, 0, leftSlice, topSlice)`
   - Top edge: `(leftSlice, 0, width-rightSlice, topSlice)`
   - Top-right corner: `(width-rightSlice, 0, width, topSlice)`
   - Left edge: `(0, topSlice, leftSlice, height-bottomSlice)`
   - Center: `(leftSlice, topSlice, width-rightSlice, height-bottomSlice)`
   - Right edge: `(width-rightSlice, topSlice, width, height-bottomSlice)`
   - Bottom-left corner: `(0, height-bottomSlice, leftSlice, height)`
   - Bottom edge: `(leftSlice, height-bottomSlice, width-rightSlice, height)`
   - Bottom-right corner: `(width-rightSlice, height-bottomSlice, width, height)`

2. **Destination Rectangles** (on element):
   - Corners are drawn at their natural size
   - Edges are stretched/repeated based on `border-image-repeat`
   - Center is only drawn if `border-image-fill: filled`

## Implementation Files

### Parsing: `src/Sandbox.UI/Styles/Styles.Set.cs`

Added methods:
- `SetImage()` - Parses `url()` syntax and creates texture loaders
- `SetBorderImage()` - Parses full border-image shorthand
- `SetBorderTexture()` - Sets the border texture reference
- `SetBackgroundImageFromTexture()` - Sets background texture
- `GetTokenValueUnderParenthesis()` - Helper to extract function parameters

### Rendering: `src/Sandbox.UI.Skia/SkiaPanelRenderer.cs`

Added method:
- `DrawBorderImage()` - Implements 9-slice rendering using SkiaSharp

Modified method:
- `DrawBorder()` - Checks for border-image first, falls back to solid borders

## Example Usage

### Windows XP Style Button

```scss
.button {
    border-image: url("XGUI/Resources/XP/control.png") 5 / 5px;
    background-image: url("XGUI/Resources/XP/control_background.png");
    background-size: contain;
    background-position: center;
    
    &:hover {
        border-image: url("XGUI/Resources/XP/control_hover.png") 5 / 5px;
        background-image: url("XGUI/Resources/XP/control_background_hover.png");
    }
    
    &:active {
        border-image: url("XGUI/Resources/XP/control_active.png") 5 / 5px;
        background-image: url("XGUI/Resources/XP/control_background_active.png");
    }
}
```

### Custom Border

```scss
.panel {
    /* Use same slice width for all sides */
    border-image: url("border.png") 10 / 8px stretch;
    
    /* Or specify each side individually */
    border-image: url("border.png") 10 15 10 15 / 8px 12px 8px 12px;
}
```

## Testing

A test window `BorderImageTest.razor` has been created to demonstrate the feature. To run it:

```bash
cd examples/SimpleDesktopApp
dotnet run
```

Or in AI mode (headless):
```bash
AVALAZOR_AI_MODE=1 dotnet run
```

## Compatibility with s&box

This implementation is directly ported from s&box's UI system and maintains full compatibility:
- Same CSS syntax
- Same property names
- Same rendering algorithm
- Same 9-slice behavior

## Known Limitations

1. **Gradient Functions**: `linear-gradient()` and `radial-gradient()` in `background-image` are not yet implemented (s&box has these)
2. **Image Assets**: The XGUI themes reference image files that must be provided separately
3. **Repeat Modes**: Only `stretch` is currently active; `round` and other repeat modes may need further testing

## Future Enhancements

- [ ] Implement gradient generation (linear-gradient, radial-gradient, conic-gradient)
- [ ] Add support for multiple background images (layering)
- [ ] Implement border-image-repeat modes fully (round, space)
- [ ] Add border-image-slice with percentage values
- [ ] Support for animated border images

## References

- [CSS border-image Specification](https://developer.mozilla.org/en-US/docs/Web/CSS/border-image)
- [s&box UI System Source](https://github.com/Facepunch/sbox-public/tree/master/engine/Sandbox.Engine/Systems/UI)
- [9-Slice Scaling](https://en.wikipedia.org/wiki/9-slice_scaling)
