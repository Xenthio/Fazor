using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FakeOperatingSystem.Experiments.Ambitious.X86.Win32;

public static class ImageResourceUtils
{
	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
	public struct RgbQuad
	{
		public byte Blue;
		public byte Green;
		public byte Red;
		public byte Reserved; // Should be 0
	}

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
	public struct BitmapInfoHeader
	{
		public uint biSize;
		public int biWidth;
		public int biHeight;
		public ushort biPlanes;
		public ushort biBitCount;
		public uint biCompression;
		public uint biSizeImage;
		public int biXPelsPerMeter;
		public int biYPelsPerMeter;
		public uint biClrUsed;
		public uint biClrImportant;

		public const uint BI_RGB = 0;
		public const uint BI_RLE8 = 1;
		public const uint BI_RLE4 = 2;
		public const uint BI_BITFIELDS = 3;
		public const int StructSize = 40;
	}

	public static bool ParseDibAndConvertToRgba( byte[] dibData, bool isIconResource, out int outWidth, out int outVisualHeight, out byte[] rgbaData )
	{
		outWidth = 0;
		outVisualHeight = 0;
		rgbaData = null;

		if ( dibData == null || dibData.Length < BitmapInfoHeader.StructSize )
		{
			Log.Warning( "ParseDib: DIB data is null or too short for header." );
			return false;
		}

		try
		{
			using ( var ms = new MemoryStream( dibData ) )
			using ( var reader = new BinaryReader( ms ) )
			{
				BitmapInfoHeader bmiHeader = new BitmapInfoHeader();
				bmiHeader.biSize = reader.ReadUInt32();

				if ( bmiHeader.biSize < 12 )
				{
					Log.Warning( $"ParseDib: BITMAPINFOHEADER biSize field is {bmiHeader.biSize}. Attempting to proceed." );
				}

				ms.Position = 4;
				bmiHeader.biWidth = reader.ReadInt32();
				bmiHeader.biHeight = reader.ReadInt32();
				bmiHeader.biPlanes = reader.ReadUInt16();
				bmiHeader.biBitCount = reader.ReadUInt16();
				bmiHeader.biCompression = reader.ReadUInt32();
				bmiHeader.biSizeImage = reader.ReadUInt32();
				bmiHeader.biXPelsPerMeter = reader.ReadInt32();
				bmiHeader.biYPelsPerMeter = reader.ReadInt32();
				bmiHeader.biClrUsed = reader.ReadUInt32();
				bmiHeader.biClrImportant = reader.ReadUInt32();

				if ( bmiHeader.biSize > BitmapInfoHeader.StructSize )
				{
					long seekAmount = (long)bmiHeader.biSize - BitmapInfoHeader.StructSize;
					if ( ms.Position + seekAmount <= ms.Length )
					{
						ms.Seek( seekAmount, SeekOrigin.Current );
					}
					else
					{
						Log.Warning( "ParseDib: biSize indicates a larger header, but not enough data in stream to skip." );
						return false;
					}
				}

				outWidth = bmiHeader.biWidth;
				int dibReportedHeight = bmiHeader.biHeight;

				outVisualHeight = Math.Abs( dibReportedHeight );
				if ( isIconResource )
				{
					outVisualHeight /= 2;
				}

				bool isBottomUp = dibReportedHeight > 0;

				if ( outWidth <= 0 || outVisualHeight <= 0 || outWidth > 8192 || outVisualHeight > 8192 )
				{
					Log.Warning( $"ParseDib: Invalid or excessively large dimensions {outWidth}x{outVisualHeight} (DIB Height: {dibReportedHeight})." );
					return false;
				}

				if ( bmiHeader.biCompression != BitmapInfoHeader.BI_RGB )
				{
					Log.Warning( $"ParseDib: Unsupported DIB compression: {bmiHeader.biCompression}. Only BI_RGB (0) is supported." );
					rgbaData = CreatePlaceholderRgba( outWidth, outVisualHeight, $"Unsupported Compression {bmiHeader.biCompression}" );
					return true;
				}

				RgbQuad[] palette = null;
				if ( bmiHeader.biBitCount <= 8 )
				{
					int paletteSize = (int)bmiHeader.biClrUsed;
					if ( paletteSize == 0 && bmiHeader.biBitCount > 0 )
					{
						paletteSize = 1 << bmiHeader.biBitCount;
					}
					if ( paletteSize > 256 ) paletteSize = 256;

					if ( paletteSize > 0 )
					{
						palette = new RgbQuad[paletteSize];
						for ( int i = 0; i < paletteSize; i++ )
						{
							if ( ms.Position + 4 > ms.Length ) { Log.Warning( "ParseDib: Unexpected end of stream while reading palette." ); return false; }
							palette[i].Blue = reader.ReadByte();
							palette[i].Green = reader.ReadByte();
							palette[i].Red = reader.ReadByte();
							palette[i].Reserved = reader.ReadByte();
						}
					}
					else if ( bmiHeader.biBitCount <= 8 )
					{
						Log.Warning( $"ParseDib: Palette size is 0 for {bmiHeader.biBitCount}-bit image, this might be an issue." );
					}
				}

				int xorStride = ((outWidth * bmiHeader.biBitCount + 31) / 32) * 4;
				rgbaData = new byte[outWidth * outVisualHeight * 4];
				long xorPixelDataStartOffset = ms.Position;

				byte[] xorRowBuffer = new byte[xorStride];

				for ( int y = 0; y < outVisualHeight; y++ )
				{
					int dibRowY = y;
					int targetRowY = isBottomUp ? (outVisualHeight - 1 - y) : y;

					ms.Position = xorPixelDataStartOffset + (long)dibRowY * xorStride;
					if ( ms.Position + xorStride > ms.Length )
					{
						Log.Warning( $"ParseDib (XOR): Attempting to read row {dibRowY} (stride {xorStride}) past end of stream. Offset: {ms.Position}, Length: {ms.Length}" );
						for ( int fillY = y; fillY < outVisualHeight; fillY++ )
						{
							int fillTargetY = isBottomUp ? (outVisualHeight - 1 - fillY) : fillY;
							for ( int fillX = 0; fillX < outWidth; fillX++ )
							{
								int destIdxFill = (fillTargetY * outWidth + fillX) * 4;
								rgbaData[destIdxFill + 0] = 255; rgbaData[destIdxFill + 1] = 0; rgbaData[destIdxFill + 2] = 255; rgbaData[destIdxFill + 3] = 255;
							}
						}
						return true;
					}
					reader.Read( xorRowBuffer, 0, xorStride );

					for ( int x = 0; x < outWidth; x++ )
					{
						int destIdx = (targetRowY * outWidth + x) * 4;
						byte r = 0, g = 0, b = 0, a = 255; // Default to opaque

						switch ( bmiHeader.biBitCount )
						{
							case 1:
								int byteIndex1 = x / 8;
								int bitOffset1 = 7 - (x % 8);
								int monoIdx = (xorRowBuffer[byteIndex1] >> bitOffset1) & 1;
								if ( palette != null && monoIdx < palette.Length ) { r = palette[monoIdx].Red; g = palette[monoIdx].Green; b = palette[monoIdx].Blue; }
								break;
							case 4:
								int byteIndex4 = x / 2;
								int fourBitIdx = (x % 2 == 0) ? (xorRowBuffer[byteIndex4] >> 4) : (xorRowBuffer[byteIndex4] & 0x0F);
								if ( palette != null && fourBitIdx < palette.Length ) { r = palette[fourBitIdx].Red; g = palette[fourBitIdx].Green; b = palette[fourBitIdx].Blue; }
								break;
							case 8:
								byte eightBitIdx = xorRowBuffer[x];
								if ( palette != null && eightBitIdx < palette.Length ) { r = palette[eightBitIdx].Red; g = palette[eightBitIdx].Green; b = palette[eightBitIdx].Blue; }
								break;
							case 16:
								ushort pixel16 = (ushort)(xorRowBuffer[x * 2] | (xorRowBuffer[x * 2 + 1] << 8));
								r = (byte)((pixel16 & 0x7C00) >> 10 << 3); r |= (byte)(r >> 5);
								g = (byte)((pixel16 & 0x03E0) >> 5 << 3); g |= (byte)(g >> 5);
								b = (byte)((pixel16 & 0x001F) << 3); b |= (byte)(b >> 5);
								break;
							case 24:
								b = xorRowBuffer[x * 3 + 0];
								g = xorRowBuffer[x * 3 + 1];
								r = xorRowBuffer[x * 3 + 2];
								break;
							case 32:
								b = xorRowBuffer[x * 4 + 0];
								g = xorRowBuffer[x * 4 + 1];
								r = xorRowBuffer[x * 4 + 2];
								a = xorRowBuffer[x * 4 + 3]; // Alpha from XOR mask for 32bpp icons
								break;
							default:
								Log.Warning( $"ParseDib: Unexpected bit count in pixel loop: {bmiHeader.biBitCount}" );
								r = g = b = 128; a = 255;
								break;
						}
						rgbaData[destIdx + 0] = r;
						rgbaData[destIdx + 1] = g;
						rgbaData[destIdx + 2] = b;
						rgbaData[destIdx + 3] = a; // Alpha is initially from XOR (for 32bpp) or default 255
					}
				}

				// Process AND mask for icons (if not 32bpp with inherent alpha)
				if ( isIconResource && bmiHeader.biBitCount < 32 )
				{
					long andMaskDataStartOffset = xorPixelDataStartOffset + (long)outVisualHeight * xorStride;
					int andStride = ((outWidth * 1 + 31) / 32) * 4; // AND mask is 1bpp
					byte[] andRowBuffer = new byte[andStride];

					for ( int y = 0; y < outVisualHeight; y++ )
					{
						int dibRowY = y;
						int targetRowY = isBottomUp ? (outVisualHeight - 1 - y) : y;

						ms.Position = andMaskDataStartOffset + (long)dibRowY * andStride;
						if ( ms.Position + andStride > ms.Length )
						{
							Log.Warning( $"ParseDib (AND): Attempting to read row {dibRowY} (stride {andStride}) past end of stream. Offset: {ms.Position}, Length: {ms.Length}" );
							// If AND mask is short, assume remaining pixels are opaque (or handle as error)
							break;
						}
						reader.Read( andRowBuffer, 0, andStride );

						for ( int x = 0; x < outWidth; x++ )
						{
							int destIdx = (targetRowY * outWidth + x) * 4;
							int byteIndexAnd = x / 8;
							int bitOffsetAnd = 7 - (x % 8);
							int andBit = (andRowBuffer[byteIndexAnd] >> bitOffsetAnd) & 1;

							if ( andBit == 1 ) // Historically, AND mask 0=transparent, 1=opaque. Some sources say 0=draw, 1=don't draw screen.
											   // For RGBA, if AND bit is 1 (opaque part of mask), alpha should be 0.
											   // If AND bit is 0 (transparent part of mask), alpha from XOR (or 255) is used.
											   // Let's clarify: Standard ICO AND mask: 0 = opaque, 1 = transparent.
							{
								rgbaData[destIdx + 3] = 0; // Set alpha to 0 (transparent) if AND mask bit is 1
							}
							// If andBit is 0, the alpha set from XOR mask (or default 255) is kept.
						}
					}
				}
				// For 32bpp icons, the alpha channel is already in rgbaData[destIdx + 3] from the XOR mask.
				// The AND mask is often all zeros (meaning all opaque according to its own logic) for 32bpp icons
				// because transparency is handled by the alpha channel of the XOR mask.
				// If biBitCount is 32, we generally trust the alpha from the XOR data.

				return true;
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"ParseDib: Exception during DIB parsing. {ex.Message}\n{ex.StackTrace}" );
			return false;
		}
	}

	private static byte[] CreatePlaceholderRgba( int width, int height, string reason )
	{
		Log.Warning( $"ParseDib: Creating placeholder RGBA data for {width}x{height} due to: {reason}." );
		byte[] placeholderRgba = new byte[width * height * 4];
		for ( int y = 0; y < height; y++ )
		{
			for ( int x = 0; x < width; x++ )
			{
				int idx = (y * width + x) * 4;
				bool checker = ((x / 8) % 2 == (y / 8) % 2);
				placeholderRgba[idx + 0] = (byte)(checker ? 255 : 255);
				placeholderRgba[idx + 1] = (byte)(checker ? 0 : 255);
				placeholderRgba[idx + 2] = (byte)(checker ? 255 : 0);
				placeholderRgba[idx + 3] = 255;
			}
		}
		return placeholderRgba;
	}
}
