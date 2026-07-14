using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Threading;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Playground.Editor;
using Nodify.Avalonia.Playground.Helpers;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.ViewModels
{
    public class PlaygroundViewModel : ReactiveObject
    {
        public NodifyEditorViewModel GraphViewModel { get; } = new NodifyEditorViewModel();

        public PlaygroundViewModel()
        {
            // CreateFromTask ties command execution to the whole async operation: post-await
            // failures surface through the command and overlapping runs are prevented
            // (Create + async void completed immediately and swallowed both).
            GenerateRandomNodesCommand = ReactiveCommand.CreateFromTask(GenerateRandomNodes);
            PerformanceTestCommand = ReactiveCommand.CreateFromTask(PerformanceTest);
            ToggleConnectionsCommand = ReactiveCommand.CreateFromTask(ToggleConnections);
            ResetCommand = ReactiveCommand.Create(ResetGraph);

            Settings.PropertyChanged += OnSettingsChanged;
        }

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlaygroundSettings.ShouldConnectNodes))
            {
                this.RaisePropertyChanged(nameof(ConnectNodesText));
            }
        }

        public ICommand GenerateRandomNodesCommand { get; }
        public ICommand PerformanceTestCommand { get; }
        public ICommand ToggleConnectionsCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand SetNextThemeCommand => ThemeManager.SetNextThemeCommand;
        public PlaygroundSettings Settings => PlaygroundSettings.Instance;

        public string ConnectNodesText => Settings.ShouldConnectNodes ? "CONNECT NODES" : "DISCONNECT NODES";

        private void ResetGraph()
        {
            GraphViewModel.Nodes.Clear();
            EditorSettings.Instance.Location = new Point(0, 0);
            EditorSettings.Instance.Zoom = 1.0d;
        }

        private async Task GenerateRandomNodes()
        {
            uint minNodesByType = Settings.MinNodes / 2;
            uint maxNodesByType = Settings.MaxNodes / 2;

            var nodes = RandomNodesGenerator.GenerateNodes<FlowNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            var verticalNodes = RandomNodesGenerator.GenerateNodes<VerticalNodeViewModel>(new NodesGeneratorSettings(minNodesByType)
            {
                MinNodesCount = minNodesByType,
                MaxNodesCount = maxNodesByType,
                MinInputCount = Settings.MinConnectors,
                MaxInputCount = Settings.MaxConnectors,
                MinOutputCount = Settings.MinConnectors,
                MaxOutputCount = Settings.MaxConnectors,
                GridSnap = EditorSettings.Instance.GridSpacing
            });

            GraphViewModel.Nodes.Clear();
            await CopyToAsync(nodes, GraphViewModel.Nodes);
            await CopyToAsync(verticalNodes, GraphViewModel.Nodes);

            if (Settings.ShouldConnectNodes)
            {
                await ConnectNodes();
            }
        }

        private async Task ToggleConnections()
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

        private async Task PerformanceTest()
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

        private Task ConnectNodes()
        {
            var schema = new GraphSchema();
            var connections = RandomNodesGenerator.GenerateConnections(GraphViewModel.Nodes);

            if (Settings.AsyncLoading)
            {
                // schema.TryAddConnection mutates GraphViewModel.Connections, so the mutation
                // must run on the UI thread even though the loading is driven off of it.
                return AddBatchedAsync(connections.Count, i =>
                {
                    var con = connections[i];
                    schema.TryAddConnection(con.Input, con.Output);
                });
            }

            for (int i = 0; i < connections.Count; i++)
            {
                var con = connections[i];
                schema.TryAddConnection(con.Input, con.Output);
            }

            return Task.CompletedTask;
        }

        private Task CopyToAsync(IList source, IList target)
        {
            if (Settings.AsyncLoading)
            {
                return AddBatchedAsync(source.Count, i => target.Add(source[i]));
            }

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }

            return Task.CompletedTask;
        }

        // Number of UI-bound mutations to apply per dispatcher hop while async loading.
        private const int UiBatchSize = 64;

        /// <summary>
        /// Runs the loading loop off the UI thread but marshals every mutation of the UI-bound
        /// <c>Nodes</c>/<c>Connections</c> collections back onto <see cref="Dispatcher.UIThread"/>
        /// in small batches, keeping the editor responsive without ever raising a change
        /// notification from a background thread.
        /// </summary>
        private static async Task AddBatchedAsync(int count, Action<int> addAt)
        {
            await Task.Run(async () =>
            {
                for (int start = 0; start < count; start += UiBatchSize)
                {
                    int begin = start;
                    int end = Math.Min(begin + UiBatchSize, count);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        for (int i = begin; i < end; i++)
                        {
                            addAt(i);
                        }
                    });
                }
            });
        }
    }
}
