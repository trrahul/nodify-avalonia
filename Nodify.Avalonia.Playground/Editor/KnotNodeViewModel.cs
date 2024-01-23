using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public class KnotNodeViewModel : NodeViewModel
    {
        private ConnectorViewModel _connector = default!;
        public ConnectorViewModel Connector
        {
            get => _connector;
            set
            {
                if (!ReferenceEquals(_connector, value))
                {
                    this.RaiseAndSetIfChanged(ref _connector, value);
                    _connector.Node = this;
                }
            }
        }

        public ConnectorFlow Flow { get; set; }

        public bool IsFlow { get; set; }
    }
}
