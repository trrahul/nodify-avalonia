using System;
using Avalonia;
using Avalonia.Media;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a line that has an arrow indicating its <see cref="BaseConnection.Direction"/>.
    /// </summary>
    public class LineConnection : BaseConnection
    {
        static LineConnection()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(LineConnection), new FrameworkPropertyMetadata(typeof(LineConnection)));
        }

        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(StreamGeometryContext context, Point source, Point target)
        {
            double direction = Direction == ConnectionDirection.Forward ? 1d : -1d;
            var spacing = new Vector(Spacing * direction, 0d);

            Point p1 = source + spacing;
            Point p2 = target - spacing;

            context.SetFillRule(FillRule.EvenOdd);
            context.BeginFigure(source, false);
            context.LineTo(p1);
            context.LineTo(p2);
            context.LineTo(target);

            return ((target, source), (source, target));
        }

        protected override void DrawDefaultArrowhead(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward)
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
                base.DrawDefaultArrowhead(context, source, target, arrowDirection);
            }
        }
    }
}
