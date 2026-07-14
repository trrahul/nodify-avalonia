using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Shapes.Canvas.UndoRedo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public partial class CanvasView : UserControl
    {
        private readonly DispatcherTimer _moveToLocationTimer;
        private readonly DispatcherTimer _generateLocationTimer;
        private readonly Random _random = new Random();
        private readonly Dictionary<UserCursorViewModel, Point> _moveToLocations = new Dictionary<UserCursorViewModel, Point>();
        private readonly Stopwatch _cursorMotionClock = new Stopwatch();
        private TimeSpan _lastCursorMotionTime;

        public CanvasView()
        {
            InitializeComponent();

            // ElementName bindings don't resolve on brushes outside the visual tree, so assign the
            // grid's Transform here. The editor's ViewportTransform is mutated in place, so once is enough.
            var grid = (VisualBrush)Resources["GridDrawingBrush"]!;
            grid.Transform = Editor.ViewportTransform;

            // Same constraint: the TargetedRoutedCommand resources can't ElementName-bind, so their targets are wired here.
            foreach (string commandKey in new[] { "ZoomInCommand", "ZoomOutCommand", "FitToScreenCommand", "BringIntoViewCommand" })
            {
                ((TargetedRoutedCommand)Resources[commandKey]!).Target = Editor;
            }

            _moveToLocationTimer = new DispatcherTimer(TimeSpan.FromSeconds(1d / 60d), DispatcherPriority.Background, OnMoveToLocationTick);
            _generateLocationTimer = new DispatcherTimer(TimeSpan.FromSeconds(3), DispatcherPriority.Background, OnGenerateNewLocation);

            // ItemsMoved is a routed event with no XAML attribute binding, so wire it via AddHandler.
            Editor.AddHandler(NodifyEditor.ItemsMovedEvent, Editor_ItemsMoved);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _lastCursorMotionTime = TimeSpan.Zero;
            _cursorMotionClock.Restart();
            _moveToLocationTimer.Start();
            _generateLocationTimer.Start();

            OnGenerateNewLocation(this, EventArgs.Empty);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Stop the cursor-animation timers so they don't keep firing on a stale DataContext after teardown.
            _cursorMotionClock.Stop();
            _moveToLocationTimer.Stop();
            _generateLocationTimer.Stop();

            base.OnDetachedFromVisualTree(e);
        }

        private void OnGenerateNewLocation(object? sender, EventArgs e)
        {
            var canvasVm = (CanvasViewModel)DataContext!;

            foreach (var cursor in canvasVm.Cursors)
            {
                _moveToLocations[cursor] = Editor.MouseLocation + new Vector(_random.Next(-1500, 1500), _random.Next(-1000, 1000));
            }
        }

        private void OnMoveToLocationTick(object? sender, EventArgs e)
        {
            var canvasVm = (CanvasViewModel)DataContext!;
            TimeSpan now = _cursorMotionClock.Elapsed;
            TimeSpan elapsed = now - _lastCursorMotionTime;
            _lastCursorMotionTime = now;
            double interpolationFactor = CursorMotion.GetInterpolationFactor(elapsed);

            for (int i = 0; i < canvasVm.Cursors.Count; i++)
            {
                var user = canvasVm.Cursors[i];

                if (!_moveToLocations.TryGetValue(user, out var targetLocation))
                {
                    continue;
                }

                Vector dir = targetLocation - user.Location;

                var newLocation = user.Location + dir * interpolationFactor;
                user.Location = newLocation;
            }
        }

        #region Drawing shapes

        private ShapeViewModel? _drawingShape;
        private Point _initialLocation;

        private void Editor_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var toolbarVm = ((CanvasViewModel)DataContext!).CanvasToolbar;
            if (toolbarVm.SelectedTool != CanvasTool.None && DrawingGesturesMappings.Instance.Draw.Matches(this, e))
            {
                _initialLocation = Editor.MouseLocation;
                _drawingShape = toolbarVm.CreateShapeAtLocation(Editor.MouseLocation);
            }
        }

        private void Editor_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_drawingShape != null)
            {
                _drawingShape.Width = Math.Abs(Editor.MouseLocation.X - _initialLocation.X);
                _drawingShape.Height = Math.Abs(Editor.MouseLocation.Y - _initialLocation.Y);

                if (Editor.MouseLocation.X < _initialLocation.X)
                {
                    _drawingShape.Location = new Point(Editor.MouseLocation.X, _drawingShape.Location.Y);
                }

                if (Editor.MouseLocation.Y < _initialLocation.Y)
                {
                    _drawingShape.Location = new Point(_drawingShape.Location.X, Editor.MouseLocation.Y);
                }
            }
        }

        private void Editor_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _drawingShape = null;
        }

        #endregion

        private void Editor_ItemsMoved(object? sender, ItemsMovedEventArgs e)
        {
            var shapes = e.Items.Cast<ShapeViewModel>().ToList();

            var canvasVm = (CanvasViewModel)DataContext!;

            // Record any selection this gesture caused BEFORE the move, or the Dispatcher-posted
            // selection flush can land after it and invert undo order.
            canvasVm.FlushPendingSelectionHistory();

            using (canvasVm.UndoRedo.Batch("Move shapes"))
            {
                var moveShapes = new MoveShapesAction(shapes, e.Offset);
                canvasVm.UndoRedo.Record(moveShapes);
            }
        }

        private void Minimap_Zoom(object? sender, ZoomEventArgs e)
        {
            Editor.ZoomAtPosition(e.Zoom, e.Location);
        }

    }
}
