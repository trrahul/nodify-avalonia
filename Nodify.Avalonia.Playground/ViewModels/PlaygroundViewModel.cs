using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Data;
using Avalonia.Styling;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Playground.Editor;
using Nodify.Avalonia.Playground.Helpers;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.ViewModels
{
    public class PlaygroundViewModel : ReactiveObject
    {
        private bool _currentThemeToggle;
        public NodifyEditorViewModel GraphViewModel { get; } = new NodifyEditorViewModel();

        public PlaygroundViewModel()
        {
            GenerateRandomNodesCommand = ReactiveCommand.Create(GenerateRandomNodes);
            PerformanceTestCommand = ReactiveCommand.Create(PerformanceTest);
            ToggleConnectionsCommand = ReactiveCommand.Create(ToggleConnections);
            ResetCommand = ReactiveCommand.Create(ResetGraph);
            //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Nodes, GraphViewModel.Nodes);
            //BindingOperations.EnableCollectionSynchronization(GraphViewModel.Connections, GraphViewModel.Connections);
        }

        public ICommand GenerateRandomNodesCommand { get; }
        public ICommand PerformanceTestCommand { get; }
        public ICommand ToggleConnectionsCommand { get; }
        public ICommand ResetCommand { get; }
        public PlaygroundSettings Settings => PlaygroundSettings.Instance;

        public bool CurrentThemeToggle
        {
            get => _currentThemeToggle;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _currentThemeToggle, value))
                {
                    Application.Current!.RequestedThemeVariant = value switch
                    {
                        true => ThemeVariant.Dark,
                        false => ThemeVariant.Light,
                        //null => ThemeVariant.Default
                    };
                }
            }
        }

        private void ResetGraph()
        {
            GraphViewModel.Nodes.Clear();
            EditorSettings.Instance.Location = new Point(0, 0);
            EditorSettings.Instance.Zoom = 1.0d;
        }

        private async void GenerateRandomNodes()
        {
            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(Settings.MinNodes)
            {
                MinNodesCount = Settings.MinNodes,
                MaxNodesCount = Settings.MaxNodes,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });
            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async void ToggleConnections()
        {
            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
            else
            {
                GraphViewModel.Connections.Clear();
            }
        }

        private async void PerformanceTest()
        {
            uint count = Settings.PerformanceTestNodes;
            int distance = 500;
            int size = (int)count / (int)Math.Sqrt(count);

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(count)
            {
                NodeLocationGenerator = (s, i) => new Point(i % size * distance, i / size * distance),
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async Task ConnectNodes()
        {
            var schema = new GraphSchema();
            var connections = RandomNodesGenerator.GenerateConnections(GraphViewModel.Nodes);

            if (Settings.AsyncLoading)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < connections.Count; i++)
                    {
                        var con = connections[i];
                        schema.TryAddConnection(con.Input, con.Output);
                    }
                });
            }
            else
            {
                for (int i = 0; i < connections.Count; i++)
                {
                    var con = connections[i];
                    schema.TryAddConnection(con.Input, con.Output);
                }
            }
        }

        private async Task CopyToAsync(IList source, IList target)
        {
            if (Settings.AsyncLoading)
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < source.Count; i++)
                    {
                        target.Add(source[i]);
                    }
                });
            }
            else
            {
                for (int i = 0; i < source.Count; i++)
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
