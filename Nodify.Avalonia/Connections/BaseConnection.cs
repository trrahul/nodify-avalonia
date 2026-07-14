using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Nodify.Avalonia;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Specifies the offset type that can be applied to a <see cref="BaseConnection"/> using the <see cref="BaseConnection.SourceOffset"/> and the <see cref="BaseConnection.TargetOffset"/> values.
    /// </summary>
    public enum ConnectionOffsetMode
    {
        /// <summary>
        /// No offset applied.
        /// </summary>
        None,

        /// <summary>
        /// The offset is applied in a circle around the point.
        /// </summary>
        Circle,

        /// <summary>
        /// The offset is applied in a rectangle shape around the point.
        /// </summary>
        Rectangle,

        /// <summary>
        /// The offset is applied in a rectangle shape around the point, perpendicular to the edges.
        /// </summary>
        Edge,

        /// <summary>
        /// The offset is applied as a fixed margin.
        /// </summary>
        Static
    }

    /// <summary>
    /// The direction in which a connection is oriented.
    /// </summary>
    public enum ConnectionDirection
    {
        /// <summary>
        /// From <see cref="BaseConnection.Source"/> to <see cref="BaseConnection.Target"/>.
        /// </summary>
        Forward,

        /// <summary>
        /// From <see cref="BaseConnection.Target"/> to <see cref="BaseConnection.Source"/>.
        /// </summary>
        Backward
    }

    /// <summary>
    /// The end at which the arrow head is drawn.
    /// </summary>
    public enum ArrowHeadEnds
    {
        /// <summary>
        /// Arrow head at start.
        /// </summary>
        Start,

        /// <summary>
        /// Arrow head at end.
        /// </summary>
        End,

        /// <summary>
        /// Arrow heads at both ends.
        /// </summary>
        Both,

        /// <summary>
        /// No arrow head.
        /// </summary>
        None
    }

    /// <summary>
    /// The shape of the arrowhead.
    /// </summary>
    public enum ArrowHeadShape
    {
        /// <summary>
        /// The default arrowhead.
        /// </summary>
        Arrowhead,

        /// <summary>
        /// An ellipse.
        /// </summary>
        Ellipse,

        /// <summary>
        /// A rectangle.
        /// </summary>
        Rectangle
    }

    /// <summary>
    /// Represents the base class for shapes that are drawn from a <see cref="Source"/> point to a <see cref="Target"/> point.
    /// </summary>
    public abstract class BaseConnection : LayeredShape, IKeyboardFocusTarget<Control>, ICustomHitTest
    {
        #region Attached Properties (Selection)

        /// <summary>Whether to prioritize <see cref="BaseConnection"/> controls inside custom connection wrappers when resolving the target for IsSelectable/IsSelected.</summary>
        public static bool PrioritizeBaseConnectionForSelection { get; set; } = true;

        public static readonly AttachedProperty<bool> IsSelectableProperty =
            AvaloniaProperty.RegisterAttached<BaseConnection, Control, bool>("IsSelectable", defaultValue: false);

        public static readonly AttachedProperty<bool> IsSelectedProperty =
            AvaloniaProperty.RegisterAttached<BaseConnection, Control, bool>("IsSelected", defaultValue: false,
                defaultBindingMode: BindingMode.TwoWay);

        public static bool GetIsSelectable(Control? elem)
            => elem?.GetValue(IsSelectableProperty) ?? false;

        public static void SetIsSelectable(Control? elem, bool value)
            => elem?.SetValue(IsSelectableProperty, value);

        public static bool GetIsSelected(Control? elem)
            => elem?.GetValue(IsSelectedProperty) ?? false;

        public static void SetIsSelected(Control? elem, bool value)
            => elem?.SetValue(IsSelectedProperty, value);

        private static void OnIsSelectedAttachedChanged(Control d, AvaloniaPropertyChangedEventArgs<bool> e)
        {
            var container = (d as BaseConnection)?.Container ?? d.FindAncestorOfType<ConnectionContainer>();
            if (container != null)
            {
                container.IsSelected = e.NewValue.Value;
            }
        }

        #endregion

        #region IKeyboardFocusTarget<Control>

        global::Avalonia.Rect IKeyboardFocusTarget<Control>.Bounds
            => new global::Avalonia.Rect(
                Direction == ConnectionDirection.Forward ? Target : Source,
                Direction == ConnectionDirection.Forward ? Target : Source);

        Control IKeyboardFocusTarget<Control>.Element => this;

        private IDisposable? _focusVisualResourceSubscription;

        internal void UpdateFocusVisual() => InvalidateBackgroundVisual();

        private void DrawFocusVisual(DrawingContext drawingContext)
        {
            IPen? focusPen = GetFocusVisualPen();
            if (focusPen is null)
            {
                return;
            }

            Geometry? outline = CreateFocusVisualOutlineGeometry(focusPen);
            if (outline != null)
            {
                drawingContext.DrawGeometry(null, CreateFocusVisualDrawingPen(focusPen), outline);
            }
        }

        internal IPen CreateFocusVisualDrawingPen(IPen focusPen)
            => new Pen(focusPen.Brush, focusPen.Thickness)
            {
                DashStyle = focusPen.DashStyle,
                LineCap = focusPen.LineCap,
                LineJoin = StrokeJoin,
                MiterLimit = focusPen.MiterLimit,
            };

        private IPen? GetFocusVisualPen()
        {
            IPen? focusPen = FocusVisualPen;
            if (!ReferenceEquals(focusPen, DefaultFocusVisualPen))
            {
                return focusPen;
            }

            return TryGetResource(FocusVisualPenKey, ActualThemeVariant, out object? connectionResource)
                && connectionResource is IPen connectionPen
                ? connectionPen
                : focusPen;
        }

        protected override void RenderBackground(DrawingContext drawingContext)
        {
            if (Container?.IsFocused == true)
            {
                DrawFocusVisual(drawingContext);
            }

        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _focusVisualResourceSubscription?.Dispose();
            _focusVisualResourceSubscription = this
                .GetResourceObservable(FocusVisualPenKey, static value => value)
                .Subscribe(new FocusVisualResourceObserver(this));
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _focusVisualResourceSubscription?.Dispose();
            _focusVisualResourceSubscription = null;
            base.OnDetachedFromVisualTree(e);
        }

        private sealed class FocusVisualResourceObserver(BaseConnection connection) : IObserver<object?>
        {
            public void OnNext(object? value) => connection.InvalidateBackgroundVisual();

            public void OnError(Exception error) { }

            public void OnCompleted() { }
        }

        #endregion

        #region FocusVisual

        private static readonly IPen DefaultFocusVisualPen = new Pen(Brushes.DodgerBlue, 0.75d)
        {
            DashStyle = new DashStyle(new double[] { 3d, 3d }, 0d)
        }.ToImmutable();

        /// <summary>
        /// The resource key used to resolve the themed focus visual pen.
        /// </summary>
        public static string FocusVisualPenKey { get; } = nameof(FocusVisualPen);

        public static readonly StyledProperty<IPen?> FocusVisualPenProperty =
            AvaloniaProperty.Register<BaseConnection, IPen?>(nameof(FocusVisualPen), DefaultFocusVisualPen);

        public static readonly StyledProperty<double> FocusVisualPaddingProperty =
            AvaloniaProperty.Register<BaseConnection, double>(nameof(FocusVisualPadding), 1d);

        /// <summary>Gets or sets the pen used to draw the keyboard-focus outline around the connection.</summary>
        public IPen? FocusVisualPen
        {
            get => GetValue(FocusVisualPenProperty);
            set => SetValue(FocusVisualPenProperty, value);
        }

        /// <summary>Gets or sets the gap between the connection stroke and its focus outline.</summary>
        public double FocusVisualPadding
        {
            get => GetValue(FocusVisualPaddingProperty);
            set => SetValue(FocusVisualPaddingProperty, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<Point> SourceProperty = AvaloniaProperty.Register<BaseConnection,Point>(nameof(Source));
        public static readonly StyledProperty<Point> TargetProperty = AvaloniaProperty.Register<BaseConnection,Point>(nameof(Target));
        public static readonly StyledProperty<Size> SourceOffsetProperty = AvaloniaProperty.Register<BaseConnection, Size>(nameof(SourceOffset), new Size(14, 0));
        public static readonly StyledProperty<Size> TargetOffsetProperty = AvaloniaProperty.Register<BaseConnection,Size>(nameof(TargetOffset), new Size(14, 0));
        public static readonly StyledProperty<ConnectionOffsetMode> SourceOffsetModeProperty = AvaloniaProperty.Register<BaseConnection, ConnectionOffsetMode>(nameof(SourceOffsetMode), ConnectionOffsetMode.Static);
        public static readonly StyledProperty<ConnectionOffsetMode> TargetOffsetModeProperty = AvaloniaProperty.Register<BaseConnection, ConnectionOffsetMode>(nameof(TargetOffsetMode), ConnectionOffsetMode.Static);
        public static readonly StyledProperty<ConnectionDirection> DirectionProperty = AvaloniaProperty.Register<BaseConnection, ConnectionDirection>(nameof(Direction));
        public static readonly StyledProperty<double> SpacingProperty = AvaloniaProperty.Register<BaseConnection, double>(nameof(Spacing), 0d);
        public static readonly StyledProperty<Size> ArrowSizeProperty = AvaloniaProperty.Register<BaseConnection, Size>(nameof(ArrowSize),new Size(8, 8));
        public static readonly StyledProperty<ArrowHeadEnds> ArrowEndsProperty = AvaloniaProperty.Register<BaseConnection, ArrowHeadEnds>(nameof(ArrowEnds),ArrowHeadEnds.End);
        public static readonly StyledProperty<ArrowHeadShape> ArrowShapeProperty = AvaloniaProperty.Register<BaseConnection, ArrowHeadShape>(nameof(ArrowShape), ArrowHeadShape.Arrowhead);
        public static readonly StyledProperty<ICommand?> SplitCommandProperty = AvaloniaProperty.Register<BaseConnection,ICommand?>(nameof(SplitCommand));
        public static readonly StyledProperty<ICommand?> DisconnectCommandProperty = Connector.DisconnectCommandProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<bool> HasCustomContextMenuProperty =
            NodifyEditor.HasCustomContextMenuProperty.AddOwner<BaseConnection>();

        public static readonly StyledProperty<Orientation> SourceOrientationProperty =
            AvaloniaProperty.Register<BaseConnection, Orientation>(nameof(SourceOrientation), Orientation.Horizontal);

        public static readonly StyledProperty<Orientation> TargetOrientationProperty =
            AvaloniaProperty.Register<BaseConnection, Orientation>(nameof(TargetOrientation), Orientation.Horizontal);

        public static readonly StyledProperty<uint> DirectionalArrowsCountProperty =
            AvaloniaProperty.Register<BaseConnection, uint>(nameof(DirectionalArrowsCount), 0u);

        public static readonly StyledProperty<double> DirectionalArrowsOffsetProperty =
            AvaloniaProperty.Register<BaseConnection, double>(nameof(DirectionalArrowsOffset), 0d);

        public static readonly StyledProperty<bool> IsAnimatingDirectionalArrowsProperty =
            AvaloniaProperty.Register<BaseConnection, bool>(nameof(IsAnimatingDirectionalArrows), false);

        public static readonly StyledProperty<double> DirectionalArrowsAnimationDurationProperty =
            AvaloniaProperty.Register<BaseConnection, double>(nameof(DirectionalArrowsAnimationDuration), 2d);

        public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
            AvaloniaProperty.Register<BaseConnection, IBrush?>(nameof(OutlineBrush));
        public static readonly StyledProperty<double> OutlineThicknessProperty =
            AvaloniaProperty.Register<BaseConnection, double>(nameof(OutlineThickness), 5d);
        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<IBrush?> TextBackgroundProperty =
            AvaloniaProperty.Register<BaseConnection, IBrush?>(nameof(TextBackground));
        public static readonly StyledProperty<Thickness> TextPaddingProperty =
            AvaloniaProperty.Register<BaseConnection, Thickness>(nameof(TextPadding), new Thickness(2d));
        public static readonly StyledProperty<double> TextCornerRadiusProperty =
            AvaloniaProperty.Register<BaseConnection, double>(nameof(TextCornerRadius), 0d);
        public static readonly StyledProperty<double> FontSizeProperty =
            TextElement.FontSizeProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<FontFamily> FontFamilyProperty =
            TextElement.FontFamilyProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<FontWeight> FontWeightProperty =
            TextElement.FontWeightProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<FontStyle> FontStyleProperty =
            TextElement.FontStyleProperty.AddOwner<BaseConnection>();
        public static readonly StyledProperty<FontStretch> FontStretchProperty =
            TextElement.FontStretchProperty.AddOwner<BaseConnection>();

        /// <summary>Gets or sets the orientation of the source connector exit direction.</summary>
        public Orientation SourceOrientation
        {
            get => GetValue(SourceOrientationProperty);
            set => SetValue(SourceOrientationProperty, value);
        }

        /// <summary>Gets or sets the orientation of the target connector entry direction.</summary>
        public Orientation TargetOrientation
        {
            get => GetValue(TargetOrientationProperty);
            set => SetValue(TargetOrientationProperty, value);
        }

        /// <summary>Gets or sets the number of directional flow arrows drawn along the connection.</summary>
        public uint DirectionalArrowsCount
        {
            get => GetValue(DirectionalArrowsCountProperty);
            set => SetValue(DirectionalArrowsCountProperty, value);
        }

        /// <summary>Gets or sets the offset (0–1) applied to the directional arrows position.</summary>
        public double DirectionalArrowsOffset
        {
            get => GetValue(DirectionalArrowsOffsetProperty);
            set => SetValue(DirectionalArrowsOffsetProperty, value);
        }

        /// <summary>Gets or sets whether the directional arrows animate (march) along the connection.</summary>
        public bool IsAnimatingDirectionalArrows { get => GetValue(IsAnimatingDirectionalArrowsProperty); set => SetValue(IsAnimatingDirectionalArrowsProperty, value); }

        /// <summary>Gets or sets the duration (seconds) for an arrow to travel from source to target.</summary>
        public double DirectionalArrowsAnimationDuration { get => GetValue(DirectionalArrowsAnimationDurationProperty); set => SetValue(DirectionalArrowsAnimationDurationProperty, value); }

        /// <summary>Gets or sets the brush used to draw the outline behind the connection geometry.</summary>
        public IBrush? OutlineBrush
        {
            get => GetValue(OutlineBrushProperty);
            set => SetValue(OutlineBrushProperty, value);
        }

        /// <summary>Gets or sets the thickness of the outline drawn behind the connection geometry.</summary>
        public double OutlineThickness
        {
            get => GetValue(OutlineThicknessProperty);
            set => SetValue(OutlineThicknessProperty, value);
        }

        /// <summary>Gets or sets the brush used to paint the connection label text.</summary>
        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>Gets or sets the text label displayed on the connection.</summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>Gets or sets the background brush drawn behind the text label.</summary>
        public IBrush? TextBackground
        {
            get => GetValue(TextBackgroundProperty);
            set => SetValue(TextBackgroundProperty, value);
        }

        /// <summary>Gets or sets the padding applied around the text label background.</summary>
        public Thickness TextPadding
        {
            get => GetValue(TextPaddingProperty);
            set => SetValue(TextPaddingProperty, value);
        }

        /// <summary>Gets or sets the corner radius of the text label background rectangle.</summary>
        public double TextCornerRadius
        {
            get => GetValue(TextCornerRadiusProperty);
            set => SetValue(TextCornerRadiusProperty, value);
        }

        /// <summary>Gets or sets the font size of the connection label text.</summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>Gets or sets the font family of the connection label text.</summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>Gets or sets the font weight of the connection label text.</summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>Gets or sets the font style of the connection label text.</summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>Gets or sets the font stretch of the connection label text.</summary>
        public FontStretch FontStretch
        {
            get => GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        /// <summary>
        /// Gets or sets the start point of this connection.
        /// </summary>
        public Point Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point of this connection.
        /// </summary>
        public Point Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets the offset from the <see cref="Source"/> point.
        /// </summary>
        public Size SourceOffset
        {
            get => GetValue(SourceOffsetProperty);
            set => SetValue(SourceOffsetProperty, value);
        }

        /// <summary>
        /// Gets or sets the offset from the <see cref="Target"/> point.
        /// </summary>
        public Size TargetOffset
        {
            get => GetValue(TargetOffsetProperty);
            set => SetValue(TargetOffsetProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ConnectionOffsetMode"/> to apply to the <see cref="Source"/> when drawing the connection.
        /// </summary>
        public ConnectionOffsetMode SourceOffsetMode
        {
            get => GetValue(SourceOffsetModeProperty);
            set => SetValue(SourceOffsetModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="ConnectionOffsetMode"/> to apply to the <see cref="Target"/> when drawing the connection.
        /// </summary>
        public ConnectionOffsetMode TargetOffsetMode
        {
            get => GetValue(TargetOffsetModeProperty);
            set => SetValue(TargetOffsetModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the direction in which this connection is oriented.
        /// </summary>
        public ConnectionDirection Direction
        {
            get => GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the arrowhead ends.
        /// </summary>
        public ArrowHeadEnds ArrowEnds 
        { 
            get => GetValue(ArrowEndsProperty);
            set => SetValue(ArrowEndsProperty, value);
        }

        /// <summary>
        /// Gets or sets the arrowhead ends.
        /// </summary>
        public ArrowHeadShape ArrowShape
        {
            get => GetValue(ArrowShapeProperty);
            set => SetValue(ArrowShapeProperty, value);
        }

        /// <summary>
        /// The distance between the start point and the where the angle breaks.
        /// </summary>
        public double Spacing
        {
            get => GetValue(SpacingProperty);
            set => SetValue(SpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the arrow head.
        /// </summary>
        public Size ArrowSize
        {
            get => GetValue(ArrowSizeProperty);
            set => SetValue(ArrowSizeProperty, value);
        }

        /// <summary>
        /// Splits the connection. Triggered by the <see cref="EditorGestures.ConnectionGestures.Split"/>
        /// gesture. Parameter is the location where the splitting ocurred.
        /// </summary>
        public ICommand? SplitCommand
        {
            get => GetValue(SplitCommandProperty);
            set => SetValue(SplitCommandProperty, value);
        }

        /// <summary>
        /// Removes this connection. Triggered by the <see cref="EditorGestures.ConnectionGestures.Disconnect"/>
        /// gesture. Parameter is the location where the disconnect ocurred.
        /// </summary>
        public ICommand? DisconnectCommand
        {
            get => (ICommand?)GetValue(DisconnectCommandProperty);
            set => SetValue(DisconnectCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this connection draws a custom context menu (not via <see cref="Control.ContextMenu"/>).
        /// Used to suppress starting an interaction so the context menu can appear.
        /// </summary>
        public bool HasCustomContextMenu
        {
            get => (bool)GetValue(HasCustomContextMenuProperty);
            set => SetValue(HasCustomContextMenuProperty, value);
        }

        /// <summary>
        /// Gets whether this connection has a context menu (either a <see cref="Control.ContextMenu"/> or a <see cref="HasCustomContextMenu"/>).
        /// </summary>
        public bool HasContextMenu => ContextMenu != null || HasCustomContextMenu;

        #endregion

        #region Routed Events

        public static readonly RoutedEvent DisconnectEvent = RoutedEvent.Register<BaseConnection,ConnectionEventArgs>(nameof(Disconnect), RoutingStrategies.Bubble);
        public static readonly RoutedEvent SplitEvent = RoutedEvent.Register<BaseConnection,ConnectionEventArgs>(nameof(Split), RoutingStrategies.Bubble);

        /// <summary>Triggered by the <see cref="EditorGestures.ConnectionGestures.Disconnect"/> gesture.</summary>
        public event EventHandler<ConnectionEventArgs> Disconnect
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        /// <summary>Triggered by the <see cref="EditorGestures.ConnectionGestures.Split"/> gesture.</summary>
        public event EventHandler<ConnectionEventArgs> Split
        {
            add => AddHandler(SplitEvent, value);
            remove => RemoveHandler(SplitEvent, value);
        }

        #endregion

        /// <summary>
        /// Gets the effective gesture mappings for this connection (per-editor override or global fallback).
        /// </summary>
        public EditorGestures ActualGestures => this.FindAncestorOfType<NodifyEditor>()?.ActualGestures ?? EditorGestures.Mappings;

        protected static readonly Vector ZeroVector = new Vector(0d, 0d);

        internal const double CuttingHitTestTolerance = 2d;

        protected override Geometry? CreateDefiningGeometry()
            => CreateConnectionGeometry(includeDirectionalArrows: true);

        internal Geometry? CreateFocusVisualGeometry()
            => CreateConnectionGeometry(includeDirectionalArrows: true);

        internal Geometry? CreateFocusVisualOutlineGeometry(IPen focusPen)
        {
            double thickness = StrokeThickness + focusPen.Thickness + FocusVisualPadding * 2d;
            var widenPen = new Pen(Brushes.Transparent, thickness)
            {
                LineCap = StrokeLineCap,
                LineJoin = StrokeJoin
            };

            Geometry silhouette = CreateConnectionLineGeometry().GetWidenedGeometry(widenPen);
            Geometry? arrowheads = CreateArrowheadsGeometry();
            if (arrowheads is null)
            {
                return silhouette;
            }

            Geometry widenedArrowheads = arrowheads.GetWidenedGeometry(widenPen);
            Geometry expandedArrowheads = new CombinedGeometry(
                GeometryCombineMode.Union,
                arrowheads,
                widenedArrowheads);
            return new CombinedGeometry(
                GeometryCombineMode.Union,
                silhouette,
                expandedArrowheads);
        }

        private Geometry CreateConnectionLineGeometry()
        {
            var geometry = new StreamGeometry();
            using StreamGeometryContext context = geometry.Open();
            (Vector sourceOffset, Vector targetOffset) = GetOffset();
            DrawLineGeometry(context, Source + sourceOffset, Target + targetOffset);
            return geometry;
        }

        private Geometry? CreateArrowheadsGeometry()
        {
            if (ArrowSize.Width == 0d || ArrowSize.Height == 0d
                || (ArrowEnds == ArrowHeadEnds.None && DirectionalArrowsCount == 0))
            {
                return null;
            }

            (Vector sourceOffset, Vector targetOffset) = GetOffset();
            Point source = Source + sourceOffset;
            Point target = Target + targetOffset;
            ((Point ArrowStartSource, Point ArrowStartTarget) ArrowStart,
             (Point ArrowEndSource, Point ArrowEndTarget) ArrowEnd) arrows;
            var lineGeometry = new StreamGeometry();
            using (StreamGeometryContext lineContext = lineGeometry.Open())
            {
                arrows = DrawLineGeometry(
                    lineContext,
                    source,
                    target);
            }

            var geometry = new StreamGeometry();
            using StreamGeometryContext context = geometry.Open();
            var reverseDirection = Direction == ConnectionDirection.Forward
                ? ConnectionDirection.Backward
                : ConnectionDirection.Forward;

            if (ArrowEnds is ArrowHeadEnds.End or ArrowHeadEnds.Both)
            {
                DrawArrowGeometry(
                    context,
                    arrows.ArrowEnd.ArrowEndSource,
                    arrows.ArrowEnd.ArrowEndTarget,
                    Direction,
                    ArrowShape,
                    TargetOrientation);
            }

            if (ArrowEnds is ArrowHeadEnds.Start or ArrowHeadEnds.Both)
            {
                DrawArrowGeometry(
                    context,
                    arrows.ArrowStart.ArrowStartSource,
                    arrows.ArrowStart.ArrowStartTarget,
                    reverseDirection,
                    ArrowShape,
                    SourceOrientation);
            }

            if (DirectionalArrowsCount > 0)
            {
                DrawDirectionalArrowsGeometry(context, source, target);
            }

            return geometry;
        }

        private Geometry? CreateConnectionGeometry(bool includeDirectionalArrows)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.SetFillRule(FillRule.EvenOdd);
                (Vector sourceOffset, Vector targetOffset) = GetOffset();
                Point source = Source + sourceOffset;
                Point target = Target + targetOffset;
                var (arrowStart, arrowEnd) = DrawLineGeometry(context, source, target);

                if (ArrowSize.Width != 0d && ArrowSize.Height != 0d)
                {
                    var reverseDirection = Direction == ConnectionDirection.Forward ? ConnectionDirection.Backward : ConnectionDirection.Forward;
                    switch (ArrowEnds)
                    {
                        case ArrowHeadEnds.Start:
                            DrawArrowGeometry(context, arrowStart.ArrowStartSource, arrowStart.ArrowStartTarget, reverseDirection, ArrowShape, SourceOrientation);
                            break;
                        case ArrowHeadEnds.End:
                            DrawArrowGeometry(context, arrowEnd.ArrowEndSource, arrowEnd.ArrowEndTarget, Direction, ArrowShape, TargetOrientation);
                            break;
                        case ArrowHeadEnds.Both:
                            DrawArrowGeometry(context, arrowEnd.ArrowEndSource, arrowEnd.ArrowEndTarget, Direction, ArrowShape, TargetOrientation);
                            DrawArrowGeometry(context, arrowStart.ArrowStartSource, arrowStart.ArrowStartTarget, reverseDirection, ArrowShape, SourceOrientation);
                            break;
                        case ArrowHeadEnds.None:
                        default:
                            break;
                    }

                    if (includeDirectionalArrows && DirectionalArrowsCount > 0)
                    {
                        DrawDirectionalArrowsGeometry(context, source, target);
                    }
                }
            }

            return geometry;
        }

        protected abstract ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(StreamGeometryContext context, Point source, Point target);

        /// <summary>
        /// Override to draw directional flow arrows along the connection path.
        /// The default implementation is a no-op.
        /// </summary>
        protected virtual void DrawDirectionalArrowsGeometry(StreamGeometryContext context, Point source, Point target) { }

        /// <summary>
        /// Draws a single directional arrowhead at <paramref name="point"/> pointing along <paramref name="direction"/>.
        /// Called by <see cref="DrawDirectionalArrowsGeometry"/> overrides.
        /// </summary>
        protected void DrawDirectionalArrowheadGeometry(StreamGeometryContext context, Vector direction, Point point)
        {
            Vector normalized = direction.Normalize();

            // If normalization failed (e.g. because the vector is too small/zero-length), just
            // don't draw anything.
            if (!double.IsFinite(normalized.X))
            {
                return;
            }

            double headWidth = ArrowSize.Width;
            double headHeight = ArrowSize.Height / 2;

            double angle = Math.Atan2(normalized.Y, normalized.X);
            double sinT = Math.Sin(angle);
            double cosT = Math.Cos(angle);

            var from = new Point(point.X + (headWidth * cosT - headHeight * sinT),
                                 point.Y + (headWidth * sinT + headHeight * cosT));
            var to   = new Point(point.X + (headWidth * cosT + headHeight * sinT),
                                 point.Y - (headHeight * cosT - headWidth * sinT));

            context.BeginFigure(point, true);
            context.LineTo(from);
            context.LineTo(to);
            context.EndFigure(true);
        }

        protected virtual void DrawArrowGeometry(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward, ArrowHeadShape shape = ArrowHeadShape.Arrowhead, Orientation orientation = Orientation.Horizontal)
        {
            switch (shape)
            {
                case ArrowHeadShape.Ellipse:
                    DrawEllipseArrowhead(context, source, target, arrowDirection, orientation);
                    break;
                case ArrowHeadShape.Rectangle:
                    DrawRectangleArrowhead(context, source, target, arrowDirection, orientation);
                    break;
                case ArrowHeadShape.Arrowhead:
                default:
                    DrawDefaultArrowhead(context, source, target, arrowDirection, orientation);
                    break;
            }
        }

        protected virtual void DrawDefaultArrowhead(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward, Orientation orientation = Orientation.Horizontal)
        {
            double direction = arrowDirection == ConnectionDirection.Forward ? 1d : -1d;
            context.SetFillRule(FillRule.EvenOdd);

            if (orientation == Orientation.Horizontal)
            {
                double headWidth  = ArrowSize.Width;
                double headHeight = ArrowSize.Height / 2;

                var from = new Point(target.X - headWidth * direction, target.Y + headHeight);
                var to   = new Point(target.X - headWidth * direction, target.Y - headHeight);
                context.BeginFigure(target, true);
                context.LineTo(from);
                context.LineTo(to);
                context.EndFigure(true);
            }
            else
            {
                double headWidth  = ArrowSize.Width / 2;
                double headHeight = ArrowSize.Height;

                var from = new Point(target.X - headWidth, target.Y - headHeight * direction);
                var to   = new Point(target.X + headWidth, target.Y - headHeight * direction);
                context.BeginFigure(target, true);
                context.LineTo(from);
                context.LineTo(to);
                context.EndFigure(true);
            }
        }

        static BaseConnection()
        {
            StrokeJoinProperty.OverrideMetadata<BaseConnection>(
                new StyledPropertyMetadata<PenLineJoin>(PenLineJoin.Round));

            AffectsRender<BaseConnection>(SourceProperty,TargetProperty,SourceOffsetProperty,TargetOffsetProperty,SourceOffsetModeProperty,TargetOffsetModeProperty,DirectionProperty,SpacingProperty,ArrowSizeProperty,ArrowEndsProperty,ArrowShapeProperty,FillProperty);
            AffectsGeometry<BaseConnection>(SourceProperty,TargetProperty,SourceOffsetProperty,TargetOffsetProperty,SourceOffsetModeProperty,TargetOffsetModeProperty,DirectionProperty,SpacingProperty,ArrowSizeProperty,ArrowEndsProperty,ArrowShapeProperty,FillProperty);
            AffectsRender<BaseConnection>(SourceOrientationProperty, TargetOrientationProperty,
                DirectionalArrowsCountProperty, DirectionalArrowsOffsetProperty);
            AffectsGeometry<BaseConnection>(SourceOrientationProperty, TargetOrientationProperty,
                DirectionalArrowsCountProperty, DirectionalArrowsOffsetProperty);
            AffectsRender<BaseConnection>(OutlineBrushProperty, OutlineThicknessProperty,
                ForegroundProperty, TextProperty, TextBackgroundProperty, TextPaddingProperty, TextCornerRadiusProperty,
                FontSizeProperty, FontFamilyProperty, FontWeightProperty, FontStyleProperty, FontStretchProperty);
            OutlineBrushProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
            OutlineThicknessProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
            StrokeLineCapProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
            StrokeJoinProperty.Changed.AddClassHandler<BaseConnection>(OnOutlinePenChanged);
            AffectsRender<BaseConnection>(IsAnimatingDirectionalArrowsProperty, DirectionalArrowsAnimationDurationProperty);
            IsAnimatingDirectionalArrowsProperty.Changed.AddClassHandler<BaseConnection>(OnIsAnimatingDirectionalArrowsChanged);
            DirectionalArrowsAnimationDurationProperty.Changed.AddClassHandler<BaseConnection>(OnDirectionalArrowsAnimationDurationChanged);
            AffectsRender<BaseConnection>(
                FocusVisualPenProperty,
                FocusVisualPaddingProperty,
                StrokeThicknessProperty,
                StrokeLineCapProperty,
                StrokeJoinProperty);
            IsSelectedProperty.Changed.AddClassHandler<Control, bool>(OnIsSelectedAttachedChanged);
        }

        protected virtual void DrawRectangleArrowhead(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward, Orientation orientation = Orientation.Horizontal)
        {
            double direction = arrowDirection == ConnectionDirection.Forward ? 1d : -1d;
            context.SetFillRule(FillRule.EvenOdd);

            if (orientation == Orientation.Horizontal)
            {
                double headWidth  = ArrowSize.Width;
                double headHeight = ArrowSize.Height / 2;
                var bottomRight = new Point(target.X,                        target.Y + headHeight);
                var bottomLeft  = new Point(target.X - headWidth * direction, target.Y + headHeight);
                var topLeft     = new Point(target.X - headWidth * direction, target.Y - headHeight);
                var topRight    = new Point(target.X,                        target.Y - headHeight);
                context.BeginFigure(target, true);
                context.LineTo(bottomRight);
                context.LineTo(bottomLeft);
                context.LineTo(topLeft);
                context.LineTo(topRight);
                context.EndFigure(true);
            }
            else
            {
                double headWidth  = ArrowSize.Width / 2;
                double headHeight = ArrowSize.Height;
                var bottomLeft  = new Point(target.X - headWidth, target.Y);
                var topLeft     = new Point(target.X - headWidth, target.Y - headHeight * direction);
                var topRight    = new Point(target.X + headWidth, target.Y - headHeight * direction);
                var bottomRight = new Point(target.X + headWidth, target.Y);
                context.BeginFigure(target, true);
                context.LineTo(bottomLeft);
                context.LineTo(topLeft);
                context.LineTo(topRight);
                context.LineTo(bottomRight);
                context.EndFigure(true);
            }
        }

        protected virtual void DrawEllipseArrowhead(StreamGeometryContext context, Point source, Point target, ConnectionDirection arrowDirection = ConnectionDirection.Forward, Orientation orientation = Orientation.Horizontal)
        {
            const double ControlPointRatio = 0.55228474983079356; // (Math.Sqrt(2) - 1) * 4 / 3;

            double direction = arrowDirection == ConnectionDirection.Forward ? 1d : -1d;
            var targetLocation = orientation == Orientation.Horizontal
                ? new Point(target.X - ArrowSize.Width  / 2 * direction, target.Y)
                : new Point(target.X,                                     target.Y - ArrowSize.Height / 2 * direction);

            double headWidth = ArrowSize.Width / 2;
            double headHeight = ArrowSize.Height / 2;

            double x0 = targetLocation.X - headWidth;
            double x1 = targetLocation.X - headWidth * ControlPointRatio;
            double x2 = targetLocation.X;
            double x3 = targetLocation.X + headWidth * ControlPointRatio;
            double x4 = targetLocation.X + headWidth;

            double y0 = targetLocation.Y - headHeight;
            double y1 = targetLocation.Y - headHeight * ControlPointRatio;
            double y2 = targetLocation.Y;
            double y3 = targetLocation.Y + headHeight * ControlPointRatio;
            double y4 = targetLocation.Y + headHeight;

            context.SetFillRule(FillRule.EvenOdd);
            context.BeginFigure(new Point(x2, y0), true);

            context.CubicBezierTo(new Point(x3, y0), new Point(x4, y1), new Point(x4, y2));
            context.CubicBezierTo(new Point(x4, y3), new Point(x3, y4), new Point(x2, y4));
            context.CubicBezierTo(new Point(x1, y4), new Point(x0, y3), new Point(x0, y2));
            context.CubicBezierTo(new Point(x0, y1), new Point(x1, y0), new Point(x2, y0));
            context.EndFigure(true);
        }

        /// <summary>
        /// Gets the resulting offset after applying the <see cref="SourceOffsetMode"/>.
        /// Includes the Orientation X↔Y swap for Vertical connectors.
        /// </summary>
        /// <returns></returns>
        protected virtual (Vector SourceOffset, Vector TargetOffset) GetOffset()
        {
            Vector sourceDelta = Target - Source;
            Vector targetDelta = Source - Target;
            double arrowDirection = Direction == ConnectionDirection.Forward ? 1d : -1d;

            Vector sourceOffset = GetOffset(SourceOffsetMode, sourceDelta, SourceOffset, arrowDirection);
            Vector targetOffset = GetOffset(TargetOffsetMode, targetDelta, TargetOffset, -arrowDirection);

            // Swap X↔Y for Vertical orientation so the offset
            // projects along the connector's axis rather than always horizontally.
            if (SourceOrientation == Orientation.Vertical)
                sourceOffset = new Vector(sourceOffset.Y, sourceOffset.X);

            if (TargetOrientation == Orientation.Vertical)
                targetOffset = new Vector(targetOffset.Y, targetOffset.X);

            return (sourceOffset, targetOffset);

            static Vector GetOffset(ConnectionOffsetMode mode, Vector delta, Size currentOffset, double arrowDirection) => mode switch
            {
                ConnectionOffsetMode.Rectangle => GetRectangleModeOffset(delta, currentOffset),
                ConnectionOffsetMode.Circle => GetCircleModeOffset(delta, currentOffset),
                ConnectionOffsetMode.Edge => GetEdgeModeOffset(delta, currentOffset),
                ConnectionOffsetMode.Static => GetStaticModeOffset(arrowDirection, currentOffset),
                ConnectionOffsetMode.None => ZeroVector,
                _ => throw new NotImplementedException()
            };

            static Vector GetStaticModeOffset(double direction, Size offset)
            {
                double xOffset = offset.Width * direction;
                double yOffset = offset.Height * direction;

                return new Vector(xOffset, yOffset);
            }

            static Vector GetEdgeModeOffset(Vector delta, Size offset)
            {
                double xOffset = Math.Min(Math.Abs(delta.X) / 2d, offset.Width) * Math.Sign(delta.X);
                double yOffset = Math.Min(Math.Abs(delta.Y) / 2d, offset.Height) * Math.Sign(delta.Y);

                return new Vector(xOffset, yOffset);
            }

            static Vector GetCircleModeOffset(Vector delta, Size offset)
            {
                if (delta.SquaredLength > 0d)
                {
                    delta = delta.Normalize();
                }

                return new Vector(delta.X * offset.Width, delta.Y * offset.Height);
            }

            static Vector GetRectangleModeOffset(Vector delta, Size offset)
            {
                if (delta.SquaredLength > 0d)
                {
                    delta = delta.Normalize();
                }

                double angle = Math.Atan2(delta.Y, delta.X);
                Vector result;

                if (offset.Width * 2d * Math.Abs(delta.Y) < offset.Height * 2d * Math.Abs(delta.X))
                {
                    var x = Math.Sign(delta.X) * offset.Width;
                    var y = Math.Tan(angle) * x;
                    result = new Vector(x, y);
                }
                else
                {
                    var y = Math.Sign(delta.Y) * offset.Height;
                    var x = 1.0d / Math.Tan(angle) * y;
                    result = new Vector(x, y);
                }

                return result;
            }
        }

        private CancellationTokenSource? _animationTokenSource;
        private ConnectionContainer? _container;

        private ConnectionContainer? Container => _container ??= this.FindAncestorOfType<ConnectionContainer>();

        private static void OnIsAnimatingDirectionalArrowsChanged(BaseConnection con, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
                con.StartAnimation(con.DirectionalArrowsAnimationDuration);
            else
                con.StopAnimation();
        }

        private static void OnDirectionalArrowsAnimationDurationChanged(BaseConnection con, AvaloniaPropertyChangedEventArgs e)
        {
            if (con.IsAnimatingDirectionalArrows)
                con.StartAnimation((double)e.NewValue!);
        }

        /// <summary>Starts the looping directional-arrows animation (animates <see cref="DirectionalArrowsOffset"/> 0..1, repeating).</summary>
        /// <param name="duration">Seconds for an arrowhead to travel from <see cref="Source"/> to <see cref="Target"/>.</param>
        public void StartAnimation(double duration = 1.5d)
        {
            StopAnimation();
            _animationTokenSource = new CancellationTokenSource();

            double fromValue = DirectionalArrowsOffset;
            double toValue = DirectionalArrowsOffset + 1d;

            var animation = new Animation
            {
                Duration = TimeSpan.FromSeconds(duration),
                IterationCount = IterationCount.Infinite,
                Children =
                {
                    new KeyFrame { Setters = { new Setter(DirectionalArrowsOffsetProperty, fromValue) }, KeyTime = TimeSpan.Zero },
                    new KeyFrame { Setters = { new Setter(DirectionalArrowsOffsetProperty, toValue) }, KeyTime = TimeSpan.FromSeconds(duration) },
                }
            };

            _ = animation.RunAsync(this, _animationTokenSource.Token);
        }

        /// <summary>Stops the animation started by <see cref="StartAnimation(double)"/>.</summary>
        public void StopAnimation()
        {
            _animationTokenSource?.Cancel();
            _animationTokenSource = null;
        }

        private Pen? _outlinePen;

        private Pen GetOutlinePen()
            => _outlinePen ??= new Pen(OutlineBrush, StrokeThickness + OutlineThickness * 2d);

        private static void OnOutlinePenChanged(BaseConnection sender, AvaloniaPropertyChangedEventArgs e)
        {
            sender._outlinePen = null;
            sender.UpdateGeometryUnderlay(
                sender.OutlineBrush,
                Math.Max(0d, sender.StrokeThickness) + Math.Max(0d, sender.OutlineThickness) * 2d,
                sender.StrokeLineCap,
                sender.StrokeJoin);
            sender.InvalidateMeasure();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desired = base.MeasureOverride(availableSize);
            if (OutlineBrush == null || CreateDefiningGeometry() is not { } geometry)
            {
                return desired;
            }

            Rect outlineBounds = geometry.GetRenderBounds(GetOutlinePen());
            return new Size(
                Math.Max(desired.Width, Math.Max(0d, outlineBounds.Right)),
                Math.Max(desired.Height, Math.Max(0d, outlineBounds.Bottom)));
        }

        /// <summary>Computes the position of the text label (centered on the connection midpoint).</summary>
        protected virtual Point GetTextPosition(FormattedText text, Point source, Point target)
        {
            double direction = Direction == ConnectionDirection.Forward ? 1d : -1d;
            var spacing = new Vector(Spacing * direction, 0d);
            var spacingVertical = new Vector(spacing.Y, spacing.X);

            var p0 = source + (SourceOrientation == Orientation.Vertical ? spacingVertical : spacing);
            var p1 = target - (TargetOrientation == Orientation.Vertical ? spacingVertical : spacing);

            return new Point((p0.X + p1.X - text.Width) / 2, (p0.Y + p1.Y - text.Height) / 2);
        }

        /// <summary>Draws the rounded background (and optional outline) behind the text label.</summary>
        protected virtual void DrawTextBackground(DrawingContext drawingContext, Rect bounds)
        {
            var padding = TextPadding;
            double radius = TextCornerRadius;

            var rectSize = new Size(bounds.Width + padding.Left + padding.Right, bounds.Height + padding.Bottom + padding.Top);
            var rect = new Rect(bounds.TopLeft - new Vector(padding.Left, padding.Top), rectSize);

            if (OutlineBrush != null)
            {
                var outlineRect = rect.Inflate(OutlineThickness);
                drawingContext.DrawRectangle(OutlineBrush, null, outlineRect, radius, radius);
            }

            drawingContext.DrawRectangle(Stroke, null, rect, radius, radius);
        }

        /// <summary>Overlay render for the text label, drawn on top of the connection geometry.</summary>
        protected override void Render(DrawingContext drawingContext)
        {
            base.Render(drawingContext);

            if (!string.IsNullOrEmpty(Text))
            {
                var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
                var text = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, FontSize, Foreground ?? Stroke);

                (Vector sourceOffset, Vector targetOffset) = GetOffset();
                var textPosition = GetTextPosition(text, Source + sourceOffset, Target + targetOffset);

                if (TextBackground != null)
                {
                    var textSize = new Size(text.Width, text.Height);
                    DrawTextBackground(drawingContext, new Rect(textPosition, textSize));
                }

                drawingContext.DrawText(text, textPosition);
            }
        }

        #region Cutting and commands

        /// <summary>
        /// Returns the connection's waypoints for features that require an offset polyline.
        /// </summary>
        protected virtual IReadOnlyList<Point> GetConnectionWaypoints()
        {
            var (sourceOffset, targetOffset) = GetOffset();
            return new[] { Source + sourceOffset, Target + targetOffset };
        }

        /// <summary>
        /// Returns true if the segment [a,b] (graph space) crosses this connection's rendered geometry.
        /// </summary>
        /// <remarks>
        /// This method requires a drawing backend that implements geometry fill and stroke
        /// containment. With Avalonia.Headless, disable headless drawing and configure a backend
        /// such as Avalonia.Skia before testing this method.
        /// </remarks>
        public bool IsCutByLine(Point a, Point b)
            => ConnectionCutting.Intersects(
                GeometryForHitTesting,
                StrokeThickness,
                StrokeLineCap,
                StrokeJoin,
                a,
                b,
                CuttingHitTestTolerance);

        /// <inheritdoc />
        public bool HitTest(Point point)
        {
            if (VisualRoot is not Visual root
                || root.TranslatePoint(point, this) is not Point localPoint
                || GeometryForHitTesting is not { } geometry)
            {
                return false;
            }

            bool fillHit = Fill != null && geometry.FillContains(localPoint);
            double hitThickness = Stroke != null ? Math.Max(0d, StrokeThickness) : 0d;
            if (OutlineBrush != null)
            {
                hitThickness = Math.Max(0d, StrokeThickness)
                    + Math.Max(0d, OutlineThickness) * 2d;
            }

            if (hitThickness <= 0d)
            {
                return fillHit;
            }

            var hitPen = new Pen(
                Brushes.Black,
                hitThickness,
                lineCap: StrokeLineCap,
                lineJoin: StrokeJoin);

            return fillHit || geometry.StrokeContains(hitPen, localPoint);
        }

        /// <summary>
        /// Splits the connection at the specified location.
        /// Raises <see cref="SplitEvent"/>; if not handled, executes <see cref="SplitCommand"/> with <paramref name="splitLocation"/>.
        /// </summary>
        public void SplitAtLocation(Point splitLocation)
        {
            var args = new ConnectionEventArgs(DataContext!)
            {
                RoutedEvent = SplitEvent,
                SplitLocation = splitLocation,
                Source = this
            };

            RaiseEvent(args);

            // Both CanExecute AND Execute receive the same parameter: splitLocation.
            if (!args.Handled && (SplitCommand?.CanExecute(splitLocation) ?? false))
            {
                SplitCommand.Execute(splitLocation);
            }
        }

        /// <summary>
        /// Removes this connection: raises <see cref="DisconnectEvent"/>, then falls back to
        /// <see cref="DisconnectCommand"/> if the event was not handled.
        /// Passes <c>null</c> to <see cref="DisconnectCommand"/>.
        /// </summary>
        public void Remove()
        {
            var args = new ConnectionEventArgs(DataContext!)
            {
                RoutedEvent = DisconnectEvent,
                Source = this
            };

            RaiseEvent(args);

            // Passes null to both CanExecute and Execute (not DataContext).
            if (!args.Handled && (DisconnectCommand?.CanExecute(null) ?? false))
            {
                DisconnectCommand.Execute(null);
            }
        }

        #endregion

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            Focus();
            this.CaptureMouseSafe(e);

            // Split fires on PRESS — its gesture is a double-click (LeftDoubleClick), and a
            // PointerGesture only matches a double-click on press (ClickCount==2), never on
            // release. Checking Split on release would make it dead, since PointerGesture.Matches
            // returns false for any double-click on a release event.
            if (ActualGestures.Connection.Split.Matches(e.Source, e))
            {
                SplitAtLocation(e.GetPosition(this));
                e.Handled = true;
            }

            // Disconnect intercepts on PRESS too: claims the gesture (Focus + Handled) without
            // executing the command yet, so the same Alt+LeftClick press isn't ALSO read as a
            // selection Remove by the parent ConnectionContainer. The actual Remove() still fires
            // on release (OnPointerReleased below).
            else if (ActualGestures.Connection.Disconnect.Matches(e.Source, e))
            {
                Focus();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // Disconnect fires on RELEASE. Split is handled on press above.
            if (ActualGestures.Connection.Disconnect.Matches(e.Source, e))
            {
                Remove();
                e.Handled = true;
            }

            if (this.IsMouseCaptured(e))
            {
                this.ReleaseMouseCapture(e);
            }
        }
    }
}
