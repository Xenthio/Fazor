using System.Runtime.InteropServices;

namespace Sandbox.UI;

/// <summary>
/// Yoga layout node reference wrapper
/// Based on s&box's YogaWrapper from engine/Sandbox.Engine/Systems/UI/Yoga/YogaWrapper.cs
/// </summary>
public sealed class YogaWrapper : IDisposable
{
    private YGNodeRef _native;
    private readonly Panel? _panel;
    private static YGConfigRef _config;

    static YogaWrapper()
    {
        _config = YogaNative.YGConfigNew();
        YogaNative.YGConfigSetUseWebDefaults(_config, true);
        YogaNative.YGConfigSetPointScaleFactor(_config, 0.0f);
    }

    public YogaWrapper(Panel? panel = null)
    {
        _native = YogaNative.YGNodeNewWithConfig(_config);
        _panel = panel;
    }

    public bool IsValid => !_native.IsNull;

    #region Layout Results

    public bool HasNewLayout => YogaNative.YGNodeGetHasNewLayout(_native);

    public float LayoutX => YogaNative.YGNodeLayoutGetLeft(_native);
    public float LayoutY => YogaNative.YGNodeLayoutGetTop(_native);
    public float LayoutWidth => YogaNative.YGNodeLayoutGetWidth(_native);
    public float LayoutHeight => YogaNative.YGNodeLayoutGetHeight(_native);

    private Rect _yogaRect;
    public Margin Margin { get; private set; }
    public Margin Padding { get; private set; }
    public Margin Border { get; private set; }

    public Rect YogaRect
    {
        get
        {
            if (!HasNewLayout)
                return _yogaRect;

            YogaNative.YGNodeSetHasNewLayout(_native, false);

            _yogaRect = new Rect(LayoutX, LayoutY, LayoutWidth, LayoutHeight);
            Margin = new Margin(
                YogaNative.YGNodeLayoutGetMargin(_native, YGEdge.Left),
                YogaNative.YGNodeLayoutGetMargin(_native, YGEdge.Top),
                YogaNative.YGNodeLayoutGetMargin(_native, YGEdge.Right),
                YogaNative.YGNodeLayoutGetMargin(_native, YGEdge.Bottom)
            );
            Padding = new Margin(
                YogaNative.YGNodeLayoutGetPadding(_native, YGEdge.Left),
                YogaNative.YGNodeLayoutGetPadding(_native, YGEdge.Top),
                YogaNative.YGNodeLayoutGetPadding(_native, YGEdge.Right),
                YogaNative.YGNodeLayoutGetPadding(_native, YGEdge.Bottom)
            );
            Border = new Margin(
                YogaNative.YGNodeLayoutGetBorder(_native, YGEdge.Left),
                YogaNative.YGNodeLayoutGetBorder(_native, YGEdge.Top),
                YogaNative.YGNodeLayoutGetBorder(_native, YGEdge.Right),
                YogaNative.YGNodeLayoutGetBorder(_native, YGEdge.Bottom)
            );

            return _yogaRect;
        }
    }

    #endregion

    #region Children

    public void AddChild(YogaWrapper? child)
    {
        if (child == null) return;
        var count = (int)YogaNative.YGNodeGetChildCount(_native);
        YogaNative.YGNodeInsertChild(_native, child._native, count);
    }

    public void RemoveChild(YogaWrapper? child)
    {
        if (child == null) return;
        YogaNative.YGNodeRemoveChild(_native, child._native);
    }

    #endregion

    #region Measure Function

    private YGMeasureFunc? _measureFunc;
    public bool IsMeasureDefined => _measureFunc != null;

    public void SetMeasureFunction(Func<YGNodeRef, float, YGMeasureMode, float, YGMeasureMode, Vector2> measureFunc)
    {
        _measureFunc = (node, w, wm, h, hm) => measureFunc(node, w, wm, h, hm);
        var fp = Marshal.GetFunctionPointerForDelegate(_measureFunc);
        YogaNative.YGNodeSetMeasureFunc(_native, fp);
    }

    public void MarkDirty()
    {
        if (!IsMeasureDefined) return;
        YogaNative.YGNodeMarkDirty(_native);
    }

    #endregion

