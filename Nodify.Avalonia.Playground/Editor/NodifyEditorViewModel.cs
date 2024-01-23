using System.Linq;
using System.Windows.Input;
using Avalonia;
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
            DeleteSelectionCommand = ReactiveCommand.Create(DeleteSelection,  this.WhenAnyValue(v => v.SelectedNodes.Count,(p1)=>p1 > 0));
            CommentSelectionCommand = ReactiveCommand.Create(() => Schema.AddCommentAroundNodes(SelectedNodes, "New comment"), this.WhenAnyValue((v) => v.SelectedNodes.Count ,(p) => p > 0));
            DisconnectConnectorCommand = ReactiveCommand.Create<ConnectorViewModel>(c => c.Disconnect());
            CreateConnectionCommand = ReactiveCommand.Create<object>(target => Schema.TryAddConnection(PendingConnection.Source!, target)); //todo,target => PendingConnection.Source != null && target != null

            Connections.WhenAdded(c =>
            {
                c.Graph = this;
                c.Input.Connections.Add(c);
                c.Output.Connections.Add(c);
            })
            // Called when the collection is cleared
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

        private Size _viewportSize;
        public Size ViewportSize
        {
            get => _viewportSize;
            set => this.RaiseAndSetIfChanged(ref _viewportSize, value);
        }

        public PendingConnectionViewModel PendingConnection { get; }
        public GraphSchema Schema { get; }

        public ICommand DeleteSelectionCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand CreateConnectionCommand { get; }
        public ICommand CommentSelectionCommand { get; }

        private void DeleteSelection()
        {
            var selected = SelectedNodes.ToList();

            for (int i = 0; i < selected.Count; i++)
            {
                Nodes.Remove(selected[i]);
            }
        }
    }
}
