using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.EditorStates;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Interactivity;
using Nodify.Avalonia.Playground.Diagnostics;

namespace Nodify.Avalonia.Playground.Editor
{
    public partial class NodifyEditorView : UserControl
    {
        public NodifyEditor EditorInstance => Editor;

        private NodifyEditorViewModel? _observedViewModel;
        private PendingConnectionViewModel? _observedPendingConnection;

        static NodifyEditorView()
        {
            // Plug the connection-retargeting interaction into the library's shared Connector input pipeline.
            InputProcessor.Shared<Connector>.ReplaceHandlerFactory<ConnectorState.Connecting>(elem => new CustomConnecting(elem));
            InputProcessor.Shared<Connector>.RegisterHandlerFactory(elem => new RetargetConnections(elem));
        }

        public NodifyEditorView()
        {
            InitializeComponent();

            // Drive the connection template from the selected style. The settings singleton outlives
            // any view, so the subscription lives in attach/detach (not the ctor) to avoid rooting views.
            ApplyConnectionTemplate(EditorSettings.Instance.ConnectionStyle);

            // Diagnostics wiring is a complete no-op unless NODIFY_DIAG_LOG is set (see DiagLog).
            Loaded += OnDiagLoaded;
            Unloaded += OnDiagUnloaded;
            DataContextChanged += OnDiagDataContextChanged;

            Editor.AddHandler(ItemContainer.DragStartedEvent, OnEditorItemsDragStarted, RoutingStrategies.Bubble);
            Editor.AddHandler(ItemContainer.DragCompletedEvent, OnEditorItemsDragCompleted, RoutingStrategies.Bubble);

            // Connection hover spotlight: fades the shared placeholder to 0.3 (every connection binds its
            // base Opacity to it); the hovered one is pinned at 1 via :pointerover. Tracked from the
            // bubbling pointer-move stream because PointerEntered/Exited are direct events.
            AddHandler(PointerMovedEvent, OnSpotlightPointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            PointerExited += (_, _) => SetConnectionSpotlight(null);

            EditorInstance.ActiveNavigationLayerChanged += DisplayActiveNavigationLayer;
        }

        private BaseConnection? _spotlightConnection;

        private void OnSpotlightPointerMoved(object? sender, PointerEventArgs e)
            => SetConnectionSpotlight(FindConnection(e.Source));

        private static BaseConnection? FindConnection(object? source)
        {
            for (var visual = source as Visual; visual != null; visual = visual.GetVisualParent())
            {
                if (visual is BaseConnection connection)
                    return connection;
            }

            return null;
        }

        private void SetConnectionSpotlight(BaseConnection? connection)
        {
            if (ReferenceEquals(_spotlightConnection, connection))
            {
                return;
            }

            _spotlightConnection = connection;

            if (Resources.TryGetResource("ConnectionAnimationPlaceholder", null, out var resource)
                && resource is Control placeholder)
            {
                if (connection is null)
                {
                    // Temporarily detach the enter transition so the opacity reset snaps
                    // back to full instead of animating.
                    Transitions? transitions = placeholder.Transitions;
                    placeholder.Transitions = null;
                    placeholder.Opacity = 1.0;
                    placeholder.Transitions = transitions;
                }
                else
                {
                    placeholder.Opacity = 0.3;
                }
            }
        }

        private void DisplayActiveNavigationLayer(KeyboardNavigationLayerId layerId)
        {
            if (DataContext is not NodifyEditorViewModel editorVm)
            {
                return;
            }

            if (layerId == KeyboardNavigationLayerId.Nodes)
            {
                editorVm.KeyboardNavigationLayer = "Nodes";
            }
            else if (layerId == KeyboardNavigationLayerId.Connections)
            {
                editorVm.KeyboardNavigationLayer = "Connections";
            }
            else if (layerId == KeyboardNavigationLayerId.Decorators)
            {
                editorVm.KeyboardNavigationLayer = "Decorators";
            }
            else
            {
                editorVm.KeyboardNavigationLayer = "Custom";
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            EditorSettings.Instance.PropertyChanged += OnEditorSettingsChanged;
            // Catch a style change that happened while this view was detached.
            ApplyConnectionTemplate(EditorSettings.Instance.ConnectionStyle);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            SetConnectionSpotlight(null);
            base.OnDetachedFromVisualTree(e);
            EditorSettings.Instance.PropertyChanged -= OnEditorSettingsChanged;
        }

        private void OnEditorSettingsChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorSettings.ConnectionStyle))
            {
                ApplyConnectionTemplate(EditorSettings.Instance.ConnectionStyle);
            }
        }

        private void Minimap_Zoom(object sender, ZoomEventArgs e)
        {
            Editor.ZoomAtPosition(e.Zoom, e.Location);
        }

        #region Diagnostics (NODIFY_DIAG_LOG opt-in instrumentation)

        private void OnDiagLoaded(object? sender, RoutedEventArgs e)
        {
            DiagLog.Write("Lifecycle", "NodifyEditorView loaded");
            AttachViewModel(DataContext as NodifyEditorViewModel);
        }

