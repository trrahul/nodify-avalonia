using System.Linq;
using Avalonia;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public enum ConnectorFlow
    {
        Input,
        Output
    }

    public enum ConnectorShape
    {
        Circle,
        Triangle,
        Square,
    }

    public class ConnectorViewModel : ReactiveObject
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set => this.RaiseAndSetIfChanged(ref _anchor, value);
        }

        private NodeViewModel _node = default!;
        public NodeViewModel Node
        {
            get => _node;
            internal set
            {
                if (!ReferenceEquals(_node, value))
                {
                    this.RaiseAndSetIfChanged(ref _node, value);
                    OnNodeChanged();
                }
            }
        }

        private ConnectorShape _shape;
        public ConnectorShape Shape
        {
            get => _shape;
            set => this.RaiseAndSetIfChanged(ref _shape, value);
        }

        public ConnectorFlow Flow { get; private set; }

        public bool IsFlow { get; private set; }

        public int MaxConnections { get; set; } = 2;

        public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();

        public ConnectorViewModel()
        {
            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;
                c.IsFlow = IsFlow;
            }).WhenRemoved(c =>
            {
                if (c.Input.Connections.Count == 0)
                {
                    c.Input.IsConnected = false;
                }

                if (c.Output.Connections.Count == 0)
                {
                    c.Output.IsConnected = false;
                }
            });
        }

        protected virtual void OnNodeChanged()
        {
            if (Node is FlowNodeViewModel flow)
            {
                if (flow.Input.Contains(this))
                {
                    Flow = ConnectorFlow.Input;
                    IsFlow = false;
                }
                else if (flow.Output.Contains(this))
                {
                    Flow = ConnectorFlow.Output;
                    IsFlow = false;
                }
                else if (flow.FlowInput.Contains(this))
                {
                    Flow = ConnectorFlow.Input;
                    IsFlow = true;
                }
                else if (flow.FlowOutput.Contains(this))
                {
                    Flow = ConnectorFlow.Output;
                    IsFlow = true;
                }
            }
            else if (Node is KnotNodeViewModel knot)
            {
                Flow = knot.Flow;
                IsFlow = knot.IsFlow;
            }
        }

        public bool IsConnectedTo(ConnectorViewModel con)
            => Connections.Any(c => c.Input == con || c.Output == con);

        public virtual bool AllowsNewConnections()
            => Connections.Count < MaxConnections;

        public void Disconnect()
            => Node.Graph.Schema.DisconnectConnector(this);
    }
}
