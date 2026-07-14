using System.Collections.Generic;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Connections;

/// <summary>
/// Represents an orthogonal (right-angle) routed connection that routes through
/// axis-aligned segments based on the <see cref="SourcePosition"/> and
/// <see cref="TargetPosition"/> of the connectors.
/// </summary>
public class StepConnection : LineConnection
{
    #region Dependency Properties

    public static readonly StyledProperty<ConnectorPosition> SourcePositionProperty =
        AvaloniaProperty.Register<StepConnection, ConnectorPosition>(
            nameof(SourcePosition), ConnectorPosition.Right);

    public static readonly StyledProperty<ConnectorPosition> TargetPositionProperty =
        AvaloniaProperty.Register<StepConnection, ConnectorPosition>(
            nameof(TargetPosition), ConnectorPosition.Left);

    /// <summary>Gets or sets the side of the source node the connection exits from.</summary>
    public ConnectorPosition SourcePosition
    {
        get => GetValue(SourcePositionProperty);
        set => SetValue(SourcePositionProperty, value);
    }

    /// <summary>Gets or sets the side of the target node the connection enters into.</summary>
    public ConnectorPosition TargetPosition
    {
        get => GetValue(TargetPositionProperty);
        set => SetValue(TargetPositionProperty, value);
    }

    #endregion

    static StepConnection()
    {
        SourcePositionProperty.Changed.AddClassHandler<StepConnection>(OnConnectorPositionChanged);
        TargetPositionProperty.Changed.AddClassHandler<StepConnection>(OnConnectorPositionChanged);
        AffectsRender<StepConnection>(SourcePositionProperty, TargetPositionProperty);
        AffectsGeometry<StepConnection>(SourcePositionProperty, TargetPositionProperty);

        SourceOrientationProperty.OverrideMetadata<StepConnection>(
            new StyledPropertyMetadata<Orientation>(
                defaultValue: Orientation.Horizontal,
                coerce: CoerceSourceOrientation));

        TargetOrientationProperty.OverrideMetadata<StepConnection>(
            new StyledPropertyMetadata<Orientation>(
                defaultValue: Orientation.Horizontal,
                coerce: CoerceTargetOrientation));

        DirectionProperty.OverrideMetadata<StepConnection>(
            new StyledPropertyMetadata<ConnectionDirection>(
                defaultValue: ConnectionDirection.Forward,
                coerce: CoerceConnectionDirection));

    }

    private static void OnConnectorPositionChanged(
        StepConnection connection, AvaloniaPropertyChangedEventArgs e)
    {
        connection.CoerceValue(DirectionProperty);
        connection.CoerceValue(SourceOrientationProperty);
        connection.CoerceValue(TargetOrientationProperty);
    }

    private static Orientation CoerceSourceOrientation(AvaloniaObject obj, Orientation baseValue)
    {
        var c = (StepConnection)obj;
        return c.SourcePosition is ConnectorPosition.Left or ConnectorPosition.Right
            ? Orientation.Horizontal
            : Orientation.Vertical;
    }

    private static Orientation CoerceTargetOrientation(AvaloniaObject obj, Orientation baseValue)
    {
        var c = (StepConnection)obj;
        return c.TargetPosition is ConnectorPosition.Left or ConnectorPosition.Right
            ? Orientation.Horizontal
            : Orientation.Vertical;
    }

    private static ConnectionDirection CoerceConnectionDirection(AvaloniaObject obj, ConnectionDirection baseValue)
    {
        var c = (StepConnection)obj;
        return c.TargetPosition is ConnectorPosition.Left or ConnectorPosition.Top
            ? ConnectionDirection.Forward
            : ConnectionDirection.Backward;
    }

    #region Drawing overrides