        private void OnDiagUnloaded(object? sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void OnDiagDataContextChanged(object? sender, EventArgs e)
        {
            AttachViewModel(DataContext as NodifyEditorViewModel);
        }

        private void AttachViewModel(NodifyEditorViewModel? viewModel)
        {
            if (ReferenceEquals(_observedViewModel, viewModel))
            {
                return;
            }

            DetachViewModel();

            _observedViewModel = viewModel;

            if (viewModel == null)
            {
                return;
            }

            foreach (NodeViewModel node in viewModel.Nodes)
            {
                SubscribeNode(node);
            }

            viewModel.Nodes.CollectionChanged += OnNodesCollectionChanged;
            viewModel.Connections.CollectionChanged += OnConnectionsCollectionChanged;

            AttachPendingConnection(viewModel.PendingConnection);
        }

        private void DetachViewModel()
        {
            if (_observedViewModel == null)
            {
                return;
            }

            foreach (NodeViewModel node in _observedViewModel.Nodes)
            {
                UnsubscribeNode(node);
            }

            _observedViewModel.Nodes.CollectionChanged -= OnNodesCollectionChanged;
            _observedViewModel.Connections.CollectionChanged -= OnConnectionsCollectionChanged;

            DetachPendingConnection();

            _observedViewModel = null;
        }

        private void AttachPendingConnection(PendingConnectionViewModel? pending)
        {
            DetachPendingConnection();

            _observedPendingConnection = pending;

            if (pending != null)
            {
                pending.PropertyChanged += OnPendingConnectionPropertyChanged;
            }
        }

        private void DetachPendingConnection()
        {
            if (_observedPendingConnection != null)
            {
                _observedPendingConnection.PropertyChanged -= OnPendingConnectionPropertyChanged;
                _observedPendingConnection = null;
            }
        }

        private void OnPendingConnectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var pending = (PendingConnectionViewModel?)sender;
            if (pending == null)
            {
                return;
            }

            if (e.PropertyName == nameof(PendingConnectionViewModel.Source))
            {
                DiagLog.Write("PendingConnection",
                    pending.Source != null
                        ? $"Started from '{DescribeConnector(pending.Source)}'"
                        : "Cleared (no source)");
            }
            else if (e.PropertyName == nameof(PendingConnectionViewModel.PreviewTarget))
            {
                DiagLog.Write("PendingConnection",
                    $"Preview target -> {DescribeTarget(pending.PreviewTarget)}");
            }
        }

        private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (NodeViewModel node in e.OldItems)
                {
                    UnsubscribeNode(node);
                    DiagLog.Write("Node", $"Removed '{DescribeNode(node)}'");
                }
            }

            if (e.NewItems != null)
            {
                foreach (NodeViewModel node in e.NewItems)
                {
                    SubscribeNode(node);
                    DiagLog.Write("Node", $"Added '{DescribeNode(node)}' at {node.Location}");
                }
            }
        }

        private void OnConnectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_spotlightConnection != null
                && (e.Action == NotifyCollectionChangedAction.Reset
                    || e.OldItems?.Cast<object?>().Contains(_spotlightConnection.DataContext) == true))
            {
                SetConnectionSpotlight(null);
            }

            if (e.OldItems != null)
            {
                foreach (ConnectionViewModel connection in e.OldItems)
                {
                    DiagLog.Write("Connection", $"Removed {DescribeConnection(connection)}");
                }
            }

            if (e.NewItems != null)
            {
                foreach (ConnectionViewModel connection in e.NewItems)
                {
                    DiagLog.Write("Connection", $"Created {DescribeConnection(connection)}");
                }
            }
        }

        private readonly Dictionary<NodeViewModel, Point> _lastKnownLocations = new();

        private void SubscribeNode(NodeViewModel node)
        {
            _lastKnownLocations[node] = node.Location;
            node.PropertyChanged += OnNodePropertyChanged;
        }

        private void UnsubscribeNode(NodeViewModel node)
        {
            node.PropertyChanged -= OnNodePropertyChanged;
            _lastKnownLocations.Remove(node);
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(NodeViewModel.Location) || sender is not NodeViewModel node)
            {
                return;
            }

            _lastKnownLocations.TryGetValue(node, out Point oldLocation);
            Point newLocation = node.Location;
            _lastKnownLocations[node] = newLocation;

            DiagLog.Write("LocationChanged", $"'{DescribeNode(node)}' {oldLocation} -> {newLocation}");
        }

        private void OnEditorItemsDragStarted(object? sender, VectorEventArgs e)
        {
            DiagLog.Write("Drag", $"ItemsDragStarted vector={e.Vector}");
        }

        private void OnEditorItemsDragCompleted(object? sender, VectorEventArgs e)
        {
            string canceled = e is DragCompletedEventArgs completed && completed.Canceled ? " (canceled)" : "";
            DiagLog.Write("Drag", $"ItemsDragCompleted vector={e.Vector}{canceled}");
        }

        private static string DescribeNode(NodeViewModel node) => node switch
        {
            FlowNodeViewModel flow => flow.Title ?? flow.GetType().Name,
            CommentNodeViewModel comment => comment.Title ?? comment.GetType().Name,
            _ => node.GetType().Name
        };

        private static string DescribeConnector(ConnectorViewModel connector)
            => connector.Title ?? (connector.Node != null ? DescribeNode(connector.Node) : "connector");

        private static string DescribeTarget(object? target) => target switch
        {
            null => "(none)",
            ConnectorViewModel connector => DescribeConnector(connector),
            NodeViewModel node => DescribeNode(node),
            _ => target.ToString() ?? target.GetType().Name
        };

        private static string DescribeConnection(ConnectionViewModel connection)
            => $"{DescribeConnector(connection.Output)} -> {DescribeConnector(connection.Input)}";

        #endregion

        private void ApplyConnectionTemplate(ConnectionStyle style)
        {
            SetConnectionSpotlight(null);

            string key = style switch
            {
                ConnectionStyle.Line => "LineConnectionTemplate",
                ConnectionStyle.Circuit => "CircuitConnectionTemplate",
                ConnectionStyle.Step => "StepConnectionTemplate",
                _ => "ConnectionTemplate",
            };

            if (this.TryFindResource(key, out var resource) && resource is DataTemplate template)
            {
                Editor.ConnectionTemplate = template;
            }
        }
    }
}
