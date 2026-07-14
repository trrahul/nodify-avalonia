using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents the visual cutting-line drawn during a cutting operation in the <see cref="NodifyEditor"/>.
    /// Derived from <see cref="LayeredShape"/> so it lives in the same viewport-transformed Canvas as connections.
    /// </summary>
    public class CuttingLine : LayeredShape
    {
        public static readonly StyledProperty<Point> StartPointProperty =
            AvaloniaProperty.Register<CuttingLine, Point>(nameof(StartPoint));

        public static readonly StyledProperty<Point> EndPointProperty =
            AvaloniaProperty.Register<CuttingLine, Point>(nameof(EndPoint));

        /// <summary>
        /// Attached property set on connection controls when the cutting line intersects
        /// with them while <see cref="NodifyEditor.EnableCuttingLinePreview"/> is true.
        /// </summary>
        public static readonly AttachedProperty<bool> IsOverElementProperty =
            PendingConnection.IsOverElementProperty.AddOwner<CuttingLine>();

        /// <summary>Gets the IsOverElement attached property value for <paramref name="element"/>.</summary>
        public static bool GetIsOverElement(Control element)
            => element.GetValue(IsOverElementProperty);

        /// <summary>Sets the IsOverElement attached property value for <paramref name="element"/>.</summary>
        public static void SetIsOverElement(Control element, bool value)
            => element.SetValue(IsOverElementProperty, value);

        /// <summary>Gets or sets the start point of the cutting line.</summary>
        public Point StartPoint
        {
            get => GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }

        /// <summary>Gets or sets the end point of the cutting line.</summary>
        public Point EndPoint
        {
            get => GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }

        static CuttingLine()
        {
            IsHitTestVisibleProperty.OverrideMetadata<CuttingLine>(
                new StyledPropertyMetadata<bool>(false));
            AffectsGeometry<CuttingLine>(StartPointProperty, EndPointProperty);
            AffectsRender<CuttingLine>(StartPointProperty, EndPointProperty);
        }

        /// <inheritdoc />
        protected override Geometry? CreateDefiningGeometry()
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(StartPoint, false);
                ctx.LineTo(EndPoint);
                ctx.EndFigure(false);
            }
            return geometry;
        }

        /// <inheritdoc />
        protected override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);

            double r = StrokeThickness * 1.2;
            drawingContext.DrawEllipse(Fill, null, StartPoint, r, r);
            drawingContext.DrawEllipse(Fill, null, EndPoint, r, r);
        }
    }
}
