using System.Linq;
using System.Windows.Input;
using Avalonia;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class CalculatorViewModel : ViewModelBase
    {
        public CalculatorViewModel()
        {
            CreateConnectionCommand =  ReactiveCommand.Create<ConnectorViewModel>(_ => CreateConnection(PendingConnection.Source, PendingConnection.Target), this.WhenAnyValue(v => v.PendingConnection.Source,v => v.PendingConnection.Target,CanCreateConnection));
            StartConnectionCommand = ReactiveCommand.Create<ConnectorViewModel>(_ => PendingConnection.IsVisible = true); // (c) => !(c.IsConnected && c.IsInput) todo
            DisconnectConnectorCommand = ReactiveCommand.Create<ConnectorViewModel>(DisconnectConnector);
            DeleteSelectionCommand = ReactiveCommand.Create(DeleteSelection);
            GroupSelectionCommand = ReactiveCommand.Create(GroupSelectedOperations,  this.WhenAnyValue(v => v.SelectedOperations.Count,(s) => s > 0));

            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;

                c.Input.Value = c.Output.Value;

                c.Output.ValueObservers.Add(c.Input);
            })
            .WhenRemoved(c =>
            {
                var ic = Connections.Count(con => con.Input == c.Input || con.Output == c.Input);
                var oc = Connections.Count(con => con.Input == c.Output || con.Output == c.Output);

                if (ic == 0)
                {
                    c.Input.IsConnected = false;
                }

                if (oc == 0)
                {
                    c.Output.IsConnected = false;
                }

                c.Output.ValueObservers.Remove(c.Input);
            });

            Operations.WhenAdded(x =>
            {
                x.Input.WhenRemoved(RemoveConnection);

                if (x is CalculatorInputOperationViewModel ci)
                {
                    ci.Output.WhenRemoved(RemoveConnection);
                }

                void RemoveConnection(ConnectorViewModel i)
                {
                    var c = Connections.Where(con => con.Input == i || con.Output == i).ToArray();
                    c.ForEach(con => Connections.Remove(con));
                }
            })
            .WhenRemoved(x =>
            {
                foreach (var input in x.Input)
                {
                    DisconnectConnector(input);
                }

                if (x.Output != null)
                {
                    DisconnectConnector(x.Output);
                }
            });

            OperationsMenu = new OperationsMenuViewModel(this);
        }

        private NodifyObservableCollection<OperationViewModel> _operations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            set => this.RaiseAndSetIfChanged(ref _operations, value);
        }

        private NodifyObservableCollection<OperationViewModel> _selectedOperations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> SelectedOperations
        {
            get => _selectedOperations;
            set => this.RaiseAndSetIfChanged(ref _selectedOperations, value);
        }

        public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();
        public PendingConnectionViewModel PendingConnection { get; set; } = new PendingConnectionViewModel();
        public OperationsMenuViewModel OperationsMenu { get; set; }

        public ICommand StartConnectionCommand { get; }
        public ICommand CreateConnectionCommand { get; }
        public ICommand DisconnectConnectorCommand { get; }
        public ICommand DeleteSelectionCommand { get; }
        public ICommand GroupSelectionCommand { get; }

        private void DisconnectConnector(ConnectorViewModel connector)
        {
            var connections = Connections.Where(c => c.Input == connector || c.Output == connector).ToList();
            connections.ForEach(c => Connections.Remove(c));
        }

        internal bool CanCreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
            => target == null || (source != target && source.Operation != target.Operation && source.IsInput != target.IsInput);

        internal void CreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
        {
            if (target == null)
            {
                PendingConnection.IsVisible = true;
                OperationsMenu.OpenAt(PendingConnection.TargetLocation);
                OperationsMenu.Closed += OnOperationsMenuClosed;
                return;
            }

            var input = source.IsInput ? source : target;
            var output = target.IsInput ? source : target;

            PendingConnection.IsVisible = false;

            DisconnectConnector(input);

            Connections.Add(new ConnectionViewModel
            {
                Input = input,
                Output = output
            });
        }

        private void OnOperationsMenuClosed()
        {
            PendingConnection.IsVisible = false;
            OperationsMenu.Closed -= OnOperationsMenuClosed;
        }

        private void DeleteSelection()
        {
            var selected = SelectedOperations.ToList();
            selected.ForEach(o => Operations.Remove(o));
        }

        private void GroupSelectedOperations()
        {
            var selected = SelectedOperations.ToList();
            var bounding = selected.GetBoundingBox(50);

            Operations.Add(new OperationGroupViewModel
            {
                Title = "Operations",
                Location = bounding.Position,
                GroupSize = new Size(bounding.Width, bounding.Height)
            });
        }
    }
}

