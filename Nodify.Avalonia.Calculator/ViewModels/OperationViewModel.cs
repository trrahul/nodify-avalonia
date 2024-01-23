using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Reactive;
using Nodify.Avalonia.Calculator.Operations;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class OperationViewModel : ViewModelBase
    {
        public OperationViewModel()
        {
            IDisposable? d = null;
            Input.WhenAdded(x =>
            {
                x.Operation = this;
                x.IsInput = true;
                d = x.WhenAnyValue(v => v.Value).Subscribe(new AnonymousObserver<double>(_ => OnInputValueChanged()));
            })
            .WhenRemoved(x =>
            {
                d?.Dispose();
            });
        }

        private void OnInputValueChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectorViewModel.Value))
            {
                OnInputValueChanged();
            }
        }

        private Point _location;
        public Point Location
        {
            get => _location;
            set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }

        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public bool IsReadOnly { get; set; }

        private IOperation? _operation;
        public IOperation? Operation
        {
            get => _operation;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _operation, value))
                {
                    OnInputValueChanged();
                }
            }
        }

        public NodifyObservableCollection<ConnectorViewModel> Input { get; } = new NodifyObservableCollection<ConnectorViewModel>();

        private ConnectorViewModel? _output;
        public ConnectorViewModel? Output
        {
            get => _output;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _output, value) && _output != null)
                {
                    _output.Operation = this;
                }
            }
        }

        protected virtual void OnInputValueChanged()
        {
            if (Output != null && Operation != null)
            {
                try
                {
                    var input = Input.Select(i => i.Value).ToArray();
                    Output.Value = Operation?.Execute(input) ?? 0;
                }
                catch
                {

                }
            }
        }
    }
}
