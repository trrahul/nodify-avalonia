using System.Collections.Generic;
using Avalonia;
using Nodify.Avalonia.Extensions;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class ConnectorViewModel : ViewModelBase
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private double _value;
        public double Value
        {
            get => _value;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _value, value))
                {
                    ValueObservers.ForEach(o => o.Value = value);
                }
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => this.RaiseAndSetIfChanged(ref _isConnected, value);
        }

        private bool _isInput;
        public bool IsInput
        {
            get => _isInput;
            set => this.RaiseAndSetIfChanged(ref _isInput, value);
        }

        private Point _anchor;
        public Point Anchor
        {
            get => _anchor;
            set => this.RaiseAndSetIfChanged(ref _anchor, value);
        }

        private OperationViewModel _operation = default!;
        public OperationViewModel Operation
        {
            get => _operation;
            set => this.RaiseAndSetIfChanged(ref _operation, value);
        }

        public List<ConnectorViewModel> ValueObservers { get; } = new List<ConnectorViewModel>();
    }
}
