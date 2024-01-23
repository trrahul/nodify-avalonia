using System.Windows.Input;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class CalculatorInputOperationViewModel : OperationViewModel
    {
        public CalculatorInputOperationViewModel()
        {
            AddOutputCommand = ReactiveCommand.Create( //todo was requery
                () => Output.Add(new ConnectorViewModel
                {
                    Title = $"In {Output.Count}"
                }), this.WhenAnyValue(v => v.Output.Count, (c) => c < 10));
            RemoveOutputCommand = ReactiveCommand.Create( //todo was requery
                () => Output.RemoveAt(Output.Count - 1), this.WhenAnyValue(v => v.Output.Count, (o) => o > 1));

            Output.Add(new ConnectorViewModel
            {
                Title = $"In {Output.Count}"
            });
        }

        public new NodifyObservableCollection<ConnectorViewModel> Output { get; set; } =
            new NodifyObservableCollection<ConnectorViewModel>();

        public ICommand AddOutputCommand { get; }
        public ICommand RemoveOutputCommand { get; }
    }
}
