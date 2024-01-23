using Avalonia;
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
    }
}
