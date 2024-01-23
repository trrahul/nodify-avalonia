﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Styling;
using Nodify.Avalonia.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.EditorStates;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;

namespace Nodify.Avalonia
{
    /// <summary>
    /// Groups <see cref="ItemContainer"/>s and <see cref="Connections.Connection"/>s in an area that you can drag, zoom and select.
    /// </summary>
    [TemplatePart(Name = "PART_ItemsHostPresenter", Type = typeof(ItemsPresenter))]
    [DefaultProperty(nameof(Decorators))]
    public class NodifyEditor : SelectingItemsControl, IRoutedCommandBindable
    {
        //protected const string ElementItemsHost = "PART_ItemsHost";

        #region Viewport

        public static readonly StyledProperty<double> ViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(ViewportZoom), 1d, defaultBindingMode:BindingMode.TwoWay, coerce: ConstrainViewportZoomToRange);

        public static readonly StyledProperty<double> MinViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(MinViewportZoom), 0.1d,coerce: CoerceMinViewportZoom);
        public static readonly StyledProperty<double> MaxViewportZoomProperty = AvaloniaProperty.Register<NodifyEditor, double>(nameof(MaxViewportZoom), 2d, coerce: CoerceMaxViewportZoom);

        public static readonly StyledProperty<Point> ViewportLocationProperty = AvaloniaProperty.Register<NodifyEditor, Point>(nameof(ViewportLocation), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<Size> ViewportSizeProperty = AvaloniaProperty.Register<NodifyEditor,Size>(nameof(ViewportSize));
        public static readonly StyledProperty<Rect> ItemsExtentProperty = AvaloniaProperty.Register<NodifyEditor,Rect>(nameof(ItemsExtent));
        public static readonly StyledProperty<Rect> DecoratorsExtentProperty = AvaloniaProperty.Register<NodifyEditor, Rect>(nameof(DecoratorsExtent));

        public static readonly DirectProperty<NodifyEditor,TransformGroup> ViewportTransformProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,TransformGroup>(nameof(ViewportTransform),o => o.ViewportTransform);

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

        /// <summary>
        /// Updates the <see cref="ViewportSize"/> and raises the <see cref="ViewportUpdatedEvent"/>.
        /// Called when the <see cref="UIElement.RenderSize"/> or <see cref="ViewportZoom"/> is changed.
        /// </summary>
        protected void OnViewportUpdated() => RaiseEvent(new RoutedEventArgs(ViewportUpdatedEvent, this));

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

        internal void UnselectAll()
        {
            Selection.Clear();
        }

        private void ApplyRenderingOptimizations()
        {
            //ToDo caching
            //if (ItemsHost != null)
            //{
            //    if (EnableRenderingContainersOptimizations && Items.Count >= OptimizeRenderingMinimumContainers)
            //    {
            //        double zoom = ViewportZoom;
            //        double availableZoomIn = 1.0 - MinViewportZoom;
            //        bool shouldCache = zoom / availableZoomIn <= OptimizeRenderingZoomOutPercent;
            //        ItemsHost.CacheMode = shouldCache ? new BitmapCache(1.0 / zoom) : null;
            //    }
            //    else
            //    {
            //        ItemsHost.CacheMode = null;
            //    }
            //}
        }

        #endregion

        #region Cosmetic Dependency Properties

        public static readonly StyledProperty<double> BringIntoViewSpeedProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(BringIntoViewSpeed), 1000d);
        public static readonly StyledProperty<double> BringIntoViewMaxDurationProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(BringIntoViewMaxDuration),1d);
        public static readonly StyledProperty<bool> DisplayConnectionsOnTopProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisplayConnectionsOnTop), false);
        public static readonly StyledProperty<bool> DisableAutoPanningProperty = AvaloniaProperty.Register<NodifyEditor,bool>(nameof(DisableAutoPanning),false);
        public static readonly StyledProperty<double> AutoPanSpeedProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(AutoPanSpeed), 15d);
        public static readonly StyledProperty<double> AutoPanEdgeDistanceProperty = AvaloniaProperty.Register<NodifyEditor,double>(nameof(AutoPanEdgeDistance), 15d);
        public static readonly StyledProperty<DataTemplate> ConnectionTemplateProperty = AvaloniaProperty.Register<NodifyEditor,DataTemplate>(nameof(ConnectionTemplate));
        public static readonly StyledProperty<DataTemplate> DecoratorTemplateProperty = AvaloniaProperty.Register<NodifyEditor,DataTemplate>(nameof(DecoratorTemplate));
        public static readonly StyledProperty<DataTemplate> PendingConnectionTemplateProperty = AvaloniaProperty.Register<NodifyEditor,DataTemplate>(nameof(PendingConnectionTemplate));
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
        public DataTemplate ConnectionTemplate
        {
            get => (DataTemplate)GetValue(ConnectionTemplateProperty);
            set => SetValue(ConnectionTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use when generating a new <see cref="DecoratorContainer"/>.
        /// </summary>
        public DataTemplate DecoratorTemplate
        {
            get => (DataTemplate)GetValue(DecoratorTemplateProperty);
            set => SetValue(DecoratorTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> to use for the <see cref="PendingConnection"/>.
        /// </summary>
        public DataTemplate PendingConnectionTemplate
        {
            get => (DataTemplate)GetValue(PendingConnectionTemplateProperty);
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

        #endregion

        #region Readonly Dependency Properties

        public static readonly DirectProperty<NodifyEditor,Rect> SelectedAreaProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,Rect>(nameof(SelectedArea),o=>o.SelectedArea, null,default(Rect));
        public static readonly DirectProperty<NodifyEditor,bool> IsSelectingProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsSelecting),o=>o.IsSelecting, null,false);
        public static readonly DirectProperty<NodifyEditor,bool> IsPanningProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,bool>(nameof(IsPanning),o=>o.IsPanning, null,false);
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
            if (ItemsSelectCompletedCommand?.CanExecute(null) ?? false)
                ItemsSelectCompletedCommand.Execute(null);
        }

        private void OnItemsSelectStarted()
        {
            if (ItemsSelectStartedCommand?.CanExecute(null) ?? false)
                ItemsSelectStartedCommand.Execute(null);
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

        private static void OnSelectedItemsSourceChanged(NodifyEditor d, AvaloniaPropertyChangedEventArgs<IList?> e)
            => d.OnSelectedItemsSourceChanged(e.OldValue.Value, e.NewValue.Value);

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
        /// Gets of sets the <see cref="FrameworkElement.DataContext"/> of the <see cref="Connections.PendingConnection"/>.
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

        public static readonly DirectProperty<NodifyEditor,ICommand?> ConnectionCompletedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ConnectionCompletedCommand),o => o.ConnectionCompletedCommand, (o,v) => o.ConnectionCompletedCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> ConnectionStartedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ConnectionStartedCommand), o => o.ConnectionStartedCommand, (o,v) => o.ConnectionStartedCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> DisconnectConnectorCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(DisconnectConnectorCommand), o => o.DisconnectConnectorCommand, (o,v) => o.DisconnectConnectorCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> RemoveConnectionCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(RemoveConnectionCommand), o => o.RemoveConnectionCommand, (o,v) => o.RemoveConnectionCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> ItemsDragStartedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ItemsDragStartedCommand), o => o.ItemsDragStartedCommand, (o,v) => o.ItemsDragStartedCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> ItemsDragCompletedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ItemsDragCompletedCommand), o => o.ItemsDragCompletedCommand, (o,v) => o.ItemsDragCompletedCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> ItemsSelectStartedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ItemsSelectStartedCommand), o => o.ItemsSelectStartedCommand, (o,v) => o.ItemsSelectStartedCommand = v);
        public static readonly DirectProperty<NodifyEditor,ICommand?> ItemsSelectCompletedCommandProperty = AvaloniaProperty.RegisterDirect<NodifyEditor,ICommand?>(nameof(ItemsSelectCompletedCommand), o => o.ItemsSelectCompletedCommand, (o,v) => o.ItemsSelectCompletedCommand = v);

        /// <summary>
        /// Invoked when the <see cref="Connections.PendingConnection"/> is completed. <br />
        /// Use <see cref="PendingConnection.StartedCommand"/> if you want to control the visibility of the connection from the viewmodel. <br />
        /// Parameter is <see cref="PendingConnection.Source"/>.
        /// </summary>
        public ICommand? ConnectionStartedCommand
        {
            get => _connectionStartedCommand;
            set => SetAndRaise(ConnectionStartedCommandProperty,ref _connectionStartedCommand, value);
        }

        /// <summary>
        /// Invoked when the <see cref="Connections.PendingConnection"/> is completed. <br />
        /// Use <see cref="PendingConnection.CompletedCommand"/> if you want to control the visibility of the connection from the viewmodel. <br />
        /// Parameter is <see cref="Tuple{T, U}"/> where <see cref="Tuple{T, U}.Item1"/> is the <see cref="PendingConnection.Source"/> and <see cref="Tuple{T, U}.Item2"/> is <see cref="PendingConnection.Target"/>.
        /// </summary>
        public ICommand? ConnectionCompletedCommand
        {
            get => _connectionCompletedCommand;
            set => SetAndRaise(ConnectionCompletedCommandProperty,ref _connectionCompletedCommand, value);
        }

        /// <summary>
        /// Invoked when the <see cref="Connector.Disconnect"/> event is raised. <br />
        /// Can also be handled at the <see cref="Connector"/> level using the <see cref="Connector.DisconnectCommand"/> command. <br />
        /// Parameter is the <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/>.
        /// </summary>
        public ICommand? DisconnectConnectorCommand
        {
            get => _disconnectConnectorCommand;
            set => SetAndRaise(DisconnectConnectorCommandProperty,ref _disconnectConnectorCommand, value);
        }

        /// <summary>
        /// Invoked when the <see cref="BaseConnection.Disconnect"/> event is raised. <br />
        /// Can also be handled at the <see cref="BaseConnection"/> level using the <see cref="BaseConnection.DisconnectCommand"/> command. <br />
        /// Parameter is the <see cref="BaseConnection"/>'s <see cref="FrameworkElement.DataContext"/>.
        /// </summary>
        public ICommand? RemoveConnectionCommand
        {
            get => _removeConnectionCommand;
            set => SetAndRaise(RemoveConnectionCommandProperty,ref _removeConnectionCommand, value);
        }

        /// <summary>
        /// Invoked when a drag operation starts for the <see cref="SelectedItems"/>.
        /// </summary>
        public ICommand? ItemsDragStartedCommand
        {
            get => _itemsDragStartedCommand;
            set => SetAndRaise(ItemsDragStartedCommandProperty,ref _itemsDragStartedCommand, value);
        }

        /// <summary>
        /// Invoked when a drag operation is completed for the <see cref="SelectedItems"/>.
        /// </summary>
        public ICommand? ItemsDragCompletedCommand
        {
            get => _itemsDragCompletedCommand;
            set => SetAndRaise(ItemsDragCompletedCommandProperty,ref _itemsDragCompletedCommand, value);
        }

        /// <summary>Invoked when a selection operation is started.</summary>
        public ICommand? ItemsSelectStartedCommand
        {
            get => _itemsSelectStartedCommand;
            set => SetAndRaise(ItemsSelectStartedCommandProperty,ref _itemsSelectStartedCommand, value);
        }

        /// <summary>Invoked when a selection operation is completed.</summary>
        public ICommand? ItemsSelectCompletedCommand
        {
            get => _itemsSelectCompletedCommand;
            set => SetAndRaise(ItemsSelectCompletedCommandProperty,ref _itemsSelectCompletedCommand, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets or sets the maximum number of pixels allowed to move the mouse before cancelling the mouse event.
        /// Useful for <see cref="ContextMenu"/>s to appear if mouse only moved a bit or not at all.
        /// </summary>
        public static double HandleRightClickAfterPanningThreshold { get; set; } = 12d;

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
        /// Tells if the <see cref="NodifyEditor"/> is doing operations on multiple items at once.
        /// </summary>
        public bool IsBulkUpdatingItems { get; protected set; }

        /// <summary>
        /// Gets the panel that holds all the <see cref="ItemContainer"/>s.
        /// </summary>
        protected internal Panel ItemsHost => ItemsPanelRoot;

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
                IList selectedItems = base.SelectedItems;
                var selectedContainers = new List<ItemContainer>(selectedItems.Count);

                for (var i = 0; i < selectedItems.Count; i++)
                {
                    var container = (ItemContainer) ContainerFromItem(selectedItems[i]);
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
            SelectedItemsProperty.Changed.AddClassHandler<NodifyEditor, IList?>(OnSelectedItemsSourceChanged);
            GridCellSizeProperty.Changed.AddClassHandler<NodifyEditor, uint>(OnGridCellSizeChanged);
            DisablePanningProperty.Changed.AddClassHandler<NodifyEditor, bool>(OnDisablePanningChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodifyEditor"/> class.
        /// </summary>
        public NodifyEditor()
        {
            //Todo
            EditorCommands.Register(this);
            AddHandler(Connector.DisconnectEvent, new ConnectorEventHandler(OnConnectorDisconnected));
            AddHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnConnectionStarted));
            AddHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnConnectionCompleted));

            AddHandler(BaseConnection.DisconnectEvent, new ConnectionEventHandler(OnRemoveConnection));

            AddHandler(ItemContainer.DragStartedEvent, new EventHandler<VectorEventArgs>(OnItemsDragStarted));
            AddHandler(ItemContainer.DragCompletedEvent, new EventHandler<DragCompletedEventArgs>(OnItemsDragCompleted));
            AddHandler(ItemContainer.DragDeltaEvent, new EventHandler<VectorEventArgs>(OnItemsDragDelta));

            var transform = new TransformGroup();
            transform.Children.Add(ScaleTransform);
            transform.Children.Add(TranslateTransform);

            SetAndRaise(ViewportTransformProperty, ref _viewportTransform, transform);
            _states.Push(GetInitialState());

            SelectionMode = SelectionMode.Multiple;
            SizeChanged += OnRenderSizeChanged;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            //_ic = e.NameScope.Find<ItemsControl>("ItemsControl")!;
            //ItemsHost = this.ItemsPanelRoot;
            OnDisableAutoPanningChanged(DisableAutoPanning);

            State.Enter(null);
        }

        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ItemContainer(this)
            {
                RenderTransform = new TranslateTransform(),
                RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Relative)
            };
        }

        //protected override bool IsItemItsOwnContainerOverride(Control item) => item is ItemContainer;

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

            if (animated && newLocation != ViewportLocation)
            {
                var wasPanning = IsPanning;
                var wasDisablePanning = DisablePanning;
                var wasDisableZooming = DisableZooming;
                IsPanning = true;
                DisablePanning = true;
                DisableZooming = true;

                double distance = newLocation.VectorSubtract(ViewportLocation).Length;
                double duration = distance / (BringIntoViewSpeed + distance / 10) * ViewportZoom;
                duration = Math.Max(0.1, Math.Min(duration, BringIntoViewMaxDuration));
               
                var animation = new Animation()
                {
                    Duration = TimeSpan.FromSeconds(duration),
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
                await animation.RunAsync(this,default);
                ViewportLocation = newLocation;
                IsPanning = wasPanning;
                DisablePanning = wasDisablePanning;
                DisableZooming = wasDisableZooming;

                onFinish?.Invoke();
            }
            else
            {
                ViewportLocation = newLocation;
                onFinish?.Invoke();
            }
        }

        public async Task BringIntoView(Rect rect, bool animated = true, Action? onFinish = null)
        {
            var right = rect.Right / ViewportZoom;
            var bottom = rect.Bottom / ViewportZoom;
            var viewportRect = new Rect(ViewportSize);
            var ho = 0d;
            var vo = 0d;
            if (viewportRect.Right < right)
            {
                ho = right - viewportRect.Right;
            }
            if (viewportRect.Bottom < bottom)
            {
                vo = bottom - viewportRect.Bottom;
            }

            var newLocation =(Point)((Vector)ViewportLocation + new Vector(ho, vo) + (ViewportSize.ToVector() / 2));
            await BringIntoView(newLocation, animated, onFinish);
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
                BringIntoView(center, animated: false);
            }
        }

        #endregion

        #region Pointer Fields

        private bool _isPointerCaptureWithin;

        #endregion

        #region Auto panning


        //todo
        private void HandleAutoPanning(object? sender, EventArgs e)
        {
            if (!IsPanning && State.CurrentPointerArgs != null && this.IsPointerCapturedWithin(State.CurrentPointerArgs))
            {
                Point mousePosition = State.CurrentPointerArgs.GetPosition(this);
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
                MouseLocation = State.CurrentPointerArgs.GetPosition(ItemsHost);

                State.HandleAutoPanning();
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

        private readonly Stack<EditorState> _states = new Stack<EditorState>();
        private bool _isSelecting;
        private Rect _selectedArea;
        private Point _mouseLocation;
        private ICommand _connectionCompletedCommand;
        private ICommand _connectionStartedCommand;
        private ICommand _itemsSelectCompletedCommand;
        private ICommand _removeConnectionCommand;
        private ICommand _disconnectConnectorCommand;
        private ICommand _itemsDragStartedCommand;
        private ICommand _itemsDragCompletedCommand;
        private ICommand _itemsSelectStartedCommand;
        private bool _isPanning;

        private TransformGroup _viewportTransform = new TransformGroup();
        //private ItemsControl _ic;

        /// <summary>The current state of the editor.</summary>
        public EditorState State => _states.Peek();

        /// <summary>Creates the initial state of the editor.</summary>
        /// <returns>The initial state.</returns>
        protected virtual EditorState GetInitialState()
            => new EditorDefaultState(this);

        /// <summary>Pushes the given state to the stack.</summary>
        /// <param name="state">The new state of the editor.</param>
        /// <remarks>Calls <see cref="EditorState.Enter"/> on the new state.</remarks>
        public void PushState(EditorState state)
        {
            var prev = State;
            _states.Push(state);
            state.Enter(prev);
        }

        /// <summary>Pops the current <see cref="State"/> from the stack.</summary>
        /// <remarks>It doesn't pop the initial state. (see <see cref="GetInitialState"/>)
        /// <br />Calls <see cref="EditorState.Exit"/> on the current state.
        /// <br />Calls <see cref="EditorState.ReEnter"/> on the previous state.
        /// </remarks>
        public void PopState()
        {
            // Never remove the default state
            if (_states.Count > 1)
            {
                EditorState prev = _states.Pop();
                prev.Exit();
                State.ReEnter(prev);
            }
        }

        /// <summary>Pops all states from the editor.</summary>
        /// <remarks>It doesn't pop the initial state. (see <see cref="GetInitialState"/>)</remarks>
        public void PopAllStates()
        {
            while (_states.Count > 1)
            {
                PopState();
            }
        }

        /// <inheritdoc />

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.Pointer.Captured == null || this.IsMouseCaptured(e))
            {
                Focus();
                e.Pointer.Capture(this);

                MouseLocation = e.GetPosition(ItemsHost);
                State.HandlePointerPressed(e);
            }
        }


        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            MouseLocation = e.GetPosition(ItemsHost);
            State.HandlePointerReleased(e);

            // Release the mouse capture if all the mouse buttons are released 
            var pointerProps = e.GetPointerPointProperties();

            if (this.IsMouseCaptured(e) && pointerProps is { IsLeftButtonPressed: false, IsMiddleButtonPressed: false,IsRightButtonPressed : false })
            {
                this.ReleaseMouseCapture(e);
            }

            if (ReferenceEquals(e.Pointer.Captured, this))
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
            State.HandlePointerMove(e);
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e) => PopAllStates();

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            State.HandlePointerWheel(e);

            if (!e.Handled && EditorGestures.Zoom == e.KeyModifiers)
            {
                var delta = e.Delta.Length * Math.Sign(e.Delta.X + e.Delta.Y);
                double zoom = Math.Pow(2.0, delta / 3.0 );
                ZoomAtPosition(zoom, e.GetPosition(ItemsHost));

                // Handle it for nested editors
                if (e.Source is NodifyEditor || (e.Source is Visual v && ReferenceEquals(v.FindAncestorOfType<NodifyEditor>(), this)))
                {
                    e.Handled = true;
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
            => State.HandleKeyUp(e);

        protected override void OnKeyDown(KeyEventArgs e)
            => State.HandleKeyDown(e);

        #endregion

        #region Selection Handlers

        private void OnSelectedItemsSourceChanged(IList? oldValue, IList? newValue)
        {
            if (oldValue is INotifyCollectionChanged oc)
            {
                oc.CollectionChanged -= OnSelectedItemsChanged;
            }

            if (newValue is INotifyCollectionChanged nc)
            {
                nc.CollectionChanged += OnSelectedItemsChanged;
            }
            //only a single collection 
            //IList selectedItems = SelectedItems;
            //Selection.BeginBatchUpdate();
            //selectedItems.Clear();
            //if (newValue != null)
            //{
            //    for (var i = 0; i < newValue.Count; i++)
            //    {
            //        selectedItems.Add(newValue[i]);
            //    }
            //}
            //Selection.EndBatchUpdate();
        }

        private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            //switch (e.Action)
            //{
            //    case NotifyCollectionChangedAction.Reset:
            //        base.SelectedItems.Clear();
            //        break;

            //    case NotifyCollectionChangedAction.Add:
            //        IList? newItems = e.NewItems;
            //        if (newItems != null)
            //        {
            //            IList selectedItems = base.SelectedItems;
            //            for (var i = 0; i < newItems.Count; i++)
            //            {
            //                selectedItems.Add(newItems[i]);
            //            }
            //        }
            //        break;

            //    case NotifyCollectionChangedAction.Remove:
            //        IList? oldItems = e.OldItems;
            //        if (oldItems != null)
            //        {
            //            IList selectedItems = base.SelectedItems;
            //            for (var i = 0; i < oldItems.Count; i++)
            //            {
            //                selectedItems.Remove(oldItems[i]);
            //            }
            //        }
            //        break;
            //}
        }

        #endregion

        #region Selection

        internal void ApplyPreviewingSelection()
        {
            var items = (IList)Items;
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ItemContainerGenerator.ContainerFromIndex(i);
                if (container.IsPreviewingSelection == true)
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
            IsSelecting = false;
        }

        internal void ClearPreviewingSelection()
        {
            var items = (IList)Items;
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ItemContainerGenerator.ContainerFromIndex(i);
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
                var container = (ItemContainer)ItemContainerGenerator.ContainerFromIndex(i);

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
            if (!append)
            {
                Selection.Clear();
            }

            var items = (IList) Items;
            IsSelecting = true;
            Selection.BeginBatchUpdate();
            for (var i = 0; i < items.Count; i++)
            {
                var container = (ItemContainer)ItemContainerGenerator.ContainerFromIndex(i);
                if (container.IsSelectableInArea(area, fit))
                {
                    Selection.Select(i);
                }
            }
            Selection.EndBatchUpdate();
            IsSelecting = false;
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
                var container = (ItemContainer)ContainerFromItem(items[i]);
                if (container.IsSelectableInArea(area, fit))
                {
                    var idx = Items.IndexOf(items[i]);
                    Selection.Deselect(idx);
                }
            }
            Selection.EndBatchUpdate();
            IsSelecting = false;
        }

        #endregion

        #region Dragging

        private void OnItemsDragDelta(object sender, VectorEventArgs e)
        {
            _draggingStrategy?.Update(e.Vector);
        }

        private void OnItemsDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (e.Canceled && ItemContainer.AllowDraggingCancellation)
            {
                _draggingStrategy?.Abort(e.Vector);
            }
            else
            {
                IsBulkUpdatingItems = true;

                _draggingStrategy?.End(e.Vector);

                IsBulkUpdatingItems = false;

                // Draw the containers at the new position.
                ItemsHost.InvalidateArrange();
            }

            if (ItemsDragCompletedCommand?.CanExecute(null) ?? false)
            {
                ItemsDragCompletedCommand.Execute(null);
            }
        }

        private void OnItemsDragStarted(object sender, VectorEventArgs e)
        {
            var selectedItems = Selection.SelectedItems;
            if (EnableDraggingContainersOptimizations)
            {
                _draggingStrategy = new DraggingOptimized(this);
            }
            else
            {
                _draggingStrategy = new DraggingSimple(this);
            }

            _draggingStrategy.Start(e.Vector);

            if (selectedItems.Count > 0)
            {
                if (ItemsDragStartedCommand?.CanExecute(null) ?? false)
                {
                    ItemsDragStartedCommand.Execute(null);
                }

                e.Handled = true;
            }
        }

        #endregion

        /// <inheritdoc />
        protected void OnRenderSizeChanged(object? sender, SizeChangedEventArgs args)
        {
            double zoom = ViewportZoom;
            var editor = (NodifyEditor)sender;
            ViewportSize = new Size( editor.Bounds.Width / zoom, editor.Bounds.Height / zoom);
            OnViewportUpdated();
        }

        public IList<RoutedCommandBinding> CommandBindings { get; } = new List<RoutedCommandBinding>();
    }
}
