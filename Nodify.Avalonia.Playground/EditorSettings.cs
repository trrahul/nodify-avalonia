using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Nodes;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    public enum ConnectionStyle
    {
        Default,
        Line,
        Circuit
    }

    public class EditorSettings : ReactiveObject
    {
        private EditorSettings() { }

        public static EditorSettings Instance { get; } = new EditorSettings();

        #region Default settings

        private ConnectionStyle _connectionStyle;
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

        private double _circuitConnectionAngle = 45;
        public double CircuitConnectionAngle
        {
            get => _circuitConnectionAngle;
            set => this.RaiseAndSetIfChanged(ref _circuitConnectionAngle, value);
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

        #endregion

        #region Advanced settings

        public double HandleRightClickAfterPanningThreshold
        {
            get => NodifyEditor.HandleRightClickAfterPanningThreshold;
            set => NodifyEditor.HandleRightClickAfterPanningThreshold = value;
        }

        public double AutoPanningTickRate
        {
            get => NodifyEditor.AutoPanningTickRate;
            set => NodifyEditor.AutoPanningTickRate = value;
        }

        public bool AllowDraggingCancellation
        {
            get => ItemContainer.AllowDraggingCancellation;
            set => ItemContainer.AllowDraggingCancellation = value;
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

        public bool EnableStickyConnectors
        {
            get => Connector.EnableStickyConnections;
            set => Connector.EnableStickyConnections = value;
        }

        #endregion
    }
}
