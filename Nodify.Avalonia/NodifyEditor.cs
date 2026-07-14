using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Nodify.Avalonia.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.EditorStates;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers.Gestures;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia
{
    /// <summary>
    /// Groups <see cref="ItemContainer"/>s and <see cref="Connections.Connection"/>s in an area that you can drag, zoom and select.
    /// </summary>
    [PseudoClasses(ConnectionsOnTopPseudoClass)]
    [TemplatePart(Name = "PART_ItemsHostPresenter", Type = typeof(ItemsPresenter))]
    [DefaultProperty(nameof(Decorators))]
    public partial class NodifyEditor : SelectingItemsControl, IRoutedCommandBindable
    {
        private const string ConnectionsOnTopPseudoClass = ":connections-on-top";

        #region InputGestures

        public static readonly StyledProperty<EditorGestures?> InputGesturesProperty =
            AvaloniaProperty.Register<NodifyEditor, EditorGestures?>(nameof(InputGestures));

        public EditorGestures? InputGestures
        {
            get => GetValue(InputGesturesProperty);
            set => SetValue(InputGesturesProperty, value);
        }

        public EditorGestures ActualGestures => InputGestures ?? EditorGestures.Mappings;

        #endregion

        #region Viewport

        public static readonly StyledProperty<double> ViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(ViewportZoom), 1d, defaultBindingMode:BindingMode.TwoWay, coerce: ConstrainViewportZoomToRange);

        public static readonly StyledProperty<double> MinViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(MinViewportZoom), 0.1d,coerce: CoerceMinViewportZoom);
        public static readonly StyledProperty<double> MaxViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor, double>(nameof(MaxViewportZoom), 2d, coerce: CoerceMaxViewportZoom);

        public static readonly StyledProperty<Point> ViewportLocationProperty = AvaloniaProperty.Register<NodifyEditor, Point>(nameof(ViewportLocation), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<Size> ViewportSizeProperty = AvaloniaProperty.Register<NodifyEditor,Size>(nameof(ViewportSize));
        public static readonly StyledProperty<Rect> ItemsExtentProperty = AvaloniaProperty.Register<NodifyEditor,Rect>(nameof(ItemsExtent));
        public static readonly StyledProperty<Rect> DecoratorsExtentProperty = AvaloniaProperty.Register<NodifyEditor, Rect>(nameof(DecoratorsExtent));

        public static readonly DirectProperty<NodifyEditor,TransformGroup> ViewportTransformProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,TransformGroup>(nameof(ViewportTransform),o => o.ViewportTransform);

        private CancellationTokenSource? _bringIntoViewCancellation;

        #region Callbacks

        private static void OnViewportLocationChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<Point> args)
        {
            var translate = args.NewValue.Value;

            editor.TranslateTransform.X = -translate.X * editor.ViewportZoom;
            editor.TranslateTransform.Y = -translate.Y * editor.ViewportZoom;

            editor.OnViewportUpdated();
        }

        private static void OnViewportZoomChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<double> args)
        {
            var zoom = args.NewValue.Value;
            var translate = editor.ViewportLocation;
            editor.ScaleTransform.ScaleX = zoom;
            editor.ScaleTransform.ScaleY = zoom;
            editor.TranslateTransform.X = -translate.X * zoom;
            editor.TranslateTransform.Y = -translate.Y * zoom;
            editor.ViewportSize = new Size(editor.Bounds.Width / zoom, editor.Bounds.Height / zoom);
            editor.ApplyRenderingOptimizations();
            editor.OnViewportUpdated();
        }

        private static void OnMinViewportZoomChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<double> args)
        {
            editor.CoerceValue(MaxViewportZoomProperty);
            editor.CoerceValue(ViewportZoomProperty);
        }

        private static double CoerceMinViewportZoom(AvaloniaObject avaloniaObject, double value)
            => value > 0.1d ? value : 0.1d;

        private static void OnMaxViewportZoomChanged(NodifyEditor nodifyEditor, AvaloniaPropertyChangedEventArgs<double> avaloniaPropertyChangedEventArgs)
        {
            nodifyEditor.CoerceValue(ViewportZoomProperty);
        }

        private static double CoerceMaxViewportZoom(AvaloniaObject avaloniaObject, double value)
        {
            var editor = (NodifyEditor)avaloniaObject;
            double min = editor.MinViewportZoom;

            return value < min ? min : value;
        }

        private static double ConstrainViewportZoomToRange(AvaloniaObject d, double value)
        {
            var editor = (NodifyEditor)d;

            double minimum = editor.MinViewportZoom;
            if (value < minimum)
            {
                return minimum;
            }

            double maximum = editor.MaxViewportZoom;
            return value > maximum ? maximum : value;
        }
        #endregion

        #region Routed Events

        public static readonly RoutedEvent ViewportUpdatedEvent = RoutedEvent.Register<NodifyEditor,RoutedEventArgs>(nameof(ViewportUpdated),RoutingStrategies.Bubble);
        /// <summary>
        /// Occurs whenever the viewport updates.
        /// </summary>
        public event EventHandler<RoutedEventArgs> ViewportUpdated
        {
            add => AddHandler(ViewportUpdatedEvent, value);
            remove => RemoveHandler(ViewportUpdatedEvent, value);
        }

        public static readonly RoutedEvent ItemsMovedEvent = RoutedEvent.Register<NodifyEditor, ItemsMovedEventArgs>(nameof(ItemsMoved), RoutingStrategies.Bubble);
        /// <summary>
        /// Occurs when <see cref="ItemContainer"/>s are moved within the editor by a drag operation.
        /// </summary>
        public event EventHandler<ItemsMovedEventArgs> ItemsMoved
        {
            add => AddHandler(ItemsMovedEvent, value);
            remove => RemoveHandler(ItemsMovedEvent, value);
        }

        /// <summary>
        /// Updates the <see cref="ViewportSize"/> and raises the <see cref="ViewportUpdatedEvent"/>.
        /// Called when the <see cref="Visual.Bounds"/> size or <see cref="ViewportZoom"/> is changed.
        /// </summary>
        protected void OnViewportUpdated()
        {
            UpdateScrollbars();
            UpdatePushedAreaOnViewport();
            RaiseEvent(new RoutedEventArgs(ViewportUpdatedEvent, this));
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the transform used to offset the viewport.
        /// </summary>
        protected readonly TranslateTransform TranslateTransform = new TranslateTransform();

        /// <summary>
        /// Gets the transform used to zoom on the viewport.
        /// </summary>
        protected readonly ScaleTransform ScaleTransform = new ScaleTransform();

        /// <summary>
        /// Gets the transform that is applied to all child controls.
        /// </summary>
        public TransformGroup ViewportTransform
        {
            get => _viewportTransform;
        }

        /// <summary>
        /// Gets the size of the viewport.
        /// </summary>
        public Size ViewportSize
        {
            get => (Size)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the viewport's top-left coordinates in graph space coordinates.
        /// </summary>
        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }


        /// <summary>
        /// Gets or sets the zoom factor of the viewport.
        /// </summary>
        public double ViewportZoom
        {
            get => (double)GetValue(ViewportZoomProperty);
            set => SetValue(ViewportZoomProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum zoom factor of the viewport
        /// </summary>
        public double MinViewportZoom
        {
            get => (double)GetValue(MinViewportZoomProperty);
            set => SetValue(MinViewportZoomProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum zoom factor of the viewport
        /// </summary>
        public double MaxViewportZoom
        {
            get => (double)GetValue(MaxViewportZoomProperty);
            set => SetValue(MaxViewportZoomProperty, value);
        }

        /// <summary>
        /// The area covered by the <see cref="ItemContainer"/>s.
        /// </summary>
        public Rect ItemsExtent
        {
            get => (Rect)GetValue(ItemsExtentProperty);
            set => SetValue(ItemsExtentProperty, value);
        }

        /// <summary>
        /// The area covered by the <see cref="DecoratorContainer"/>s.
        /// </summary>
        public Rect DecoratorsExtent
        {
            get => (Rect)GetValue(DecoratorsExtentProperty);
            set => SetValue(DecoratorsExtentProperty, value);
        }

        #endregion

        private void ApplyRenderingOptimizations()
        {
            if (ItemsPanelRoot is not Panel itemsHost)
            {
                return;
            }

            if (EnableRenderingContainersOptimizations &&
                Items.Count >= OptimizeRenderingMinimumContainers)
            {
                double zoom = ViewportZoom;
                double availableZoomIn = 1.0 - MinViewportZoom;
                bool shouldCache = zoom / availableZoomIn <= OptimizeRenderingZoomOutPercent;
                itemsHost.CacheMode = shouldCache
                    ? new BitmapCache { RenderAtScale = 1.0 / zoom }
                    : null;
            }
            else
            {
                itemsHost.CacheMode = null;
            }
        }

        private void OnItemsViewChangedForRenderingOptimization(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            ApplyRenderingOptimizations();
            RoutedCommand.RequestRequerySuggested();
        }

        /// <summary>
        /// Deselects all <see cref="ItemContainer"/>s in the editor.
        /// </summary>
        public void UnselectAll()
        {
            Selection.Clear();
        }

        #region Panning API

        private Point _initialPanningLocation;

        /// <summary>
        /// Starts the panning operation from the current <see cref="ViewportLocation"/>.
        /// Call <see cref="EndPanning"/> to end the panning operation.
        /// </summary>
        /// <remarks>This method has no effect if a panning operation is already in progress.</remarks>
        public void BeginPanning() => BeginPanning(ViewportLocation);

        /// <summary>
        /// Starts the panning operation from the specified <paramref name="location"/>. Call
        /// <see cref="EndPanning"/> to end the panning operation.
        /// </summary>
        /// <param name="location">The initial location where panning starts, in graph space coordinates.</param>
        /// <remarks>This method has no effect if a panning operation is already in progress.</remarks>
        public void BeginPanning(Point location)
        {
            if (IsPanning)
            {
                return;
            }

            _initialPanningLocation = location;
            ViewportLocation = location;
            IsPanning = true;
        }

        /// <summary>
        /// Pans the viewport by the specified <paramref name="amount"/> (graph-space units).
        /// </summary>
        public void UpdatePanning(Vector amount) => ViewportLocation -= amount;

        /// <summary>
        /// Ends the current panning operation, retaining the current <see cref="ViewportLocation"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no panning operation in progress.</remarks>
        public void EndPanning()
        {
            IsPanning = false;
        }

        /// <summary>
        /// Cancels the current panning operation and reverts the viewport to its initial location if
        /// <see cref="AllowPanningCancellation"/> is true. Otherwise, ends the panning operation by
        /// calling <see cref="EndPanning"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no panning operation in progress.</remarks>
        public void CancelPanning()
        {
            if (!AllowPanningCancellation)
            {
                EndPanning();
                return;
            }

            if (IsPanning)
            {
                ViewportLocation = _initialPanningLocation;
                IsPanning = false;
            }
        }

        #endregion

        /// <summary>
        /// Gets the number of currently selected <see cref="ItemContainer"/>s.
        /// </summary>
        public int SelectedContainersCount => SelectedContainers.Count;

        /// <summary>
        /// Gets the number of currently selected connections.
        /// </summary>
        public int SelectedConnectionsCount => SelectedConnections?.Count ?? 0;

        /// <summary>
        /// Clears the connection selection by delegating to the <see cref="Connections.ConnectionsMultiSelector"/>.
        /// </summary>
        public void UnselectAllConnections()
        {
            (ConnectionsHost as Connections.ConnectionsMultiSelector)?.UnselectAll();
        }

        /// <summary>
        /// Ensures <paramref name="area"/> (graph space) is fully visible within the viewport, with
        /// <paramref name="offsetFromEdge"/> margin kept around it. Does nothing if <paramref name="area"/>
        /// is already fully visible, animates a minimal edge-pan if it intersects the viewport, or animates
        /// a centering pan if it's fully outside the viewport.
        /// </summary>
        /// <param name="area">The area (graph space) to bring into view.</param>
        /// <param name="offsetFromEdge">The margin to keep around <paramref name="area"/> (graph space).</param>
        public void BringIntoView(Rect area, double offsetFromEdge = 32d)
        {
            Point? target = ComputeBringIntoViewTarget(area, offsetFromEdge);

            if (target.HasValue)
            {
                _ = BringIntoView(target.Value, animated: true);
            }
        }

        /// <summary>
        /// Computes the viewport-center <see cref="Point"/> that <see cref="BringIntoView(Rect, double)"/> should
        /// animate to, or <see langword="null"/> if <paramref name="area"/> (inflated by <paramref name="offsetFromEdge"/>)
        /// is already fully visible. Split out from <see cref="BringIntoView(Rect, double)"/> so the targeting math
        /// can be tested without driving a real animation.
        /// </summary>
        internal Point? ComputeBringIntoViewTarget(Rect area, double offsetFromEdge)
        {
            var viewport = new Rect(ViewportLocation, ViewportSize);

            double left = area.Left - offsetFromEdge;
            double top = area.Top - offsetFromEdge;
            double right = area.Right + offsetFromEdge;
            double bottom = area.Bottom + offsetFromEdge;

            bool fullyVisible = left >= viewport.Left && top >= viewport.Top
                && right <= viewport.Right && bottom <= viewport.Bottom;

            if (fullyVisible)
            {
                return null;
            }

            bool intersects = right >= viewport.Left && left <= viewport.Right
                && bottom >= viewport.Top && top <= viewport.Bottom;

            if (intersects)
            {
                double newX = viewport.X;
                double newY = viewport.Y;

                if (left < viewport.Left)
                {
                    newX = left;
                }
                else if (right > viewport.Right)
                {
                    newX = right - viewport.Width;
                }

                if (top < viewport.Top)
                {
                    newY = top;
                }
                else if (bottom > viewport.Bottom)
                {
                    newY = bottom - viewport.Height;
                }

                return new Point(newX + viewport.Width / 2, newY + viewport.Height / 2);
            }

            // Fully outside the viewport: center on the (un-inflated) area - inflating symmetrically
            // doesn't move its center.
            return new Point(area.X + area.Width / 2, area.Y + area.Height / 2);
        }

        #endregion

        #region Cosmetic Dependency Properties

        public static readonly StyledProperty<double> BringIntoViewSpeedProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(BringIntoViewSpeed), 1000d);
        public static readonly StyledProperty<double> BringIntoViewMaxDurationProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(BringIntoViewMaxDuration),1d);
        public static readonly StyledProperty<bool> DisplayConnectionsOnTopProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisplayConnectionsOnTop), false);
        public static readonly StyledProperty<bool> DisableAutoPanningProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisableAutoPanning),false);
        public static readonly StyledProperty<double> AutoPanSpeedProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(AutoPanSpeed), 15d);
        public static readonly StyledProperty<double> AutoPanEdgeDistanceProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(AutoPanEdgeDistance), 15d);
        public static readonly StyledProperty<IDataTemplate?> ConnectionTemplateProperty = AvaloniaProperty.Register<NodifyEditor, IDataTemplate?>(nameof(ConnectionTemplate));
        public static readonly StyledProperty<IDataTemplate?> DecoratorTemplateProperty = AvaloniaProperty.Register<NodifyEditor, IDataTemplate?>(nameof(DecoratorTemplate));
        public static readonly StyledProperty<IDataTemplate?> PendingConnectionTemplateProperty = AvaloniaProperty.Register<NodifyEditor, IDataTemplate?>(nameof(PendingConnectionTemplate));
        public static readonly StyledProperty<ControlTheme> SelectionRectangleThemeProperty = AvaloniaProperty.Register<NodifyEditor,ControlTheme>(nameof(SelectionRectangleTheme));
        public static readonly StyledProperty<ControlTheme> DecoratorContainerThemeProperty = AvaloniaProperty.Register<NodifyEditor,ControlTheme>(nameof(DecoratorContainerTheme));

        private static void OnDisableAutoPanningChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<bool> args)
            => editor.OnDisableAutoPanningChanged(args.NewValue.Value);

        /// <summary>
        /// Gets or sets the maximum animation duration in seconds for bringing a location into view.
        /// </summary>
        public double BringIntoViewMaxDuration
        {
            get => (double)GetValue(BringIntoViewMaxDurationProperty);
            set => SetValue(BringIntoViewMaxDurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation speed in pixels per second for bringing a location into view.
        /// </summary>
        /// <remarks>Total animation duration is calculated based on distance and clamped between 0.1 and <see cref="BringIntoViewMaxDuration"/>.</remarks>
        public double BringIntoViewSpeed
        {
            get => (double)GetValue(BringIntoViewSpeedProperty);
            set => SetValue(BringIntoViewSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to display connections on top of <see cref="ItemContainer"/>s or not.
        /// </summary>
        public bool DisplayConnectionsOnTop
        {
            get => (bool)GetValue(DisplayConnectionsOnTopProperty);
            set => SetValue(DisplayConnectionsOnTopProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to disable the auto panning when selecting or dragging near the edge of the editor configured by <see cref="AutoPanEdgeDistance"/>.
        /// </summary>
        public bool DisableAutoPanning
        {
            get => (bool)GetValue(DisableAutoPanningProperty);
            set => SetValue(DisableAutoPanningProperty, value);
        }

        /// <summary>
        /// Gets or sets the speed used when auto-panning scaled by <see cref="AutoPanningTickRate"/>
        /// </summary>
        public double AutoPanSpeed
        {
            get => (double)GetValue(AutoPanSpeedProperty);
            set => SetValue(AutoPanSpeedProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum distance in pixels from the edge of the editor that will trigger auto-panning.
        /// </summary>
        public double AutoPanEdgeDistance
        {
            get => (double)GetValue(AutoPanEdgeDistanceProperty);
            set => SetValue(AutoPanEdgeDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use when generating a new <see cref="BaseConnection"/>.
        /// </summary>
        /// <remarks>Supply an <see cref="IDataTemplate"/> whose Match/Build implements per-item selection.</remarks>
        public IDataTemplate? ConnectionTemplate
        {
            get => GetValue(ConnectionTemplateProperty);
            set => SetValue(ConnectionTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use when generating a new <see cref="DecoratorContainer"/>.
        /// </summary>
        /// <remarks>Supply an <see cref="IDataTemplate"/> whose Match/Build implements per-item selection.</remarks>
        public IDataTemplate? DecoratorTemplate
        {
            get => GetValue(DecoratorTemplateProperty);
            set => SetValue(DecoratorTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use for the <see cref="PendingConnection"/>.
        /// </summary>
        /// <remarks>Supply an <see cref="IDataTemplate"/> whose Match/Build implements per-item selection.</remarks>
        public IDataTemplate? PendingConnectionTemplate
        {
            get => GetValue(PendingConnectionTemplateProperty);
            set => SetValue(PendingConnectionTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the style to use for the selection rectangle.
        /// </summary>
        public ControlTheme SelectionRectangleTheme
        {
            get => GetValue(SelectionRectangleThemeProperty);
            set => SetValue(SelectionRectangleThemeProperty, value);
        }

        /// <summary>
        /// Gets or sets the style to use for the <see cref="DecoratorContainer"/>.
        /// </summary>
        public ControlTheme DecoratorContainerTheme
        {
            get => GetValue(DecoratorContainerThemeProperty);
            set => SetValue(DecoratorContainerThemeProperty, value);
        }

        public static readonly StyledProperty<bool> HasCustomContextMenuProperty = AvaloniaProperty.Register<NodifyEditor, bool>(nameof(HasCustomContextMenu));

        /// <summary>
        /// Gets or sets whether this editor draws a custom context menu (not via <see cref="Control.ContextMenu"/>).
        /// Used to suppress starting an interaction so the context menu can appear.
        /// </summary>
        public bool HasCustomContextMenu
        {
            get => (bool)GetValue(HasCustomContextMenuProperty);
            set => SetValue(HasCustomContextMenuProperty, value);
        }

        /// <summary>
        /// Gets whether this editor has a context menu (either a <see cref="Control.ContextMenu"/> or a <see cref="HasCustomContextMenu"/>).
        /// </summary>
        public bool HasContextMenu => ContextMenu != null || HasCustomContextMenu;

        #endregion

        #region Readonly Dependency Properties

        public static readonly DirectProperty<NodifyEditor,Rect> SelectedAreaProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,Rect>(nameof(SelectedArea),o=>o.SelectedArea, null,default(Rect));
        public static readonly DirectProperty<NodifyEditor,bool> IsSelectingProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsSelecting),o=>o.IsSelecting, null,false);
        public static readonly DirectProperty<NodifyEditor,bool> IsPanningProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsPanning),o=>o.IsPanning, null,false);
        public static readonly DirectProperty<NodifyEditor,bool> IsCuttingProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsCutting),o=>o.IsCutting, null,false);
        public static readonly DirectProperty<NodifyEditor,bool> IsDraggingProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsDragging),o=>o.IsDragging, null,false);
        public static readonly DirectProperty<NodifyEditor,Point> MouseLocationProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,Point>(nameof(MouseLocation),o=>o.MouseLocation, null,default(Point));

        private void OnIsSelectingChanged(bool value)
        {
            if (value)
                OnItemsSelectStarted();
            else
                OnItemSelectCompleted();
        }

        private void OnItemSelectCompleted()
        {
            if (ItemsSelectCompletedCommand?.CanExecute(DataContext) ?? false)
                ItemsSelectCompletedCommand.Execute(DataContext);
        }

        private void OnItemsSelectStarted()
        {
            if (ItemsSelectStartedCommand?.CanExecute(DataContext) ?? false)
                ItemsSelectStartedCommand.Execute(DataContext);
        }

        /// <summary>
        /// Gets the currently selected area while <see cref="IsSelecting"/> is true.
        /// </summary>
        public Rect SelectedArea
        {
            get => _selectedArea;
            internal set => SetAndRaise(SelectedAreaProperty, ref _selectedArea, value);
        }

        /// <summary>
        /// Gets a value that indicates whether a selection operation is in progress.
        /// </summary>
        public bool IsSelecting
        {
            get => _isSelecting;
            internal set
            {
                if (SetAndRaise(IsSelectingProperty, ref _isSelecting, value))
                {
                    OnIsSelectingChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether a panning operation is in progress.
        /// </summary>
        public bool IsPanning
        {
            get => _isPanning;
            protected internal set => SetAndRaise(IsPanningProperty,ref _isPanning, value);
        }

        private void OnIsCuttingChanged(bool value)
        {
            if (value)
                OnCuttingStarted();
            else
                OnCuttingCompleted();
        }

        /// <summary>
        /// Gets a value that indicates whether a cutting-line operation is in progress.
        /// </summary>
        public bool IsCutting
        {
            get => _isCutting;
            protected internal set
            {
                if (SetAndRaise(IsCuttingProperty, ref _isCutting, value))
                {
                    OnIsCuttingChanged(value);
                }
            }
        }

        private void OnIsDraggingChanged(bool value)
        {
            if (value)
                OnItemsDragStarted();
            else
                OnItemsDragCompleted();
        }

        private void OnItemsDragCompleted()
        {
            if (ItemsDragCompletedCommand?.CanExecute(DataContext) ?? false)
                ItemsDragCompletedCommand.Execute(DataContext);
        }

        private void OnItemsDragStarted()
        {
            if (ItemsDragStartedCommand?.CanExecute(DataContext) ?? false)
                ItemsDragStartedCommand.Execute(DataContext);
        }

        /// <summary>
        /// Gets a value that indicates whether a dragging operation is in progress
        /// (container drag, keyboard drag, or the pushing-items operation).
        /// </summary>
        public bool IsDragging
        {
            get => _isDragging;
            private set
            {
                if (SetAndRaise(IsDraggingProperty, ref _isDragging, value))
                {
                    OnIsDraggingChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets the current mouse location in graph space coordinates (relative to the <see cref="ItemsHost" />).
        /// </summary>
        public Point MouseLocation
        {
            get => _mouseLocation;
            protected set => SetAndRaise(MouseLocationProperty,ref _mouseLocation,value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<IEnumerable> ConnectionsProperty = AvaloniaProperty.Register<NodifyEditor,IEnumerable>(nameof(Connections));
        public new static readonly DirectProperty<SelectingItemsControl,IList?> SelectedItemsProperty = SelectingItemsControl.SelectedItemsProperty;
        public static readonly StyledProperty<object> PendingConnectionProperty = AvaloniaProperty.Register<NodifyEditor,object>(nameof(PendingConnection));
        public static readonly StyledProperty<uint> GridCellSizeProperty = AvaloniaProperty.Register<NodifyEditor,uint>(nameof(GridCellSize), 1u,coerce: OnCoerceGridCellSize);
        public static readonly StyledProperty<bool> DisableZoomingProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisableZooming), false);
        public static readonly StyledProperty<bool> DisablePanningProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisablePanning), false);
        public static readonly StyledProperty<bool> EnableRealtimeSelectionProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(EnableRealtimeSelection), false);
        public static readonly StyledProperty<IEnumerable> DecoratorsProperty = AvaloniaProperty.Register<NodifyEditor,IEnumerable>(nameof(Decorators));

        private static uint OnCoerceGridCellSize(AvaloniaObject avaloniaObject, uint value)
            => value > 0u ? value : 1u;

        private static void OnGridCellSizeChanged(NodifyEditor nodifyEditor, AvaloniaPropertyChangedEventArgs<uint> avaloniaPropertyChangedEventArgs) { }

        private static void OnDisablePanningChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<bool> avaloniaPropertyChangedEventArgs)
        {
            editor.OnDisableAutoPanningChanged(editor.DisableAutoPanning || editor.DisablePanning);
        }

        /// <summary>
        /// Gets or sets the items that will be rendered in the decorators layer via <see cref="DecoratorContainer"/>s.
        /// </summary>
        public IEnumerable Decorators
        {
            get => (IEnumerable)GetValue(DecoratorsProperty);
            set => SetValue(DecoratorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the value of an invisible grid used to adjust locations (snapping) of <see cref="ItemContainer"/>s.
        /// </summary>
        public uint GridCellSize
        {
            get => (uint)GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the data source that <see cref="BaseConnection"/>s will be generated for.
        /// </summary>
        public IEnumerable Connections
        {
            get => (IEnumerable)GetValue(ConnectionsProperty);
            set => SetValue(ConnectionsProperty, value);
        }

        /// <summary>
        /// Gets of sets the <see cref="StyledElement.DataContext"/> of the <see cref="Connections.PendingConnection"/>.
        /// </summary>
        public object PendingConnection
        {
            get => GetValue(PendingConnectionProperty);
            set => SetValue(PendingConnectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the items in the <see cref="NodifyEditor"/> that are selected.
        /// </summary>
        public new IList? SelectedItems
        {
            get => base.SelectedItems;
            set => base.SelectedItems = value;
        }

        /// <summary>
        /// Gets or sets whether zooming should be disabled.
        /// </summary>
        public bool DisableZooming
        {
            get => (bool)GetValue(DisableZoomingProperty);
            set => SetValue(DisableZoomingProperty, value);
        }

        /// <summary>
        /// Gets or sets whether panning should be disabled.
        /// </summary>
        public bool DisablePanning
        {
            get => (bool)GetValue(DisablePanningProperty);
            set => SetValue(DisablePanningProperty, value);
        }

        /// <summary>
        /// Enables selecting and deselecting items while the <see cref="SelectedArea"/> changes.
        /// Disable for maximum performance when hundreds of items are generated.
        /// </summary>
        public bool EnableRealtimeSelection
        {
            get => (bool)GetValue(EnableRealtimeSelectionProperty);
            set => SetValue(EnableRealtimeSelectionProperty, value);
        }

        #endregion

        #region Command Dependency Properties

        public static readonly StyledProperty<ICommand?> ConnectionCompletedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ConnectionCompletedCommand));
        public static readonly StyledProperty<ICommand?> ConnectionStartedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ConnectionStartedCommand));
        public static readonly StyledProperty<ICommand?> DisconnectConnectorCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(DisconnectConnectorCommand));
        public static readonly StyledProperty<ICommand?> RemoveConnectionCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(RemoveConnectionCommand));
        public static readonly StyledProperty<ICommand?> ItemsDragStartedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ItemsDragStartedCommand));
        public static readonly StyledProperty<ICommand?> ItemsDragCompletedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ItemsDragCompletedCommand));
        public static readonly StyledProperty<ICommand?> ItemsSelectStartedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ItemsSelectStartedCommand));
        public static readonly StyledProperty<ICommand?> ItemsSelectCompletedCommandProperty = AvaloniaProperty.Register<NodifyEditor,ICommand?>(nameof(ItemsSelectCompletedCommand));

        /// <summary>
        /// Invoked when the <see cref="Connections.PendingConnection"/> is completed. <br />
        /// Use <see cref="PendingConnection.StartedCommand"/> if you want to control the visibility of the connection from the viewmodel. <br />
        /// Parameter is <see cref="PendingConnection.Source"/>.
        /// </summary>
        public ICommand? ConnectionStartedCommand
        {
            get => GetValue(ConnectionStartedCommandProperty);
            set => SetValue(ConnectionStartedCommandProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="Connections.PendingConnection"/> is completed. <br />
        /// Use <see cref="PendingConnection.CompletedCommand"/> if you want to control the visibility of the connection from the viewmodel. <br />
        /// Parameter is <see cref="Tuple{T, U}"/> where <see cref="Tuple{T, U}.Item1"/> is the <see cref="PendingConnection.Source"/> and <see cref="Tuple{T, U}.Item2"/> is <see cref="PendingConnection.Target"/>.
        /// </summary>
        public ICommand? ConnectionCompletedCommand
        {
            get => GetValue(ConnectionCompletedCommandProperty);
            set => SetValue(ConnectionCompletedCommandProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="Connector.Disconnect"/> event is raised. <br />
        /// Can also be handled at the <see cref="Connector"/> level using the <see cref="Connector.DisconnectCommand"/> command. <br />
        /// Parameter is the <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/>.
        /// </summary>
        public ICommand? DisconnectConnectorCommand
        {
            get => GetValue(DisconnectConnectorCommandProperty);
            set => SetValue(DisconnectConnectorCommandProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="BaseConnection.Disconnect"/> event is raised. <br />
        /// Can also be handled at the <see cref="BaseConnection"/> level using the <see cref="BaseConnection.DisconnectCommand"/> command. <br />
        /// Parameter is the <see cref="BaseConnection"/>'s <see cref="StyledElement.DataContext"/>.
        /// </summary>
        public ICommand? RemoveConnectionCommand
        {
            get => GetValue(RemoveConnectionCommandProperty);
            set => SetValue(RemoveConnectionCommandProperty, value);
        }

        /// <summary>
        /// Invoked when a drag operation starts for the <see cref="SelectedContainers"/>, or when a
        /// pushing-items operation starts (see <see cref="IsDragging"/>).
        /// </summary>
        public ICommand? ItemsDragStartedCommand
        {
            get => GetValue(ItemsDragStartedCommandProperty);
            set => SetValue(ItemsDragStartedCommandProperty, value);
        }

        /// <summary>
        /// Invoked when a drag operation is completed for the <see cref="SelectedContainers"/>, or when a
        /// pushing-items operation is completed (see <see cref="IsDragging"/>).
        /// </summary>
        public ICommand? ItemsDragCompletedCommand
        {
            get => GetValue(ItemsDragCompletedCommandProperty);
            set => SetValue(ItemsDragCompletedCommandProperty, value);
        }

        /// <summary>Invoked when a selection operation is started.</summary>
        public ICommand? ItemsSelectStartedCommand
        {
            get => GetValue(ItemsSelectStartedCommandProperty);
            set => SetValue(ItemsSelectStartedCommandProperty, value);
        }

        /// <summary>Invoked when a selection operation is completed.</summary>
        public ICommand? ItemsSelectCompletedCommand
        {
            get => GetValue(ItemsSelectCompletedCommandProperty);
            set => SetValue(ItemsSelectCompletedCommandProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// The maximum distance the mouse can move (in pixels) during a right-click drag before the
        /// subsequent context menu / mouse action is suppressed.
        /// </summary>
        public static double MouseActionSuppressionThreshold { get; set; } = 12d;

        /// <summary>
        /// Correct <see cref="ItemContainer"/>'s position after moving if starting position is not snapped to grid.
        /// </summary>
        public static bool EnableSnappingCorrection { get; set; } = true;

        /// <summary>
        /// Gets or sets how often the new <see cref="ViewportLocation"/> is calculated in milliseconds when <see cref="DisableAutoPanning"/> is false.
        /// </summary>
        public static double AutoPanningTickRate { get; set; } = 1;

        /// <summary>
        /// Gets or sets if <see cref="NodifyEditor"/>s should enable optimizations based on <see cref="OptimizeRenderingMinimumContainers"/> and <see cref="OptimizeRenderingZoomOutPercent"/>.
        /// </summary>
        /// <remarks>When enabled, sufficiently large item panels are bitmap-cached while zoomed out.</remarks>
        public static bool EnableRenderingContainersOptimizations { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum number of <see cref="ItemContainer"/>s needed to trigger optimizations when reaching the <see cref="OptimizeRenderingZoomOutPercent"/>.
        /// </summary>
        public static uint OptimizeRenderingMinimumContainers { get; set; } = 700;

        /// <summary>
        /// Gets or sets the minimum zoom out percent needed to start optimizing the rendering for <see cref="ItemContainer"/>s.
        /// Value is between 0 and 1.
        /// </summary>
        public static double OptimizeRenderingZoomOutPercent { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the margin to add in all directions to the <see cref="ItemsExtent"/> or area parameter when using <see cref="FitToScreen(Rect?)"/>.
        /// </summary>
        public static double FitToScreenExtentMargin { get; set; } = 30;

        /// <summary>
        /// Gets or sets if the current position of containers that are being dragged should not be committed until the end of the dragging operation.
        /// </summary>
        public static bool EnableDraggingContainersOptimizations { get; set; } = true;

        /// <summary>
        /// Gets or sets whether cancelling a dragging operation is allowed.
        /// </summary>
        public static bool AllowDraggingCancellation { get; set; } = true;

        /// <summary>
        /// Tells if the <see cref="NodifyEditor"/> is doing operations on multiple items at once.
        /// </summary>
        public bool IsBulkUpdatingItems { get; protected set; }

        /// <summary>
        /// Gets the panel that holds all the <see cref="ItemContainer"/>s.
        /// </summary>
        protected internal Panel ItemsHost => ItemsPanelRoot!;

        private IDraggingStrategy? _draggingStrategy;
        private DispatcherTimer? _autoPanningTimer;

        /// <summary>
        /// Gets a list of <see cref="ItemContainer"/>s that are selected.
        /// </summary>
        /// <remarks>Cache the result before using it to avoid extra allocations.</remarks>
        protected internal IReadOnlyList<ItemContainer> SelectedContainers 
        {
            get
            {
                IList selectedItems = base.SelectedItems!;
                var selectedContainers = new List<ItemContainer>(selectedItems.Count);

                for (var i = 0; i < selectedItems.Count; i++)
                {
                    var container = ContainerFromItem(selectedItems[i]!) as ItemContainer;
                    if (container != null)
                    {
                        selectedContainers.Add(container);
                    }
                }

                return selectedContainers;
            }
        }

        #endregion

        #region Construction

        static NodifyEditor()
        {
            FocusableProperty.OverrideMetadata(typeof(NodifyEditor), new StyledPropertyMetadata<bool>(true));
            ViewportZoomProperty.Changed.AddClassHandler<NodifyEditor,double>(OnViewportZoomChanged);
            DisableAutoPanningProperty.Changed.AddClassHandler<NodifyEditor,bool>(OnDisableAutoPanningChanged);
            MinViewportZoomProperty.Changed.AddClassHandler<NodifyEditor, double>(OnMinViewportZoomChanged);
            MaxViewportZoomProperty.Changed.AddClassHandler<NodifyEditor, double>(OnMaxViewportZoomChanged);
            ViewportLocationProperty.Changed.AddClassHandler<NodifyEditor, Point>(OnViewportLocationChanged);
            GridCellSizeProperty.Changed.AddClassHandler<NodifyEditor, uint>(OnGridCellSizeChanged);
            DisablePanningProperty.Changed.AddClassHandler<NodifyEditor, bool>(OnDisablePanningChanged);
            ItemsExtentProperty.Changed.AddClassHandler<NodifyEditor, Rect>((editor, _) => editor.UpdateScrollbars());
            CanSelectMultipleItemsProperty.Changed.AddClassHandler<NodifyEditor, bool>(OnCanSelectMultipleItemsChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodifyEditor"/> class.
        /// </summary>
        public NodifyEditor()
        {
            EditorCommands.Register(this);
            AddHandler(Connector.DisconnectEvent, new ConnectorEventHandler(OnConnectorDisconnected));
            AddHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnConnectionStarted));
            AddHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnConnectionCompleted));

            AddHandler(BaseConnection.DisconnectEvent, new ConnectionEventHandler(OnRemoveConnection));

            AddHandler(ItemContainer.DragStartedEvent, new EventHandler<VectorEventArgs>(OnContainerDragStarted));
            AddHandler(ItemContainer.DragCompletedEvent, new EventHandler<DragCompletedEventArgs>(OnContainerDragCompleted));
            AddHandler(ItemContainer.DragDeltaEvent, new EventHandler<VectorEventArgs>(OnItemsDragDelta));

            // Trackpad pinch-to-zoom. See NodifyEditor.PinchZoom.cs.
            AddHandler(InputElement.PointerTouchPadGestureMagnifyEvent, OnPointerTouchPadGestureMagnify);

            var transform = new TransformGroup();
            transform.Children.Add(ScaleTransform);
            transform.Children.Add(TranslateTransform);

            SetAndRaise(ViewportTransformProperty, ref _viewportTransform, transform);
            InputProcessor.AddSharedHandlers(this);

            SelectionMode = SelectionMode.Multiple;
            SizeChanged += OnRenderSizeChanged;
            ItemsView.CollectionChanged += OnItemsViewChangedForRenderingOptimization;
            SelectionChanged += OnSelectionChangedForCommandRequery;

            InitializeKeyboardNavigation();
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            OnDisableAutoPanningChanged(DisableAutoPanning);
            ConnectionsHost = e.NameScope.Find<Control>("PART_ConnectionsHost");
            PseudoClasses.Set(ConnectionsOnTopPseudoClass, DisplayConnectionsOnTop);
            ApplyRenderingOptimizations();
            Dispatcher.UIThread.Post(ApplyRenderingOptimizations);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DisplayConnectionsOnTopProperty)
            {
                PseudoClasses.Set(ConnectionsOnTopPseudoClass, DisplayConnectionsOnTop);
            }

            if (change.Property == ViewportZoomProperty
                || change.Property == MinViewportZoomProperty
                || change.Property == MaxViewportZoomProperty
                || change.Property == DisablePanningProperty
                || change.Property == IsSelectingProperty
                || change.Property == CanSelectMultipleItemsProperty)
            {
                RoutedCommand.RequestRequerySuggested();
            }
        }

        private void OnSelectionChangedForCommandRequery(object? sender, SelectionChangedEventArgs e)
            => RoutedCommand.RequestRequerySuggested();

        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ItemContainer(this)
            {
                RenderTransform = new TranslateTransform(),
                RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative)
            };
        }

        /// <inheritdoc />
        protected override bool NeedsContainerOverride(
            object? item,
            int index,
            out object? recycleKey)
        {
            if (item is ItemContainer)
            {
                recycleKey = null;
                return false;
            }

            recycleKey = typeof(ItemContainer);
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Zoom in at the viewports center
        /// </summary>
        public void ZoomIn() => ZoomAtPosition(Math.Pow(2.0, 120.0 / 3.0 /  PointerHelper.PointerWheelDeltaForOneLine), (Point)((Vector)ViewportLocation + ViewportSize.ToVector() / 2));

        /// <summary>
        /// Zoom out at the viewports center
        /// </summary>
        public void ZoomOut() => ZoomAtPosition(Math.Pow(2.0, -120.0 / 3.0 / PointerHelper.PointerWheelDeltaForOneLine), (Point)((Vector)ViewportLocation + ViewportSize.ToVector() / 2));

        /// <summary>
        /// Zoom at the specified location in graph space coordinates.
        /// </summary>
        /// <param name="zoom">The zoom factor.</param>
        /// <param name="location">The location to focus when zooming.</param>
        public void ZoomAtPosition(double zoom, Point location)
        {
            if (!DisableZooming)
            {
                double prevZoom = ViewportZoom;
                ViewportZoom *= zoom;

                if (Math.Abs(prevZoom - ViewportZoom) > 0.001)
                {
                    // get the actual zoom value because Zoom might have been coerced
                    zoom = ViewportZoom / prevZoom;
                    Vector position = (Vector)location;

                    var dist = position - (Vector)ViewportLocation;
                    var zoomedDist = dist * zoom;
                    var diff = zoomedDist - dist;
                    ViewportLocation += diff / zoom;
                }
            }
        }

        /// <summary>
        /// Moves the viewport center at the specified location.
        /// </summary>
        /// <param name="point">The location in graph space coordinates.</param>
        /// <param name="animated">True to animate the movement.</param>
        /// <param name="onFinish">The callback invoked when movement is finished.</param>
        /// <remarks>Temporarily disables editor controls when animated.</remarks>
        public async Task BringIntoView(Point point, bool animated = true, Action? onFinish = null)
        {
            Point newLocation = (Point)((Vector)point - ViewportSize.ToVector() / 2);
            bool shouldAnimate = animated && newLocation != ViewportLocation;
            var operation = new CancellationTokenSource();
            var previousOperation = _bringIntoViewCancellation;
            _bringIntoViewCancellation = operation;
            bool ownsInteractionLock = shouldAnimate || previousOperation is not null;

            try
            {
                previousOperation?.Cancel();

                if (!ReferenceEquals(_bringIntoViewCancellation, operation))
                {
                    return;
                }

                if (shouldAnimate)
                {
                    IsPanning = true;

                    if (!ReferenceEquals(_bringIntoViewCancellation, operation))
                    {
                        return;
                    }

                    SetCurrentValue(DisablePanningProperty, true);

                    if (!ReferenceEquals(_bringIntoViewCancellation, operation))
                    {
                        return;
                    }

                    SetCurrentValue(DisableZoomingProperty, true);

                    if (!ReferenceEquals(_bringIntoViewCancellation, operation))
                    {
                        return;
                    }

                    double distance = newLocation.VectorSubtract(ViewportLocation).Length;
                    double duration = distance / (BringIntoViewSpeed + distance / 10) * ViewportZoom;
                    duration = Math.Max(0.1, Math.Min(duration, BringIntoViewMaxDuration));

                    var animation = new Animation()
                    {
                        Duration = TimeSpan.FromSeconds(duration),
                        FillMode = FillMode.Forward,
                        Children =
                        {
                            new KeyFrame()
                            {
                                Setters =
                                {
                                    new Setter(ViewportLocationProperty,ViewportLocation)
                                },
                                Cue = new Cue(0)
                            },
                            new KeyFrame()
                            {
                                Setters =
                                {
                                    new Setter(ViewportLocationProperty, newLocation),
                                },
                                Cue = new Cue(1)
                            }
                        }
                    };

                    await animation.RunAsync(this, operation.Token);
                }

                if (ReferenceEquals(_bringIntoViewCancellation, operation))
                {
                    SetCurrentValue(ViewportLocationProperty, newLocation);

                    if (ownsInteractionLock && ReleaseBringIntoViewInteractionLock(operation))
                    {
                        ownsInteractionLock = false;
                    }

                    if (ReferenceEquals(_bringIntoViewCancellation, operation))
                    {
                        onFinish?.Invoke();
                    }
                }
            }
            catch (OperationCanceledException) when (operation.IsCancellationRequested)
            {
            }
            finally
            {
                if (ownsInteractionLock)
                {
                    ReleaseBringIntoViewInteractionLock(operation);
                }

                if (ReferenceEquals(_bringIntoViewCancellation, operation))
                {
                    _bringIntoViewCancellation = null;
                }

                operation.Dispose();
            }
        }

        private bool ReleaseBringIntoViewInteractionLock(CancellationTokenSource operation)
        {
            if (!ReferenceEquals(_bringIntoViewCancellation, operation))
            {
                return false;
            }

            IsPanning = false;

            if (!ReferenceEquals(_bringIntoViewCancellation, operation))
            {
                return false;
            }

            SetCurrentValue(DisablePanningProperty, false);

            if (!ReferenceEquals(_bringIntoViewCancellation, operation))
            {
                return false;
            }

            SetCurrentValue(DisableZoomingProperty, false);
            return ReferenceEquals(_bringIntoViewCancellation, operation);
        }

        /// <summary>
        /// Moves the viewport center to the center of the specified area (graph space).
        /// </summary>
        public Task BringIntoView(Rect area, bool animated = true, Action? onFinish = null)
            => BringIntoView(new Point(area.X + area.Width / 2, area.Y + area.Height / 2), animated, onFinish);

        /// <summary>
        /// Moves the viewport center to the center of the specified area (graph space), animated.
        /// </summary>
        /// <remarks>
        /// Dedicated single-argument overload so a bare <c>BringIntoView(area)</c> call resolves
        /// unambiguously. Without it, <see cref="BringIntoView(Rect, double)"/> and
        /// <see cref="BringIntoView(Rect, bool, Action?)"/> both apply via default parameters and the
        /// call is ambiguous (CS0121). This overload centers the viewport on <paramref name="area"/>'s
        /// center rather than doing the minimal edge-pan the <c>offsetFromEdge</c> overload does.
        /// </remarks>
        /// <param name="area">The area (graph space) to center the viewport on.</param>
        public Task BringIntoView(Rect area)
            => BringIntoView(area, animated: true);

        /// <summary>
        /// Resets the viewport location to (0, 0) and the viewport zoom to 1.
        /// </summary>
        /// <param name="animated">Whether the viewport transition is animated.</param>
        /// <param name="onFinish">Callback invoked when the transition finishes.</param>
        /// <remarks>
        /// The location animation runs first, then the zoom animates back to 1 once the location has
        /// settled. <paramref name="onFinish"/> fires after the zoom animation completes.
        /// </remarks>
        public async Task ResetViewport(bool animated = true, Action? onFinish = null)
        {
            await BringIntoView(new Point(ViewportSize.Width / 2, ViewportSize.Height / 2), animated);

            if (animated && Math.Abs(ViewportZoom - 1d) > double.Epsilon)
            {
                var animation = new Animation()
                {
                    Duration = TimeSpan.FromSeconds(BringIntoViewMaxDuration),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame()
                        {
                            Setters = { new Setter(ViewportZoomProperty, ViewportZoom) },
                            Cue = new Cue(0)
                        },
                        new KeyFrame()
                        {
                            Setters = { new Setter(ViewportZoomProperty, 1d) },
                            Cue = new Cue(1)
                        }
                    }
                };
                try
                {
                    await animation.RunAsync(this, default);
                }
                finally
                {
                    // Pin the final value even if the animation is cancelled/faulted.
                    ViewportZoom = 1d;
                }
            }
            else
            {
                ViewportZoom = 1d;
            }

            onFinish?.Invoke();
        }

        /// <summary>
        /// Scales the viewport to fit the specified <paramref name="area"/> or all the <see cref="ItemContainer"/>s if that's possible.
        /// </summary>
        /// <remarks>Does nothing if <paramref name="area"/> is null and there's no items.</remarks>
        public void FitToScreen(Rect? area = null)
        {
            Rect extent = area ?? ItemsExtent;
            extent = extent.Inflate(FitToScreenExtentMargin, FitToScreenExtentMargin);

            if (extent.Width > 0 && extent.Height > 0)
            {
                double widthRatio = ViewportSize.Width / extent.Width;
                double heightRatio = ViewportSize.Height / extent.Height;

                double zoom = Math.Min(widthRatio, heightRatio);
                var center = new Point(extent.X + extent.Width / 2, extent.Y + extent.Height / 2);

                ZoomAtPosition(zoom, center);
                _ = BringIntoView(center, animated: false);
            }
        }

        #endregion

        #region Auto panning

        private void HandleAutoPanning(object? sender, EventArgs e)
        {
            if (!IsPanning && _currentPointerArgs != null && this.IsPointerCapturedWithin(_currentPointerArgs))
            {
                Point mousePosition = _currentPointerArgs.GetPosition(this);
                double edgeDistance = AutoPanEdgeDistance;
                double autoPanSpeed = Math.Min(AutoPanSpeed, AutoPanSpeed * AutoPanningTickRate) / (ViewportZoom * 2);
                double x = ViewportLocation.X;
                double y = ViewportLocation.Y;

                if (mousePosition.X <= edgeDistance)
                {
                    x -= autoPanSpeed;
                }
                else if (mousePosition.X >= Bounds.Width - edgeDistance)
                {
                    x += autoPanSpeed;
                }

                if (mousePosition.Y <= edgeDistance)
                {
                    y -= autoPanSpeed;
                }
                else if (mousePosition.Y >= Bounds.Height - edgeDistance)
                {
                    y += autoPanSpeed;
                }

                ViewportLocation = new Point(x, y);
                MouseLocation = _currentPointerArgs.GetPosition(ItemsHost);

                // Re-dispatch the last real PointerMoved event so the active Selecting drag
                // calls Update(MouseLocation) with the freshly-nudged viewport position.
                // We never re-dispatch a press event, so TryBeginDragging cannot fire twice.
                if (_lastMoveArgs != null)
                {
                    _lastMoveArgs.Handled = false;
                    InputProcessor.ProcessEvent(_lastMoveArgs);
                }
            }
        }

        /// <summary>
        /// Called when the <see cref="DisableAutoPanning"/> changes.
        /// </summary>
        /// <param name="shouldDisable">Whether to enable or disable auto panning.</param>
        protected virtual void OnDisableAutoPanningChanged(bool shouldDisable)
        {
            if (shouldDisable) 
            {
                _autoPanningTimer?.Stop();
            }
            else if (_autoPanningTimer == null)
            {
                _autoPanningTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(AutoPanningTickRate),
                    DispatcherPriority.Background, HandleAutoPanning);
            }
            else
            {
                _autoPanningTimer.Interval = TimeSpan.FromMilliseconds(AutoPanningTickRate);
                _autoPanningTimer.Start();
            }
        }

        /// <inheritdoc />
        /// <remarks>Stops the auto-panning timer so a detached editor (e.g. TabControl/view swap)
        /// doesn't leak a live DispatcherTimer.</remarks>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            OnDisableAutoPanningChanged(true);
        }

        #endregion

        #region Connector handling

        private void OnConnectorDisconnected(object sender, ConnectorEventArgs e)
        {
            if (!e.Handled && (DisconnectConnectorCommand?.CanExecute(e.Connector) ?? false))
            {
                DisconnectConnectorCommand.Execute(e.Connector);
                e.Handled = true;
            }
        }

        private void OnConnectionStarted(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Canceled && ConnectionStartedCommand != null)
            {
                e.Canceled = !ConnectionStartedCommand.CanExecute(e.SourceConnector);
                if (!e.Canceled)
                {
                    ConnectionStartedCommand.Execute(e.SourceConnector);
                }
            }
        }

        private void OnConnectionCompleted(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Canceled)
            {
                (object SourceConnector, object? TargetConnector) result = (e.SourceConnector, e.TargetConnector);
                if (ConnectionCompletedCommand?.CanExecute(result) ?? false)
                {
                    ConnectionCompletedCommand.Execute(result);
                }
            }
        }

        private void OnRemoveConnection(object sender, ConnectionEventArgs e)
        {
            if (RemoveConnectionCommand?.CanExecute(e.Connection) ?? false)
            {
                RemoveConnectionCommand.Execute(e.Connection);
            }
        }

        #endregion

        #region State Handling

        /// <summary>
        /// Gets the <see cref="Connections.ConnectionsMultiSelector"/> host looked up during template application.
        /// </summary>
        protected internal Control? ConnectionsHost { get; private set; }

        private bool _lastInputWasKeyboard;

        internal bool LastInputWasKeyboard => _lastInputWasKeyboard;

        private PointerEventArgs? _currentPointerArgs;
        private PointerEventArgs? _lastMoveArgs;
        private bool _isSelecting;
        private Rect _selectedArea;
        private Point _mouseLocation;
        private bool _isPanning;
        private bool _isCutting;
        private bool _isDragging;
        private Point _cuttingLineStart;
        private Point _cuttingLineEnd;
        private List<Control>? _cuttingLinePreviousConnections;

        private TransformGroup _viewportTransform = new TransformGroup();

        /// <summary>The input processor used to dispatch input events to the registered handler states.</summary>
        protected InputProcessor InputProcessor { get; } = new InputProcessor();

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _lastInputWasKeyboard = false;

            // Keep MouseLocation current on every press, even when a child element (e.g. an
            // ItemContainer that just captured the pointer) owns capture. Leaving this inside the
            // capture gate made the first container-drag delta subtract a stale MouseLocation,
            // teleporting the node.
            MouseLocation = e.GetPosition(ItemsHost);

            if (e.Pointer.Captured == null || this.IsMouseCaptured(e))
            {
                Focus();
                e.Pointer.Capture(this);

                _currentPointerArgs = e;
                InputProcessor.ProcessEvent(e);
            }
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            MouseLocation = e.GetPosition(ItemsHost);
            _currentPointerArgs = e;
            InputProcessor.ProcessEvent(e);

            var pointerProps = e.GetPointerPointProperties();

            if (!InputProcessor.RequiresInputCapture && this.IsMouseCaptured(e) && pointerProps is { IsLeftButtonPressed: false, IsMiddleButtonPressed: false, IsRightButtonPressed: false })
            {
                this.ReleaseMouseCapture(e);
            }

            if (!InputProcessor.RequiresInputCapture && ReferenceEquals(e.Pointer.Captured, this))
            {
                e.Pointer.Capture(null);
            }

            // Disable context menu if selecting
            if (IsSelecting)
            {
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            MouseLocation = e.GetPosition(ItemsHost);
            _currentPointerArgs = e;
            _lastMoveArgs = e;
            InputProcessor.ProcessEvent(e);
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            InputProcessor.ProcessEvent(e);
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            MouseLocation = e.GetPosition(ItemsHost);
            _currentPointerArgs = e;
            InputProcessor.ProcessEvent(e);
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            InputProcessor.ProcessEvent(e);

            // Release the mouse capture if there's no interaction in progress (e.g. a keyboard-driven
            // Cancel gesture ended a pan/select drag without a PointerReleased event).
            if (!InputProcessor.RequiresInputCapture && _currentPointerArgs != null && this.IsMouseCaptured(_currentPointerArgs))
            {
                this.ReleaseMouseCapture(_currentPointerArgs);
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            _lastInputWasKeyboard = true;

            if (!TryExecuteCommandGesture(e))
            {
                InputProcessor.ProcessEvent(e);
            }
        }

        private bool TryExecuteCommandGesture(KeyEventArgs e)
        {
            foreach (RoutedCommandBinding binding in CommandBindings)
            {
                RoutedCommand command = binding.Command;
                bool matches = false;
                if (command.Gesture is { } gestures)
                {
                    foreach (InputGesture gesture in gestures)
                    {
                        if (gesture.Matches(this, e))
                        {
                            matches = true;
                            break;
                        }
                    }
                }

                if (!matches)
                {
                    continue;
                }

                if (!command.CanExecute(parameter: null, target: this))
                {
                    continue;
                }

                command.Execute(parameter: null, target: this);
                e.Handled = true;
                return true;
            }

            return false;
        }

        #endregion

        #region Selection

        internal void ApplyPreviewingSelection()
        {
            var items = (IList)Items;
            // This method does NOT touch IsSelecting. The caller (SelectionHelper.End) already sets
            // IsSelecting = false before calling this, so each container's IsSelected flip below runs
            // with IsSelecting already false and raises its IsSelectedChangedEvent.
            Selection.BeginBatchUpdate();
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ContainerFromIndex(i)!;
                // Guard on IsSelectable so a rubber-band never selects an opted-out container.
                if (container.IsPreviewingSelection == true && container.IsSelectable)
                {
                    Selection.Select(i);
                }
                else if (container.IsPreviewingSelection == false)
                {
                    Selection.Deselect(i);
                }
                container.IsPreviewingSelection = null;
            }
            Selection.EndBatchUpdate();
        }

        internal void ClearPreviewingSelection()
        {
            var items = (IList)Items;
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ContainerFromIndex(i)!;
                container.IsPreviewingSelection = null;
            }
        }

        /// <summary>
        /// Inverts the <see cref="ItemContainer"/>s selection in the specified <paramref name="area"/>.
        /// </summary>
        /// <param name="area">The area to look for <see cref="ItemContainer"/>s.</param>
        /// <param name="fit">True to check if the <paramref name="area"/> contains the <see cref="ItemContainer"/>. <br /> False to check if <paramref name="area"/> intersects the <see cref="ItemContainer"/>.</param>
        public void InvertSelection(Rect area, bool fit = false)
        {
            var items = (IList) Items;
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ContainerFromIndex(i)!;

                if (container.IsSelectableInArea(area, fit))
                {
                    object? item = items[i];
                    if (container.IsSelected)
                    {
                        Selection.Deselect(i);
                    }
                    else
                    {
                        Selection.Select(i);
                    }
                }
            }
            Selection.EndBatchUpdate();
            IsSelecting = false;
        }

        /// <summary>
        /// Selects the <see cref="ItemContainer"/>s in the specified <paramref name="area"/>.
        /// </summary>
        /// <param name="area">The area to look for <see cref="ItemContainer"/>s.</param>
        /// <param name="append">If true, it will add to the existing selection.</param>
        /// <param name="fit">True to check if the <paramref name="area"/> contains the <see cref="ItemContainer"/>. <br /> False to check if <paramref name="area"/> intersects the <see cref="ItemContainer"/>.</param>
        public void SelectArea(Rect area, bool append = false, bool fit = false)
        {
            var items = (IList) Items;
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            try
            {
                if (!append)
                {
                    Selection.Clear();
                }

                for (var i = 0; i < items.Count; i++)
                {
                    var container = (ItemContainer)ContainerFromIndex(i)!;
                    if (container.IsSelectableInArea(area, fit))
                    {
                        Selection.Select(i);
                    }
                }
            }
            finally
            {
                Selection.EndBatchUpdate();
                IsSelecting = false;
            }
        }
        public void SelectAll()
        {
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            Selection.SelectAll();
            Selection.EndBatchUpdate();
            IsSelecting = false;
        }

        /// <summary>
        /// Unselect the <see cref="ItemContainer"/>s in the specified <paramref name="area"/>.
        /// </summary>
        /// <param name="area">The area to look for <see cref="ItemContainer"/>s.</param>
        /// <param name="fit">True to check if the <paramref name="area"/> contains the <see cref="ItemContainer"/>. <br /> False to check if <paramref name="area"/> intersects the <see cref="ItemContainer"/>.</param>
        public void UnselectArea(Rect area, bool fit = false)
        {
            var items = Selection.SelectedItems;
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ContainerFromItem(items[i]!)!;
                if (container.IsSelectableInArea(area, fit))
                {
                    var idx = Items.IndexOf(items[i]);
                    Selection.Deselect(idx);
                }
            }
            Selection.EndBatchUpdate();
            IsSelecting = false;
        }

        private SelectionHelper? _selectionOperation;

        /// <summary>
        /// Initiates a rubber-band selection operation from the current <see cref="MouseLocation"/>.
        /// Call <see cref="UpdateSelection(Point)"/> to grow the area and <see cref="EndSelecting"/> /
        /// <see cref="CancelSelecting"/> to finish.
        /// </summary>
        /// <remarks>This method has no effect if a selection operation is already in progress.</remarks>
        public void BeginSelecting(SelectionHelper.SelectionType selectionType = SelectionHelper.SelectionType.Replace)
            => BeginSelecting(MouseLocation, selectionType);

        /// <summary>
        /// Initiates a rubber-band selection operation from the specified <paramref name="location"/>.
        /// </summary>
        /// <remarks>This method has no effect if a selection operation is already in progress.</remarks>
        public void BeginSelecting(Point location, SelectionHelper.SelectionType selectionType = SelectionHelper.SelectionType.Replace)
        {
            if (IsSelecting)
            {
                return;
            }

            _selectionOperation = new SelectionHelper(this);
            _selectionOperation.Start(location, selectionType);
        }

        /// <summary>
        /// Expands or modifies the selection area to the specified <paramref name="location"/>
        /// (graph space coordinates).
        /// </summary>
        public void UpdateSelection(Point location)
        {
            _selectionOperation?.Update(location);
        }

        /// <summary>
        /// Completes the selection operation and applies any pending changes.
        /// </summary>
        /// <remarks>This method has no effect if there's no selection operation in progress.</remarks>
        public void EndSelecting()
        {
            _selectionOperation?.End();
            _selectionOperation = null;
        }

        /// <summary>
        /// Cancels the current selection operation and reverts any changes made during the selection
        /// process if <see cref="AllowSelectionCancellation"/> is true. Otherwise, ends the selection
        /// operation by calling <see cref="EndSelecting"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no selection operation in progress.</remarks>
        public void CancelSelecting()
        {
            if (!AllowSelectionCancellation)
            {
                EndSelecting();
                return;
            }

            if (IsSelecting)
            {
                _selectionOperation?.Abort();
                _selectionOperation = null;
            }
        }

        #endregion

        #region Pushing items

        public static readonly StyledProperty<ControlTheme> PushedAreaThemeProperty = AvaloniaProperty.Register<NodifyEditor, ControlTheme>(nameof(PushedAreaTheme));
        public static readonly DirectProperty<NodifyEditor, Rect> PushedAreaProperty = AvaloniaProperty.RegisterDirect<NodifyEditor, Rect>(nameof(PushedArea), o => o.PushedArea, null, default(Rect));
        public static readonly DirectProperty<NodifyEditor, bool> IsPushingItemsProperty = AvaloniaProperty.RegisterDirect<NodifyEditor, bool>(nameof(IsPushingItems), o => o.IsPushingItems, null, false);
        public static readonly DirectProperty<NodifyEditor, Orientation> PushedAreaOrientationProperty = AvaloniaProperty.RegisterDirect<NodifyEditor, Orientation>(nameof(PushedAreaOrientation), o => o.PushedAreaOrientation, null, Orientation.Horizontal);

        private Rect _pushedArea;
        private bool _isPushingItems;
        private Orientation _pushedAreaOrientation = Orientation.Horizontal;
        private IPushStrategy? _pushStrategy;

        /// <summary>
        /// Gets or sets the theme used for the pushed-area rectangle visual.
        /// </summary>
        public ControlTheme PushedAreaTheme
        {
            get => GetValue(PushedAreaThemeProperty);
            set => SetValue(PushedAreaThemeProperty, value);
        }

        /// <summary>
        /// Gets the currently pushed area while <see cref="IsPushingItems"/> is true.
        /// </summary>
        public Rect PushedArea
        {
            get => _pushedArea;
            private set => SetAndRaise(PushedAreaProperty, ref _pushedArea, value);
        }

        /// <summary>
        /// Gets a value that indicates whether a pushing operation is in progress.
        /// </summary>
        public bool IsPushingItems
        {
            get => _isPushingItems;
            private set => SetAndRaise(IsPushingItemsProperty, ref _isPushingItems, value);
        }

        /// <summary>
        /// Gets the orientation of the <see cref="PushedArea"/>.
        /// </summary>
        public Orientation PushedAreaOrientation
        {
            get => _pushedAreaOrientation;
            private set => SetAndRaise(PushedAreaOrientationProperty, ref _pushedAreaOrientation, value);
        }

        /// <summary>
        /// Gets or sets whether push items cancellation is allowed (see <see cref="EditorGestures.NodifyEditorGestures.CancelAction"/>).
        /// </summary>
        public static bool AllowPushItemsCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the same gesture both starts and stops the pushing-items operation.
        /// </summary>
        public static bool EnableToggledPushingItemsMode { get; set; }

        /// <summary>
        /// Starts the pushing-items operation at the specified location with the specified orientation.
        /// </summary>
        /// <param name="location">The starting location for pushing items, in graph space coordinates.</param>
        /// <param name="orientation">The orientation of the <see cref="PushedArea"/>.</param>
        /// <remarks>This method has no effect if a pushing operation is already in progress.</remarks>
        public void BeginPushingItems(Point location, Orientation orientation)
        {
            if (IsPushingItems)
            {
                return;
            }

            IsPushingItems = true;
            PushedAreaOrientation = orientation;

            _pushStrategy = CreatePushStrategy(orientation);
            PushedArea = _pushStrategy.Start(location);
        }

        /// <summary>
        /// Updates the pushed area by the specified amount, taking <see cref="PushedAreaOrientation"/> into account.
        /// </summary>
        public void UpdatePushedArea(Vector amount)
        {
            Debug.Assert(IsPushingItems);
            PushedArea = _pushStrategy!.Push(amount);
        }

        /// <summary>
        /// Ends the current pushing operation and finalizes the pushed-area state.
        /// </summary>
        /// <remarks>This method has no effect if there's no pushing operation in progress.</remarks>
        public void EndPushingItems()
        {
            if (!IsPushingItems)
            {
                return;
            }

            PushedArea = _pushStrategy!.End();
            _pushStrategy = null;
            IsPushingItems = false;
        }

        /// <summary>
        /// Cancels the current pushing operation, reverting the <see cref="PushedArea"/> if
        /// <see cref="AllowPushItemsCancellation"/> is true; otherwise ends it.
        /// </summary>
        /// <remarks>This method has no effect if there's no pushing operation in progress.</remarks>
        public void CancelPushingItems()
        {
            if (!AllowPushItemsCancellation)
            {
                EndPushingItems();
                return;
            }

            if (IsPushingItems)
            {
                PushedArea = _pushStrategy!.Cancel();
                IsPushingItems = false;
            }
        }

        private IPushStrategy CreatePushStrategy(Orientation orientation)
            => orientation == Orientation.Horizontal
                ? new HorizontalPushStrategy(this)
                : new VerticalPushStrategy(this);

        private void UpdatePushedAreaOnViewport()
        {
            if (IsPushingItems)
            {
                PushedArea = _pushStrategy!.GetPushedArea();
            }
        }

        #endregion

        #region Cutting

        public static readonly StyledProperty<ControlTheme> CuttingLineThemeProperty =
            AvaloniaProperty.Register<NodifyEditor, ControlTheme>(nameof(CuttingLineTheme));

        public static readonly DirectProperty<NodifyEditor, Point> CuttingLineStartProperty =
            AvaloniaProperty.RegisterDirect<NodifyEditor, Point>(
                nameof(CuttingLineStart), o => o.CuttingLineStart, null, default(Point));

        public static readonly DirectProperty<NodifyEditor, Point> CuttingLineEndProperty =
            AvaloniaProperty.RegisterDirect<NodifyEditor, Point>(
                nameof(CuttingLineEnd), o => o.CuttingLineEnd, null, default(Point));

        public static readonly StyledProperty<ICommand?> CuttingStartedCommandProperty =
            AvaloniaProperty.Register<NodifyEditor, ICommand?>(nameof(CuttingStartedCommand));

        public static readonly StyledProperty<ICommand?> CuttingCompletedCommandProperty =
            AvaloniaProperty.Register<NodifyEditor, ICommand?>(nameof(CuttingCompletedCommand));

        /// <summary>Gets or sets the theme used for the cutting-line visual.</summary>
        public ControlTheme CuttingLineTheme
        {
            get => GetValue(CuttingLineThemeProperty);
            set => SetValue(CuttingLineThemeProperty, value);
        }

        /// <summary>Gets the start point of the <see cref="Connections.CuttingLine"/> while <see cref="IsCutting"/> is true.</summary>
        public Point CuttingLineStart
        {
            get => _cuttingLineStart;
            private set => SetAndRaise(CuttingLineStartProperty, ref _cuttingLineStart, value);
        }

        /// <summary>Gets the end point of the <see cref="Connections.CuttingLine"/> while <see cref="IsCutting"/> is true.</summary>
        public Point CuttingLineEnd
        {
            get => _cuttingLineEnd;
            private set => SetAndRaise(CuttingLineEndProperty, ref _cuttingLineEnd, value);
        }

        /// <summary>Invoked when a cutting operation is started.</summary>
        public ICommand? CuttingStartedCommand
        {
            get => GetValue(CuttingStartedCommandProperty);
            set => SetValue(CuttingStartedCommandProperty, value);
        }

        /// <summary>Invoked when a cutting operation is completed.</summary>
        public ICommand? CuttingCompletedCommand
        {
            get => GetValue(CuttingCompletedCommandProperty);
            set => SetValue(CuttingCompletedCommandProperty, value);
        }

        /// <summary>Gets or sets whether cancelling a cutting operation is allowed.</summary>
        public static bool AllowCuttingCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the cutting line should preview the intersecting connections
        /// (sets <see cref="Connections.CuttingLine.IsOverElementProperty"/> on each).
        /// </summary>
        public static bool EnableCuttingLinePreview { get; set; } = false;

        /// <summary>Gets or sets whether the same gesture both starts and stops the cutting operation.</summary>
        public static bool EnableToggledCuttingMode { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether panning cancellation is allowed
        /// (see <see cref="EditorGestures.NodifyEditorGestures.CancelAction"/>).
        /// </summary>
        public static bool AllowPanningCancellation { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether cancelling a selection operation is allowed
        /// (see <see cref="EditorGestures.SelectionGestures.Cancel"/>).
        /// </summary>
        public static bool AllowSelectionCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether panning is allowed while selecting items in the editor.
        /// </summary>
        public static bool AllowPanningWhileSelecting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether panning is allowed while cutting connections in the editor.
        /// </summary>
        public static bool AllowPanningWhileCutting { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether panning is allowed while pushing items in the editor.
        /// </summary>
        public static bool AllowPanningWhilePushingItems { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether zooming is allowed while selecting items in the editor.
        /// </summary>
        public static bool AllowZoomingWhileSelecting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether zooming is allowed while cutting connections in the editor.
        /// </summary>
        public static bool AllowZoomingWhileCutting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether zooming is allowed while pushing items in the editor.
        /// </summary>
        public static bool AllowZoomingWhilePushingItems { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether zooming is allowed while panning the editor viewport.
        /// </summary>
        public static bool AllowZoomingWhilePanning { get; set; } = true;

        /// <summary>
        /// Determines whether toggled panning mode is enabled, allowing the user to start and end
        /// the interaction in two steps with the same input gesture.
        /// </summary>
        public static bool EnableToggledPanningMode { get; set; } = false;

        /// <summary>
        /// Determines whether toggled selecting mode is enabled, allowing the user to start and end
        /// the interaction in two steps with the same input gesture.
        /// </summary>
        public static bool EnableToggledSelectingMode { get; set; } = false;

        /// <summary>
        /// The exact set of connection types that participate in cutting. The four built-in connection
        /// types are registered by default. Clear the set to disable cutting, or add custom connection
        /// types explicitly.
        /// </summary>
        public static readonly HashSet<Type> CuttingConnectionTypes = CreateDefaultCuttingConnectionTypes();

        private static HashSet<Type> CreateDefaultCuttingConnectionTypes()
            =>
            [
                typeof(Connections.Connection),
                typeof(Connections.LineConnection),
                typeof(Connections.CircuitConnection),
                typeof(Connections.StepConnection)
            ];

        /// <summary>
        /// Starts the cutting operation at the current <see cref="MouseLocation"/>.
        /// </summary>
        public void BeginCutting()
            => BeginCutting(MouseLocation);

        /// <summary>
        /// Starts the cutting operation at the specified <paramref name="location"/>.
        /// Has no effect if a cutting operation is already in progress.
        /// </summary>
        public void BeginCutting(Point location)
        {
            if (IsCutting)
                return;

            CuttingLineStart = location;
            CuttingLineEnd = location;
            IsCutting = true;
        }

        /// <summary>
        /// Updates the end point of the cutting line to <paramref name="location"/> and
        /// optionally applies the preview style to intersecting connections.
        /// </summary>
        public void UpdateCuttingLine(Point location)
        {
            CuttingLineEnd = location;

            if (EnableCuttingLinePreview)
            {
                ResetCuttingPreview();
                ApplyCuttingPreview();
            }
        }

        /// <summary>
        /// Adjusts the cutting-line end point by <paramref name="amount"/> and calls
        /// <see cref="UpdateCuttingLine(Point)"/>.
        /// </summary>
        public void UpdateCuttingLine(Vector amount)
        {
            CuttingLineEnd += amount;
            UpdateCuttingLine(CuttingLineEnd);
        }

        /// <summary>
        /// Completes the cutting operation, removing all intersected connections.
        /// Has no effect if there is no cutting operation in progress.
        /// </summary>
        /// <remarks>
        /// Rendered-geometry containment requires a drawing backend implementation. Tests using
        /// Avalonia.Headless must disable headless drawing and configure a backend such as Avalonia.Skia.
        /// </remarks>
        public void EndCutting()
        {
            if (!IsCutting)
                return;

            ResetCuttingPreview();

            var connections = GetIntersectingConnections(CuttingLineStart, CuttingLineEnd);

            if (RemoveConnectionCommand != null)
            {
                foreach (var connection in connections)
                {
                    if (RemoveConnectionCommand.CanExecute(connection.DataContext))
                        RemoveConnectionCommand.Execute(connection.DataContext);
                }
            }
            else
            {
                foreach (var connection in connections)
                {
                    if (connection is BaseConnection baseConnection)
                        baseConnection.Remove();
                }
            }

            IsCutting = false;
        }

        /// <summary>
        /// Cancels the cutting operation without removing any connections.
        /// If <see cref="AllowCuttingCancellation"/> is false, ends the operation instead.
        /// Has no effect if there is no cutting operation in progress.
        /// </summary>
        public void CancelCutting()
        {
            if (!AllowCuttingCancellation)
            {
                EndCutting();
                return;
            }

            if (IsCutting)
            {
                ResetCuttingPreview();
                IsCutting = false;
            }
        }

        private void OnCuttingStarted()
        {
            if (CuttingStartedCommand?.CanExecute(DataContext) ?? false)
                CuttingStartedCommand.Execute(DataContext);
        }

        private void OnCuttingCompleted()
        {
            if (CuttingCompletedCommand?.CanExecute(DataContext) ?? false)
                CuttingCompletedCommand.Execute(DataContext);
        }

        /// <summary>Gets the connectors whose anchor lies within the current viewport.</summary>
        public IEnumerable<Connections.Connector> GetConnectorsInViewport()
        {
            var viewport = new global::Avalonia.Rect(ViewportLocation, ViewportSize);
            foreach (var connector in ItemsHost.GetVisualDescendants().OfType<Connections.Connector>())
            {
                connector.UpdateAnchor();
                if (viewport.Contains(connector.Anchor))
                    yield return connector;
            }
        }

        private List<Control> GetIntersectingConnections(Point a, Point b)
        {
            var result = new List<Control>();
            if (ConnectionsHost is not Control connectionsHost)
                return result;

            IEnumerable<Control> candidates = connectionsHost.GetVisualDescendants()
                .OfType<Control>()
                .Where(control => CuttingConnectionTypes.Contains(control.GetType()));

            foreach (var candidate in candidates)
            {
                if (ConnectionCutting.Intersects(
                        candidate,
                        connectionsHost,
                        a,
                        b,
                        BaseConnection.CuttingHitTestTolerance))
                {
                    result.Add(candidate);
                }
            }

            return result;
        }

        private void ApplyCuttingPreview()
        {
            var connections = GetIntersectingConnections(CuttingLineStart, CuttingLineEnd);
            foreach (var c in connections)
                CuttingLine.SetIsOverElement(c, true);
            _cuttingLinePreviousConnections = connections;
        }

        private void ResetCuttingPreview()
        {
            if (_cuttingLinePreviousConnections != null)
            {
                foreach (var c in _cuttingLinePreviousConnections)
                    CuttingLine.SetIsOverElement(c, false);
                _cuttingLinePreviousConnections = null;
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Translates the specified location to graph space coordinates (relative to the
        /// <see cref="ItemsHost"/>).
        /// </summary>
        /// <param name="location">The location coordinates relative to <paramref name="relativeTo"/>.</param>
        /// <param name="relativeTo">The element where the <paramref name="location"/> was calculated from.</param>
        /// <returns>A location inside the graph.</returns>
        public Point GetLocationInsideEditor(Point location, IInputElement relativeTo)
            => relativeTo is Visual visual ? visual.TranslatePoint(location, ItemsHost) ?? location : location;

        /// <summary>
        /// Translates the event location to graph space coordinates (relative to the
        /// <see cref="ItemsHost"/>).
        /// </summary>
        /// <param name="e">The pointer event.</param>
        /// <returns>A location inside the graph.</returns>
        public Point GetLocationInsideEditor(PointerEventArgs e)
            => e.GetPosition(ItemsHost);

        /// <summary>
        /// Translates the event location of <paramref name="args"/> to graph space coordinates
        /// (relative to the <see cref="ItemsHost"/>).
        /// </summary>
        /// <param name="args">The drag event.</param>
        /// <returns>A location inside the graph.</returns>
        public Point GetLocationInsideEditor(DragEventArgs args)
            => args.GetPosition(ItemsHost);

        #endregion

        #region Dragging

        /// <summary>
        /// Snaps the given value down to the nearest multiple of <see cref="GridCellSize"/>.
        /// </summary>
        public double SnapToGrid(double value) => (int)value / GridCellSize * GridCellSize;

        /// <summary>
        /// Enumerates all realized <see cref="ItemContainer"/>s in the editor.
        /// </summary>
        public IEnumerable<ItemContainer> ItemContainers
        {
            get
            {
                var items = (IList)Items;
                for (int i = 0; i < items.Count; i++)
                {
                    if (ContainerFromIndex(i) is ItemContainer container)
                    {
                        yield return container;
                    }
                }
            }
        }

        private IDraggingStrategy CreateDraggingStrategy(IEnumerable<ItemContainer> containers)
            => EnableDraggingContainersOptimizations
                ? new DraggingOptimized(this, containers)
                : new DraggingSimple(this, containers);

        private ItemsMovedEventArgs BuildItemsMovedArgs(IDraggingStrategy strategy)
            => new ItemsMovedEventArgs(strategy.Containers.Select(c => c.DataContext!).ToList(), strategy.Offset)
            {
                RoutedEvent = ItemsMovedEvent,
                Source = this
            };

        private void OnItemsDragDelta(object? sender, VectorEventArgs e)
        {
            _draggingStrategy?.Update(e.Vector);
        }

        private void OnContainerDragCompleted(object? sender, DragCompletedEventArgs e)
        {
            ItemsMovedEventArgs? movedEvent = null;

            if (e.Canceled && NodifyEditor.AllowDraggingCancellation)
            {
                _draggingStrategy?.Abort(e.Vector);
            }
            else
            {
                // Capture the moved items and offset from the strategy BEFORE End() clears it.
                if (_draggingStrategy != null)
                {
                    movedEvent = BuildItemsMovedArgs(_draggingStrategy);
                }

                IsBulkUpdatingItems = true;

                _draggingStrategy?.End(e.Vector);

                IsBulkUpdatingItems = false;

                ItemsHost.InvalidateArrange();
            }

            _draggingStrategy = null;

            // Fires ItemsDragCompletedCommand (via OnIsDraggingChanged): the command runs before
            // ItemsMoved is raised below.
            IsDragging = false;

            // Raised last (after the command). Not raised on cancel.
            if (movedEvent != null)
            {
                RaiseEvent(movedEvent);
            }
        }

        private void OnContainerDragStarted(object? sender, VectorEventArgs e)
        {
            _draggingStrategy = CreateDraggingStrategy(SelectedContainers);
            _draggingStrategy.Start(e.Vector);

            IsDragging = true;
            e.Handled = true;
        }

        /// <summary>
        /// Initiates a dragging operation for the specified <paramref name="containers"/>.
        /// Call <see cref="UpdateDragging"/> to move them and <see cref="EndDragging"/> /
        /// <see cref="CancelDragging"/> to finish. Used by the pushing-items operation and by
        /// keyboard drag (<see cref="ItemContainer.BeginDragging()"/> forwards here too).
        /// </summary>
        /// <remarks>This method has no effect if a dragging operation is already in progress.</remarks>
        public void BeginDragging(IEnumerable<ItemContainer> containers)
        {
            if (IsDragging)
            {
                return;
            }

            // Set before creating the strategy so ItemsDragStartedCommand fires first.
            IsDragging = true;
            _draggingStrategy = CreateDraggingStrategy(containers);
            _draggingStrategy.Start(default);
        }

        /// <summary>Begins dragging the currently selected containers (used by keyboard drag).</summary>
        public void BeginDragging() => BeginDragging(SelectedContainers);

        /// <summary>
        /// Moves the items in the active dragging operation by <paramref name="amount"/> (graph space).
        /// </summary>
        public void UpdateDragging(Vector amount)
        {
            _draggingStrategy?.Update(amount);
        }

        /// <summary>
        /// Completes the active dragging operation, committing the new positions and raising
        /// <see cref="ItemsMoved"/>. Fires <see cref="ItemsDragCompletedCommand"/> (via the
        /// <see cref="IsDragging"/> transition) before <see cref="ItemsMoved"/> is raised.
        /// </summary>
        public void EndDragging()
        {
            if (!IsDragging)
            {
                return;
            }

            var movedEvent = BuildItemsMovedArgs(_draggingStrategy!);

            IsBulkUpdatingItems = true;
            _draggingStrategy!.End(default);
            IsBulkUpdatingItems = false;

            ItemsHost.InvalidateArrange();

            _draggingStrategy = null;
            IsDragging = false;

            RaiseEvent(movedEvent);
        }

        /// <summary>
        /// Cancels the active dragging operation, reverting the items to their original positions if
        /// <see cref="AllowDraggingCancellation"/> is true. Otherwise, ends the dragging operation by
        /// calling <see cref="EndDragging"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no dragging operation in progress.</remarks>
        public void CancelDragging()
        {
            if (!AllowDraggingCancellation)
            {
                EndDragging();
                return;
            }

            if (IsDragging)
            {
                _draggingStrategy?.Abort(default);
                _draggingStrategy = null;
                IsDragging = false;
            }
        }

        #endregion

        #region Alignment

        /// <summary>
        /// Aligns the <see cref="SelectedContainers"/> based on the specified <paramref name="alignment"/>.
        /// </summary>
        /// <param name="alignment">The alignment type to apply to the selected containers.</param>
        /// <param name="relativeTo">An optional container to use as a reference for alignment. If null, the alignment is based on the containers themselves.</param>
        /// <remarks>This method has no effect if a dragging operation is in progress.</remarks>
        public void AlignSelection(Alignment alignment, ItemContainer? relativeTo = default)
            => AlignContainers(SelectedContainers, alignment, relativeTo);

        /// <summary>
        /// Aligns a collection of containers based on the specified <paramref name="alignment"/>.
        /// Guards on <see cref="IsDragging"/> and toggles it around the alignment so
        /// <see cref="ItemsDragStartedCommand"/>/<see cref="ItemsDragCompletedCommand"/> fire exactly
        /// like any other drag operation (via the <see cref="IsDragging"/> property transition) —
        /// this method does NOT fire those commands directly.
        /// </summary>
        /// <param name="containers">The collection of item containers to align.</param>
        /// <param name="alignment">The alignment type to apply to the containers.</param>
        /// <param name="relativeTo">An optional container to use as a reference for alignment. If null, the alignment is based on the containers themselves.</param>
        /// <remarks>This method has no effect if a dragging operation is in progress.</remarks>
        public void AlignContainers(IEnumerable<ItemContainer> containers, Alignment alignment, ItemContainer? relativeTo = default)
        {
            if (IsDragging)
            {
                return;
            }

            var containerList = containers as IReadOnlyCollection<ItemContainer> ?? containers.ToList();

            IsDragging = true;
            IsBulkUpdatingItems = true;

            switch (alignment)
            {
                case Alignment.Top:
                {
                    double top = relativeTo?.Location.Y ?? containerList.Min(x => x.Location.Y);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(c.Location.X, top);
                    }
                    break;
                }

                case Alignment.Left:
                {
                    double left = relativeTo?.Location.X ?? containerList.Min(x => x.Location.X);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(left, c.Location.Y);
                    }
                    break;
                }

                case Alignment.Bottom:
                {
                    double bottom = relativeTo != null
                        ? relativeTo.Location.Y + relativeTo.Bounds.Height
                        : containerList.Max(x => x.Location.Y + x.Bounds.Height);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(c.Location.X, bottom - c.Bounds.Height);
                    }
                    break;
                }

                case Alignment.Right:
                {
                    double right = relativeTo != null
                        ? relativeTo.Location.X + relativeTo.Bounds.Width
                        : containerList.Max(x => x.Location.X + x.Bounds.Width);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(right - c.Bounds.Width, c.Location.Y);
                    }
                    break;
                }

                case Alignment.Middle:
                {
                    double mid = relativeTo != null
                        ? relativeTo.Location.Y + relativeTo.Bounds.Height / 2
                        : containerList.Average(c => c.Location.Y + c.Bounds.Height / 2);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(c.Location.X, mid - c.Bounds.Height / 2);
                    }
                    break;
                }

                case Alignment.Center:
                {
                    double center = relativeTo != null
                        ? relativeTo.Location.X + relativeTo.Bounds.Width / 2
                        : containerList.Average(c => c.Location.X + c.Bounds.Width / 2);
                    foreach (var c in containerList)
                    {
                        c.Location = new Point(center - c.Bounds.Width / 2, c.Location.Y);
                    }
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null);
            }

            IsBulkUpdatingItems = false;
            ItemsHost.InvalidateArrange();

            IsDragging = false;
        }

        #endregion

        #region Selection Locking

        /// <summary>
        /// Locks the position of the <see cref="SelectedContainers"/>.
        /// </summary>
        public void LockSelection()
        {
            foreach (var container in SelectedContainers)
            {
                container.IsDraggable = false;
            }
        }

        /// <summary>
        /// Unlocks the position of the <see cref="SelectedContainers"/>.
        /// </summary>
        public void UnlockSelection()
        {
            foreach (var container in SelectedContainers)
            {
                container.IsDraggable = true;
            }
        }

        #endregion

        protected void OnRenderSizeChanged(object? sender, SizeChangedEventArgs args)
        {
            double zoom = ViewportZoom;
            var editor = (NodifyEditor)sender!;
            ViewportSize = new Size( editor.Bounds.Width / zoom, editor.Bounds.Height / zoom);
            OnViewportUpdated();
        }

        public IList<RoutedCommandBinding> CommandBindings { get; } = new List<RoutedCommandBinding>();
    }
}
