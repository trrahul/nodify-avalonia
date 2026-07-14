using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace Nodify.Avalonia.Helpers
{
    internal class DraggingOptimized : IDraggingStrategy
    {
        private readonly NodifyEditor _editor;
        private Vector _dragAccumulator;
        private readonly List<ItemContainer> _selectedContainers;

        public IReadOnlyCollection<ItemContainer> Containers => _selectedContainers;
        public Vector Offset { get; private set; }

        public DraggingOptimized(NodifyEditor editor, IEnumerable<ItemContainer> containers)
        {
            _editor = editor;
            _selectedContainers = containers.Where(c => c.IsDraggable).ToList();
        }

        public void Abort(Vector change)
        {
            for (var i = 0; i < _selectedContainers.Count; i++)
            {
                ItemContainer container = _selectedContainers[i];
                var r = (TranslateTransform)container.RenderTransform!;

                r.X = 0;
                r.Y = 0;

                container.OnPreviewLocationChanged(container.Location);
            }

            _selectedContainers.Clear();
        }

        public void End(Vector change)
        {
            for (var i = 0; i < _selectedContainers.Count; i++)
            {
                ItemContainer container = _selectedContainers[i];
                var r = (TranslateTransform)container.RenderTransform!;

                Point result = container.Location + new Vector(r.X, r.Y);

                if (NodifyEditor.EnableSnappingCorrection && (r.X != 0 || r.Y != 0))
                {
                    var x = (int)result.X / _editor.GridCellSize * _editor.GridCellSize;
                    var y = (int)result.Y / _editor.GridCellSize * _editor.GridCellSize;
                    result = new Point(x, y);
                }

                container.SetCurrentValue(ItemContainer.LocationProperty, result);

                r.X = 0;
                r.Y = 0;
            }

            _selectedContainers.Clear();
        }

        public void Start(Vector change)
        {
            _dragAccumulator = new Vector(0, 0);
            Offset = new Vector(0, 0);
        }

        public void Update(Vector change)
        {
            _dragAccumulator += change;
            var delta = new Vector(((int)_dragAccumulator.X / _editor.GridCellSize) * _editor.GridCellSize, ((int)_dragAccumulator.Y / _editor.GridCellSize) * _editor.GridCellSize);
            _dragAccumulator -= delta;

            if (delta.X != 0 || delta.Y != 0)
            {
                Offset += delta;

                for (var i = 0; i < _selectedContainers.Count; i++)
                {
                    ItemContainer container = _selectedContainers[i];
                    var r = (TranslateTransform)container.RenderTransform!;

                    r.X += delta.X ;
                    r.Y += delta.Y ;

                    Point result = container.Location + new Vector(r.X, r.Y);

                    if (NodifyEditor.EnableSnappingCorrection)
                    {
                        var x = (int)result.X / _editor.GridCellSize * _editor.GridCellSize;
                        var y = (int)result.Y / _editor.GridCellSize * _editor.GridCellSize;
                        result = new Point(x, y);
                    }

                    container.PreviewLocation = result;

                    container.OnPreviewLocationChanged(container.Location + new Vector(r.X, r.Y));
                }
            }
        }
    }
}