    #region Layout Calculation

    public void CalculateLayout(float width = float.NaN, float height = float.NaN)
    {
        YogaNative.YGNodeCalculateLayout(_native, width, height, YGDirection.LTR);
    }

    #endregion

    #region Style Properties

    public bool Initialized { get; set; }

    // Helper to get parent layout values
    private YogaWrapper? Parent => _panel?.Parent?.YogaNode;

    public Length? Width
    {
        set => SetLength(value, () => Parent?.LayoutWidth ?? 0,
            YogaNative.YGNodeStyleSetWidthAuto,
            YogaNative.YGNodeStyleSetWidth,
            YogaNative.YGNodeStyleSetWidthPercent);
    }

    public Length? Height
    {
        set => SetLength(value, () => Parent?.LayoutHeight ?? 0,
            YogaNative.YGNodeStyleSetHeightAuto,
            YogaNative.YGNodeStyleSetHeight,
            YogaNative.YGNodeStyleSetHeightPercent);
    }

    public Length? MinWidth { set => SetLengthNoAuto(value, YogaNative.YGNodeStyleSetMinWidth, YogaNative.YGNodeStyleSetMinWidthPercent); }
    public Length? MinHeight { set => SetLengthNoAuto(value, YogaNative.YGNodeStyleSetMinHeight, YogaNative.YGNodeStyleSetMinHeightPercent); }
    public Length? MaxWidth { set => SetLengthNoAuto(value, YogaNative.YGNodeStyleSetMaxWidth, YogaNative.YGNodeStyleSetMaxWidthPercent); }
    public Length? MaxHeight { set => SetLengthNoAuto(value, YogaNative.YGNodeStyleSetMaxHeight, YogaNative.YGNodeStyleSetMaxHeightPercent); }

    public DisplayMode? Display
    {
        set => YogaNative.YGNodeStyleSetDisplay(_native, value switch
        {
            DisplayMode.None => YGDisplay.None,
            DisplayMode.Contents => YGDisplay.Flex, // No contents support in yoga
            _ => YGDisplay.Flex
        });
    }

    public Length? Left { set => SetEdgeLength(value, YGEdge.Left, YogaNative.YGNodeStyleSetPosition, YogaNative.YGNodeStyleSetPositionPercent); }
    public Length? Right { set => SetEdgeLength(value, YGEdge.Right, YogaNative.YGNodeStyleSetPosition, YogaNative.YGNodeStyleSetPositionPercent); }
    public Length? Top { set => SetEdgeLength(value, YGEdge.Top, YogaNative.YGNodeStyleSetPosition, YogaNative.YGNodeStyleSetPositionPercent); }
    public Length? Bottom { set => SetEdgeLength(value, YGEdge.Bottom, YogaNative.YGNodeStyleSetPosition, YogaNative.YGNodeStyleSetPositionPercent); }

    public Length? MarginLeft { set => SetEdgeLengthWithAuto(value, YGEdge.Left, YogaNative.YGNodeStyleSetMarginAuto, YogaNative.YGNodeStyleSetMargin, YogaNative.YGNodeStyleSetMarginPercent); }
    public Length? MarginRight { set => SetEdgeLengthWithAuto(value, YGEdge.Right, YogaNative.YGNodeStyleSetMarginAuto, YogaNative.YGNodeStyleSetMargin, YogaNative.YGNodeStyleSetMarginPercent); }
    public Length? MarginTop { set => SetEdgeLengthWithAuto(value, YGEdge.Top, YogaNative.YGNodeStyleSetMarginAuto, YogaNative.YGNodeStyleSetMargin, YogaNative.YGNodeStyleSetMarginPercent); }
    public Length? MarginBottom { set => SetEdgeLengthWithAuto(value, YGEdge.Bottom, YogaNative.YGNodeStyleSetMarginAuto, YogaNative.YGNodeStyleSetMargin, YogaNative.YGNodeStyleSetMarginPercent); }

