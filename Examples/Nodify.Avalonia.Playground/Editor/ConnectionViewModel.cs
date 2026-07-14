using System.Reactive;
using System.Windows.Input;
using Avalonia;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public class ConnectionViewModel : ReactiveObject
    {
        private NodifyEditorViewModel _graph = default!;
        public NodifyEditorViewModel Graph
        {
            get => _graph;
            internal set => this.RaiseAndSetIfChanged(ref _graph, value);
        }

        private ConnectorViewModel _input = default!;
        public ConnectorViewModel Input
        {
            get => _input;
            set => this.RaiseAndSetIfChanged(ref _input, value);
        }

        private ConnectorViewModel _output = default!;

        public ConnectorViewModel Output
        {
            get => _output;
            set => this.RaiseAndSetIfChanged(ref _output, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public void Split(Point point)
            => Graph.Schema.SplitConnection(this, point);

        public void Remove()
            => Graph.Connections.Remove(this);

        public ICommand SplitCommand { get; }
        public ICommand DisconnectCommand { get; }

        public ConnectionViewModel()
        {
            SplitCommand = ReactiveCommand.Create<Point>(Split);
            DisconnectCommand = ReactiveCommand.Create(Remove);
        }
    }
}
