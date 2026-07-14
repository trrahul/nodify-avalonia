using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Nodify.Avalonia.Playground.Editor;
using Nodify.Avalonia.Playground.ViewModels;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Playground.Views
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch _frameWatch = new Stopwatch();
        private readonly Random _rand = new Random();

        public MainWindow()
        {
            InitializeComponent();

            FitToScreenButton.Command = new TargetedRoutedCommand
            {
                Command = EditorCommands.FitToScreen,
                Target = EditorView.Editor
            };

            AddHandler(InputElement.GotFocusEvent,
                (_, e) => Title = e.Source?.ToString() ?? Title,
                RoutingStrategies.Bubble,
                handledEventsToo: true);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_frameWatch.IsRunning)
            {
                double fps = FrameRateEstimator.FromFrameDuration(_frameWatch.Elapsed);
                Dispatcher.UIThread.Post(() => FPSText.Text = fps.ToString("###"));
            }

            _frameWatch.Restart();
            // Schedule the next root render after the current pass, so the sampled interval
            // reflects the scene's actual render cadence.
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Send);
        }

        private void BringIntoView_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PlaygroundViewModel model)
            {
                NodifyObservableCollection<NodeViewModel> nodes = model.GraphViewModel.Nodes;
                int index = _rand.Next(nodes.Count);

                if (nodes.Count > index)
                {
                    NodeViewModel node = nodes[index];
                    EditorCommands.BringIntoView.Execute(node.Location, EditorView.Editor); 
                }
            }
        }

        private void AnimateConnections_Click(object sender, RoutedEventArgs e)
        {
            EditorSettings.Instance.IsAnimatingConnections = !EditorSettings.Instance.IsAnimatingConnections;
        }
    }
}
