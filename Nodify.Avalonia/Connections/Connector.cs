using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a connector control that can start and complete a <see cref="PendingConnection"/>.
    /// Has a <see cref="ElementConnector"/> that the <see cref="Anchor"/> is calculated from for the <see cref="PendingConnection"/>. Center of this control is used if missing.
    /// </summary>
    [TemplatePart(Name = ElementConnector, Type = typeof(Control))]
    [PseudoClasses(IsConnectedPseudoClass,PendingConnection.IsOverElementPseudoClass)]
    public class Connector : TemplatedControl, ICustomHitTest
    {
        protected const string ElementConnector = "PART_Connector";
        private const string IsConnectedPseudoClass = ":isconnected";

        #region Routed Events

        public static readonly RoutedEvent PendingConnectionStartedEvent = RoutedEvent.Register<Connector,PendingConnectionEventArgs>(nameof(PendingConnectionStarted),RoutingStrategies.Bubble);
        public static readonly RoutedEvent PendingConnectionCompletedEvent = RoutedEvent.Register<Connector,PendingConnectionEventArgs>(nameof(PendingConnectionCompleted),RoutingStrategies.Bubble);
        public static readonly RoutedEvent PendingConnectionDragEvent = RoutedEvent.Register<Connector, PendingConnectionEventArgs>(nameof(PendingConnectionDrag), RoutingStrategies.Bubble);
        public static readonly RoutedEvent DisconnectEvent = RoutedEvent.Register<Connector, ConnectorEventArgs>(nameof(Disconnect), RoutingStrategies.Bubble);

        /// <summary>Triggered by the <see cref="EditorGestures.Connector.Connect"/> gesture.</summary>
        public event EventHandler<PendingConnectionEventArgs> PendingConnectionStarted
        {
            add => AddHandler(PendingConnectionStartedEvent, value);
            remove => RemoveHandler(PendingConnectionStartedEvent, value);
        }

        /// <summary>Triggered by the <see cref="EditorGestures.Connector.Connect"/> gesture.</summary>
        public event EventHandler<PendingConnectionEventArgs> PendingConnectionCompleted
        {
            add => AddHandler(PendingConnectionCompletedEvent, value);
            remove => RemoveHandler(PendingConnectionCompletedEvent, value);
        }

        /// <summary>
        /// Occurs when the mouse is changing position and the <see cref="Connector"/> has mouse capture.
        /// </summary>
        public event EventHandler<PendingConnectionEventArgs> PendingConnectionDrag
        {
            add => AddHandler(PendingConnectionDragEvent, value);
            remove => RemoveHandler(PendingConnectionDragEvent, value);
        }

        /// <summary>Triggered by the <see cref="EditorGestures.Connector.Disconnect"/> gesture.</summary>
        public event EventHandler<ConnectorEventArgs> Disconnect
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<Point> AnchorProperty = AvaloniaProperty.Register<Connector,Point>(nameof(Anchor));
        public static readonly StyledProperty<bool> IsConnectedProperty = AvaloniaProperty.Register<Connector,bool>(nameof(IsConnected));
        public static readonly StyledProperty<bool> IsFlowProperty = AvaloniaProperty.Register<Connector,bool>(nameof(IsFlow));
        public static readonly StyledProperty<ICommand?> DisconnectCommandProperty = AvaloniaProperty.Register<Connector,ICommand?>(nameof(DisconnectCommand));
        public static readonly DirectProperty<Connector, bool> IsPendingConnectionProperty =
            AvaloniaProperty.RegisterDirect<Connector, bool>(nameof(IsPendingConnection), o => o.IsPendingConnection);

        /// <summary>
        /// Gets the location where <see cref="Connection"/>s can be attached to. 
        /// Bind with <see cref="BindingMode.OneWayToSource"/>
        /// </summary>
        public Point Anchor
        {
            get => (Point)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        /// <summary>
        /// If this is set to false, the <see cref="Disconnect"/> event will not be invoked and the connector will stop updating its <see cref="Anchor"/> when moved, resized etc.
        /// </summary>
        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }
        public bool IsFlow
        {
            get => (bool)GetValue(IsFlowProperty);
            set => SetValue(IsFlowProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether a <see cref="PendingConnection"/> is in progress for this <see cref="Connector"/>.
        /// </summary>
        public bool IsPendingConnection
        {
            get => _isPendingConnection;
            protected set => SetAndRaise(IsPendingConnectionProperty,ref _isPendingConnection, value);
        }

        /// <summary>
        /// Invoked if the <see cref="Disconnect"/> event is not handled.
        /// Parameter is the <see cref="FrameworkElement.DataContext"/> of this control.
        /// </summary>
        public ICommand? DisconnectCommand
        {
            get => (ICommand?)GetValue(DisconnectCommandProperty);
            set => SetValue(DisconnectCommandProperty, value);
        }

        #endregion

        static Connector()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(Connector), new FrameworkPropertyMetadata(typeof(Connector)));
            FocusableProperty.OverrideMetadata(typeof(Connector), new StyledPropertyMetadata<bool>(true));
            IsConnectedProperty.Changed.AddClassHandler<Connector, bool>(OnIsConnectedChanged);
        }

        internal static void OnIsOverElementChanged(Connector connector, AvaloniaPropertyChangedEventArgs<bool> args)
        {
            connector.PseudoClasses.Set(PendingConnection.IsOverElementPseudoClass, args.NewValue.Value);
        }

        public Connector()
        {
            SizeChanged += OnRenderSizeChanged;
        }

        #region Fields

        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> used to calculate the <see cref="Anchor"/>.
        /// </summary>
        protected Control? Thumb { get; private set; }

        /// <summary>
        /// Gets the <see cref="ItemContainer"/> that contains this <see cref="Connector"/>.
        /// </summary>
        protected ItemContainer? Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        protected internal NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets or sets the safe zone outside the editor's viewport that will not trigger optimizations.
        /// </summary>
        public static double OptimizeSafeZone = 1000d;

        /// <summary>
        /// Gets or sets the minimum selected items needed to trigger optimizations when outside of the <see cref="OptimizeSafeZone"/>.
        /// </summary>
        public static uint OptimizeMinimumSelectedItems = 100;

        /// <summary>
        /// Gets or sets if <see cref="Connector"/>s should enable optimizations based on <see cref="OptimizeSafeZone"/> and <see cref="OptimizeMinimumSelectedItems"/>.
        /// </summary>
        public static bool EnableOptimizations = true; //todo

        /// <summary>
        /// Gets or sets whether cancelling a pending connection is allowed.
        /// </summary>
        public static bool AllowPendingConnectionCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the connection should be completed in two steps.
        /// </summary>
        public static bool EnableStickyConnections { get; set; }

        private Point _lastUpdatedContainerPosition;
        private Point _thumbCenter;
        private bool _isHooked;
        private bool _isPendingConnection;

        #endregion

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            Thumb = e.NameScope.Find<Control>(ElementConnector) ?? this;
            Container = this.FindAncestorOfType<ItemContainer>();
            Editor = Container?.Editor ?? this.FindAncestorOfType<NodifyEditor>();

            Loaded += OnConnectorLoaded;
            Unloaded += OnConnectorUnloaded;
        }

        #region Update connector

        // Toggle events that could be used to update the Anchor
        private void TrySetAnchorUpdateEvents(bool value)
        {
            if (Container != null && Editor != null)
            {
                // If events are not already hooked and we are asked to subscribe
                if (value && !_isHooked)
                {
                    Container.PreviewLocationChanged += UpdateAnchorOptimized;
                    Container.LocationChanged += OnLocationChanged;
                    Container.SizeChanged += OnContainerSizeChanged;
                    Editor.ViewportUpdated += OnViewportUpdated;
                    _isHooked = true;
                }
                // If events are already hooked and we are asked to unsubscribe
                else if (_isHooked && !value)
                {
                    Container.PreviewLocationChanged -= UpdateAnchorOptimized;
                    Container.LocationChanged -= OnLocationChanged;
                    Container.SizeChanged -= OnContainerSizeChanged;
                    Editor.ViewportUpdated -= OnViewportUpdated;
                    _isHooked = false;
                }
            }
        }

        private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAnchorOptimized(Container!.Location);
        }

        private void OnConnectorLoaded(object sender, RoutedEventArgs? e)
            => TrySetAnchorUpdateEvents(true);

        private void OnConnectorUnloaded(object sender, RoutedEventArgs e)
            => TrySetAnchorUpdateEvents(false);

        private static void OnIsConnectedChanged(Connector connector, AvaloniaPropertyChangedEventArgs<bool> args)
        {
            if (args.NewValue.Value)
            {
                connector.UpdateAnchor();
            }
            connector.PseudoClasses.Set(IsConnectedPseudoClass, args.NewValue.Value);
        }

        /// <inheritdoc />
        protected void OnRenderSizeChanged(object? sender, SizeChangedEventArgs args)
        {
            // Subscribe to events if not already subscribed 
            // Useful for advanced connectors that start collapsed because the loaded event is not called
            Size newSize = args.NewSize;
            if (newSize.Width > 0d || newSize.Height > 0d)
            {
                TrySetAnchorUpdateEvents(true);

                if (Container != null)
                {
                    UpdateAnchorOptimized(Container!.Location);
                }
            }
        }

        private void OnLocationChanged(object sender, RoutedEventArgs e)
            => UpdateAnchorOptimized(Container!.Location);

        private void OnViewportUpdated(object sender, RoutedEventArgs args)
        {
            if (Container != null && !Container.IsPreviewingLocation && _lastUpdatedContainerPosition != Container.Location)
            {
                UpdateAnchorOptimized(Container.Location);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> and applies optimizations if needed based on <see cref="EnableOptimizations"/> flag
        /// </summary>
        /// <param name="location"></param>
        protected void UpdateAnchorOptimized(Point location)
        {
            // Update only connectors that are connected
            if (Editor != null && IsConnected)
            {
                bool shouldOptimize = EnableOptimizations && Editor.SelectedItems?.Count > OptimizeMinimumSelectedItems;

                if (shouldOptimize)
                {
                    UpdateAnchorBasedOnLocation(Editor, location);
                }
                else
                {
                    UpdateAnchor(location);
                }
            }
        }

        private void UpdateAnchorBasedOnLocation(NodifyEditor editor, Point location)
        {
            var viewport = new Rect(editor.ViewportLocation, editor.ViewportSize);
            double offset = OptimizeSafeZone / editor.ViewportZoom;

            Rect area = viewport.Inflate(offset, offset);

            // Update only the connectors that are in the viewport or will be in the viewport
            if (area.Contains(location))
            {
                UpdateAnchor(location);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> relative to a location. (usually <see cref="Container"/>'s location)
        /// </summary>
        /// <param name="location">The relative location</param>
        protected void UpdateAnchor(Point location)
        {
            _lastUpdatedContainerPosition = location;

            if (Thumb != null && Container != null)
            {
                var thumbSize = Thumb.Bounds.Size.ToVector();
                Vector containerMargin = Container.Bounds.Size.ToVector() - Container.DesiredSize.ToVector();
                Point relativeLocation = Thumb.TranslatePoint((Point)(thumbSize / 2 - containerMargin/2), Container) ?? default;
                Anchor = new Point(location.X + relativeLocation.X, location.Y + relativeLocation.Y);
            }
        }

        /// <summary>
        /// Updates the <see cref="Anchor"/> based on <see cref="Container"/>'s location.
        /// </summary>
        public void UpdateAnchor()
        {
            if (Container != null)
            {
                UpdateAnchor(Container.Location);
            }
        }

        #endregion

        #region Event Handlers

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            OnConnectorDragCompleted(cancel: true);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            Focus();
            this.CaptureMouseSafe(e);
            e.Handled = true;

            if (EditorGestures.Connector.Disconnect.Matches(e.Source, e))
            {
                OnDisconnect();
            }
            else if (EditorGestures.Connector.Connect.Matches(e.Source, e))
            {
                if (EnableStickyConnections && IsPendingConnection)
                {
                    OnConnectorDragCompleted();
                }
                else
                {
                    UpdateAnchor();
                    OnConnectorDragStarted(e);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // Don't give the ItemContainer the chance to handle selection
            e.Handled = EnableStickyConnections;

            if (!EnableStickyConnections && EditorGestures.Connector.Connect.Matches(e.Source, e))
            {
                OnConnectorDragCompleted();
                e.Handled = true;
            }
            else if (AllowPendingConnectionCancellation && EditorGestures.Connector.CancelAction.Matches(e.Source, e))
            {
                // Cancel pending connection
                OnConnectorDragCompleted(cancel: true);
                this.ReleaseMouseCapture(e);

                // Don't show context menu
                e.Handled = true;
            }

            if (e.Pointer.Captured == this && !IsPendingConnection)
            {
                this.ReleaseMouseCapture(e);
            }
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (AllowPendingConnectionCancellation && EditorGestures.Connector.CancelAction.Matches(e.Source, e))
            {
                // Cancel pending connection
                OnConnectorDragCompleted(cancel: true);
            }
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (IsPendingConnection)
            {
                Vector offset = e.GetPosition(Thumb) - _thumbCenter;
                OnConnectorDrag(offset);
            }
        }

        protected virtual void OnConnectorDrag(Vector offset)
        {
            var args = new PendingConnectionEventArgs(DataContext)
            {
                RoutedEvent = PendingConnectionDragEvent,
                OffsetX = offset.X,
                OffsetY = offset.Y,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);
        }

        protected virtual void OnConnectorDragStarted(PointerPressedEventArgs e)
        {
            if (Thumb != null)
            {
                _thumbCenter = new Point(Thumb.Bounds.Width / 2, Thumb.Bounds.Height / 2);
            }

            var args = new PendingConnectionEventArgs(DataContext)
            {
                RoutedEvent = PendingConnectionStartedEvent,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);
            IsPendingConnection = !args.Canceled;

            if (e.Pointer.Captured == this && !IsPendingConnection)
            {
                e.Pointer.Capture(null);
            }
        }

        protected virtual void OnConnectorDragCompleted( bool cancel = false)
        {
            if (IsPendingConnection)
            {
                Control? elem = Editor != null ? PendingConnection.GetPotentialConnector(Editor,Editor.State.CurrentPointerArgs.GetPosition(Editor.ItemsHost), PendingConnection.GetAllowOnlyConnectorsAttached(Editor)) : null;

                var args = new PendingConnectionEventArgs(DataContext)
                {
                    TargetConnector = elem?.DataContext,
                    RoutedEvent = PendingConnectionCompletedEvent,
                    Anchor = Anchor,
                    Source = this,
                    Canceled = cancel
                };

                IsPendingConnection = false;
                RaiseEvent(args);
            }
        }

        protected virtual void OnDisconnect()
        {
            if (IsConnected && !IsPendingConnection)
            {
                object? connector = DataContext;
                var args = new ConnectorEventArgs(connector)
                {
                    RoutedEvent = DisconnectEvent,
                    Anchor = Anchor,
                    Source = this
                };

                RaiseEvent(args);

                // Raise DisconnectCommand if event is Disconnect not handled
                if (!args.Handled && (DisconnectCommand?.CanExecute(connector) ?? false))
                {
                    DisconnectCommand.Execute(connector);
                }
            }
        }


        
        #endregion

        public bool HitTest(Point point)
        {
            return this.Bounds.Contains(point);
        }
    }
}
