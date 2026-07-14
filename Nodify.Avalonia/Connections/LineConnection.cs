using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a line that has an arrow indicating its <see cref="BaseConnection.Direction"/>.
    /// </summary>
    public class LineConnection : BaseConnection
    {
        public static readonly StyledProperty<double> CornerRadiusProperty =
            AvaloniaProperty.Register<LineConnection, double>(nameof(CornerRadius), 5d);

        /// <summary>The radius of the corners between line segments. 0 = sharp corners.</summary>
        public double CornerRadius
        {
            get => GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        static LineConnection()
        {
            AffectsRender<LineConnection>(CornerRadiusProperty);
            AffectsGeometry<LineConnection>(CornerRadiusProperty);
        }

        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            var (p0, p1) = GetLinePoints(source, target);

            context.SetFillRule(FillRule.EvenOdd);
            context.BeginFigure(source, false);

            // Rounded corners require both a corner radius and spacing (spacing creates the bend points).
            if (CornerRadius > 0 && Spacing > 0)
            {
                AddSmoothCorner(context, source, p0, p1, CornerRadius);
                AddSmoothCorner(context, p0, p1, target, CornerRadius);
            }
            else
            {
                context.LineTo(p0);
                context.LineTo(p1);
            }
            context.LineTo(target);

            return ((target, source), (source, target));
        }

        /// <inheritdoc />
        protected override void DrawDirectionalArrowsGeometry(StreamGeometryContext context, Point source, Point target)
        {
            var (p0, p1) = GetLinePoints(source, target);
            var direction = p0 - p1;

            double spacing = 1d / (DirectionalArrowsCount + 1);
            for (int i = 1; i <= DirectionalArrowsCount; i++)
            {
                double t = (spacing * i + DirectionalArrowsOffset).WrapToRange(0d, 1d);
                var to = InterpolateLineSegment(p0, p1, t);

                DrawDirectionalArrowheadGeometry(context, direction, to);
            }
        }

        private (Point P0, Point P1) GetLinePoints(Point source, Point target)
        {
            double direction = Direction == ConnectionDirection.Forward ? 1d : -1d;
            var spacing = new Vector(Spacing * direction, 0d);
            var spacingVertical = new Vector(spacing.Y, spacing.X);

            var p0 = source + (SourceOrientation == Orientation.Vertical ? spacingVertical : spacing);
            var p1 = target - (TargetOrientation == Orientation.Vertical ? spacingVertical : spacing);

            return (p0, p1);
        }

        protected override void DrawDefaultArrowhead(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward, Orientation orientation = Orientation.Horizontal)
        {
            if (Spacing < 1d)
            {
                Vector delta = source - target;
                double headWidth = ArrowSize.Width;
                double headHeight = ArrowSize.Height / 2;

                double angle = Math.Atan2(delta.Y, delta.X);
                double sinT = Math.Sin(angle);
                double cosT = Math.Cos(angle);

                var from = new Point(target.X + (headWidth * cosT - headHeight * sinT), target.Y + (headWidth * sinT + headHeight * cosT));
                var to = new Point(target.X + (headWidth * cosT + headHeight * sinT), target.Y - (headHeight * cosT - headWidth * sinT));

                context.BeginFigure(target, true);
                context.LineTo(from);
                context.LineTo(to);
                context.EndFigure(true);
            }
            else
            {
                base.DrawDefaultArrowhead(context, source, target, arrowDirection, orientation);
            }
        }

        /// <summary>Linearly interpolates a point along segment [p0, p1] at parameter t (0–1).</summary>
        protected static Point InterpolateLineSegment(Point p0, Point p1, double t)
            => (Point)((1 - t) * (Vector)p0 + t * (Vector)p1);

        /// <summary>
        /// Interpolates along a 4-point polyline [p0-p1-p2-p3] by arc-length-normalised parameter t.
        /// Returns the active segment (start, end) and the interpolated point.
        /// </summary>
        protected static ((Point SegmentStart, Point SegmentEnd), Point InterpolatedPoint)
            InterpolateLine(Point p0, Point p1, Point p2, Point p3, double t)
        {
            double l1 = p1.VectorSubtract(p0).Length;
            double l2 = p2.VectorSubtract(p1).Length;
            double l3 = p3.VectorSubtract(p2).Length;
            double total = l1 + l2 + l3;

            double r1 = l1 / total;
            double r2 = l2 / total;

            if (t <= r1)
                return ((p0, p1), InterpolateLineSegment(p0, p1, t / r1));
            if (t <= r1 + r2)
                return ((p1, p2), InterpolateLineSegment(p1, p2, (t - r1) / r2));
            return ((p2, p3), InterpolateLineSegment(p2, p3, (t - r1 - r2) / (1 - r1 - r2)));
        }

        /// <summary>
        /// Interpolates along a 3-point polyline [p0-p1-p2] by arc-length-normalised parameter t.
        /// Returns the active segment (start, end) and the interpolated point.
        /// Used by <see cref="CircuitConnection"/> for its 3-waypoint route.
        /// </summary>
        protected static ((Point SegmentStart, Point SegmentEnd), Point InterpolatedPoint)
            InterpolateLine(Point p0, Point p1, Point p2, double t)
        {
            double l1 = p1.VectorSubtract(p0).Length;
            double l2 = p2.VectorSubtract(p1).Length;
            double total = l1 + l2;

            double r1 = l1 / total;

            if (t <= r1)
                return ((p0, p1), InterpolateLineSegment(p0, p1, t / r1));
            return ((p1, p2), InterpolateLineSegment(p1, p2, (t - r1) / (1 - r1)));
        }

        /// <summary>
        /// Appends a rounded corner to <paramref name="context"/> from <paramref name="start"/> through
        /// <paramref name="corner"/> toward <paramref name="end"/>, clipped to <paramref name="radius"/>.
        /// </summary>
        protected static void AddSmoothCorner(
            StreamGeometryContext context,
            Point start, Point corner, Point end,
            double radius)
        {
            Vector ab = corner.VectorSubtract(start);
            Vector bc = end.VectorSubtract(corner);

            double distAB = ab.SquaredLength;
            double distBC = bc.SquaredLength;

            double bendSize = Math.Sqrt(Math.Min(distAB, distBC)) / 2;
            radius = Math.Min(bendSize, radius);

            Vector dirToCorner   = distAB != 0 ? ab.Normalize() : ab;
            Vector dirFromCorner = distBC != 0 ? bc.Normalize() : bc;

            Point curveStart = corner - dirToCorner   * radius;
            Point curveEnd   = corner + dirFromCorner * radius;

            context.LineTo(curveStart);
            context.QuadraticBezierTo(corner, curveEnd);
        }
    }
}
