using System.Windows.Input;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class ExpandoOperationViewModel : OperationViewModel
    {
        public ExpandoOperationViewModel()
        {
            AddInputCommand = ReactiveCommand.Create( //todo was requery
                () => Input.Add(new ConnectorViewModel()),
                this.WhenAnyValue(v => v.Input.Count,v => v.MaxInput,(c,m) => c < m));

            RemoveInputCommand = ReactiveCommand.Create( // todo was requery
                () => Input.RemoveAt(Input.Count - 1),
                this.WhenAnyValue(v => v.Input.Count, v => v.MinInput, (c, m) => c > m));
        }

        public ICommand AddInputCommand { get; }
        public ICommand RemoveInputCommand { get; }

        private uint _minInput = 0;
        public uint MinInput
        {
            get => _minInput;
            set => this.RaiseAndSetIfChanged(ref _minInput, value);
        }

        private uint _maxInput = uint.MaxValue;
        public uint MaxInput
        {
            get => _maxInput;
            set => this.RaiseAndSetIfChanged(ref _maxInput, value);
        }
    }
}
