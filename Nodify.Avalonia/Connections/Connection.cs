using System;
using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a quadratic curve.
    /// </summary>
    public class Connection : BaseConnection
    {
        static Connection()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(Connection), new FrameworkPropertyMetadata(typeof(Connection)));
            //AffectsRender<Connection>(SourceProperty,TargetProperty,SourceOffsetProperty,TargetOffsetProperty,SourceOffsetModeProperty,TargetOffsetModeProperty,DirectionProperty,SpacingProperty,ArrowSizeProperty,ArrowEndsProperty,ArrowShapeProperty);
        }

        // ReSharper disable once InconsistentNaming
        private const double _baseOffset = 100d;
        // ReSharper disable once InconsistentNaming
        private const double _offsetGrowthRate = 25d;

        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            double direction = Direction == ConnectionDirection.Forward ? 1d : -1d;
            var spacing = new Vector(Spacing * direction, 0d);
            Point startPoint = source + spacing;
            Point endPoint = target - spacing;

            Vector delta = target - source;
            double height = Math.Abs(delta.Y);
            double width = Math.Abs(delta.X);

            // Smooth curve when distance is lower than base offset
            double smooth = Math.Min(_baseOffset, height);
            // Calculate offset based on distance
            double offset = Math.Max(smooth, width / 2d);
            // Grow slowly with distance
            offset = Math.Min(_baseOffset + Math.Sqrt(width * _offsetGrowthRate), offset);

            var controlPoint = new Vector(offset * direction, 0d);
            context.SetFillRule(FillRule.EvenOdd);
            context.BeginFigure(source, false);
            context.LineTo(startPoint);
            context.CubicBezierTo(startPoint + controlPoint, endPoint - controlPoint, endPoint);
            context.LineTo(target);
            //context.EndFigure(false);
            return ((target, source), (source, target));
        }
    }
}
