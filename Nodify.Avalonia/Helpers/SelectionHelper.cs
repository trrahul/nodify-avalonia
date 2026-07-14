using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia;

namespace Nodify.Avalonia.Helpers
{
    /// <summary>
    /// Helps with selecting <see cref="ItemContainer"/>s and updating the <see cref="NodifyEditor.SelectedArea"/> and <see cref="NodifyEditor.IsSelecting"/> properties.
    /// </summary>
    public sealed class SelectionHelper
    {
        private readonly NodifyEditor _host;
        private Point _startLocation;
        private SelectionType _selectionType;
        private bool _isRealtime;
        private IReadOnlyList<ItemContainer> _selectableContainers = Array.Empty<ItemContainer>();
        private IReadOnlyList<ItemContainer> _initialSelection = Array.Empty<ItemContainer>();

        /// <summary>Constructs a new instance of a <see cref="SelectionHelper"/>.</summary>
        /// <param name="host">The editor to select items from.</param>
        public SelectionHelper(NodifyEditor host)
            => _host = host;

        /// <summary>Available selection logic.</summary>
        public enum SelectionType
        {
            /// <summary>Replaces the old selection.</summary>
            Replace,
            /// <summary>Removes items from existing selection.</summary>
            Remove,
            /// <summary>Adds items to the current selection.</summary>
            Append,
            /// <summary>Inverts the selection.</summary>
            Invert
        }

        /// <summary>Attempts to start a new selection.</summary>
        /// <param name="location">The location inside the graph.</param>
        /// <param name="selectionType">The type of selection.</param>
        /// <remarks>Will not do anything if selection is in progress.</remarks>
        public void Start(Point location, SelectionType selectionType)
        {
            if (!_host.IsSelecting)
            {
                _selectionType = selectionType;
                _selectableContainers = GetSelectableContainers();
                _initialSelection = _host.SelectedContainers;

                _isRealtime = _host.EnableRealtimeSelection;
                _startLocation = location;

                _host.SelectedArea = new Rect();
                _host.IsSelecting = true;
            }
        }

        /// <summary>Update the end location for the selection.</summary>
        /// <param name="endLocation">An absolute location.</param>
        public void Update(Point endLocation)
        {
            double left = endLocation.X < _startLocation.X ? endLocation.X : _startLocation.X;
            double top = endLocation.Y < _startLocation.Y ? endLocation.Y : _startLocation.Y;
            double width = Math.Abs(endLocation.X - _startLocation.X);
            double height = Math.Abs(endLocation.Y - _startLocation.Y);

            _host.SelectedArea = new Rect(left, top, width, height);

            if (_isRealtime)
            {
                PreviewSelection(_host.SelectedArea);
            }
        }

        /// <summary>Commits the current selection to the editor.</summary>
        public void End()
        {
            try
            {
                if (_host.IsSelecting)
                {
                    // A Replace selection (e.g. clicking empty canvas) also clears the connection
                    // selection, so a selected connection is deselected when the user clicks elsewhere.
                    if (_selectionType == SelectionType.Replace)
                    {
                        _host.UnselectAllConnections();
                    }

                    PreviewSelection(_host.SelectedArea);

                    // IsSelecting flips to false BEFORE the preview is committed, so
                    // ItemContainer.OnSelectedChanged (gated on !Editor.IsSelecting) raises
                    // IsSelectedChangedEvent for each container the commit selects/deselects.
                    _host.IsSelecting = false;
                    _host.ApplyPreviewingSelection();
                }
            }
            finally
            {
                ReleaseSnapshots();
            }
        }

        /// <summary>Aborts the current selection.</summary>
        public void Abort()
        {
            try
            {
                if (_host.IsSelecting)
                {
                    _host.ClearPreviewingSelection();
                    _host.IsSelecting = false;
                }
            }
            finally
            {
                ReleaseSnapshots();
            }
        }

        private void PreviewSelection(Rect area)
        {
            switch (_selectionType)
            {
                case SelectionType.Replace:
                    PreviewSelectArea(area);
                    break;

                case SelectionType.Remove:
                    PreviewSelectContainers(_initialSelection);

                    PreviewUnselectArea(area);
                    break;

                case SelectionType.Append:
                    PreviewUnselectAll();
                    PreviewSelectContainers(_initialSelection);

                    PreviewSelectArea(area, true);
                    break;

                case SelectionType.Invert:
                    PreviewUnselectAll();
                    PreviewSelectContainers(_initialSelection);

                    PreviewInvertSelection(area);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(SelectionType));
            }
        }

        private void PreviewUnselectAll()
        {
            for (var i = 0; i < _selectableContainers.Count; i++)
            {
                _selectableContainers[i].IsPreviewingSelection = false;
            }
        }

        private void PreviewSelectArea(Rect area, bool append = false, bool fit = false)
        {
            if (!append)
            {
                PreviewUnselectAll();
            }

            if (area.X != 0 || area.Y != 0 || area.Width > 0 || area.Height > 0)
            {
                for (var i = 0; i < _selectableContainers.Count; i++)
                {
                    var container = _selectableContainers[i];
                    if (container.IsSelectableInArea(area, fit))
                    {
                        container.IsPreviewingSelection = true;
                    }
                }
            }
        }

        private void PreviewUnselectArea(Rect area, bool fit = false)
        {
            for (var i = 0; i < _selectableContainers.Count; i++)
            {
                var container = _selectableContainers[i];
                if (container.IsSelectableInArea(area, fit))
                {
                    container.IsPreviewingSelection = false;
                }
            }
        }

        private void PreviewSelectContainers(IReadOnlyList<ItemContainer> containers)
        {
            for (var i = 0; i < containers.Count; i++)
            {
                containers[i].IsPreviewingSelection = true;
            }
        }

        private void PreviewInvertSelection(Rect area, bool fit = false)
        {
            for (var i = 0; i < _selectableContainers.Count; i++)
            {
                var container = _selectableContainers[i];
                if (container.IsSelectableInArea(area, fit))
                {
                    container.IsPreviewingSelection = !container.IsPreviewingSelection;
                }
            }
        }

        private IReadOnlyList<ItemContainer> GetSelectableContainers()
        {
            var items = (IList)_host.Items;
            var containers = new List<ItemContainer>(items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                if (_host.ContainerFromIndex(i) is ItemContainer { IsSelectable: true } container)
                {
                    containers.Add(container);
                }
            }

            return containers;
        }

        private void ReleaseSnapshots()
        {
            _selectableContainers = Array.Empty<ItemContainer>();
            _initialSelection = Array.Empty<ItemContainer>();
        }
    }
}
