using System.Linq;
using System.Windows.Input;
using Avalonia;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public class NodifyEditorViewModel : ReactiveObject
    {
        public NodifyEditorViewModel()
        {
            Schema = new GraphSchema();
            
            PendingConnection = new PendingConnectionViewModel
            {
                Graph = this
            };
            DeleteSelectionCommand = ReactiveCommand.Create(DeleteSelection,
                this.WhenAnyValue(
                    v => v.SelectedNodes.Count,
                    v => v.SelectedConnections.Count,
                    (nodes, conns) => nodes > 0 || conns > 0));
            CommentSelectionCommand = ReactiveCommand.Create(() => Schema.AddCommentAroundNodes(SelectedNodes, "New comment"), this.WhenAnyValue((v) => v.SelectedNodes.Count ,(p) => p > 0));
            DisconnectConnectorCommand = ReactiveCommand.Create<ConnectorViewModel>(c => c.Disconnect());
            // Uses DelegateCommand<T> (not ReactiveCommand) because PendingConnection.CompletedCommand
            // calls ICommand.CanExecute(target) with the actual drop target for every candidate before
            // executing (see PendingConnection.cs) — ReactiveCommand's CanExecute ignores the parameter.
            CreateConnectionCommand = new DelegateCommand<object>(
                target => Schema.TryAddConnection(PendingConnection.Source!, target),
                target => PendingConnection.Source != null && target != null && Schema.CanAddConnection(PendingConnection.Source, target));

            Connections.WhenAdded(c =>
            {
                c.Graph = this;
                c.Input.Connections.Add(c);
                c.Output.Connections.Add(c);
            })
            .WhenRemoved(c =>
            {
                c.Input.Connections.Remove(c);
                c.Output.Connections.Remove(c);
            });

            Nodes.WhenAdded(x => x.Graph = this)
                 // Not called when the collection is cleared
                 .WhenRemoved(x =>
                 {
                     if (x is FlowNodeViewModel flow)
                     {
                         flow.Disconnect();
                     }
                     else if (x is KnotNodeViewModel knot)
                     {
                         knot.Connector.Disconnect();
                     }
                 })
                 .WhenCleared(x => Connections.Clear());
        }

        private NodifyObservableCollection<NodeViewModel> _nodes = new NodifyObservableCollection<NodeViewModel>();
        public NodifyObservableCollection<NodeViewModel> Nodes
        {
            get => _nodes;
            set => this.RaiseAndSetIfChanged(ref _nodes, value);
        }

        private NodifyObservableCollection<NodeViewModel> _selectedNodes = new NodifyObservableCollection<NodeViewModel>();
        public NodifyObservableCollection<NodeViewModel> SelectedNodes
        {
            get => _selectedNodes;
            set => this.RaiseAndSetIfChanged(ref _selectedNodes, value);
        }

        private NodifyObservableCollection<ConnectionViewModel> _connections = new NodifyObservableCollection<ConnectionViewModel>();
        public NodifyObservableCollection<ConnectionViewModel> Connections
        {
            get => _connections;
            set => this.RaiseAndSetIfChanged(ref _connections, value);
        }

        private NodifyObservableCollection<ConnectionViewModel> _selectedConnections = new NodifyObservableCollection<ConnectionViewModel>();
        public NodifyObservableCollection<ConnectionViewModel> SelectedConnections
        {
            get => _selectedConnections;
            set => this.RaiseAndSetIfChanged(ref _selectedConnections, value);
        }

        private ConnectionViewModel? _selectedConnection;
        public ConnectionViewModel? SelectedConnection
        {
            get => _selectedConnection;
            set => this.RaiseAndSetIfChanged(ref _selectedConnection, value);
        }

        private NodeViewModel? _selectedNode;
        public NodeViewModel? SelectedNode
        {
            get => _selectedNode;
            set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
        }

        private Size _viewportSize;
        public Size ViewportSize
        {
            get => _viewportSize;
            set => this.RaiseAndSetIfChanged(ref _viewportSize, value);
        }

        private string? _keyboardNavigationLayer;
        /// <summary>
        /// Name of the currently active keyboard-navigation layer (Nodes/Connections/Decorators/Custom),
        /// or null when keyboard navigation is inactive. Fed by <see cref="NodifyEditor.ActiveNavigationLayerChanged"/>
        /// (see NodifyEditorView.axaml.cs) and displayed as a HUD badge in MainWindow.
        /// </summary>
        public string? KeyboardNavigationLayer
        {
            get => _keyboardNavigationLayer;
            set => this.RaiseAndSetIfChanged(ref _keyboardNavigationLayer, value);
        }

        public PendingConnectionViewModel PendingConnection { get; }
        public GraphSchema Schema { get; }

        public ICommand DeleteSelectionCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand CreateConnectionCommand { get; }
        public ICommand CommentSelectionCommand { get; }

        private void DeleteSelection()
        {
            foreach (var connection in SelectedConnections.ToList())
            {
                connection.Remove();
            }

            var selected = SelectedNodes.ToList();

            for (int i = 0; i < selected.Count; i++)
            {
                Nodes.Remove(selected[i]);
            }
        }
    }
}
