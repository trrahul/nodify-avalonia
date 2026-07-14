using System.Windows.Input;
using Avalonia;
using global::Avalonia.Layout;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public abstract class NodeViewModel : ReactiveObject
    {
        private NodifyEditorViewModel _graph = default!;
        public NodifyEditorViewModel Graph
        {
            get => _graph;
            internal set => this.RaiseAndSetIfChanged(ref _graph, value);
        }

        private Point _location;
        public Point Location
        {
            get => _location;
            set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        public Orientation Orientation { get; protected set; }

        public ICommand DeleteCommand { get; }

        protected NodeViewModel()
        {
            DeleteCommand = ReactiveCommand.Create(() => Graph.Nodes.Remove(this));
        }
    }
}
