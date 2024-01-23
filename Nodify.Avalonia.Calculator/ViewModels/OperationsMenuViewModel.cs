using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Nodify.Avalonia.Calculator.Operations;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class OperationsMenuViewModel : ViewModelBase
    {
        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                this.RaiseAndSetIfChanged(ref _isVisible, value);
                if (!value)
                {
                    Closed?.Invoke();
                }
            }
        }

        public Rect Bounds { get; set; }

        private Point _location;
        public Point Location
        {
            get => _location;
            set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        public event Action? Closed;

        public void OpenAt(Point targetLocation)
        {
            Close();
            Location = targetLocation;
            IsVisible = true;
        }

        public void Close()
        {
            IsVisible = false;
        }

        public NodifyObservableCollection<OperationInfoViewModel> AvailableOperations { get; }
        public ICommand CreateOperationCommand { get; }
        private readonly CalculatorViewModel _calculator;
        private Rect _bounds;

        public OperationsMenuViewModel(CalculatorViewModel calculator)
        {
            _calculator = calculator;
            List<OperationInfoViewModel> operations = new List<OperationInfoViewModel>
            {
                new OperationInfoViewModel
                {
                    Type = OperationType.Graph,
                    Title = "(New) Operation Graph",
                },
                new OperationInfoViewModel
                {
                    Type = OperationType.Calculator,
                    Title = "Calculator"
                },
                new OperationInfoViewModel
                {
                    Type = OperationType.Expression,
                    Title = "Custom",
                }
            };
            operations.AddRange(OperationFactory.GetOperationsInfo(typeof(OperationsContainer)));

            AvailableOperations = new NodifyObservableCollection<OperationInfoViewModel>(operations);
            CreateOperationCommand = ReactiveCommand.Create<OperationInfoViewModel>(CreateOperation);
        }

        private void CreateOperation(OperationInfoViewModel operationInfo)
        {
            OperationViewModel op = OperationFactory.GetOperation(operationInfo);
            op.Location = Location;

            _calculator.Operations.Add(op);

            var pending = _calculator.PendingConnection;
            if (pending.IsVisible)
            {
                var connector = pending.Source.IsInput ? op.Output : op.Input.FirstOrDefault();
                if (connector != null && _calculator.CanCreateConnection(pending.Source, connector))
                {
                    _calculator.CreateConnection(pending.Source, connector);
                }
            }
            Close();
        }
    }
}
