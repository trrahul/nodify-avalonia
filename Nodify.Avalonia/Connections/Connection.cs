using System;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a cubic bezier curve connection.
    /// </summary>
    public class Connection : BaseConnection
    {
        // ReSharper disable once InconsistentNaming
        private const double _baseOffset = 100d;
        // ReSharper disable once InconsistentNaming
        private const double _offsetGrowthRate = 25d;

        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget))
            DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            var (p0, p1, p2, p3) = GetBezierControlPoints(source, target);

            context.SetFillRule(FillRule.EvenOdd);
            context.BeginFigure(source, false);
            context.LineTo(p0);
            context.CubicBezierTo(p1, p2, p3);
            context.LineTo(target);

            return ((target, source), (source, target));
        }

        protected override void DrawDirectionalArrowsGeometry(StreamGeometryContext context, Point source, Point target)
        {
            var (p0, p1, p2, p3) = GetBezierControlPoints(source, target);

            double spacing = 1d / (DirectionalArrowsCount + 1);
            for (int i = 1; i <= (int)DirectionalArrowsCount; i++)
            {
                double t = (spacing * i + DirectionalArrowsOffset).WrapToRange(0d, 1d);
                var to = InterpolateCubicBezier(p0, p1, p2, p3, t);
                var direction = GetBezierTangent(p0, p1, p2, p3, t);
                DrawDirectionalArrowheadGeometry(context, direction, to);
            }
        }

        protected override Point GetTextPosition(FormattedText text, Point source, Point target)
        {
            var (p0, p1, p2, p3) = GetBezierControlPoints(source, target);
            var textCenter = new Vector(text.Width / 2, text.Height / 2);
            return InterpolateCubicBezier(p0, p1, p2, p3, 0.5) - textCenter;
        }

        private (Point P0, Point P1, Point P2, Point P3) GetBezierControlPoints(Point source, Point target)
        {
            double direction = Direction == ConnectionDirection.Forward ? 1d : -1d;
            var spacing = new Vector(Spacing * direction, 0d);
            var spacingVertical = new Vector(spacing.Y, spacing.X);

            Point startPoint = source + (SourceOrientation == Orientation.Vertical ? spacingVertical : spacing);
            Point endPoint = target - (TargetOrientation == Orientation.Vertical ? spacingVertical : spacing);

            Vector delta = target - source;
            double height = Math.Abs(delta.Y);
            double width = Math.Abs(delta.X);

            double smooth = Math.Min(_baseOffset, height);
            double offset = Math.Max(smooth, width / 2d);
            offset = Math.Min(_baseOffset + Math.Sqrt(width * _offsetGrowthRate), offset);

            var controlPoint = new Vector(offset * direction, 0d);
            var controlPointVertical = new Vector(controlPoint.Y, controlPoint.X);

            if (TargetOrientation != SourceOrientation)
            {
                controlPoint *= 0.5;
            }

            Point p0 = startPoint;
            Point p1 = startPoint + (SourceOrientation == Orientation.Vertical ? controlPointVertical : controlPoint);
            Point p2 = endPoint - (TargetOrientation == Orientation.Vertical ? controlPointVertical : controlPoint);
            Point p3 = endPoint;

            return (p0, p1, p2, p3);
        }

        internal static Vector GetBezierTangent(Point p0, Point p1, Point p2, Point p3, double t)
        {
            // Negated derivative of the cubic Bezier (the arrowhead direction convention).
            return -((Vector)p0 * (-3 * (1 - t) * (1 - t))
                   + (Vector)p1 * (3 * (1 - t) * (1 - t) - 6 * t * (1 - t))
                   + (Vector)p2 * (6 * t * (1 - t) - 3 * t * t)
                   + (Vector)p3 * (3 * t * t));
        }

        internal static Point InterpolateCubicBezier(Point p0, Point p1, Point p2, Point p3, double t)
        {
            // B = (1-t)^3*P0 + 3t(1-t)^2*P1 + 3t^2(1-t)*P2 + t^3*P3
            return (Point)
                 ((Vector)p0 * ((1 - t) * (1 - t) * (1 - t))
                + (Vector)p1 * (3 * t * (1 - t) * (1 - t))
                + (Vector)p2 * (3 * t * t * (1 - t))
                + (Vector)p3 * (t * t * t));
        }
    }
}
