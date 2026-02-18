namespace Sandbox.UI
{
	public partial class Styles
	{
		/// <summary>
		/// Override to handle CSS property aliases and shorthand properties
		/// Based on s&box's Styles.Set.cs
		/// </summary>
		public override bool Set( string property, string value )
		{
			// Use property alias lookup like s&box does
			property = StyleParser.GetPropertyFromAlias( property );

			// Handle border shorthand properties and other common CSS properties
			switch ( property )
			{
				case "transition":
				case "transition-delay":
				case "transition-duration":
				case "transition-property":
				case "transition-timing-function":
					Transitions = TransitionDesc.ParseProperty( property, value, Transitions );
					return true;

				case "display":
					return SetDisplay( value );

				case "pointer-events":
					return SetPointerEvents( value );

				case "position":
					return SetPosition( value );

				case "flex-direction":
					return SetFlexDirection( value );

				case "justify-content":
					return SetJustifyContent( value );

				case "flex-wrap":
					return SetFlexWrap( value );

				case "flex":
					return SetFlex( value );

				case "gap":
					return SetGap( value );

				case "padding":
					return SetPadding( value );

				case "margin":
					return SetMargin( value );

				case "border-radius":
					return SetBorderRadius( value );

				case "border":
					return SetBorder( value, w => BorderWidth = w, c => BorderColor = c );

				case "border-left":
					return SetBorder( value, w => BorderLeftWidth = w, c => BorderLeftColor = c );

				case "border-right":
					return SetBorder( value, w => BorderRightWidth = w, c => BorderRightColor = c );

				case "border-top":
					return SetBorder( value, w => BorderTopWidth = w, c => BorderTopColor = c );

				case "border-bottom":
					return SetBorder( value, w => BorderBottomWidth = w, c => BorderBottomColor = c );

				case "border-image":
					return SetBorderImage( value );

				case "border-color":
					{
						var borderColor = Color.Parse( value );
						BorderColor = borderColor;
						return borderColor.HasValue;
					}

				case "border-width":
					{
						var borderWidth = Length.Parse( value );
						BorderWidth = borderWidth;
						return borderWidth.HasValue;
					}

				case "backdrop-filter":
					return SetBackdropFilter( value );

				case "filter":
					return SetFilter( value );

				case "font-weight":
					return SetFontWeight( value );

				case "box-shadow":
					return SetShadow( value, ref BoxShadow );

				case "text-shadow":
					return SetShadow( value, ref TextShadow );

				case "filter-drop-shadow":
					return SetShadow( value, ref FilterDropShadow );

				case "align-content":
					AlignContent = GetAlign( value );
					return AlignContent.HasValue;

				case "align-self":
					AlignSelf = GetAlign( value );
					return AlignSelf.HasValue;

				case "align-items":
					AlignItems = GetAlign( value );
					return AlignItems.HasValue;

				case "text-align":
					return SetTextAlign( value );

				case "text-overflow":
					return SetTextOverflow( value );

				case "text-filter":
					return SetTextFilter( value );

				case "word-break":
					return SetWordBreak( value );

				case "text-decoration":
					return SetTextDecoration( value );

				case "text-decoration-line":
					return SetTextDecorationLine( value );

				case "text-decoration-skip-ink":
					return SetTextDecorationSkipInk( value );

				case "text-decoration-style":
					return SetTextDecorationStyle( value );

				case "text-stroke":
					return SetTextStroke( value );

				case "text-transform":
					return SetTextTransform( value );

				case "font-style":
					return SetFontStyle( value );

				case "white-space":
					return SetWhiteSpace( value );

				case "transform":
					return SetTransform( value );

				case "transform-origin":
					return SetTransformOrigin( value );

				case "perspective-origin":
					return SetPerspectiveOrigin( value );

				case "background":
					return SetBackground( value );

				case "background-image":
					return SetImage( value, SetBackgroundImageFromTexture, SetBackgroundSize, SetBackgroundRepeat, SetBackgroundAngle );

				case "background-size":
					return SetBackgroundSize( value );

				case "background-position":
					return SetBackgroundPosition( value );

				case "background-repeat":
					return SetBackgroundRepeat( value );

				case "image-rendering":
					return SetImageRendering( value );

				case "font-color":
					return SetFontColor( value );

				case "caret-color":
					return SetCaretColor( value );

				case "animation-iteration-count":
					if ( value == "infinite" )
					{
						AnimationIterationCount = float.PositiveInfinity;
						return true;
					}
					break;

				case "animation":
					return SetAnimation( value );

				case "mask":
					return SetMask( value );

				case "mask-image":
					return SetImage( value, SetMaskImageFromTexture, SetMaskSize, SetMaskRepeat, SetMaskAngle );

				case "mask-mode":
					return SetMaskMode( value );

				case "mask-size":
					return SetMaskSize( value );

				case "mask-repeat":
					return SetMaskRepeat( value );

				case "mask-position":
					return SetMaskPosition( value );

				case "mask-scope":
					return SetMaskScope( value );

				case "font-smooth":
					return SetFontSmooth( value );

				case "object-fit":
					return SetObjectFit( value );
			}

			return base.Set( property, value );
		}

		/// <summary>
		/// Parse border shorthand syntax: "1px solid #808080" or "2px dashed red"
		/// Based on s&box's SetBorder implementation
		/// </summary>
		private bool SetBorder( string value, System.Action<Length?> setWidth, System.Action<Color?> setColor )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();

			while ( !p.IsEnd )
			{
				if ( p.TryReadLineStyle( out var lineStyle ) )
				{
					if ( lineStyle == "none" )
					{
						setWidth( Length.Pixels( 0 ) );
						return true;
					}
				}
				else if ( p.TryReadLength( out var lengthValue ) )
				{
					setWidth( lengthValue );
				}
				else if ( p.TryReadColor( out var colorValue ) )
				{
					setColor( colorValue );
				}
				else
				{
					return false;
				}

				p = p.SkipWhitespaceAndNewlines();
			}

			return true;
		}

		/// <summary>
		/// Parse padding shorthand: "5px" or "5px 10px" or "5px 10px 15px 20px"
		/// Based on s&box's SetPadding implementation
		/// </summary>
		private bool SetPadding( string value )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return false;

			if ( p.TryReadLength( out var a ) )
			{
				Padding = a;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var b ) )
			{
				PaddingLeft = b;
				PaddingRight = b;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var c ) )
			{
				PaddingBottom = c;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var d ) )
			{
				PaddingTop = a;
				PaddingRight = b;
				PaddingBottom = c;
				PaddingLeft = d;
			}

			return true;
		}

		/// <summary>
		/// Parse margin shorthand: "5px" or "5px 10px" or "5px 10px 15px 20px"
		/// Based on s&box's SetMargin implementation
		/// </summary>
		private bool SetMargin( string value )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return false;

			if ( p.TryReadLength( out var a ) )
			{
				MarginLeft = a;
				MarginTop = a;
				MarginRight = a;
				MarginBottom = a;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var b ) )
			{
				MarginLeft = b;
				MarginRight = b;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var c ) )
			{
				MarginBottom = c;
			}

			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return true;

			if ( p.TryReadLength( out var d ) )
			{
				MarginTop = a;
				MarginRight = b;
				MarginBottom = c;
				MarginLeft = d;
			}

			return true;
		}

		/// <summary>
		/// Parse alignment values for align-items, align-self, align-content
		/// Based on s&box's GetAlign implementation
		/// </summary>
		private Align? GetAlign( string value )
		{
			switch ( value )
			{
				case "auto": return Align.Auto;
				case "flex-end": return Align.FlexEnd;
				case "flex-start": return Align.FlexStart;
				case "center": return Align.Center;
				case "stretch": return Align.Stretch;
				case "space-between": return Align.SpaceBetween;
				case "space-around": return Align.SpaceAround;
				case "space-evenly": return Align.SpaceEvenly;
				case "baseline": return Align.Baseline;
				default:
					return null;
			}
		}

		/// <summary>
		/// Parse gap shorthand: "10px" (both row and column) or "10px 20px" (row column)
		/// Based on s&box's SetGap implementation
		/// </summary>
		private bool SetGap( string value )
		{
			var parts = value.Split( new[] { ' ', ',' }, System.StringSplitOptions.RemoveEmptyEntries );

			if ( parts.Length == 0 )
				return false;

			var gap = Length.Parse( parts[0] );
			if ( !gap.HasValue )
				return false;

			RowGap = gap;
			ColumnGap = gap;

			if ( parts.Length > 1 )
			{
				var colGap = Length.Parse( parts[1] );
				if ( colGap.HasValue )
					ColumnGap = colGap;
			}

			return true;
		}

		/// <summary>
		/// Parse flex shorthand: "1" (flex-grow) or "1 0" (grow shrink) or "1 0 auto" (grow shrink basis)
		/// Also supports "none", "auto", "initial" keywords
		/// Based on s&box's SetFlex implementation
		/// </summary>
		private bool SetFlex( string value )
		{
			/*
			 * flex: none | [ <'flex-grow'> <'flex-shrink'>? || <'flex-basis'> ]
			 * https://drafts.csswg.org/css-flexbox/#flex-property
			 */

			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			int floatCount = 0;

			while ( !p.IsEnd )
			{
				var word = p.ReadWord( " ", true ).ToLower();
				p.Pointer -= word.Length;

				if ( word == "none" )
				{
					// "none" expands to 0 0 auto
					FlexShrink ??= 0;
					FlexGrow ??= 0;
					FlexBasis = Length.Auto;

					return true;
				}
				else if ( word == "auto" )
				{
					// "auto" expands to 1 1 auto
					FlexShrink ??= 1;
					FlexGrow ??= 1;
					FlexBasis = Length.Auto;

					return true;
				}
				else if ( word == "initial" )
				{
					// "initial" expands to 0 1 auto
					FlexShrink ??= 0;
					FlexGrow ??= 1;
					FlexBasis = Length.Auto;

					return true;
				}
				else
				{
					var maybeLength = p;
					var maybeFloat = p.ReadUntilWhitespaceOrNewlineOrEnd();

					// TryReadFloat eats lengths, TryReadLength eats floats
					// settle it with this
					if ( float.TryParse( maybeFloat, out float val ) )
					{
						if ( floatCount == 0 )
						{
							FlexGrow = val;

							// "flex: 1" expands to <number [1]> 1 0
							if ( val == 1 )
							{
								FlexShrink = 1;
								FlexBasis = 0;
							}
						}
						else
						{
							FlexShrink = val;
						}

						floatCount++;
					}
					else if ( maybeLength.TryReadLength( out var len ) )
					{
						FlexGrow ??= 0;
						FlexShrink ??= 1;
						FlexBasis = len;
						return true;
					}
					else
					{
						return false;
					}
				}

				p.SkipWhitespaceAndNewlines();
			}

			return true;
		}

		/// <summary>
		/// Parse font-smooth values including never/always aliases
		/// Based on s&box's font-smooth handling
		/// </summary>
		private bool SetFontSmooth( string value )
		{
			switch ( value )
			{
				case "never":
				case "none":
					FontSmooth = UI.FontSmooth.None;
					return true;

				case "always":
				case "antialiased":
					FontSmooth = UI.FontSmooth.Antialiased;
					return true;

				case "grayscale-antialiased":
					FontSmooth = UI.FontSmooth.GrayscaleAntialiased;
					return true;

				case "auto":
					FontSmooth = UI.FontSmooth.Auto;
					return true;

				default:
					return false;
			}
		}

		/// <summary>
		/// Parse flex-direction values
		/// Based on s&box's SetFlexDirection implementation
		/// </summary>
		private bool SetFlexDirection( string value )
		{
			switch ( value )
			{
				case "column":
					FlexDirection = UI.FlexDirection.Column;
					return true;
				case "column-reverse":
					FlexDirection = UI.FlexDirection.ColumnReverse;
					return true;
				case "row":
					FlexDirection = UI.FlexDirection.Row;
					return true;
				case "row-reverse":
					FlexDirection = UI.FlexDirection.RowReverse;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse border-radius shorthand
		/// Based on s&box's SetBorderRadius implementation
		/// </summary>
		private bool SetBorderRadius( string value )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();

			if ( p.IsEnd )
				return false;

			if ( !p.TryReadLength( out var a ) )
				return false;

			if ( p.IsEnd || !p.TryReadLength( out var b ) )
			{
				BorderTopLeftRadius = a;
				BorderTopRightRadius = a;
				BorderBottomRightRadius = a;
				BorderBottomLeftRadius = a;
				return true;
			}

			if ( p.IsEnd || !p.TryReadLength( out var c ) )
			{
				BorderTopLeftRadius = a;
				BorderTopRightRadius = b;
				BorderBottomRightRadius = a;
				BorderBottomLeftRadius = b;
				return true;
			}

			if ( p.IsEnd || !p.TryReadLength( out var d ) )
			{
				BorderTopLeftRadius = a;
				BorderTopRightRadius = b;
				BorderBottomRightRadius = c;
				BorderBottomLeftRadius = b;
				return true;
			}

			BorderTopLeftRadius = a;
			BorderTopRightRadius = b;
			BorderBottomRightRadius = c;
			BorderBottomLeftRadius = d;
			return true;
		}

		/// <summary>
		/// Parse justify-content values
		/// Based on s&box's SetJustifyContent implementation
		/// </summary>
		private bool SetJustifyContent( string value )
		{
			switch ( value )
			{
				case "flex-start":
					JustifyContent = UI.Justify.FlexStart;
					return true;
				case "center":
					JustifyContent = UI.Justify.Center;
					return true;
				case "flex-end":
					JustifyContent = UI.Justify.FlexEnd;
					return true;
				case "space-between":
					JustifyContent = UI.Justify.SpaceBetween;
					return true;
				case "space-around":
					JustifyContent = UI.Justify.SpaceAround;
					return true;
				case "space-evenly":
					JustifyContent = UI.Justify.SpaceEvenly;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse flex-wrap values
		/// Based on s&box's SetFlexWrap implementation
		/// </summary>
		private bool SetFlexWrap( string value )
		{
			switch ( value )
			{
				case "nowrap":
					FlexWrap = Wrap.NoWrap;
					return true;
				case "wrap":
					FlexWrap = Wrap.Wrap;
					return true;
				case "wrap-reverse":
					FlexWrap = Wrap.WrapReverse;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse display mode values
		/// Based on s&box's SetDisplay implementation
		/// </summary>
		private bool SetDisplay( string value )
		{
			switch ( value )
			{
				case "none":
					Display = DisplayMode.None;
					return true;
				case "flex":
					Display = DisplayMode.Flex;
					return true;
				case "contents":
					Display = DisplayMode.Contents;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse pointer-events values
		/// Based on s&box's SetPointerEvents implementation
		/// </summary>
		private bool SetPointerEvents( string value )
		{
			switch ( value )
			{
				case "auto":
					PointerEvents = null;
					return true;
				case "none":
					PointerEvents = UI.PointerEvents.None;
					return true;
				case "all":
					PointerEvents = UI.PointerEvents.All;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse position mode values
		/// Based on s&box's SetPosition implementation
		/// </summary>
		private bool SetPosition( string value )
		{
			switch ( value )
			{
				case "static":
					Position = PositionMode.Static;
					return true;
				case "absolute":
					Position = PositionMode.Absolute;
					return true;
				case "relative":
					Position = PositionMode.Relative;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse text-align values
		/// Based on s&box's SetTextAlign implementation
		/// </summary>
		private bool SetTextAlign( string value )
		{
			switch ( value )
			{
				case "center":
					TextAlign = UI.TextAlign.Center;
					return true;
				case "left":
					TextAlign = UI.TextAlign.Left;
					return true;
				case "right":
					TextAlign = UI.TextAlign.Right;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse text-overflow values
		/// Based on s&box's SetTextOverflow implementation
		/// </summary>
		private bool SetTextOverflow( string value )
		{
			switch ( value )
			{
				case "ellipsis":
					TextOverflow = UI.TextOverflow.Ellipsis;
					return true;
				case "clip":
					TextOverflow = UI.TextOverflow.Clip;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse word-break values
		/// Based on s&box's SetWordBreak implementation
		/// </summary>
		private bool SetWordBreak( string value )
		{
			switch ( value )
			{
				case "normal":
					WordBreak = UI.WordBreak.Normal;
					return true;
				case "break-all":
					WordBreak = UI.WordBreak.BreakAll;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse white-space values
		/// Based on s&box's SetWhiteSpace implementation
		/// </summary>
		private bool SetWhiteSpace( string value )
		{
			switch ( value )
			{
				case "normal":
					WhiteSpace = UI.WhiteSpace.Normal;
					return true;
				case "nowrap":
					WhiteSpace = UI.WhiteSpace.NoWrap;
					return true;
				case "pre-line":
					WhiteSpace = UI.WhiteSpace.PreLine;
					return true;
				case "pre":
					WhiteSpace = UI.WhiteSpace.Pre;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse object-fit values
		/// Based on s&box's SetObjectFit implementation
		/// </summary>
		private bool SetObjectFit( string value )
		{
			value = value.Trim();

			if ( System.Enum.TryParse<ObjectFit>( value, true, out var objectFit ) )
			{
				ObjectFit = objectFit;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parse image properties (background-image, mask-image, border-image source)
		/// Based on s&box's SetImage implementation
		/// </summary>
		private bool SetImage( string value, System.Func<System.Lazy<Texture>, bool> setImage )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			if ( p.Is( "none", 0, true ) )
			{
				setImage( new System.Lazy<Texture>( Texture.Invalid ) );
				return true;
			}

			if ( GetTokenValueUnderParenthesis( p, "url", out string url ) )
			{
				url = url.Trim( ' ', '"', '\'' );
				setImage( new System.Lazy<Texture>( () =>
				{
					return Texture.Load( url ) ?? Texture.Invalid;
				} ) );
				return true;
			}

			// Linear gradients, radial gradients etc. would go here
			// For now we only support url() and none

			return false;
		}

		/// <summary>
		/// Helper to extract value inside parentheses for functions like url(), linear-gradient(), etc.
		/// Based on s&box's GetTokenValueUnderParenthesis implementation
		/// </summary>
		private bool GetTokenValueUnderParenthesis( Parse p, string tokenName, out string result )
		{
			if ( p.Is( tokenName, 0, true ) )
			{
				p.Pointer += tokenName.Length;
				p = p.SkipWhitespaceAndNewlines();

				if ( p.Current != '(' )
				{
					result = "";
					return false;
				}

				p.Pointer++;

				int stack = 1;
				var wordStart = p;

				while ( !p.IsEnd && stack > 0 )
				{
					p.Pointer++;
					if ( p.Current == '(' ) stack++;
					if ( p.Current == ')' ) stack--;
				}

				if ( p.IsEnd )
				{
					result = "";
					return false;
				}

				result = wordStart.Read( p.Pointer - wordStart.Pointer );
				return true;
			}
			result = "";
			return false;
		}

		/// <summary>
		/// Parse border-image shorthand property
		/// Based on s&box's SetBorderImage implementation
		/// Syntax: border-image: source slice / width repeat fill
		/// </summary>
		private bool SetBorderImage( string value )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();

			// Parse the image source
			if ( !SetImage( p.Text, SetBorderTexture ) )
			{
				return false;
			}

			// Skip past the url(...) part - safely
			if (!p.IsEnd && p.Current == ')')
			{
				p.Pointer++; // Skip the closing paren
			}

			var borderSliceList = new System.Collections.Generic.List<Length>();
			var borderWidthList = new System.Collections.Generic.List<Length>();

			// 0 = parsing slice, 1 = parsing width
			int parseType = 0;

			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();
				if (p.IsEnd) break;
				
				bool charConsumed = false;
				
				if ( p.Is( "stretch", 0, true ) )
				{
					p.Pointer += "stretch".Length;
					BorderImageRepeat = UI.BorderImageRepeat.Stretch;
					charConsumed = true;
				}
				else if ( p.Is( "round", 0, true ) )
				{
					p.Pointer += "round".Length;
					BorderImageRepeat = UI.BorderImageRepeat.Round;
					charConsumed = true;
				}
				else if ( p.Is( "fill", 0, true ) )
				{
					p.Pointer += "fill".Length;
					BorderImageFill = UI.BorderImageFill.Filled;
					charConsumed = true;
				}
				else if ( p.Is( "/", 0, true ) )
				{
					p.Pointer++;
					charConsumed = true;

					// Needs to have at least one element before we do it
					if ( borderSliceList.Count == 0 )
					{
						return false;
					}

					// We don't support anything else
					if ( parseType == 1 )
					{
						return false;
					}

					parseType = 1;
				}
				else if ( p.TryReadLength( out Length lengthValue ) )
				{
					if ( parseType == 0 )
					{
						borderSliceList.Add( lengthValue );
					}
					else
					{
						borderWidthList.Add( lengthValue );
					}
					charConsumed = true;
				}

				if ( p.IsEnd )
					break;

				// Only advance if no condition consumed characters
				if (!charConsumed && !p.IsEnd)
				{
					p.Pointer++;
				}
			}

			// Parse our border slice pixel sizes
			switch ( borderSliceList.Count )
			{
				// 33.3% of texture size - only if texture is valid and has non-zero dimensions
				case 0:
					if ( BorderImageSource != null && BorderImageSource.Width > 0 )
					{
						BorderImageWidthLeft = BorderImageWidthRight = BorderImageWidthTop = BorderImageWidthBottom = BorderImageSource.Width / 3.0f;
					}
					break;

				// Uniform
				case 1:
					BorderImageWidthLeft = BorderImageWidthRight = BorderImageWidthTop = BorderImageWidthBottom = borderSliceList[0];
					break;

				// Top-Bottom and Left-Right
				case 2:
					BorderImageWidthTop = BorderImageWidthBottom = borderSliceList[0];
					BorderImageWidthLeft = BorderImageWidthRight = borderSliceList[1];
					break;

				// Top, Left-Right and Bottom
				case 3:
					BorderImageWidthTop = borderSliceList[0];
					BorderImageWidthLeft = BorderImageWidthRight = borderSliceList[1];
					BorderImageWidthBottom = borderSliceList[2];
					break;

				// Top, Right, Bottom, Left
				case 4:
					BorderImageWidthTop = borderSliceList[0];
					BorderImageWidthRight = borderSliceList[1];
					BorderImageWidthBottom = borderSliceList[2];
					BorderImageWidthLeft = borderSliceList[3];
					break;
			}

			// Parse our border width pixel sizes, we re use BorderWidth so we don't need to pass another uniform to the shader
			switch ( borderWidthList.Count )
			{
				// Just copy whatever is on slice if nothing is set
				case 0:
					BorderLeftWidth = BorderImageWidthLeft;
					BorderRightWidth = BorderImageWidthRight;
					BorderTopWidth = BorderImageWidthTop;
					BorderBottomWidth = BorderImageWidthBottom;
					break;

				// Uniform
				case 1:
					BorderLeftWidth = BorderRightWidth = BorderTopWidth = BorderBottomWidth = borderWidthList[0];
					break;

				// Top-Bottom and Left-Right
				case 2:
					BorderTopWidth = BorderBottomWidth = borderWidthList[0];
					BorderLeftWidth = BorderRightWidth = borderWidthList[1];
					break;

				// Top, Left-Right and Bottom
				case 3:
					BorderTopWidth = borderWidthList[0];
					BorderLeftWidth = BorderRightWidth = borderWidthList[1];
					BorderBottomWidth = borderWidthList[2];
					break;

				// Top, Right, Bottom, Left
				case 4:
					BorderTopWidth = borderWidthList[0];
					BorderRightWidth = borderWidthList[1];
					BorderBottomWidth = borderWidthList[2];
					BorderLeftWidth = borderWidthList[3];
					break;
			}

			return true;
		}

		/// <summary>
		/// Set border texture from parsed image
		/// Based on s&box's SetBorderTexture implementation
		/// </summary>
		private bool SetBorderTexture( System.Lazy<Texture> t )
		{
			_borderImageSource = t;
			return true;
		}

		/// <summary>
		/// Set background image from parsed texture
		/// Based on s&box's SetBackgroundImageFromTexture implementation
		/// </summary>
		private bool SetBackgroundImageFromTexture( System.Lazy<Texture> texture )
		{
			if ( texture == null )
				return true;

			_backgroundImage = texture;
			Dirty();

			return true;
		}

		/// <summary>
		/// Parse transform property value
		/// Based on s&box's SetTransform implementation
		/// </summary>
		private bool SetTransform( string value )
		{
			if ( string.IsNullOrEmpty( value ) || value == "none" )
			{
				Transform = null;
				return true;
			}

			var t = new PanelTransform();
			t.Parse( value );

			Transform = t;

			return true;
		}

		/// <summary>
		/// Parse transform-origin shorthand property
		/// Based on s&box's SetTransformOrigin implementation
		/// </summary>
		private bool SetTransformOrigin( string value )
		{
			var p = new Parse( value );

			if ( !p.TryReadLength( out var x ) )
				return false;

			TransformOriginX = x;

			if ( !p.TryReadLength( out var y ) )
			{
				TransformOriginY = x;
				return true;
			}

			TransformOriginY = y;
			return true;
		}

		/// <summary>
		/// Parse font-color with gradient support
		/// Based on s&box's SetFontColor implementation
		/// </summary>
		private bool SetFontColor( string value )
		{
			var fontColor = Color.Parse( value );
			if ( fontColor.HasValue )
			{
				FontColor = fontColor;
				return true;
			}

			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			if ( GetTokenValueUnderParenthesis( p, "linear-gradient", out string gradient ) )
			{
				SetTextGradientLinear( gradient );
				return true;
			}

			if ( GetTokenValueUnderParenthesis( p, "radial-gradient", out string radialGradient ) )
			{
				SetTextGradientRadial( radialGradient );
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parse caret-color property
		/// Based on s&box's SetCaretColor implementation
		/// </summary>
		private bool SetCaretColor( string value )
		{
			var caretColor = Color.Parse( value );
			if ( caretColor.HasValue )
			{
				CaretColor = caretColor;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Parse font-weight with named values
		/// Based on s&box's SetFontWeight implementation
		/// </summary>
		private bool SetFontWeight( string value )
		{
			if ( int.TryParse( value, out var i ) )
			{
				FontWeight = i;
				return true;
			}

			switch ( value )
			{
				case "hairline":
				case "thin":
					FontWeight = 100;
					return true;
				case "ultralight":
				case "extralight":
					FontWeight = 200;
					return true;
				case "light":
					FontWeight = 300;
					return true;
				case "regular":
				case "normal":
					FontWeight = 400;
					return true;
				case "medium":
					FontWeight = 500;
					return true;
				case "demibold":
				case "semibold":
					FontWeight = 600;
					return true;
				case "bold":
					FontWeight = 700;
					return true;
				case "ultrabold":
				case "extrabold":
					FontWeight = 800;
					return true;
				case "heavy":
				case "black":
					FontWeight = 900;
					return true;
				case "extrablack":
				case "ultrablack":
					FontWeight = 950;
					return true;
				case "bolder":
					FontWeight = 900;
					return true;
				case "lighter":
					FontWeight = 200;
					return true;
			}

			return false;
		}

		/// <summary>
		/// Parse shadow properties (box-shadow, text-shadow, filter-drop-shadow)
		/// Based on s&box's SetShadow implementation
		/// </summary>
		private bool SetShadow( string value, ref ShadowList shadowList )
		{
			var p = new Parse( value );

			shadowList.Clear();

			if ( p.Is( "none", 0, true ) )
			{
				shadowList.IsNone = true;
				return true;
			}

			while ( !p.IsEnd )
			{
				var shadow = new Shadow();

				if ( !p.TryReadLength( out var x ) )
					return false;

				if ( !p.TryReadLength( out var y ) )
					return false;

				shadow.OffsetX = x.Value;
				shadow.OffsetY = y.Value;

				if ( p.TryReadLength( out var blur ) )
				{
					shadow.Blur = blur.Value;

					if ( p.TryReadLength( out var spread ) )
					{
						shadow.Spread = spread.Value;
					}
				}

				if ( p.TryReadColor( out var color ) )
				{
					shadow.Color = color;
				}

				p.SkipWhitespaceAndNewlines();

				if ( p.TryReadShadowInset( out var inset ) )
				{
					shadow.Inset = inset;
				}

				shadowList.Add( shadow );

				p.SkipWhitespaceAndNewlines();

				if ( p.IsEnd || p.Current != ',' )
					return true;

				p.Pointer++;
				p.SkipWhitespaceAndNewlines();
			}

			return true;
		}

		/// <summary>
		/// Parse text-stroke property
		/// Based on s&box's SetTextStroke implementation
		/// </summary>
		private bool SetTextStroke( string value )
		{
			var p = new Parse( value );

			if ( !p.TryReadLength( out var width ) )
				return false;

			if ( !p.TryReadColor( out var color ) )
				return false;

			TextStrokeWidth = width;
			TextStrokeColor = color;

			return true;
		}

		/// <summary>
		/// Parse text-filter property
		/// Based on s&box's SetTextFilter implementation
		/// </summary>
		private bool SetTextFilter( string value )
		{
			switch ( value )
			{
				case "linear":
				case "bilinear":
					TextFilter = Rendering.FilterMode.Bilinear;
					return true;
				case "point":
					TextFilter = Rendering.FilterMode.Point;
					return true;
				case "trilinear":
					TextFilter = Rendering.FilterMode.Trilinear;
					return true;
				case "anisotropic":
					TextFilter = Rendering.FilterMode.Anisotropic;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Get text decoration from value string
		/// Based on s&box's GetTextDecorationFromValue implementation
		/// </summary>
		private UI.TextDecoration GetTextDecorationFromValue( string value )
		{
			var td = UI.TextDecoration.None;

			if ( value.Contains( "underline" ) ) td |= UI.TextDecoration.Underline;
			if ( value.Contains( "line-through" ) ) td |= UI.TextDecoration.LineThrough;
			if ( value.Contains( "overline" ) ) td |= UI.TextDecoration.Overline;

			return td;
		}

		/// <summary>
		/// Parse text-decoration property
		/// Based on s&box's SetTextDecoration implementation
		/// </summary>
		private bool SetTextDecoration( string value )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();
			if ( p.IsEnd ) return false;

			var td = UI.TextDecoration.None;

			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();
				if ( p.TryReadLength( out var decorationThickness ) )
				{
					TextDecorationThickness = decorationThickness;
					continue;
				}

				if ( p.TryReadColor( out var decorationColor ) )
				{
					TextDecorationColor = decorationColor;
					continue;
				}

				var subValue = p.ReadWord( null, true );

				var textDecoration = GetTextDecorationFromValue( subValue );
				if ( textDecoration != UI.TextDecoration.None )
				{
					td |= textDecoration;
					continue;
				}

				if ( !SetTextDecorationStyle( subValue ) )
				{
					return false;
				}
			}

			if ( td != UI.TextDecoration.None )
			{
				TextDecorationLine = td;
			}

			return true;
		}

		/// <summary>
		/// Parse text-decoration-line property
		/// Based on s&box's SetTextDecorationLine implementation
		/// </summary>
		private bool SetTextDecorationLine( string value )
		{
			TextDecorationLine = GetTextDecorationFromValue( value );
			return true;
		}

		/// <summary>
		/// Parse text-decoration-skip-ink property
		/// Based on s&box's SetTextDecorationSkipInk implementation
		/// </summary>
		private bool SetTextDecorationSkipInk( string value )
		{
			switch ( value )
			{
				case "auto":
				case "all":
					TextDecorationSkipInk = UI.TextSkipInk.All;
					return true;
				case "none":
					TextDecorationSkipInk = UI.TextSkipInk.None;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse text-decoration-style property
		/// Based on s&box's SetTextDecorationStyle implementation
		/// </summary>
		private bool SetTextDecorationStyle( string value )
		{
			switch ( value )
			{
				case "solid":
					TextDecorationStyle = UI.TextDecorationStyle.Solid;
					return true;
				case "double":
					TextDecorationStyle = UI.TextDecorationStyle.Double;
					return true;
				case "dotted":
					TextDecorationStyle = UI.TextDecorationStyle.Dotted;
					return true;
				case "dashed":
					TextDecorationStyle = UI.TextDecorationStyle.Dashed;
					return true;
				case "wavy":
					TextDecorationStyle = UI.TextDecorationStyle.Wavy;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse font-style property
		/// Based on s&box's SetFontStyle implementation
		/// </summary>
		private bool SetFontStyle( string value )
		{
			var fs = UI.FontStyle.None;

			if ( value.Contains( "italic" ) ) fs |= UI.FontStyle.Italic;
			if ( value.Contains( "oblique" ) ) fs |= UI.FontStyle.Oblique;

			FontStyle = fs;
			return true;
		}

		/// <summary>
		/// Parse text-transform property
		/// Based on s&box's SetTextTransform implementation
		/// </summary>
		private bool SetTextTransform( string value )
		{
			switch ( value )
			{
				case "capitalize":
					TextTransform = UI.TextTransform.Capitalize;
					return true;
				case "uppercase":
					TextTransform = UI.TextTransform.Uppercase;
					return true;
				case "lowercase":
					TextTransform = UI.TextTransform.Lowercase;
					return true;
				case "none":
					TextTransform = UI.TextTransform.None;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse perspective-origin property
		/// Based on s&box's SetPerspectiveOrigin implementation
		/// </summary>
		private bool SetPerspectiveOrigin( string value )
		{
			var p = new Parse( value );

			if ( !p.TryReadLength( out var x ) )
				return false;

			PerspectiveOriginX = x;

			if ( !p.TryReadLength( out var y ) )
			{
				PerspectiveOriginY = x;
				return true;
			}

			PerspectiveOriginY = y;
			return true;
		}

		/// <summary>
		/// Parse backdrop-filter property
		/// Based on s&box's SetBackdropFilter implementation
		/// </summary>
		private bool SetBackdropFilter( string value )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();
				if ( p.IsEnd ) return true;

				var name = p.ReadWord( "(" );
				var innervalue = p.ReadInnerBrackets();

				if ( name == "blur" )
				{
					BackdropFilterBlur = Length.Parse( innervalue );
					continue;
				}

				if ( name == "invert" )
				{
					BackdropFilterInvert = Length.Parse( innervalue );
					continue;
				}

				if ( name == "contrast" )
				{
					BackdropFilterContrast = Length.Parse( innervalue );
					continue;
				}

				if ( name == "brightness" )
				{
					BackdropFilterBrightness = Length.Parse( innervalue );
					continue;
				}

				if ( name == "grayscale" )
				{
					BackdropFilterSaturate = Length.Parse( innervalue );

					if ( BackdropFilterSaturate.HasValue )
					{
						var val = BackdropFilterSaturate.Value.GetPixels( 1 );
						BackdropFilterSaturate = 1 - val;
					}

					continue;
				}

				if ( name == "saturate" )
				{
					BackdropFilterSaturate = Length.Parse( innervalue );
					continue;
				}

				if ( name == "sepia" )
				{
					BackdropFilterSepia = Length.Parse( innervalue );
					continue;
				}

				if ( name == "hue-rotate" )
				{
					BackdropFilterHueRotate = Length.Parse( innervalue );
					continue;
				}

				return false;
			}

			return true;
		}

		/// <summary>
		/// Parse filter property
		/// Based on s&box's SetFilter implementation
		/// </summary>
		private bool SetFilter( string value )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();
				if ( p.IsEnd ) return true;

				var name = p.ReadWord( "(" );
				var innervalue = p.ReadInnerBrackets();

				switch ( name )
				{
					case "blur":
						FilterBlur = Length.Parse( innervalue );
						break;
					case "saturate":
						FilterSaturate = Length.Parse( innervalue );
						break;
					case "greyscale":
					case "grayscale":
						FilterSaturate = Length.Parse( innervalue );

						if ( FilterSaturate.HasValue )
						{
							var val = FilterSaturate.Value.GetPixels( 1 );
							FilterSaturate = 1 - val;
						}
						break;
					case "sepia":
						FilterSepia = Length.Parse( innervalue );
						break;
					case "brightness":
						FilterBrightness = Length.Parse( innervalue );
						break;
					case "contrast":
						FilterContrast = Length.Parse( innervalue );
						break;
					case "hue-rotate":
						FilterHueRotate = Length.Parse( innervalue );
						break;
					case "invert":
						FilterInvert = Length.Parse( innervalue );
						break;
					case "tint":
						FilterTint = Color.Parse( innervalue );
						break;
					case "drop-shadow":
						var shadowList = new ShadowList();
						SetShadow( innervalue, ref shadowList );
						FilterDropShadow = shadowList;
						break;
					case "border-wrap":
						SetFilterBorderWrap( innervalue );
						break;
					default:
						return false;
				}

			}

			return true;
		}

		/// <summary>
		/// Parse filter-border-wrap property
		/// Based on s&box's SetFilterBorderWrap implementation
		/// </summary>
		private bool SetFilterBorderWrap( string value )
		{
			var p = new Parse( value );

			p = p.SkipWhitespaceAndNewlines();

			while ( !p.IsEnd )
			{
				if ( p.TryReadLength( out var lengthValue ) )
					FilterBorderWidth = lengthValue;
				else if ( p.TryReadColor( out var colorValue ) )
					FilterBorderColor = colorValue;
				else
					return false;

				p = p.SkipWhitespaceAndNewlines();
			}

			return true;
		}

		/// <summary>
		/// Parse image-rendering property
		/// Based on s&box's SetImageRendering implementation
		/// </summary>
		private bool SetImageRendering( string value )
		{
			switch ( value )
			{
				case "auto":
				case "anisotropic":
					ImageRendering = UI.ImageRendering.Anisotropic;
					return true;
				case "bilinear":
					ImageRendering = UI.ImageRendering.Bilinear;
					return true;
				case "trilinear":
					ImageRendering = UI.ImageRendering.Trilinear;
					return true;
				case "point":
				case "pixelated":
				case "nearest-neighbor":
					ImageRendering = UI.ImageRendering.Point;
					return true;
			}

			return false;
		}

		/// <summary>
		/// Parse background-size property
		/// Based on s&box's SetBackgroundSize implementation
		/// </summary>
		private bool SetBackgroundSize( string value )
		{
			var p = new Parse( value );
			if ( p.TryReadLength( out var lenx ) )
			{
				BackgroundSizeX = lenx;
				BackgroundSizeY = lenx;

				if ( p.TryReadLength( out var leny ) )
				{
					BackgroundSizeY = leny;
				}
			}

			return true;
		}

		/// <summary>
		/// Parse background-position property
		/// Based on s&box's SetBackgroundPosition implementation
		/// </summary>
		private bool SetBackgroundPosition( string value )
		{
			var p = new Parse( value );
			if ( p.TryReadLength( out var lenx ) )
			{
				BackgroundPositionX = lenx;
				BackgroundPositionY = lenx;

				if ( p.TryReadLength( out var leny ) )
				{
					BackgroundPositionY = leny;
				}
			}

			return true;
		}

		/// <summary>
		/// Parse background-repeat property
		/// Based on s&box's SetBackgroundRepeat implementation
		/// </summary>
		private bool SetBackgroundRepeat( string value )
		{
			switch ( value )
			{
				case "no-repeat":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.NoRepeat;
					return true;

				case "repeat-x":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.RepeatX;
					return true;

				case "repeat-y":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.RepeatY;
					return true;

				case "repeat":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.Repeat;
					return true;

				case "round":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.Round;
					return true;

				case "clamp":
					BackgroundRepeat = Sandbox.UI.BackgroundRepeat.Clamp;
					return true;
			}

			return false;
		}

		/// <summary>
		/// Set background angle from gradient
		/// Based on s&box's SetBackgroundAngle implementation
		/// </summary>
		private bool SetBackgroundAngle( float value )
		{
			if ( value < 0 )
				return false;

			BackgroundAngle = value;
			return true;
		}

		/// <summary>
		/// Parse mask-position property
		/// Based on s&box's SetMaskPosition implementation
		/// </summary>
		private bool SetMaskPosition( string value )
		{
			var p = new Parse( value );
			if ( p.TryReadLength( out var lenx ) )
			{
				MaskPositionX = lenx;
				MaskPositionY = lenx;

				if ( p.TryReadLength( out var leny ) )
				{
					MaskPositionY = leny;
				}
			}

			return true;
		}

		/// <summary>
		/// Parse mask-size property
		/// Based on s&box's SetMaskSize implementation
		/// </summary>
		private bool SetMaskSize( string value )
		{
			var p = new Parse( value );
			if ( p.TryReadLength( out var lenx ) )
			{
				MaskSizeX = lenx;
				MaskSizeY = lenx;

				if ( p.TryReadLength( out var leny ) )
				{
					MaskSizeY = leny;
				}
			}

			return true;
		}

		/// <summary>
		/// Parse mask-repeat property
		/// Based on s&box's SetMaskRepeat implementation
		/// </summary>
		private bool SetMaskRepeat( string value )
		{
			switch ( value )
			{
				case "no-repeat":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.NoRepeat;
					return true;

				case "repeat-x":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.RepeatX;
					return true;

				case "repeat-y":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.RepeatY;
					return true;

				case "repeat":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.Repeat;
					return true;

				case "round":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.Round;
					return true;

				case "clamp":
					MaskRepeat = Sandbox.UI.BackgroundRepeat.Clamp;
					return true;
			}

			return false;
		}

		/// <summary>
		/// Set mask angle from gradient
		/// Based on s&box's SetMaskAngle implementation
		/// </summary>
		private bool SetMaskAngle( float value )
		{
			if ( value < 0 )
				return false;

			MaskAngle = value;
			return true;
		}

		/// <summary>
		/// Parse mask-mode property
		/// Based on s&box's SetMaskMode implementation
		/// </summary>
		private bool SetMaskMode( string value )
		{
			switch ( value )
			{
				case "match-source":
					MaskMode = UI.MaskMode.MatchSource;
					return true;
				case "alpha":
					MaskMode = UI.MaskMode.Alpha;
					return true;
				case "luminance":
					MaskMode = UI.MaskMode.Luminance;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Parse mask-scope property
		/// Based on s&box's SetMaskScope implementation
		/// </summary>
		private bool SetMaskScope( string value )
		{
			switch ( value )
			{
				case "default":
					MaskScope = Sandbox.UI.MaskScope.Default;
					return true;

				case "filter":
					MaskScope = Sandbox.UI.MaskScope.Filter;
					return true;
			}

			return false;
		}

		/// <summary>
		/// Set mask image from texture
		/// Based on s&box's SetMaskImageFromTexture implementation
		/// </summary>
		private bool SetMaskImageFromTexture( System.Lazy<Texture> texture )
		{
			if ( texture == null )
				return true;

			_maskImage = texture;

			return true;
		}

		/// <summary>
		/// Parse background shorthand property (simplified version)
		/// Based on s&box's SetBackground implementation
		/// </summary>
		private bool SetBackground( string value )
		{
			// Simplified implementation - full implementation requires more complex parsing
			var p = new Parse( value );

			// Try to parse as color
			if ( p.TryReadColor( out var bgColor ) )
			{
				BackgroundColor = bgColor;
				return true;
			}

			// Try as image
			return SetImage( value, SetBackgroundImageFromTexture, SetBackgroundSize, SetBackgroundRepeat, SetBackgroundAngle );
		}

		/// <summary>
		/// Parse mask shorthand property (simplified version)
		/// Based on s&box's SetMask implementation
		/// </summary>
		private bool SetMask( string value )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			// Parse the image source
			if ( !SetImage( p.Text, SetMaskImageFromTexture, SetMaskSize, SetMaskRepeat, SetMaskAngle ) )
				return false;

			// Skip past the url(...) part if present
			while ( !p.IsEnd && p.Current != ')' )
				p.Pointer++;

			if ( !p.IsEnd && p.Current == ')' )
				p.Pointer++;

			p = p.SkipWhitespaceAndNewlines();

			// Try to read position and size
			if ( p.TryReadPositionAndSize( out var positionX, out var positionY, out var sizeX, out var sizeY ) )
			{
				MaskPositionX = positionX;
				MaskPositionY = positionY;
				if ( sizeX.Unit != LengthUnit.Auto ) MaskSizeX = sizeX;
				if ( sizeY.Unit != LengthUnit.Auto ) MaskSizeY = sizeY;
			}

			// Try to read repeat
			if ( p.TryReadRepeat( out var repeat ) )
				SetMaskRepeat( repeat );

			// Try to read mask mode
			if ( p.TryReadMaskMode( out var maskMode ) )
				SetMaskMode( maskMode );

			return true;
		}

		/// <summary>
		/// Parse animation shorthand property (simplified version)
		/// Based on s&box's SetAnimation implementation
		/// </summary>
		private bool SetAnimation( string value )
		{
			var p = new Parse( value );

			// animation: none;
			if ( p.Is( "none", 0, true ) )
			{
				AnimationName = "none";
				return true;
			}

			int timeCount = 0;
			while ( !p.IsEnd )
			{
				p = p.SkipWhitespaceAndNewlines();

				// Parse time values
				if ( p.TryReadTime( out var time ) )
				{
					if ( timeCount == 0 )
						AnimationDuration = time / 1000.0f; // ms to s
					else
						AnimationDelay = time / 1000.0f; // ms to s

					timeCount++;
					continue;
				}

				// Parse other keywords
				var word = p.ReadWord( null, true ).ToLower();

				if ( word == "infinite" )
				{
					AnimationIterationCount = float.PositiveInfinity;
				}
				else if ( int.TryParse( word, out int iterationCount ) )
				{
					AnimationIterationCount = iterationCount;
				}
				else if ( word == "normal" || word == "reverse" || word == "alternate" || word == "alternate-reverse" )
				{
					AnimationDirection = word;
				}
				else if ( word == "none" || word == "forwards" || word == "backwards" || word == "both" )
				{
					AnimationFillMode = word;
				}
				else if ( word == "running" || word == "paused" )
				{
					AnimationPlayState = word;
				}
				else if ( !string.IsNullOrEmpty( word ) )
				{
					// Assume it's the animation name or timing function
					AnimationName = word;
				}
			}

			return true;
		}

		/// <summary>
		/// Enhanced SetImage with gradient support
		/// Based on s&box's SetImage implementation
		/// </summary>
		private bool SetImage( string value, System.Func<System.Lazy<Texture>, bool> setImage = null, System.Func<string, bool> setSize = null, System.Func<string, bool> setRepeat = null, System.Func<float, bool> setAngle = null )
		{
			var p = new Parse( value );
			p = p.SkipWhitespaceAndNewlines();

			if ( p.Is( "none", 0, true ) )
			{
				setImage?.Invoke( new System.Lazy<Texture>( Texture.Invalid ) );
				return true;
			}

			if ( GetTokenValueUnderParenthesis( p, "url", out string url ) )
			{
				url = url.Trim( ' ', '"', '\'' );
				setImage?.Invoke( new System.Lazy<Texture>( () =>
				{
					return Texture.Load( url ) ?? Texture.Invalid;
				} ) );
				return true;
			}

			// Linear gradients support (simplified - full implementation needs texture generation)
			if ( GetTokenValueUnderParenthesis( p, "linear-gradient", out string gradient ) )
			{
				// For now, just acknowledge gradient support exists
				// Full implementation would call GenerateLinearGradientTexture
				setImage?.Invoke( new System.Lazy<Texture>( Texture.Invalid ) );
				setSize?.Invoke( "100%" );
				setRepeat?.Invoke( "clamp" );
				return true;
			}

			// Radial gradients support (simplified)
			if ( GetTokenValueUnderParenthesis( p, "radial-gradient", out string radialGradient ) )
			{
				setImage?.Invoke( new System.Lazy<Texture>( Texture.Invalid ) );
				setSize?.Invoke( "100%" );
				setRepeat?.Invoke( "clamp" );
				return true;
			}

			// Conic gradients support (simplified)
			if ( GetTokenValueUnderParenthesis( p, "conic-gradient", out string conicGradient ) )
			{
				setImage?.Invoke( new System.Lazy<Texture>( Texture.Invalid ) );
				setSize?.Invoke( "100%" );
				setRepeat?.Invoke( "clamp" );
				return true;
			}

			return false;
		}

		/// <summary>
		/// Set text gradient linear (simplified - requires full gradient infrastructure)
		/// Based on s&box's SetTextGradientLinear implementation
		/// </summary>
		private bool SetTextGradientLinear( string gradient )
		{
			// Simplified implementation - full version would parse and generate gradient
			// For now, just return true to acknowledge the property was set
			return true;
		}

		/// <summary>
		/// Set text gradient radial (simplified - requires full gradient infrastructure)
		/// Based on s&box's SetTextGradientRadial implementation
		/// </summary>
		private bool SetTextGradientRadial( string gradient )
		{
			// Simplified implementation - full version would parse and generate gradient
			// For now, just return true to acknowledge the property was set
			return true;
		}
	}
}
