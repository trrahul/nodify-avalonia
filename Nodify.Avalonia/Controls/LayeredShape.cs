using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Media;

namespace Nodify.Avalonia
{
    /// <summary>
    /// Hosts layered rendering for connection shapes: <see cref="Shape.Render"/> is sealed, so this
    /// control composes an underlay renderer, an inner <see cref="Shape"/> that draws the geometry,
    /// and an overlay renderer instead of subclassing <see cref="Shape"/>. It exposes Shape-compatible
    /// properties plus overridable <see cref="CreateDefiningGeometry"/>, <see cref="RenderBackground"/>,
    /// and <see cref="Render"/> hooks.
    /// </summary>
    public class LayeredShape : Panel
    {
        private InnerRenderer? _backgroundRenderer;
        private InnerShape? _underlayShape;
        private InnerShape? _hitTestShape;
        private InnerShape? _shape;
        private InnerRenderer? _renderer;

        public static readonly StyledProperty<IBrush?> FillProperty = Shape.FillProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<Stretch> StretchProperty = Shape.StretchProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<IBrush?> StrokeProperty = Shape.StrokeProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty = Shape.StrokeDashArrayProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<double> StrokeDashOffsetProperty = Shape.StrokeDashOffsetProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<double> StrokeThicknessProperty = Shape.StrokeThicknessProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty = Shape.StrokeLineCapProperty.AddOwner<LayeredShape>();

        public static readonly StyledProperty<PenLineJoin> StrokeJoinProperty = Shape.StrokeJoinProperty.AddOwner<LayeredShape>();

        protected static void AffectsGeometry<TShape>(params AvaloniaProperty[] properties) where TShape : LayeredShape
        {
            foreach (AvaloniaProperty property in properties)
                property.Changed.AddClassHandler<TShape>(AffectsGeometryInvalidate);
        }

        protected new static void AffectsRender<T>(params AvaloniaProperty[] properties) where T : LayeredShape
        {
            foreach (AvaloniaProperty property in properties)
                property.Changed.AddClassHandler<T>((control, e) => control.InvalidateVisual());
        }

        private static void AffectsGeometryInvalidate(LayeredShape control, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == BoundsProperty && e.OldValue is Rect oldBounds && e.NewValue is Rect newBounds && oldBounds.Size == newBounds.Size)
                return;

            control.InvalidateGeometry();
        }

        public LayeredShape()
        {
            _backgroundRenderer = new InnerRenderer { IsHitTestVisible = false };
            _underlayShape = new InnerShape { IsHitTestVisible = false };
            _underlayShape.Bind(StretchProperty, this.GetObservable(StretchProperty), BindingPriority.Template);
            _hitTestShape = new InnerShape { IsHitTestVisible = false };
            _hitTestShape.Bind(StretchProperty, this.GetObservable(StretchProperty), BindingPriority.Template);
            _shape = new InnerShape();
            _shape.Bind(StretchProperty, this.GetObservable(StretchProperty), BindingPriority.Template);
            _shape.Bind(FillProperty, this.GetObservable(FillProperty), BindingPriority.Template);
            _shape.Bind(StrokeProperty, this.GetObservable(StrokeProperty), BindingPriority.Template);
            _shape.Bind(StrokeThicknessProperty, this.GetObservable(StrokeThicknessProperty), BindingPriority.Template);
            _shape.Bind(StrokeDashArrayProperty, this.GetObservable(StrokeDashArrayProperty), BindingPriority.Template);
            _shape.Bind(StrokeDashOffsetProperty, this.GetObservable(StrokeDashOffsetProperty), BindingPriority.Template);
            _shape.Bind(StrokeLineCapProperty, this.GetObservable(StrokeLineCapProperty), BindingPriority.Template);
            _shape.Bind(StrokeJoinProperty, this.GetObservable(StrokeJoinProperty), BindingPriority.Template);
            _renderer = new InnerRenderer();
            Children.Add(_backgroundRenderer);
            Children.Add(_underlayShape);
            Children.Add(_hitTestShape);
            Children.Add(_shape);
            Children.Add(_renderer);
            _backgroundRenderer.OnRender += OnRenderBackground;
            _renderer.OnRender += OnRender;
            _underlayShape.OnCreateDefiningGeometry += OnCreateDefiningGeometry;
            _hitTestShape.OnCreateDefiningGeometry += OnCreateDefiningGeometry;
            _shape.OnCreateDefiningGeometry += OnCreateDefiningGeometry;
        }

        private void InvalidateGeometry()
        {
            _underlayShape?.InvalidateGeometry();
            _hitTestShape?.InvalidateGeometry();
            _shape?.InvalidateGeometry();
        }