    protected override ((Point ArrowStartSource, Point ArrowStartTarget),
                        (Point ArrowEndSource,   Point ArrowEndTarget))
        DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
    {
        var (p0, p1, p2, p3) = GetLinePoints(source, target);

        if (CornerRadius > 0)
            DrawSmoothLine(context, source, target, p0, p1, p2, p3, CornerRadius);
        else
            DrawDefaultLine(context, source, target, p0, p1, p2, p3);

        if (Spacing < 1d)
            return ((p1, source), (p2, target));

        return ((target, source), (source, target));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Places the label at the midpoint of the longest of the 3 orthogonal segments
    /// (p0-p1, p1-p2, p2-p3) of the route.
    /// </remarks>
    protected override Point GetTextPosition(FormattedText text, Point source, Point target)
    {
        var (p0, p1, p2, p3) = GetLinePoints(source, target);

        Vector delta1 = p1 - p0;
        Vector delta2 = p2 - p1;
        Vector delta3 = p3 - p2;

        Vector max = GetMax(delta1, GetMax(delta2, delta3));

        if (max == delta1)
        {
            return new Point((p0.X + p1.X - text.Width) / 2, (p0.Y + p1.Y - text.Height) / 2);
        }
        else if (max == delta2)
        {
            return new Point((p2.X + p1.X - text.Width) / 2, (p2.Y + p1.Y - text.Height) / 2);
        }

        return new Point((p3.X + p2.X - text.Width) / 2, (p3.Y + p2.Y - text.Height) / 2);

        static Vector GetMax(in Vector a, in Vector b)
            => a.SquaredLength > b.SquaredLength ? a : b;
    }

    private static void DrawDefaultLine(
        StreamGeometryContext context,
        Point source, Point target,
        Point p0, Point p1, Point p2, Point p3)
    {
        context.BeginFigure(source, false);
        context.LineTo(p0);
        context.LineTo(p1);
        context.LineTo(p2);
        context.LineTo(p3);
        context.LineTo(target);
    }

    private static void DrawSmoothLine(
        StreamGeometryContext context,
        Point source, Point target,
        Point p0, Point p1, Point p2, Point p3,
        double cornerRadius)
    {
        context.BeginFigure(source, false);
        AddSmoothCorner(context, source, p0, p1, cornerRadius);

        if (p1 == p2)
        {
            // p1 and p2 overlap — skip the middle corner
            AddSmoothCorner(context, p0, p1, p3, cornerRadius);
        }
        else
        {
            AddSmoothCorner(context, p0, p1, p2, cornerRadius);
            AddSmoothCorner(context, p1, p2, p3, cornerRadius);
        }

        AddSmoothCorner(context, p2, p3, target, cornerRadius);
        context.LineTo(target);
    }

    protected override void DrawDirectionalArrowsGeometry(
        StreamGeometryContext context, Point source, Point target)
    {
        var (p0, p1, p2, p3) = GetLinePoints(source, target);

        double spacing = 1d / (DirectionalArrowsCount + 1);
        for (int i = 1; i <= (int)DirectionalArrowsCount; i++)
        {
            double t = (spacing * i + DirectionalArrowsOffset).WrapToRange(0d, 1d);
            var (segment, to) = InterpolateLine(p0, p1, p2, p3, t);
            var direction = segment.SegmentStart.VectorSubtract(segment.SegmentEnd);
            DrawDirectionalArrowheadGeometry(context, direction, to);
        }
    }

    #endregion

    protected override (Vector SourceOffset, Vector TargetOffset) GetOffset()
    {
        var baseOffset = base.GetOffset();
        var flippedSourceOffset = StepConnectionGeometry.FlipSourceOffset(baseOffset.SourceOffset, SourcePosition);
        return (flippedSourceOffset, baseOffset.TargetOffset);
    }

    private (Point P0, Point P1, Point P2, Point P3) GetLinePoints(Point source, Point target)
        => StepConnectionGeometry.GetLinePoints(
            source, target, SourcePosition, TargetPosition, Spacing);

    /// <inheritdoc />
    protected override IReadOnlyList<Point> GetConnectionWaypoints()
    {
        var (sourceOffset, targetOffset) = GetOffset();
        Point source = Source + sourceOffset;
        Point target = Target + targetOffset;

        var (p0, p1, p2, p3) = GetLinePoints(source, target);
        return new[] { source, p0, p1, p2, p3, target };
    }
}
