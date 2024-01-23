using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class ConnectionViewModel : ViewModelBase
    {
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
    }
}
