using System.Collections.Generic;
using System.ComponentModel;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Interactivity;
using Nodify.Avalonia.Nodes;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    public enum ConnectionStyle
    {
        Default,
        Line,
        Circuit,
        Step
    }

    public class EditorSettings : ReactiveObject
    {
        private readonly IReadOnlyList<ISettingViewModel> _settings;
        private readonly IReadOnlyList<ISettingViewModel> _advancedSettings;

        public IEnumerable<ISettingViewModel> Settings => PlaygroundSettings.Instance.FilterAndSort(_settings);
        public IEnumerable<ISettingViewModel> AdvancedSettings => PlaygroundSettings.Instance.FilterAndSort(_advancedSettings);

        private EditorSettings()
        {
            PlaygroundSettings.Instance.PropertyChanged += OnSearchTextChanged;

            _settings = new List<ISettingViewModel>
            {
                new ProxySettingViewModel<bool>(() => EnableRealtimeSelection, v => EnableRealtimeSelection = v,
                    "Realtime selection: ", "Selects items when finished if disabled."),
                new ProxySettingViewModel<bool>(() => SelectableNodes, v => SelectableNodes = v,
                    "Selectable nodes: ", "Whether nodes can be selected."),
                new ProxySettingViewModel<bool>(() => DraggableNodes, v => DraggableNodes = v,
                    "Draggable nodes: ", "Whether nodes can be dragged."),
                new ProxySettingViewModel<bool>(() => EnablePendingConnectionSnapping, v => EnablePendingConnectionSnapping = v,
                    "Pending connection snapping: ", "Whether to snap the pending connection to connectors"),
                new ProxySettingViewModel<bool>(() => EnablePendingConnectionPreview, v => EnablePendingConnectionPreview = v,
                    "Pending connection preview: ", "Show information about the pending connection."),
                new ProxySettingViewModel<bool>(() => AllowConnectingToConnectorsOnly, v => AllowConnectingToConnectorsOnly = v,
                    "Disable drop connection on node: ", "Can connect directly to nodes if enabled"),
                new ProxySettingViewModel<bool>(() => DisableAutoPanning, v => DisableAutoPanning = v,
                    "Disable auto panning: ", null),
                new ProxySettingViewModel<bool>(() => DisablePanning, v => DisablePanning = v,
                    "Disable panning: ", null),
                new ProxySettingViewModel<bool>(() => DisableZooming, v => DisableZooming = v,
                    "Disable zooming: ", null),
                new ProxySettingViewModel<uint>(() => GridSpacing, v => GridSpacing = v,
                    "Grid spacing: ", "Snapping value in pixels"),
                new ProxySettingViewModel<PointEditor>(() => Location, v => Location = v,
                    "Location: ", "The viewport's location."),
                new ProxySettingViewModel<double>(() => Zoom, v => Zoom = v,
                    "Zoom: ", "The viewport's zoom. Not accurate when trying to zoom outside the MinViewportZoom and MaxViewportZoom because of dependency property coercion not updating the binding with the final result."),
                new ProxySettingViewModel<double>(() => MinZoom, v => MinZoom = v,
                    "Min zoom: ", null),
                new ProxySettingViewModel<double>(() => MaxZoom, v => MaxZoom = v,
                    "Max zoom: ", null),
                new ProxySettingViewModel<double>(() => AutoPanningSpeed, v => AutoPanningSpeed = v,
                    "Auto panning speed: ", "Speed value in pixels per tick"),
                new ProxySettingViewModel<double>(() => AutoPanningEdgeDistance, v => AutoPanningEdgeDistance = v,
                    "Auto panning edge distance: ", "Distance from edge to trigger auto panning"),
                new ProxySettingViewModel<bool>(() => EnableStickyConnectors, v => EnableStickyConnectors = v,
                    "Enable sticky connectors: ", "The connection can be completed in two steps (e.g. click to create pending connection, click to connect)"),
                new ProxySettingViewModel<bool>(() => SelectableConnections, v => SelectableConnections = v,
                    "Selectable connections: ", "Whether connections can be selected."),
                new ProxySettingViewModel<bool>(() => CanSelectMultipleNodes, v => CanSelectMultipleNodes = v,
                    "Can select multiple nodes: ", null),
                new ProxySettingViewModel<bool>(() => CanSelectMultipleConnections, v => CanSelectMultipleConnections = v,
                    "Can select multiple connections: ", null),
                new ProxySettingViewModel<ConnectionStyle>(() => ConnectionStyle, v => ConnectionStyle = v,
                    "Connection style: ", null),
                new ProxySettingViewModel<string>(() => ConnectionText, v => ConnectionText = v,
                    "Connection text: ", null),
                new ProxySettingViewModel<double>(() => CircuitConnectionAngle, v => CircuitConnectionAngle = v,
                    "Connection angle: ", "Applies to circuit connection style"),
                new ProxySettingViewModel<double>(() => ConnectionCornerRadius, v => ConnectionCornerRadius = v,
                    "Connection corner radius: ", "The corner radius between the line segments."),
                new ProxySettingViewModel<double>(() => ConnectionSpacing, v => ConnectionSpacing = v,
                    "Connection spacing: ", "The distance between the start point and the where the angle breaks"),
                new ProxySettingViewModel<PointEditor>(() => ConnectionArrowSize, v => ConnectionArrowSize = v,
                    "Connection arrowhead size: ", "The size of the arrowhead."),
                new ProxySettingViewModel<uint>(() => DirectionalArrowsCount, v => DirectionalArrowsCount = v,
                    "Directional arrows count: ", "The number of arrowheads to draw on the line flowing in the direction of the connection."),
                new ProxySettingViewModel<double>(() => DirectionalArrowsOffset, v => DirectionalArrowsOffset = v,
                    "Directional arrows offset: ", "Used to animate the directional arrowheads flowing in the direction of the connection (value is between 0 and 1)."),
                new ProxySettingViewModel<bool>(() => IsAnimatingConnections, v => IsAnimatingConnections = v,
                    "Animate directional arrows: ", "Used to animate the directional arrowheads by animating the DirectionalArrowsOffset value"),
                new ProxySettingViewModel<double>(() => DirectionalArrowsAnimationDuration, v => DirectionalArrowsAnimationDuration = v,
                    "Arrows animation duration: ", "The duration in seconds of a directional arrowhead flowing from start to end."),
                new ProxySettingViewModel<ArrowHeadEnds>(() => ArrowHeadEnds, v => ArrowHeadEnds = v,
                    "Connection arrowhead end: ", "The location of the arrowhead."),
                new ProxySettingViewModel<ArrowHeadShape>(() => ArrowHeadShape, v => ArrowHeadShape = v,
                    "Connection arrowhead shape: ", "The shape of the arrow head."),
                new ProxySettingViewModel<ConnectionOffsetMode>(() => ConnectionSourceOffsetMode, v => ConnectionSourceOffsetMode = v,
                    "Connection source offset mode: ", null),
                new ProxySettingViewModel<ConnectionOffsetMode>(() => ConnectionTargetOffsetMode, v => ConnectionTargetOffsetMode = v,
                    "Connection target offset mode: ", null),
                new ProxySettingViewModel<PointEditor>(() => ConnectionSourceOffset, v => ConnectionSourceOffset = v,
                    "Connection source offset: ", null),
                new ProxySettingViewModel<PointEditor>(() => ConnectionTargetOffset, v => ConnectionTargetOffset = v,
                    "Connection target offset: ", null),
                new ProxySettingViewModel<double>(() => ConnectionStrokeThickness, v => ConnectionStrokeThickness = v,
                    "Connection stroke thickness: ", null),
                new ProxySettingViewModel<double>(() => ConnectionOutlineThickness, v => ConnectionOutlineThickness = v,
                    "Connection outline thickness: ", null),
                new ProxySettingViewModel<double>(() => ConnectionFocusVisualPadding, v => ConnectionFocusVisualPadding = v,
                    "Connection focus visual padding: ", null),
                new ProxySettingViewModel<bool>(() => DisplayConnectionsOnTop, v => DisplayConnectionsOnTop = v,
                    "Display connections on top: ", null),
                new ProxySettingViewModel<double>(() => BringIntoViewSpeed, v => BringIntoViewSpeed = v,
                    "Bring into view speed: ", "Bring location into view animation speed in pixels per second"),
                new ProxySettingViewModel<double>(() => BringIntoViewMaxDuration, v => BringIntoViewMaxDuration = v,
                    "Bring into view max duration: ", "Bring location into view max animation duration"),
                new ProxySettingViewModel<GroupingMovementMode>(() => GroupingNodeMovement, v => GroupingNodeMovement = v,
                    "Grouping node movement: ", "Whether the grouping node is sticky or not"),
            };

            _advancedSettings = new List<ISettingViewModel>
            {
                new ProxySettingViewModel<uint>(() => MaxHotKeys, v => MaxHotKeys = v,
                    "Max hot keys: ", "The maximum number of generated hot keys"),
                new ProxySettingViewModel<HotKeysDisplayMode>(() => HotKeysDisplayMode, v => HotKeysDisplayMode = v,
                    "Hot keys display mode: ", "Specifies how hotkeys are displayed for a pending connection."),
                new ProxySettingViewModel<double>(() => MouseActionSuppressionThreshold, v => MouseActionSuppressionThreshold = v,
                    "Context menu suppression threshold: ", "Disable context menu after mouse moved this far."),
                new ProxySettingViewModel<bool>(() => PreserveSelectionOnRightClick, v => PreserveSelectionOnRightClick = v,
                    "Preserve selection on right click: ", "Whether right-click on the container should preserve the current selection."),
                new ProxySettingViewModel<double>(() => AutoPanningTickRate, v => AutoPanningTickRate = v,
                    "Auto panning tick rate: ", "How often is the new position calculated in milliseconds. Disable and enable auto panning for this to have effect."),
                new ProxySettingViewModel<bool>(() => EnableSnappingCorrection, v => EnableSnappingCorrection = v,
                    "Enable snapping correction: ", "Correct the final position when moving a selection."),
                new ProxySettingViewModel<bool>(() => EnableCuttingLinePreview, v => EnableCuttingLinePreview = v,
                    "Enable cutting line preview: ", "Applies custom connection style on intersection (hurts performance due to hit testing)."),
                new ProxySettingViewModel<bool>(() => EnablePendingConnectionHitTesting, v => EnablePendingConnectionHitTesting = v,
                    "Enable pending connection hit testing: ", "Disable to improve performance."),
                new ProxySettingViewModel<bool>(() => AllowCuttingCancellation, v => AllowCuttingCancellation = v,
                    "Allow cutting cancellation: ", "Right click to cancel cutting."),
                new ProxySettingViewModel<bool>(() => AllowPushItemsCancellation, v => AllowPushItemsCancellation = v,
                    "Allow push nodes cancellation: ", "Right click to cancel pushing nodes."),
                new ProxySettingViewModel<bool>(() => AllowPanningCancellation, v => AllowPanningCancellation = v,
                    "Allow panning cancellation: ", "Press Escape to cancel panning."),
                new ProxySettingViewModel<bool>(() => AllowSelectionCancellation, v => AllowSelectionCancellation = v,
                    "Allow selection cancellation: ", "Press Escape to cancel selecting."),
                new ProxySettingViewModel<bool>(() => EnableToggledCutting, v => EnableToggledCutting = v,
                    "Enable toggled cutting mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
                new ProxySettingViewModel<bool>(() => EnableToggledPushingItems, v => EnableToggledPushingItems = v,
                    "Enable toggled pushing items mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
                new ProxySettingViewModel<bool>(() => EnableToggledPanning, v => EnableToggledPanning = v,
                    "Enable toggled panning mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
                new ProxySettingViewModel<bool>(() => EnableToggledSelecting, v => EnableToggledSelecting = v,
                    "Enable toggled selecting mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
                new ProxySettingViewModel<bool>(() => EnableToggledDragging, v => EnableToggledDragging = v,
                    "Enable toggled dragging mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
                new ProxySettingViewModel<bool>(() => AllowDraggingCancellation, v => AllowDraggingCancellation = v,
                    "Allow dragging cancellation: ", "Right click to cancel dragging."),
                new ProxySettingViewModel<bool>(() => AllowPendingConnectionCancellation, v => AllowPendingConnectionCancellation = v,
                    "Allow pending connection cancellation: ", "Right click to cancel pending connection."),
                new ProxySettingViewModel<bool>(() => EnableConnectorOptimizations, v => EnableConnectorOptimizations = v,
                    "Enable connector optimizations: ", "Enables optimizations for connectors based on viewport distance and minimum selected nodes."),
                new ProxySettingViewModel<double>(() => OptimizeSafeZone, v => OptimizeSafeZone = v,
                    "Optimize connectors safe zone: ", "The minimum distance from the viewport where connectors will start optimizing"),
                new ProxySettingViewModel<uint>(() => OptimizeMinimumSelectedItems, v => OptimizeMinimumSelectedItems = v,
                    "Optimize connectors minimum selection: ", "The minimum selected items needed to start optimizing when outside the safe zone."),
                new ProxySettingViewModel<bool>(() => EnableRenderingOptimizations, v => EnableRenderingOptimizations = v,
                    "Enable nodes rendering optimization: ", "Enables rendering optimizations for nodes based on zoom out percent and nodes count. (zoom in/out to apply optimization)"),
                new ProxySettingViewModel<double>(() => OptimizeRenderingZoomOutPercent, v => OptimizeRenderingZoomOutPercent = v,
                    "Rendering optimization zoom out percent: ", "The zoom out percent that triggers the optimization. (percent of x = 1 - MinViewportZoom)"),
                new ProxySettingViewModel<uint>(() => OptimizeRenderingMinimumNodes, v => OptimizeRenderingMinimumNodes = v,
                    "Rendering optimization minimum nodes: ", "The minimum nodes needed to start optimizing when zoom out percent is met."),
                new ProxySettingViewModel<bool>(() => EnableDraggingOptimizations, v => EnableDraggingOptimizations = v,
                    "Enable nodes dragging optimizations: ", "Simulates dragging visually but only commits the changes at the end."),
                new ProxySettingViewModel<double>(() => FitToScreenExtentMargin, v => FitToScreenExtentMargin = v,
                    "Fit to screen extent margin: ", "Adds some margin to the nodes extent when fit to screen"),
                new ProxySettingViewModel<bool>(() => AllowPanningWhileSelecting, v => AllowPanningWhileSelecting = v,
                    "Allow panning while selecting: ", "Whether panning is allowed while selecting items in the editor."),
                new ProxySettingViewModel<bool>(() => AllowPanningWhileCutting, v => AllowPanningWhileCutting = v,
                    "Allow panning while cutting: ", "Whether panning is allowed while cutting connections in the editor."),
                new ProxySettingViewModel<bool>(() => AllowPanningWhilePushingItems, v => AllowPanningWhilePushingItems = v,
                    "Allow panning while pushing items: ", "Whether panning is allowed while pushing items items in the editor."),
                new ProxySettingViewModel<bool>(() => AllowZoomingWhileSelecting, v => AllowZoomingWhileSelecting = v,
                    "Allow zooming while selecting: ", "Whether zooming is allowed while selecting items in the editor."),
                new ProxySettingViewModel<bool>(() => AllowZoomingWhileCutting, v => AllowZoomingWhileCutting = v,
                    "Allow zooming while cutting: ", "Whether zooming is allowed while cutting connections in the editor."),
                new ProxySettingViewModel<bool>(() => AllowZoomingWhilePushingItems, v => AllowZoomingWhilePushingItems = v,
                    "Allow zooming while pushing items: ", "Whether zooming is allowed while pushing items connections in the editor."),
                new ProxySettingViewModel<bool>(() => AllowZoomingWhilePanning, v => AllowZoomingWhilePanning = v,
                    "Allow zooming while panning: ", "Whether zooming is allowed while panning connections in the editor."),
                new ProxySettingViewModel<bool>(() => AllowMinimapPanningCancellation, v => AllowMinimapPanningCancellation = v,
                    "Allow minimap panning cancellation: ", "Press Escape to cancel panning the minimap."),
                new ProxySettingViewModel<bool>(() => EnableMinimapToggledPanning, v => EnableMinimapToggledPanning = v,
                    "Enable minimap toggled panning mode: ", "The interaction will be completed in two steps using the same gesture to start and end."),
            };

            EnableCuttingLinePreview = true;
        }

        private void OnSearchTextChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaygroundSettings.SearchText))
            {
                this.RaisePropertyChanged(nameof(Settings));
                this.RaisePropertyChanged(nameof(AdvancedSettings));
            }
        }

        public static EditorSettings Instance { get; } = new EditorSettings();

        #region Default settings

        private ConnectionStyle _connectionStyle = ConnectionStyle.Default;
        public ConnectionStyle ConnectionStyle
        {
            get => _connectionStyle;
            set => this.RaiseAndSetIfChanged(ref _connectionStyle, value);
        }

        private bool _enablePendingConnectionSnapping = true;
        public bool EnablePendingConnectionSnapping
        {
            get => _enablePendingConnectionSnapping;
            set => this.RaiseAndSetIfChanged(ref _enablePendingConnectionSnapping, value);
        }

        private bool _enablePendingConnectionPreview = true;
        public bool EnablePendingConnectionPreview
        {
            get => _enablePendingConnectionPreview;
            set => this.RaiseAndSetIfChanged(ref _enablePendingConnectionPreview, value);
        }

        private bool _allowConnectingToConnectorsOnly;
        public bool AllowConnectingToConnectorsOnly
        {
            get => _allowConnectingToConnectorsOnly;
            set => this.RaiseAndSetIfChanged(ref _allowConnectingToConnectorsOnly, value);
        }

        private bool _realtimeSelection = true;
        public bool EnableRealtimeSelection
        {
            get => _realtimeSelection;
            set => this.RaiseAndSetIfChanged(ref _realtimeSelection, value);
        }

        private bool _disableAutoPanning = false;
        public bool DisableAutoPanning
        {
            get => _disableAutoPanning;
            set => this.RaiseAndSetIfChanged(ref _disableAutoPanning, value);
        }

        private double _autoPanningSpeed = 15d;
        public double AutoPanningSpeed
        {
            get => _autoPanningSpeed;
            set => this.RaiseAndSetIfChanged(ref _autoPanningSpeed, value);
        }

        private double _autoPanningEdgeDistance = 15d;
        public double AutoPanningEdgeDistance
        {
            get => _autoPanningEdgeDistance;
            set => this.RaiseAndSetIfChanged(ref _autoPanningEdgeDistance, value);
        }

        private bool _disablePanning = false;
        public bool DisablePanning
        {
            get => _disablePanning;
            set => this.RaiseAndSetIfChanged(ref _disablePanning, value);
        }

        private bool _disableZooming = false;
        public bool DisableZooming
        {
            get => _disableZooming;
            set => this.RaiseAndSetIfChanged(ref _disableZooming, value);
        }

        private uint _gridSpacing = 15u;
        public uint GridSpacing
        {
            get => _gridSpacing;
            set => this.RaiseAndSetIfChanged(ref _gridSpacing, value);
        }

        private double _minZoom = 0.1;
        public double MinZoom
        {
            get => _minZoom;
            set => this.RaiseAndSetIfChanged(ref _minZoom, value);
        }

        private double _maxZoom = 2;
        public double MaxZoom
        {
            get => _maxZoom;
            set => this.RaiseAndSetIfChanged(ref _maxZoom, value);
        }

        private double _zoom = 1;
        public double Zoom
        {
            get => _zoom;
            set => this.RaiseAndSetIfChanged(ref _zoom, value);
        }

        private PointEditor _location = new PointEditor();
        public PointEditor Location
        {
            get => _location;
            set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        private bool _selectableNodes = true;
        public bool SelectableNodes
        {
            get => _selectableNodes;
            set => this.RaiseAndSetIfChanged(ref _selectableNodes, value);
        }

        private bool _draggableNodes = true;
        public bool DraggableNodes
        {
            get => _draggableNodes;
            set => this.RaiseAndSetIfChanged(ref _draggableNodes, value);
        }

        private bool _selectableConnections = true;
        public bool SelectableConnections
        {
            get => _selectableConnections;
            set => this.RaiseAndSetIfChanged(ref _selectableConnections, value);
        }

        private bool _canSelectMultipleConnections = true;
        public bool CanSelectMultipleConnections
        {
            get => _canSelectMultipleConnections;
            set => this.RaiseAndSetIfChanged(ref _canSelectMultipleConnections, value);
        }

        private bool _canSelectMultipleNodes = true;
        public bool CanSelectMultipleNodes
        {
            get => _canSelectMultipleNodes;
            set => this.RaiseAndSetIfChanged(ref _canSelectMultipleNodes, value);
        }

        private double _circuitConnectionAngle = 45;
        public double CircuitConnectionAngle
        {
            get => _circuitConnectionAngle;
            set => this.RaiseAndSetIfChanged(ref _circuitConnectionAngle, value);
        }

        private double _connectionCornerRadius = 10;
        public double ConnectionCornerRadius
        {
            get => _connectionCornerRadius;
            set => this.RaiseAndSetIfChanged(ref _connectionCornerRadius, value);
        }

        private double _connectionSpacing = 20;
        public double ConnectionSpacing
        {
            get => _connectionSpacing;
            set => this.RaiseAndSetIfChanged(ref _connectionSpacing, value);
        }

        private ConnectionOffsetMode _srcConnectionOffsetMode = ConnectionOffsetMode.Static;
        public ConnectionOffsetMode ConnectionSourceOffsetMode
        {
            get => _srcConnectionOffsetMode;
            set => this.RaiseAndSetIfChanged(ref _srcConnectionOffsetMode, value);
        }

        private ConnectionOffsetMode _targetConnectionOffsetMode = ConnectionOffsetMode.Static;
        public ConnectionOffsetMode ConnectionTargetOffsetMode
        {
            get => _targetConnectionOffsetMode;
            set => this.RaiseAndSetIfChanged(ref _targetConnectionOffsetMode, value);
        }

        private ArrowHeadEnds _arrowHeadEnds = ArrowHeadEnds.End;
        public ArrowHeadEnds ArrowHeadEnds
        {
            get => _arrowHeadEnds;
            set => this.RaiseAndSetIfChanged(ref _arrowHeadEnds, value);
        }

        private ArrowHeadShape _arrowHeadShape = ArrowHeadShape.Arrowhead;
        public ArrowHeadShape ArrowHeadShape
        {
            get => _arrowHeadShape;
            set => this.RaiseAndSetIfChanged(ref _arrowHeadShape, value);
        }

        private PointEditor _connectionSourceOffset = new PointEditor { X = 14, Y = 0 };
        public PointEditor ConnectionSourceOffset
        {
            get => _connectionSourceOffset;
            set => this.RaiseAndSetIfChanged(ref _connectionSourceOffset, value);
        }

        private PointEditor _connectionTargetOffset = new PointEditor { X = 14, Y = 0 };
        public PointEditor ConnectionTargetOffset
        {
            get => _connectionTargetOffset;
            set => this.RaiseAndSetIfChanged(ref _connectionTargetOffset, value);
        }

        private PointEditor _connectionArrowSize = new PointEditor { X = 8, Y = 8 };
        public PointEditor ConnectionArrowSize
        {
            get => _connectionArrowSize;
            set => this.RaiseAndSetIfChanged(ref _connectionArrowSize, value);
        }

        private double _connectionStrokeThickness = 3;
        public double ConnectionStrokeThickness
        {
            get => _connectionStrokeThickness;
            set => this.RaiseAndSetIfChanged(ref _connectionStrokeThickness, value);
        }

        private double _connectionOutlineThickness = 5;
        public double ConnectionOutlineThickness
        {
            get => _connectionOutlineThickness;
            set => this.RaiseAndSetIfChanged(ref _connectionOutlineThickness, value);
        }

        private bool _displayConnectionsOnTop;
        public bool DisplayConnectionsOnTop
        {
            get => _displayConnectionsOnTop;
            set => this.RaiseAndSetIfChanged(ref _displayConnectionsOnTop, value);
        }

        private double _bringIntoViewSpeed = 1000;
        public double BringIntoViewSpeed
        {
            get => _bringIntoViewSpeed;
            set => this.RaiseAndSetIfChanged(ref _bringIntoViewSpeed, value);
        }

        private double _bringIntoViewMaxDuration = 1;
        public double BringIntoViewMaxDuration
        {
            get => _bringIntoViewMaxDuration;
            set => this.RaiseAndSetIfChanged(ref _bringIntoViewMaxDuration, value);
        }

        private GroupingMovementMode _groupingNodeMovement;
        public GroupingMovementMode GroupingNodeMovement
        {
            get => _groupingNodeMovement;
            set => this.RaiseAndSetIfChanged(ref _groupingNodeMovement, value);
        }

        private uint _directionalArrowsCount = 3u;
        public uint DirectionalArrowsCount
        {
            get => _directionalArrowsCount;
            set => this.RaiseAndSetIfChanged(ref _directionalArrowsCount, value);
        }

        private double _directionalArrowsOffset;
        public double DirectionalArrowsOffset
        {
            get => _directionalArrowsOffset;
            set => this.RaiseAndSetIfChanged(ref _directionalArrowsOffset, value);
        }

        private bool _isAnimatingConnections;
        public bool IsAnimatingConnections
        {
            get => _isAnimatingConnections;
            set => this.RaiseAndSetIfChanged(ref _isAnimatingConnections, value);
        }

        private double _directionalArrowsAnimationDuration = 2.0;
        public double DirectionalArrowsAnimationDuration
        {
            get => _directionalArrowsAnimationDuration;
            set => this.RaiseAndSetIfChanged(ref _directionalArrowsAnimationDuration, value);
        }

        #endregion

        #region Feature demo settings

        // Text label drawn on every connection (empty = no label).
        private string _connectionText = string.Empty;
        public string ConnectionText
        {
            get => _connectionText;
            set => this.RaiseAndSetIfChanged(ref _connectionText, value);
        }

        #endregion

        #region Advanced settings

        public uint MaxHotKeys
        {
            get => PendingConnection.MaxHotKeys;
            set => PendingConnection.MaxHotKeys = value;
        }

        public HotKeysDisplayMode HotKeysDisplayMode
        {
            get => PendingConnection.HotKeysDisplayMode;
            set => PendingConnection.HotKeysDisplayMode = value;
        }

        public bool PreserveSelectionOnRightClick
        {
            get => ItemContainer.PreserveSelectionOnRightClick;
            set => ItemContainer.PreserveSelectionOnRightClick = value;
        }

        public double AutoPanningTickRate
        {
            get => NodifyEditor.AutoPanningTickRate;
            set => NodifyEditor.AutoPanningTickRate = value;
        }

        public bool AllowCuttingCancellation
        {
            get => NodifyEditor.AllowCuttingCancellation;
            set => NodifyEditor.AllowCuttingCancellation = value;
        }

        public bool AllowPushItemsCancellation
        {
            get => NodifyEditor.AllowPushItemsCancellation;
            set => NodifyEditor.AllowPushItemsCancellation = value;
        }

        public bool AllowDraggingCancellation
        {
            get => NodifyEditor.AllowDraggingCancellation;
            set => NodifyEditor.AllowDraggingCancellation = value;
        }

        public bool AllowPendingConnectionCancellation
        {
            get => Connector.AllowPendingConnectionCancellation;
            set => Connector.AllowPendingConnectionCancellation = value;
        }

        public bool EnableSnappingCorrection
        {
            get => NodifyEditor.EnableSnappingCorrection;
            set => NodifyEditor.EnableSnappingCorrection = value;
        }

        public bool EnableCuttingLinePreview
        {
            get => NodifyEditor.EnableCuttingLinePreview;
            set => NodifyEditor.EnableCuttingLinePreview = value;
        }

        public bool EnableToggledCutting
        {
            get => NodifyEditor.EnableToggledCuttingMode;
            set => NodifyEditor.EnableToggledCuttingMode = value;
        }

        public bool EnableToggledPushingItems
        {
            get => NodifyEditor.EnableToggledPushingItemsMode;
            set => NodifyEditor.EnableToggledPushingItemsMode = value;
        }

        public bool EnableConnectorOptimizations
        {
            get => Connector.EnableOptimizations;
            set => Connector.EnableOptimizations = value;
        }

        public double OptimizeSafeZone
        {
            get => Connector.OptimizeSafeZone;
            set => Connector.OptimizeSafeZone = value;
        }

        public uint OptimizeMinimumSelectedItems
        {
            get => Connector.OptimizeMinimumSelectedItems;
            set => Connector.OptimizeMinimumSelectedItems = value;
        }

        public bool EnableRenderingOptimizations
        {
            get => NodifyEditor.EnableRenderingContainersOptimizations;
            set => NodifyEditor.EnableRenderingContainersOptimizations = value;
        }

        public uint OptimizeRenderingMinimumNodes
        {
            get => NodifyEditor.OptimizeRenderingMinimumContainers;
            set => NodifyEditor.OptimizeRenderingMinimumContainers = value;
        }

        public double OptimizeRenderingZoomOutPercent
        {
            get => NodifyEditor.OptimizeRenderingZoomOutPercent;
            set => NodifyEditor.OptimizeRenderingZoomOutPercent = value;
        }

        public double FitToScreenExtentMargin
        {
            get => NodifyEditor.FitToScreenExtentMargin;
            set => NodifyEditor.FitToScreenExtentMargin = value;
        }

        public bool EnableDraggingOptimizations
        {
            get => NodifyEditor.EnableDraggingContainersOptimizations;
            set => NodifyEditor.EnableDraggingContainersOptimizations = value;
        }

        public bool AllowPanningCancellation
        {
            get => NodifyEditor.AllowPanningCancellation;
            set => NodifyEditor.AllowPanningCancellation = value;
        }

        public bool AllowSelectionCancellation
        {
            get => NodifyEditor.AllowSelectionCancellation;
            set => NodifyEditor.AllowSelectionCancellation = value;
        }

        public bool AllowPanningWhileSelecting
        {
            get => NodifyEditor.AllowPanningWhileSelecting;
            set => NodifyEditor.AllowPanningWhileSelecting = value;
        }

        public bool AllowPanningWhileCutting
        {
            get => NodifyEditor.AllowPanningWhileCutting;
            set => NodifyEditor.AllowPanningWhileCutting = value;
        }

        public bool AllowPanningWhilePushingItems
        {
            get => NodifyEditor.AllowPanningWhilePushingItems;
            set => NodifyEditor.AllowPanningWhilePushingItems = value;
        }

        public bool AllowZoomingWhileSelecting
        {
            get => NodifyEditor.AllowZoomingWhileSelecting;
            set => NodifyEditor.AllowZoomingWhileSelecting = value;
        }

        public bool AllowZoomingWhileCutting
        {
            get => NodifyEditor.AllowZoomingWhileCutting;
            set => NodifyEditor.AllowZoomingWhileCutting = value;
        }

        public bool AllowZoomingWhilePushingItems
        {
            get => NodifyEditor.AllowZoomingWhilePushingItems;
            set => NodifyEditor.AllowZoomingWhilePushingItems = value;
        }

        public bool AllowZoomingWhilePanning
        {
            get => NodifyEditor.AllowZoomingWhilePanning;
            set => NodifyEditor.AllowZoomingWhilePanning = value;
        }

        public bool EnableToggledPanning
        {
            get => NodifyEditor.EnableToggledPanningMode;
            set => NodifyEditor.EnableToggledPanningMode = value;
        }

        public bool EnableToggledSelecting
        {
            get => NodifyEditor.EnableToggledSelectingMode;
            set => NodifyEditor.EnableToggledSelectingMode = value;
        }

        public bool EnableToggledDragging
        {
            get => ContainerState.EnableToggledDraggingMode;
            set => ContainerState.EnableToggledDraggingMode = value;
        }

        public bool EnablePendingConnectionHitTesting
        {
            get => PendingConnection.EnableHitTesting;
            set => PendingConnection.EnableHitTesting = value;
        }

        public bool EnableStickyConnectors
        {
            get => Connector.EnableStickyConnections;
            set => Connector.EnableStickyConnections = value;
        }

        // Bound per-connection via the ConnectionStyleClass style onto BaseConnection.FocusVisualPadding,
        // which is an instance property, not static.
        private double _connectionFocusVisualPadding = 1d;
        public double ConnectionFocusVisualPadding
        {
            get => _connectionFocusVisualPadding;
            set => this.RaiseAndSetIfChanged(ref _connectionFocusVisualPadding, value);
        }

        public double MouseActionSuppressionThreshold
        {
            get => NodifyEditor.MouseActionSuppressionThreshold;
            set => NodifyEditor.MouseActionSuppressionThreshold = value;
        }

        public bool AllowMinimapPanningCancellation
        {
            get => Minimap.AllowPanningCancellation;
            set => Minimap.AllowPanningCancellation = value;
        }

        public bool EnableMinimapToggledPanning
        {
            get => Minimap.EnableToggledPanningMode;
            set => Minimap.EnableToggledPanningMode = value;
        }

        #endregion
    }
}
