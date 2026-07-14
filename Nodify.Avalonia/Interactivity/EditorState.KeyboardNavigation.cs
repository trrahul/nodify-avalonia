using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Nodes;
using static Nodify.Avalonia.Helpers.SelectionHelper;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Represents the keyboard navigation state of the <see cref="NodifyEditor"/>, allowing users to
        /// navigate and interact with nodes and connections using the keyboard.
        /// </summary>
        /// <remarks>
        /// Supported gestures:
        ///   - Arrow keys move focus between nodes (NavigateSelection).
        ///   - Space+Arrow pans the viewport (Pan).
        ///   - Ctrl+Arrow moves selected nodes (DragSelection).
        ///   - Space / Enter toggles selection on the focused node (ToggleSelected).
        ///   - Escape deselects all nodes (Nodes layer) or all connections (Connections layer).
        ///   - Ctrl+] / Ctrl+[ cycles navigation layers (Nodes↔Connections).
        ///   - Ctrl+Tab focuses the parent container when focused inside a nested element.
        ///   - Ctrl+Space / Ctrl+Enter toggles group content selection on a selected GroupingNode.
        /// </remarks>
        public class KeyboardNavigation : InputElementState<NodifyEditor>
        {
            /// <summary>Initializes a new instance of <see cref="KeyboardNavigation"/>.</summary>
            public KeyboardNavigation(NodifyEditor element) : base(element)
            {
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (!Element.IsKeyboardFocusWithin || e.Source is not Visual originalSource)
                {
                    return;
                }

                double navigationStepSize = GetNavigationStepSize();
                var gestures = Element.ActualGestures.Editor.Keyboard;

                if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control))
                {
                    var parentContainer = FindNavigationTriggerAncestor(originalSource);
                    e.Handled = parentContainer?.Focus() is true;
                }
                else if (Element.IsNavigationTrigger(e.Source))
                {
                    if (gestures.Pan.TryGetNavigationDirection(e, out var panDirection))
                    {
                        var panning = new Vector(-panDirection.X * navigationStepSize, panDirection.Y * navigationStepSize);
                        Element.UpdatePanning(panning);
                        e.Handled = true;
                    }
                    else if (CanDragSelection() && gestures.DragSelection.TryGetNavigationDirection(e, out var dragDirection))
                    {
                        var dragging = new Vector(dragDirection.X * navigationStepSize, -dragDirection.Y * navigationStepSize);
                        Element.BeginDragging();
                        Element.UpdateDragging(dragging);
                        Element.EndDragging();

                        if (NodifyEditor.PanViewportOnKeyboardDrag)
                        {
                            var panning = new Vector(-dragDirection.X * navigationStepSize, dragDirection.Y * navigationStepSize);
                            Element.UpdatePanning(panning);
                        }

                        e.Handled = true;
                    }
                    else if (gestures.NavigateSelection.TryGetFocusDirection(e, out var direction))
                    {
                        Element.MoveFocus(direction);
                        e.Handled = true;
                    }
                }
            }

            protected override void OnKeyUp(KeyEventArgs e)
            {
                var gestures = Element.ActualGestures.Editor.Keyboard;

                if (gestures.ToggleSelected.Matches(e.Source, e))
                {
                    if (FocusedElement() is ItemContainer itemContainer)
                    {
                        itemContainer.Select(SelectionType.Invert);
                        if (NodifyEditor.AutoPanOnNodeFocus)
                        {
                            Element.BringIntoView(((IKeyboardFocusTarget<ItemContainer>)itemContainer).Bounds, NodifyEditor.BringIntoViewEdgeOffset);
                        }
                    }
                    else if (FocusedElement() is ConnectionContainer connectionContainer)
                    {
                        connectionContainer.Select(SelectionType.Invert);
                        if (NodifyEditor.AutoPanOnNodeFocus)
                        {
                            Element.BringIntoView(((IKeyboardFocusTarget<ConnectionContainer>)connectionContainer).Bounds, NodifyEditor.BringIntoViewEdgeOffset);
                        }
                    }

                    e.Handled = true;
                }
                else if (gestures.DeselectAll.Matches(e.Source, e))
                {
                    if (Element.SelectedContainersCount > 0 && Element.ActiveNavigationLayer?.Id == KeyboardNavigationLayerId.Nodes)
                    {
                        Element.UnselectAll();
                        e.Handled = true;
                    }
                    else if (Element.SelectedConnectionsCount > 0 && Element.ActiveNavigationLayer?.Id == KeyboardNavigationLayerId.Connections)
                    {
                        Element.UnselectAllConnections();
                        e.Handled = true;
                    }
                }
                else if (gestures.NextNavigationLayer.Matches(e.Source, e))
                {
                    Element.ActivateNextNavigationLayer();
                    e.Handled = true;
                }
                else if (gestures.PrevNavigationLayer.Matches(e.Source, e))
                {
                    Element.ActivatePreviousNavigationLayer();
                    e.Handled = true;
                }
                else if (FocusedElement() is ItemContainer { IsSelected: true } container
                    && Element.ActualGestures.GroupingNode.ToggleContentSelection.Matches(e.Source, e))
                {
                    var groupingNode = container.FindDescendantOfType<GroupingNode>();
                    if (groupingNode != null)
                    {
                        groupingNode.ToggleContentSelection();
                        e.Handled = true;
                    }
                }
            }

            private bool CanDragSelection()
                => Element.ActiveNavigationLayer?.Id == KeyboardNavigationLayerId.Nodes
                   && Element.SelectedContainersCount > 0;

            private double GetNavigationStepSize()
            {
                double cellSize = Element.GridCellSize;

                if (cellSize >= NodifyEditor.MinimumNavigationStepSize)
                    return cellSize;

                int factor = (int)Math.Ceiling(NodifyEditor.MinimumNavigationStepSize / cellSize);
                return factor * cellSize / Element.ViewportZoom;
            }

            private Control? FocusedElement()
                => TopLevel.GetTopLevel(Element)?.FocusManager?.GetFocusedElement() as Control;

            private Control? FindNavigationTriggerAncestor(Visual source)
            {
                Visual? current = source.GetVisualParent();
                while (current != null)
                {
                    if (current is Control ctrl && Element.IsNavigationTrigger(ctrl))
                        return ctrl;
                    current = current.GetVisualParent();
                }
                return null;
            }
        }
    }
}
