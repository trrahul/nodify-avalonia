using Avalonia;
using Nodify.Avalonia.Extensions;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class PendingConnectionViewModel : ViewModelBase
    {
        private ConnectorViewModel _source = default!;
        public ConnectorViewModel Source
        {
            get => _source;
            set => this.RaiseAndSetIfChanged(ref _source, value);
        }

        private ConnectorViewModel? _target;
        public ConnectorViewModel? Target
        {
            get => _target;
            set => this.RaiseAndSetIfChanged(ref _target, value);
        }

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        private Point _targetLocation;

        public Point TargetLocation
        {
            get => _targetLocation;
            set => this.RaiseAndSetIfChanged(ref _targetLocation, value);
        }
    }
}
