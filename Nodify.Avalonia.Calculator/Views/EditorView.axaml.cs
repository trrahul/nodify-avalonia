using System;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Nodify.Avalonia.Calculator.Operations;
using Nodify.Avalonia.Calculator.ViewModels;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Calculator.Views
{
    public partial class EditorView : UserControl
    {
        private static readonly DataFormat<OperationInfoViewModel> DragDropFormat =
            DataFormat.CreateInProcessFormat<OperationInfoViewModel>(nameof(OperationInfoViewModel));

        // Four 2s legs sweeping around the border (cues 0/.25/.5/.75/1) make one 8s cycle.
        internal static readonly TimeSpan AnimatedBorderCycleDuration = TimeSpan.FromSeconds(8);

        private CancellationTokenSource? _animatedBorderCts;

        public EditorView()
        {
            InitializeComponent();
            AddHandler(PointerPressedEvent, CloseOperationsMenuFromPointer, RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(ItemContainer.DragStartedEvent, CloseOperationsMenu, RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(PointerReleasedEvent, OpenOperationsMenu, RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(DragDrop.DropEvent, OnDropNode, RoutingStrategies.Bubble, handledEventsToo: true);
            AddHandler(KeyDownEvent, OnOpenContextMenuKey);
            Loaded += OnEditorViewLoaded;
            Unloaded += OnEditorViewUnloaded;
        }

        internal static NodifyEditor? FindEditorFromEventSource(object? source)
            => (source as Visual)?.FindAncestorOfType<NodifyEditor>(includeSelf: true);

        // Sweeps the "AnimatedBrush" gradient around the border; see the NodifyEditor.Decorators comment in EditorView.axaml.
        private void OnEditorViewLoaded(object? sender, RoutedEventArgs e)
        {
            if (Resources.TryGetResource("AnimatedBrush", null, out var resource)
                && resource is LinearGradientBrush brush)
            {
                _animatedBorderCts?.Cancel();
                _animatedBorderCts = new CancellationTokenSource();
                RunAnimatedBorderLoop(brush, _animatedBorderCts.Token);
            }
        }

        private void OnEditorViewUnloaded(object? sender, RoutedEventArgs e)
        {
            _animatedBorderCts?.Cancel();
            _animatedBorderCts = null;
        }

        private static async void RunAnimatedBorderLoop(LinearGradientBrush brush, CancellationToken token)
        {
            var animation = new Animation
            {
                Duration = AnimatedBorderCycleDuration,
                Easing = new LinearEasing(),
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(LinearGradientBrush.StartPointProperty, new RelativePoint(0, 0, RelativeUnit.Relative)),
                            new Setter(LinearGradientBrush.EndPointProperty, new RelativePoint(1, 0, RelativeUnit.Relative)),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.25d),
                        Setters =
                        {
                            new Setter(LinearGradientBrush.StartPointProperty, new RelativePoint(1, 0, RelativeUnit.Relative)),
                            new Setter(LinearGradientBrush.EndPointProperty, new RelativePoint(1, 1, RelativeUnit.Relative)),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.5d),
                        Setters =
                        {
                            new Setter(LinearGradientBrush.StartPointProperty, new RelativePoint(1, 1, RelativeUnit.Relative)),
                            new Setter(LinearGradientBrush.EndPointProperty, new RelativePoint(0, 1, RelativeUnit.Relative)),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(0.75d),
                        Setters =
                        {
                            new Setter(LinearGradientBrush.StartPointProperty, new RelativePoint(0, 1, RelativeUnit.Relative)),
                            new Setter(LinearGradientBrush.EndPointProperty, new RelativePoint(0, 0, RelativeUnit.Relative)),
                        },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters =
                        {
                            new Setter(LinearGradientBrush.StartPointProperty, new RelativePoint(0, 0, RelativeUnit.Relative)),
                            new Setter(LinearGradientBrush.EndPointProperty, new RelativePoint(1, 0, RelativeUnit.Relative)),
                        },
                    },
                },
            };

            // RunAsync is not allowed on a looping (Infinite) animation, so loop a
            // single-iteration animation manually until the view is unloaded (token cancels).
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await animation.RunAsync(brush, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the view is unloaded.
            }
        }

        private void CloseOperationsMenuFromPointer(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetChangedButton() == MouseButton.Left)
            {
                CloseOperationsMenu(sender,e);
            }
        }

        // Open only for an unhandled right release: a right-drag past the editor's suppression
        // threshold commits a pan and arrives handled, while a sub-threshold click arrives unhandled.
        private void OpenOperationsMenu(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Handled || e.GetChangedButton() != MouseButton.Right)
            {
                return;
            }

            if (FindEditorFromEventSource(e.Source) is { DataContext: CalculatorViewModel calculator } editor)
            {
                calculator.OperationsMenu.OpenAt(editor.MouseLocation);
            }
        }

        // Shift+F10 / Menu key toggles the operations menu near the viewport center. The library's
        // ApplicationCommands.ContextMenu is a stub, so match the key gestures directly.
        private void OnOpenContextMenuKey(object? sender, KeyEventArgs e)
        {
            bool isContextMenuGesture = e.Key == Key.Apps
                || (e.Key == Key.F10 && e.KeyModifiers.HasFlag(KeyModifiers.Shift));

            if (!isContextMenuGesture || e.Handled)
            {
                return;
            }

            var editor = FindEditorFromEventSource(e.Source);
            if (editor?.DataContext is CalculatorViewModel calculator)
            {
                e.Handled = true;

                if (calculator.OperationsMenu.IsVisible)
                {
                    calculator.OperationsMenu.Close();
                }
                else
                {
                    calculator.OperationsMenu.OpenAt(editor.ViewportLocation
                        + new Vector(editor.ViewportSize.Width / 3, editor.ViewportSize.Height / 3));
                }
            }
        }

        private void CloseOperationsMenu(object? sender, RoutedEventArgs e)
        {
            // Resolve the editor from the event source, not the sender: sender is always this one
            // EditorView, so a press in a nested operation-graph editor must close THAT editor's menu.
            var editor = FindEditorFromEventSource(e.Source);

            if (editor?.DataContext is CalculatorViewModel calculator)
            {
                calculator.OperationsMenu.Close();
            }
        }

        private void OnOperationNodeDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Control { DataContext: CalculatorOperationViewModel operation }
                && DataContext is EditorViewModel editor
                && editor.OpenCalculatorCommand.CanExecute(operation.InnerCalculator))
            {
                editor.OpenCalculatorCommand.Execute(operation.InnerCalculator);
            }
        }

        private void OnDropNode(object? sender, DragEventArgs e)
        {
            if (FindEditorFromEventSource(e.Source) is { } editor
                && editor.DataContext is CalculatorViewModel calculator
                && e.DataTransfer.TryGetValue(DragDropFormat) is { } operation)
            {
                var op = OperationFactory.GetOperation(operation);
                op.Location = editor.GetLocationInsideEditor(e);
                calculator.Operations.Add(op);
                e.Handled = true;
            }
        }

        private void OnNodeDragPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed
                && sender is Control { DataContext: OperationInfoViewModel operation })
            {
                BeginNodeDrag(e, operation);
            }
        }

        private static async void BeginNodeDrag(PointerPressedEventArgs pressedArgs, OperationInfoViewModel operation)
        {
            var data = new DataTransfer();
            data.Add(DataTransferItem.Create(DragDropFormat, operation));

            try
            {
                await DragDrop.DoDragDropAsync(pressedArgs, data, DragDropEffects.Copy);
            }
            catch (Exception ex)
            {
                // No logging infra in this sample; surface the failure on the console.
                Console.Error.WriteLine($"Drag-and-drop failed: {ex}");
            }
        }
    }
}
