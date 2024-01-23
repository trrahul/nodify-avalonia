using System;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class EditorViewModel : ViewModelBase 
    {
        public event Action<EditorViewModel, CalculatorViewModel>? OnOpenInnerCalculator;

        public EditorViewModel? Parent { get; set; }

        public EditorViewModel()
        {
            Calculator = new CalculatorViewModel();
            OpenCalculatorCommand = ReactiveCommand.Create<CalculatorViewModel>(calculator =>
            {
                OnOpenInnerCalculator?.Invoke(this, calculator);
            });
        }

        public ICommand OpenCalculatorCommand { get; }

        public Guid Id { get; } = Guid.NewGuid();

        private CalculatorViewModel _calculator = default!;
        public CalculatorViewModel Calculator 
        {
            get => _calculator;
            set => this.RaiseAndSetIfChanged(ref _calculator, value);
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public object? Header //todo temp fix for issue with tabitem header
        {
            get => Name;
            set => Name = (string?)value;
        }
    }
}