    public Length? PaddingLeft { set => SetEdgeLength(value, YGEdge.Left, YogaNative.YGNodeStyleSetPadding, YogaNative.YGNodeStyleSetPaddingPercent); }
    public Length? PaddingRight { set => SetEdgeLength(value, YGEdge.Right, YogaNative.YGNodeStyleSetPadding, YogaNative.YGNodeStyleSetPaddingPercent); }
    public Length? PaddingTop { set => SetEdgeLength(value, YGEdge.Top, YogaNative.YGNodeStyleSetPadding, YogaNative.YGNodeStyleSetPaddingPercent); }
    public Length? PaddingBottom { set => SetEdgeLength(value, YGEdge.Bottom, YogaNative.YGNodeStyleSetPadding, YogaNative.YGNodeStyleSetPaddingPercent); }

    public Length? BorderLeftWidth { set => SetEdgeLengthNoPercent(value, YGEdge.Left, YogaNative.YGNodeStyleSetBorder); }
    public Length? BorderRightWidth { set => SetEdgeLengthNoPercent(value, YGEdge.Right, YogaNative.YGNodeStyleSetBorder); }
    public Length? BorderTopWidth { set => SetEdgeLengthNoPercent(value, YGEdge.Top, YogaNative.YGNodeStyleSetBorder); }
    public Length? BorderBottomWidth { set => SetEdgeLengthNoPercent(value, YGEdge.Bottom, YogaNative.YGNodeStyleSetBorder); }

    public PositionMode? PositionType
    {
        set => YogaNative.YGNodeStyleSetPositionType(_native, value switch
        {
            PositionMode.Absolute => YGPositionType.Absolute,
            PositionMode.Relative => YGPositionType.Relative,
            _ => YGPositionType.Static
        });
    }

    public float? AspectRatio { set => YogaNative.YGNodeStyleSetAspectRatio(_native, value ?? float.NaN); }
    public float? FlexGrow { set => YogaNative.YGNodeStyleSetFlexGrow(_native, value ?? 0); }
    public float? FlexShrink { set => YogaNative.YGNodeStyleSetFlexShrink(_native, value ?? 1); }

    public Length? FlexBasis
    {
        set => SetLength(value, () => Parent?.LayoutWidth ?? 0,
            YogaNative.YGNodeStyleSetFlexBasisAuto,
            YogaNative.YGNodeStyleSetFlexBasis,
            YogaNative.YGNodeStyleSetFlexBasisPercent);
    }

    public Wrap? Wrap
    {
        set => YogaNative.YGNodeStyleSetFlexWrap(_native, value switch
        {
            UI.Wrap.Wrap => YGWrap.Wrap,
            UI.Wrap.WrapReverse => YGWrap.WrapReverse,
            _ => YGWrap.NoWrap
        });
    }

    public Align? AlignContent
    {
        set => YogaNative.YGNodeStyleSetAlignContent(_native, ToYGAlign(value ?? Align.FlexStart));
    }

    public Align? AlignItems
    {
        set => YogaNative.YGNodeStyleSetAlignItems(_native, ToYGAlign(value ?? Align.Stretch));
    }

    public Align? AlignSelf
    {
        set => YogaNative.YGNodeStyleSetAlignSelf(_native, ToYGAlign(value ?? Align.Auto));
    }

    public FlexDirection? FlexDirection
    {
        set => YogaNative.YGNodeStyleSetFlexDirection(_native, (value ?? UI.FlexDirection.Row) switch
        {
            UI.FlexDirection.Row => YGFlexDirection.Row,
            UI.FlexDirection.RowReverse => YGFlexDirection.RowReverse,
            UI.FlexDirection.Column => YGFlexDirection.Column,
            UI.FlexDirection.ColumnReverse => YGFlexDirection.ColumnReverse,
            _ => YGFlexDirection.Row
        });
    }

    public Justify? JustifyContent
    {
        set => YogaNative.YGNodeStyleSetJustifyContent(_native, value switch
        {
            Justify.FlexEnd => YGJustify.FlexEnd,
            Justify.Center => YGJustify.Center,
            Justify.SpaceBetween => YGJustify.SpaceBetween,
            Justify.SpaceAround => YGJustify.SpaceAround,
            Justify.SpaceEvenly => YGJustify.SpaceEvenly,
            _ => YGJustify.FlexStart
        });
    }

