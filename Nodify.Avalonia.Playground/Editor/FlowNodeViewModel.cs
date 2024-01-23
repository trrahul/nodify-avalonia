using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public class FlowNodeViewModel : NodeViewModel
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        public NodifyObservableCollection<ConnectorViewModel> Input { get; } = new NodifyObservableCollection<ConnectorViewModel>();
        public NodifyObservableCollection<ConnectorViewModel> Output { get; } = new NodifyObservableCollection<ConnectorViewModel>();
        public NodifyObservableCollection<ConnectorViewModel> FlowInput { get; } = new NodifyObservableCollection<ConnectorViewModel>();
        public NodifyObservableCollection<ConnectorViewModel> FlowOutput { get; } = new NodifyObservableCollection<ConnectorViewModel>();

        public FlowNodeViewModel()
        {
            Input.WhenAdded(c => c.Node = this)
                 .WhenRemoved(c => c.Disconnect());

            Output.WhenAdded(c => c.Node = this)
                 .WhenRemoved(c => c.Disconnect());
            FlowInput.WhenAdded(c => c.Node = this)
                .WhenRemoved(c => c.Disconnect());

            FlowOutput.WhenAdded(c => c.Node = this)
                .WhenRemoved(c => c.Disconnect());
        }

        public void Disconnect()
        {
            Input.Clear();
            Output.Clear();
            FlowInput.Clear();
            FlowOutput.Clear();
        }
    }
}
