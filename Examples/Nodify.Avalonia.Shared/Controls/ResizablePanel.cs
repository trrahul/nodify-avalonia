using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Nodify.Avalonia.Shared.Controls
{
    public class ResizablePanel : ContentControl
    {
        internal static readonly ResizeDirections BoxedResizeDirection = ResizeDirections.All;

        public static readonly StyledProperty<ResizeDirections> DirectionsProperty
            = AvaloniaProperty.Register<ResizablePanel, ResizeDirections>(nameof(Directions), BoxedResizeDirection);

        public static readonly StyledProperty<ICommand?> ResizeStartedCommandProperty
            = AvaloniaProperty.Register<ResizablePanel, ICommand?>(nameof(ResizeStartedCommand));

        public static readonly StyledProperty<ICommand?> ResizeCompletedCommandProperty
            = AvaloniaProperty.Register<ResizablePanel, ICommand?>(nameof(ResizeCompletedCommand));

        public ResizeDirections Directions
        {
            get => GetValue(DirectionsProperty);
            set => SetValue(DirectionsProperty, value);
        }

        public ICommand? ResizeStartedCommand
        {
            get => GetValue(ResizeStartedCommandProperty);
            set => SetValue(ResizeStartedCommandProperty, value);
        }

        public ICommand? ResizeCompletedCommand
        {
            get => GetValue(ResizeCompletedCommandProperty);
            set => SetValue(ResizeCompletedCommandProperty, value);
        }

        static ResizablePanel()
        {
            ClipToBoundsProperty.OverrideDefaultValue<ResizablePanel>(false);
        }

        public ResizablePanel()
        {
            AddHandler(Thumb.DragDeltaEvent, new EventHandler<VectorEventArgs>(OnResize));
            AddHandler(Thumb.DragStartedEvent, new EventHandler<VectorEventArgs>(OnDragStarted));
            AddHandler(Thumb.DragCompletedEvent, new EventHandler<VectorEventArgs>(OnDragCompleted));
        }

        private void OnDragStarted(object? sender, VectorEventArgs e)
        {
            if (ResizeStartedCommand?.CanExecute(null) ?? false)
            {
                ResizeStartedCommand.Execute(null);
            }
        }

        private void OnDragCompleted(object? sender, VectorEventArgs e)
        {
            if (ResizeCompletedCommand?.CanExecute(null) ?? false)
            {
                ResizeCompletedCommand.Execute(null);
            }
        }

        private void OnResize(object? sender, VectorEventArgs e)
        {
            if (e.Source is Resizer resizer)
            {
                double resizeX = 0;
                double resizeY = 0;

                double moveX = 0;
                double moveY = 0;

                if (resizer.Direction.HasFlag(ResizeDirections.Top))
                {
                    moveY = resizeY = ResizeTop(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.Bottom))
                {
                    resizeY = ResizeBottom(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.Left))
                {
                    moveX = resizeX = ResizeLeft(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.Right))
                {
                    resizeX = ResizeRight(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.TopLeft))
                {
                    moveY = resizeY = ResizeTop(e);
                    moveX = resizeX = ResizeLeft(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.TopRight))
                {
                    moveY = resizeY = ResizeTop(e);
                    resizeX = ResizeRight(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.BottomLeft))
                {
                    resizeY = ResizeBottom(e);
                    moveX = resizeX = ResizeLeft(e);
                }

                if (resizer.Direction.HasFlag(ResizeDirections.BottomRight))
                {
                    resizeY = ResizeBottom(e);
                    resizeX = ResizeRight(e);
                }

                OnProcessDelta(ref resizeX, ref resizeY);
                OnProcessDelta(ref moveX, ref moveY);

                OnMove(moveX, moveY);

                Width -= resizeX;
                Height -= resizeY;

                e.Handled = true;
            }
        }

        // e.Vector.Y is positive for a downward drag delta.
        private double ResizeBottom(VectorEventArgs e)
        {
            return Math.Min(-e.Vector.Y, Bounds.Height - MinHeight);
        }

        // For a top-edge drag: a positive delta moves the top edge down, shrinking the height.
        private double ResizeTop(VectorEventArgs e)
        {
            return Math.Min(e.Vector.Y, Bounds.Height - MinHeight);
        }

        private double ResizeRight(VectorEventArgs e)
        {
            return Math.Min(-e.Vector.X, Bounds.Width - MinWidth);
        }

        private double ResizeLeft(VectorEventArgs e)
        {
            return Math.Min(e.Vector.X, Bounds.Width - MinWidth);
        }

        protected virtual void OnMove(double x, double y)
        {
            Canvas.SetTop(this, Canvas.GetTop(this) + y);
            Canvas.SetLeft(this, Canvas.GetLeft(this) + x);
        }

        protected virtual void OnProcessDelta(ref double dx, ref double dy)
        {
        }
    }

    public class Resizer : Thumb
    {
        public static readonly StyledProperty<ResizeDirections> DirectionProperty
            = AvaloniaProperty.Register<Resizer, ResizeDirections>(nameof(Direction), ResizablePanel.BoxedResizeDirection);

        public ResizeDirections Direction
        {
            get => GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        static Resizer()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Resizer>(false);
        }
    }

    [Flags]
    public enum ResizeDirections
    {
        Top = 1,
        Left = 2,
        Bottom = 4,
        Right = 8,
        TopLeft = 16,
        TopRight = 32,
        BottomLeft = 64,
        BottomRight = 128,

        Edges = Top | Left | Bottom | Right,
        Corners = TopLeft | TopRight | BottomLeft | BottomRight,
        All = Edges | Corners
    }
}
