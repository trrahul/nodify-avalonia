using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Rendering.Composition;
using Nodify.Avalonia.Playground.Editor;
using Nodify.Avalonia.Playground.ViewModels;

namespace Nodify.Avalonia.Playground.Views
{
    public partial class MainWindow : Window
    {
        public static class CompositionTargetEx
        {
            private static TimeSpan _last = TimeSpan.Zero;
            private static event Action<double>? FrameUpdating;

            public static event Action<double> Rendering
            {
                add
                {
                    if (FrameUpdating == null)
                    {
                        //CompositionTarget.Rendering += OnRendering;
                    }
                    FrameUpdating += value;
                }
                remove
                {
                    FrameUpdating -= value;
                    if (FrameUpdating == null)
                    {
                        //CompositionTarget.Rendering -= OnRendering;
                    }
                }
            }

            //private static void OnRendering(object? sender, EventArgs e)
            //{
            //    RenderingEventArgs args = (RenderingEventArgs)e;
            //    var renderingTime = args.RenderingTime;
            //    if (renderingTime == _last)
            //        return;

            //    double fps = 1000 / (renderingTime - _last).TotalMilliseconds;
            //    _last = renderingTime;
            //    FrameUpdating?.Invoke(fps);
            //}
        }

        private readonly Random _rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
            //CompositionTargetEx.Rendering += OnRendering;
        }

        //private void OnRendering(double fps)
        //{
        //    FPSText.Text = fps.ToString("0");
        //}

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

        private void FitToScreen_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is PlaygroundViewModel model)
            {
                EditorCommands.FitToScreen.Execute(null,EditorView.Editor);
            }
        }
    }
}