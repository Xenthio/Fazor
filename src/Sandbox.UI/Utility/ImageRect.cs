using System.Numerics;

namespace Sandbox.UI;

/// <summary>
/// We share a lot of code between mask-image and background-image - so this handles all image rect calculations.
/// Ported from s&box: engine/Sandbox.Engine/Systems/UI/Utility/ImageRect.cs
/// </summary>
internal static class ImageRect
{
	public readonly record struct Result( Vector4 Rect, bool IsCover );

	public record struct Input
	{
		public Texture Image;

		public Rect PanelRect;

		public Length? ImageSizeX;
		public Length? ImageSizeY;
		public Length? ImagePositionX;
		public Length? ImagePositionY;

		/// <summary>
		/// Scale factor to convert pixel values to screen coordinates (typically 1.0 or DPI scale factor)
		/// </summary>
		public float ScaleToScreen;

		/// <summary>
		/// Default size to use when no size is specified (typically Length.Auto)
		/// </summary>
		public Length DefaultSize;
	}

	public static Result Calculate( Input input )
	{
		var x = 0.0f;
		var y = 0.0f;
		var w = (float)input.Image.Width;
		var h = (float)input.Image.Height;
		var sizeX = input.ImageSizeX ?? input.DefaultSize;
		var aspect = h / w;

		if ( input.ImageSizeX.HasValue && input.ImageSizeX.Value.Unit == LengthUnit.Undefined )
			sizeX = input.DefaultSize;

		bool cover = false;

		if ( sizeX.Unit == LengthUnit.Cover )
		{
			w = input.PanelRect.Width;
			h = w * aspect;

			if ( h < input.PanelRect.Height )
			{
				h = input.PanelRect.Height;
				w = h / aspect;
			}

			cover = true;
		}
		else if ( sizeX.Unit == LengthUnit.Contain )
		{
			w = input.PanelRect.Width;
			h = w * aspect;

			if ( h > input.PanelRect.Height )
			{
				h = input.PanelRect.Height;
				w = h / aspect;
			}
		}
		else if ( sizeX.Unit == LengthUnit.Auto )
		{
			// Already is I think
		}
		else if ( sizeX.Unit == LengthUnit.Pixels || sizeX.Unit == LengthUnit.Percentage )
		{
			w = input.ImageSizeX?.GetPixels( input.PanelRect.Width ) ?? w;
			h = input.ImageSizeY?.GetPixels( input.PanelRect.Height ) ?? h;

			// scale to screen
			if ( input.ImageSizeX?.Unit == LengthUnit.Pixels )
				w *= input.ScaleToScreen;

			if ( input.ImageSizeY?.Unit == LengthUnit.Pixels )
				h *= input.ScaleToScreen;
		}

		x = input.ImagePositionX?.GetPixels( input.PanelRect.Width ) ?? x;
		y = input.ImagePositionY?.GetPixels( input.PanelRect.Height ) ?? y;

		var res = new Result(
			new Vector4( x, y, w, h ),
			cover
		);

		return res;
	}
}
