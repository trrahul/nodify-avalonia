using Avalonia;
using Nodify.Avalonia.Shared;
using System;
using System.ComponentModel;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public class ShapeToolbarViewModel : ObservableObject, ICanvasDecorator
    {
        private static readonly Vector ToolbarOffset = new Vector(0, -70);

        private ShapeViewModel? _shape;
        public ShapeViewModel? Shape
        {
            get => _shape;
            set
            {
                var prevShape = _shape;
                if (SetProperty(ref _shape, value))
                {
                    _hiddenShape = null;
                    HookLocationEvents(prevShape, value);
                }
            }
        }

        private ShapeViewModel? _hiddenShape;

        private Point _location;
        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        private void HookLocationEvents(ShapeViewModel? prevShape, ShapeViewModel? newShape)
        {
            if (prevShape != null)
                prevShape.PropertyChanged -= OnLocationChanged;

            if (newShape != null)
            {
                newShape.PropertyChanged += OnLocationChanged;
                UpdateLocation(newShape);
            }
        }

        private void OnLocationChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(ShapeViewModel.Location))
                UpdateLocation((ShapeViewModel)sender!);
        }

        private void UpdateLocation(ShapeViewModel shape)
        {
            // Clamp to the top edge so the swatches stay reachable. Also sidesteps
            // zero-sized decorator-template measurement at negative coordinates.
            var location = shape.Location + ToolbarOffset;
            Location = new Point(location.X, Math.Max(0, location.Y));
        }

        public void Hide()
        {
            _hiddenShape = _shape;
            _shape = null;

            OnPropertyChanged(nameof(Shape));
        }

        public void Show()
        {
            if (_hiddenShape != null)
            {
                _shape = _hiddenShape;
                _hiddenShape = null;

                OnPropertyChanged(nameof(Shape));
            }
        }
    }
}
