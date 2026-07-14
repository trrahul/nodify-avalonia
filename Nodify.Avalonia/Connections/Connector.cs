using System;
using System.Diagnostics;
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
using Nodify.Avalonia.Interactivity;

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

        /// <summary>Triggered by the <see cref="EditorGestures.ConnectorGestures.Connect"/> gesture.</summary>
        public event EventHandler<PendingConnectionEventArgs> PendingConnectionStarted
        {
            add => AddHandler(PendingConnectionStartedEvent, value);
            remove => RemoveHandler(PendingConnectionStartedEvent, value);
        }

        /// <summary>Triggered by the <see cref="EditorGestures.ConnectorGestures.Connect"/> gesture.</summary>
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

        /// <summary>Triggered by the <see cref="EditorGestures.ConnectorGestures.Disconnect"/> gesture.</summary>
        public event EventHandler<ConnectorEventArgs> Disconnect
        {
            add => AddHandler(DisconnectEvent, value);
            remove => RemoveHandler(DisconnectEvent, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<Point> AnchorProperty = AvaloniaProperty.Register<Connector,Point>(nameof(Anchor));
        public static readonly StyledProperty<bool> IsConnectedProperty = AvaloniaProperty.Register<Connector,bool>(nameof(IsConnected));
        public static readonly StyledProperty<ICommand?> DisconnectCommandProperty = AvaloniaProperty.Register<Connector,ICommand?>(nameof(DisconnectCommand));
        public static readonly StyledProperty<bool> HasCustomContextMenuProperty = NodifyEditor.HasCustomContextMenuProperty.AddOwner<Connector>();
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
        /// Parameter is the <see cref="StyledElement.DataContext"/> of this control.
        /// </summary>
        public ICommand? DisconnectCommand
        {
            get => (ICommand?)GetValue(DisconnectCommandProperty);
            set => SetValue(DisconnectCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this connector draws a custom context menu (not via <see cref="Control.ContextMenu"/>).
        /// Used to suppress starting an interaction so the context menu can appear.
        /// </summary>
        public bool HasCustomContextMenu
        {
            get => (bool)GetValue(HasCustomContextMenuProperty);
            set => SetValue(HasCustomContextMenuProperty, value);
        }

        /// <summary>
        /// Gets whether this connector has a context menu (either a <see cref="Control.ContextMenu"/> or a <see cref="HasCustomContextMenu"/>).
        /// </summary>
        public bool HasContextMenu => ContextMenu != null || HasCustomContextMenu;

        #endregion

        static Connector()
        {
            FocusableProperty.OverrideMetadata(typeof(Connector), new StyledPropertyMetadata<bool>(true));
            IsConnectedProperty.Changed.AddClassHandler<Connector, bool>(OnIsConnectedChanged);
        }

        internal static void OnIsOverElementChanged(Connector connector, AvaloniaPropertyChangedEventArgs<bool> args)
        {
            connector.PseudoClasses.Set(PendingConnection.IsOverElementPseudoClass, args.NewValue.Value);
        }

        public Connector()
        {
            InputProcessor.AddSharedHandlers(this);
            SizeChanged += OnRenderSizeChanged;
        }

        /// <summary>
        /// The input processor that dispatches pointer/keyboard events to the shared
        /// <see cref="ConnectorState"/> handlers (and any registered via
        /// <see cref="InputProcessor.Shared{TElement}.RegisterHandlerFactory{THandler}"/>).
        /// </summary>
        protected InputProcessor InputProcessor { get; } = new InputProcessor();

        #region Fields

        /// <summary>
        /// Gets the <see cref="Control"/> used to calculate the <see cref="Anchor"/>.
        /// </summary>
        protected Control? Thumb { get; private set; }

        /// <summary>
        /// Gets the <see cref="ItemContainer"/> that contains this <see cref="Connector"/>.
        /// </summary>
        public ItemContainer? Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        public NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets the effective gesture mappings for this connector (per-editor override or global fallback).
        /// </summary>
        public EditorGestures ActualGestures => Editor?.ActualGestures ?? EditorGestures.Mappings;

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
        public static bool EnableOptimizations = false;

        /// <summary>
        /// Gets or sets whether cancelling a pending connection is allowed.
        /// </summary>
        public static bool AllowPendingConnectionCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the connection should be completed in two steps.
        /// </summary>
        public static bool EnableStickyConnections { get; set; }

        private Point _lastUpdatedContainerPosition;
        private Point _pendingConnectionEndPosition;
        private bool _isHooked;
        private bool _isPendingConnection;
        private IPointer? _activePointer;

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

        private void TrySetAnchorUpdateEvents(bool value)
        {
            if (Container != null && Editor != null)
            {
                if (value && !_isHooked)
                {
                    Container.PreviewLocationChanged += UpdateAnchorOptimized;
                    Container.LocationChanged += OnLocationChanged;
                    Container.SizeChanged += OnContainerSizeChanged;
                    Editor.ViewportUpdated += OnViewportUpdated;
                    _isHooked = true;
                }
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

        private void OnContainerSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            UpdateAnchorOptimized(Container!.Location);
        }

        private void OnConnectorLoaded(object? sender, RoutedEventArgs? e)
            => TrySetAnchorUpdateEvents(true);

        private void OnConnectorUnloaded(object? sender, RoutedEventArgs e)
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
            // Advanced connectors that start collapsed never fire Loaded, so subscribe here instead.
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

        private void OnLocationChanged(object? sender, RoutedEventArgs e)
            => UpdateAnchorOptimized(Container!.Location);

        private void OnViewportUpdated(object? sender, RoutedEventArgs args)
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
            if (Editor != null && IsConnected)
            {
                bool shouldOptimize = EnableOptimizations && Editor.SelectedContainersCount >= OptimizeMinimumSelectedItems;

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
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            _activePointer = e.Pointer;
            InputProcessor.ProcessEvent(e);
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            InputProcessor.ProcessEvent(e);

            // Release the mouse capture if all the buttons are released and there's no interaction in progress
            var props = e.GetPointerPointProperties();
            if (!InputProcessor.RequiresInputCapture && this.IsMouseCaptured(e)
                && !props.IsRightButtonPressed && !props.IsLeftButtonPressed && !props.IsMiddleButtonPressed)
            {
                this.ReleaseMouseCapture(e);
            }
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
            => InputProcessor.ProcessEvent(e);

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
            => InputProcessor.ProcessEvent(e);

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            _activePointer = null;
            InputProcessor.ProcessEvent(e);
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            InputProcessor.ProcessEvent(e);

            // Release the mouse capture if there's no interaction in progress (e.g. a keyboard-driven
            // Connect/Cancel gesture ended the drag without a PointerReleased event).
            if (!InputProcessor.RequiresInputCapture)
            {
                ReleaseMouseCapture();
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
            => InputProcessor.ProcessEvent(e);

        #endregion

        #region Methods

        /// <summary>
        /// Initiates a new pending connection from this connector (see <see cref="IsPendingConnection"/>).
        /// </summary>
        /// <remarks>This method has no effect if a pending connection is already in progress.</remarks>
        public void BeginConnecting()
            => BeginConnecting(new Vector(0, 0));

        /// <summary>
        /// Initiates a new pending connection from this connector with the specified offset (see <see cref="IsPendingConnection"/>).
        /// </summary>
        /// <remarks>This method has no effect if a pending connection is already in progress.</remarks>
        public void BeginConnecting(Vector offset)
        {
            if (IsPendingConnection)
            {
                return;
            }

            UpdateAnchor();
            _pendingConnectionEndPosition = Anchor + offset;

            var args = new PendingConnectionEventArgs(DataContext!)
            {
                RoutedEvent = PendingConnectionStartedEvent,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);
            IsPendingConnection = !args.Canceled;
        }

        /// <summary>
        /// Updates the endpoint of the pending connection by adjusting its position with the specified offset.
        /// </summary>
        /// <param name="offset">The amount to adjust the pending connection's endpoint.</param>
        public void UpdatePendingConnection(Vector offset)
            => UpdatePendingConnection(_pendingConnectionEndPosition + offset);

        /// <summary>
        /// Updates the endpoint of the pending connection to the specified position.
        /// </summary>
        /// <param name="position">The new position for the connection's endpoint.</param>
        public void UpdatePendingConnection(Point position)
        {
            Debug.Assert(IsPendingConnection);

            _pendingConnectionEndPosition = position;

            var args = new PendingConnectionEventArgs(DataContext!)
            {
                RoutedEvent = PendingConnectionDragEvent,
                OffsetX = _pendingConnectionEndPosition.X - Anchor.X,
                OffsetY = _pendingConnectionEndPosition.Y - Anchor.Y,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);
        }

        /// <summary>
        /// Cancels the current pending connection without completing it if <see cref="AllowPendingConnectionCancellation"/> is true.
        /// Otherwise, it completes the pending connection by calling <see cref="EndConnecting()"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no pending connection.</remarks>
        public void CancelConnecting()
        {
            if (!AllowPendingConnectionCancellation)
            {
                EndConnecting();
                return;
            }

            if (IsPendingConnection)
            {
                var args = new PendingConnectionEventArgs(DataContext!)
                {
                    RoutedEvent = PendingConnectionCompletedEvent,
                    Anchor = Anchor,
                    Source = this,
                    Canceled = true
                };
                RaiseEvent(args);

                IsPendingConnection = false;
            }
        }

        /// <summary>
        /// Completes the current pending connection, attempting to identify a target connector near the connection's endpoint.
        /// </summary>
        /// <remarks>This method has no effect if there's no pending connection.</remarks>
        public void EndConnecting()
        {
            if (!IsPendingConnection)
            {
                return;
            }

            Control? elem = FindConnectionTarget(_pendingConnectionEndPosition);
            EndConnecting(elem?.DataContext);
        }

        /// <summary>Completes the current pending connection using the specified <paramref name="connector"/> as the target.</summary>
        /// <param name="connector">The connector to use as the connection target.</param>
        /// <remarks>This method has no effect if there's no pending connection.</remarks>
        public void EndConnecting(Connector connector)
            => EndConnecting(connector.DataContext);

        private void EndConnecting(object? targetDataContext)
        {
            if (!IsPendingConnection)
            {
                return;
            }

            var args = new PendingConnectionEventArgs(DataContext!)
            {
                TargetConnector = targetDataContext,
                RoutedEvent = PendingConnectionCompletedEvent,
                Anchor = Anchor,
                Source = this
            };
            RaiseEvent(args);

            IsPendingConnection = false;
            _pendingConnectionEndPosition = Anchor;
        }

        /// <summary>
        /// Removes all connections associated with this connector.
        /// </summary>
        /// <remarks>This method has no effect if a pending connection is in progress or the connector is not connected (see <see cref="IsConnected"/>).</remarks>
        public void RemoveConnections()
        {
            if (!IsConnected || IsPendingConnection)
            {
                return;
            }

            object? connector = DataContext;
            var args = new ConnectorEventArgs(connector!)
            {
                RoutedEvent = DisconnectEvent,
                Anchor = Anchor,
                Source = this
            };

            RaiseEvent(args);

            if (!args.Handled && (DisconnectCommand?.CanExecute(connector) ?? false))
            {
                DisconnectCommand.Execute(connector);
            }
        }

        // Caller must UpdateAnchor() first if the Anchor is stale.
        internal Point GetLocationInsideEditor(PointerEventArgs e)
        {
            if (Thumb == null)
            {
                return Anchor;
            }

            Vector thumbOffset = e.GetPosition(Thumb) - new Point(Thumb.Bounds.Width / 2, Thumb.Bounds.Height / 2);
            return Anchor + thumbOffset;
        }

        /// <summary>Searches for a <see cref="Connector"/> at the specified position.</summary>
        /// <param name="position">The position in the editor to check for a connector.</param>
        public Connector? FindTargetConnector(Point position)
        {
            if (Editor != null)
            {
                return (Connector?)PendingConnection.GetPotentialConnector(Editor, position, true);
            }

            return null;
        }

        /// <summary>Searches for a potential <see cref="Connector"/> or <see cref="ItemContainer"/> at the specified position within the editor.</summary>
        /// <param name="position">The position in the editor to check for a potential connection target.</param>
        public Control? FindConnectionTarget(Point position)
        {
            if (Editor != null)
            {
                return PendingConnection.GetPotentialConnector(Editor, position, PendingConnection.GetAllowOnlyConnectorsAttached(Editor));
            }

            return null;
        }

        #endregion

        internal void ReleaseMouseCapture()
        {
            if (_activePointer?.Captured == this)
            {
                _activePointer.Capture(null);
            }
        }

        public bool HitTest(Point point)
        {
            if (VisualRoot is not Visual root)
            {
                return false;
            }

            Point? localPoint = root.TranslatePoint(point, this);
            return localPoint is Point local && new Rect(Bounds.Size).Contains(local);
        }
    }
}