    public OverflowMode? Overflow
    {
        set => YogaNative.YGNodeStyleSetOverflow(_native, value switch
        {
            OverflowMode.Hidden => YGOverflow.Hidden,
            OverflowMode.Scroll => YGOverflow.Scroll,
            _ => YGOverflow.Visible
        });
    }

    public Length? RowGap { set => YogaNative.YGNodeStyleSetGap(_native, YGGutter.Row, value?.GetPixels(0) ?? float.NaN); }
    public Length? ColumnGap { set => YogaNative.YGNodeStyleSetGap(_native, YGGutter.Column, value?.GetPixels(0) ?? float.NaN); }

    #endregion

    #region Helper Methods

    private static YGAlign ToYGAlign(Align align) => align switch
    {
        Align.Auto => YGAlign.Auto,
        Align.FlexStart => YGAlign.FlexStart,
        Align.FlexEnd => YGAlign.FlexEnd,
        Align.Center => YGAlign.Center,
        Align.Stretch => YGAlign.Stretch,
        Align.Baseline => YGAlign.Baseline,
        Align.SpaceBetween => YGAlign.SpaceBetween,
        Align.SpaceAround => YGAlign.SpaceAround,
        _ => YGAlign.Auto
    };

    private void SetLength(Length? value, Func<float> getDimension, Action<YGNodeRef> setAuto, Action<YGNodeRef, float> setUnit, Action<YGNodeRef, float> setPercent)
    {
        if (!value.HasValue || value.Value.Unit == LengthUnit.Undefined)
        {
            setUnit(_native, float.NaN);
            return;
        }

        var v = value.Value;
        switch (v.Unit)
        {
            case LengthUnit.Auto:
                setAuto(_native);
                break;
            case LengthUnit.Pixels:
                setUnit(_native, v.Value);
                break;
            case LengthUnit.Percentage:
                setPercent(_native, v.Value);
                break;
            default:
                setUnit(_native, v.GetPixels(getDimension()));
                break;
        }
    }

    private void SetLengthNoAuto(Length? value, Action<YGNodeRef, float> setUnit, Action<YGNodeRef, float> setPercent)
    {
        if (!value.HasValue || value.Value.Unit == LengthUnit.Undefined)
        {
            setUnit(_native, float.NaN);
            return;
        }

        var v = value.Value;
        if (v.Unit == LengthUnit.Percentage)
            setPercent(_native, v.Value);
        else
            setUnit(_native, v.GetPixels(0));
    }

    private void SetEdgeLength(Length? value, YGEdge edge, Action<YGNodeRef, YGEdge, float> setUnit, Action<YGNodeRef, YGEdge, float> setPercent)
    {
        if (!value.HasValue || value.Value.Unit == LengthUnit.Undefined)
        {
            setUnit(_native, edge, float.NaN);
            return;
        }

        var v = value.Value;
        if (v.Unit == LengthUnit.Percentage)
            setPercent(_native, edge, v.Value);
        else
            setUnit(_native, edge, v.GetPixels(0));
    }

    private void SetEdgeLengthWithAuto(Length? value, YGEdge edge, Action<YGNodeRef, YGEdge> setAuto, Action<YGNodeRef, YGEdge, float> setUnit, Action<YGNodeRef, YGEdge, float> setPercent)
    {
        if (!value.HasValue || value.Value.Unit == LengthUnit.Undefined)
        {
            setUnit(_native, edge, float.NaN);
            return;
        }

        var v = value.Value;
        switch (v.Unit)
        {
            case LengthUnit.Auto:
                setAuto(_native, edge);
                break;
            case LengthUnit.Percentage:
                setPercent(_native, edge, v.Value);
                break;
            default:
                setUnit(_native, edge, v.GetPixels(0));
                break;
        }
    }

    private void SetEdgeLengthNoPercent(Length? value, YGEdge edge, Action<YGNodeRef, YGEdge, float> setUnit)
    {
        if (!value.HasValue || value.Value.Unit == LengthUnit.Undefined)
        {
            setUnit(_native, edge, float.NaN);
            return;
        }

        setUnit(_native, edge, value.Value.GetPixels(0));
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        if (!_native.IsNull)
        {
            YogaNative.YGNodeFree(_native);
            _native = YGNodeRef.Null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~YogaWrapper()
    {
        Dispose();
    }

    #endregion
}