        private Geometry? OnCreateDefiningGeometry() => CreateDefiningGeometry();

        private new void InvalidateVisual()
        {
            _backgroundRenderer?.InvalidateVisual();
            _renderer?.InvalidateVisual();
        }

        protected void InvalidateBackgroundVisual() => _backgroundRenderer?.InvalidateVisual();

        protected void UpdateGeometryUnderlay(
            IBrush? brush,
            double thickness,
            PenLineCap lineCap,
            PenLineJoin lineJoin)
        {
            if (_underlayShape != null)
            {
                _underlayShape.Fill = brush;
                _underlayShape.Stroke = brush;
                _underlayShape.StrokeThickness = thickness;
                _underlayShape.StrokeLineCap = lineCap;
                _underlayShape.StrokeJoin = lineJoin;
            }

            if (_hitTestShape != null)
            {
                _hitTestShape.Fill = null;
                _hitTestShape.Stroke = brush != null ? Brushes.Transparent : null;
                _hitTestShape.StrokeThickness = thickness;
                _hitTestShape.StrokeLineCap = lineCap;
                _hitTestShape.StrokeJoin = lineJoin;
                _hitTestShape.IsHitTestVisible = brush != null;
            }
        }

        private void OnRenderBackground(DrawingContext drawingContext) => RenderBackground(drawingContext);

        private void OnRender(DrawingContext drawingContext) => Render(drawingContext);

        protected virtual Geometry? CreateDefiningGeometry() => null;

        protected virtual void RenderBackground(DrawingContext drawingContext) { }

        protected new virtual void Render(DrawingContext drawingContext) { }

        internal Geometry? GeometryForHitTesting =>
            _shape?.RenderedGeometry ?? CreateDefiningGeometry();

        internal bool IsGeometryHost(Visual visual)
            => ReferenceEquals(_underlayShape, visual)
               || ReferenceEquals(_hitTestShape, visual)
               || ReferenceEquals(_shape, visual);

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the shape's interior is painted.
        /// </summary>
        public IBrush? Fill
        {
            get => this.GetValue<IBrush?>(FillProperty);
            set => this.SetValue<IBrush?>(FillProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="Stretch"/> enumeration value that describes how the shape fills its allocated space.
        /// </summary>
        public Stretch Stretch
        {
            get => this.GetValue<Stretch>(StretchProperty);
            set => this.SetValue<Stretch>(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="IBrush"/> that specifies how the shape's outline is painted.
        /// </summary>
        public IBrush? Stroke
        {
            get => this.GetValue<IBrush?>(StrokeProperty);
            set => this.SetValue<IBrush?>(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets a collection of <see cref="double"/> values that indicate the pattern of dashes and gaps that is used to outline shapes.
        /// </summary>
        public AvaloniaList<double>? StrokeDashArray
        {
            get => this.GetValue<AvaloniaList<double>?>(StrokeDashArrayProperty);
            set => this.SetValue<AvaloniaList<double>?>(StrokeDashArrayProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
        /// </summary>
        public double StrokeDashOffset
        {
            get => this.GetValue<double>(StrokeDashOffsetProperty);
            set => this.SetValue<double>(StrokeDashOffsetProperty, value);
        }

        /// <summary>Gets or sets the width of the shape outline.</summary>
        public double StrokeThickness
        {
            get => this.GetValue<double>(StrokeThicknessProperty);
            set => this.SetValue<double>(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
        /// </summary>
        public PenLineCap StrokeLineCap
        {
            get => this.GetValue<PenLineCap>(StrokeLineCapProperty);
            set => this.SetValue<PenLineCap>(StrokeLineCapProperty, value);
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineJoin"/> enumeration value that specifies the type of join that is used at the vertices of a Shape.
        /// </summary>
        public PenLineJoin StrokeJoin
        {
            get => this.GetValue<PenLineJoin>(StrokeJoinProperty);
            set => this.SetValue<PenLineJoin>(StrokeJoinProperty, value);
        }

        private class InnerShape : Shape
        {
            public event Func<Geometry?>? OnCreateDefiningGeometry;

            protected override Geometry? CreateDefiningGeometry()
            {
                return OnCreateDefiningGeometry?.Invoke();
            }

            public new void InvalidateGeometry()
            {
                base.InvalidateGeometry();
            }
        }

        private class InnerRenderer : Control
        {
            public event Action<DrawingContext>? OnRender;

            public override void Render(DrawingContext context)
            {
                base.Render(context);
                OnRender?.Invoke(context);
            }
        }
    }
}
