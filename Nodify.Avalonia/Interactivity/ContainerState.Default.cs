using Avalonia;
using Avalonia.Input;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class ContainerState
    {
        /// <summary>
        /// The default (selecting) state for an <see cref="ItemContainer"/>.
        /// Wraps the nested <see cref="SelectingState"/>.
        /// </summary>
        public sealed class Default : InputElementStateStack<ItemContainer>
        {
            /// <summary>Initialises the stack and pushes the initial selecting state.</summary>
            public Default(ItemContainer container) : base(container)
            {
                PushState(new SelectingState(this));
            }

            private sealed class SelectingState : InputElementState
            {
                private bool _canBeDragging;
                private bool _canceled;

                private SelectionHelper.SelectionType? _selectionType;

                private Point _initialPosition;

                public SelectingState(InputElementStateStack<ItemContainer> stack) : base(stack)
                {
                }

                private bool PreserveSelectionOnRightClick
                    => Element.HasContextMenu || ItemContainer.PreserveSelectionOnRightClick;

                public override void Enter(IInputElementState? from)
                {
                    if (from is Dragging drag)
                    {
                        Element.IsSelected = true;
                        _canceled = drag.Canceled;
                    }

                    _canBeDragging = false;
                    _selectionType = null;
                }

                protected override void OnMouseDown(PointerPressedEventArgs e)
                {
                    _canceled = false;
                    _selectionType = null;
                    _canBeDragging = false;

                    // Ignore presses outside the selectable area so the container neither
                    // captures nor selects.
                    if (!Element.IsSelectableLocation(e.GetPosition(Element)))
                    {
                        return;
                    }

                    var gestures = Element.ActualGestures.ItemContainer;

                    if (gestures.Drag.Matches(e.Source, e))
                    {
                        // Dragging requires mouse capture, so only arm the drag when we can
                        // actually take capture. Without the CanCaptureMouse guard, pressing a
                        // child Connector (which already captured the pointer to start a connection)
                        // would still arm the container's drag, so the node would move while
                        // dragging a connector.
                        _canBeDragging = Element.IsDraggable && CanCaptureMouse(e);
                    }

                    // Determine the selection type from the pressed modifiers so we can use
                    // it at drag-time (GetSelectionTypeForDragging) and at click-time (OnMouseUp).
                    if (gestures.Selection.Select.Matches(e.Source, e))
                    {
                        _selectionType = gestures.Selection.GetSelectionType(e);
                    }
                    else if (e.GetPointerUpdateKind() == PointerUpdateKind.RightButtonPressed
                             && PreserveSelectionOnRightClick)
                    {
                        // Right-click on selected → Append (keep selection for context menu);
                        // right-click on unselected → Replace.
                        _selectionType = Element.IsSelected
                            ? SelectionHelper.SelectionType.Append
                            : SelectionHelper.SelectionType.Replace;
                    }

                    // Captured pointer events bubble through the container before the editor updates
                    // MouseLocation, so use this event's graph-space position directly.
                    _initialPosition = Element.Editor.GetLocationInsideEditor(e);

                    // Only take focus + capture when we will actually drag or select, and only if
                    // nothing else owns the pointer (CanCaptureMouse). Mark the event handled so the
                    // editor underneath does not also start panning/selecting.
                    if (_canBeDragging || _selectionType.HasValue)
                    {
                        e.Handled = true;
                        CaptureMouseSafe(e);
                    }

                    DragTrace.Log($"  SelectingState.OnMouseDown: selLoc={Element.IsSelectableLocation(e.GetPosition(Element))} dragMatch={gestures.Drag.Matches(e.Source, e)} IsDraggable={Element.IsDraggable} canCapture={CanCaptureMouse(e)} _canBeDragging={_canBeDragging} _selectionType={_selectionType} initPos={_initialPosition} ML={Element.Editor.MouseLocation} captured={e.Pointer.Captured?.GetType().Name ?? "null"}");
                }

                private bool CanCaptureMouse(PointerEventArgs e)
                    => e.Pointer.Captured == null || Element.IsMouseCaptured(e);

                private void CaptureMouseSafe(PointerEventArgs e)
                {
                    if (CanCaptureMouse(e))
                    {
                        Element.Focus();
                        Element.CaptureMouseSafe(e);
                    }
                }

                protected override void OnMouseUp(PointerReleasedEventArgs e)
                {
                    // Only act on selection when a selection/drag was actually armed at press-time
                    // (i.e. the press was inside IsSelectableLocation and matched a selection gesture,
                    // so _selectionType was set). Without this guard, a press+release that lands on the
                    // container but OUTSIDE the selectable area (where OnMouseDown early-returned) would
                    // still re-match the selection gesture against the RELEASE event and wrongly select
                    // the node / clear the rest.
                    //
                    // The selection TYPE itself is never re-derived from the release event: it was
                    // captured at press time into _selectionType. Re-matching Append/Invert/Remove
                    // gestures against the release, combined with PointerGesture's release-modifier
                    // bypass, would mean ANY gesture matches ANY release, so a plain click would
                    // re-match Append and append to the selection instead of replacing it.
                    if (!_canceled && _selectionType.HasValue)
                    {
                        // Right-click participates in selection only when allowed (context menu /
                        // global opt-in). An already-selected node keeps the whole selection so the
                        // context menu applies to it.
                        bool isContextMenuRightClick = e.GetPointerUpdateKind() == PointerUpdateKind.RightButtonReleased
                                                        && Element.IsSelected
                                                        && PreserveSelectionOnRightClick;
                        if (!isContextMenuRightClick)
                        {
                            Element.Select(_selectionType.Value);
                        }

                        _canBeDragging = false;
                    }

                    _canceled = false;
                }

                protected override void OnMouseMove(PointerEventArgs e)
                {
                    DragTrace.Log($"  SelectingState.OnMouseMove: _canBeDragging={_canBeDragging} ML={Element.Editor.MouseLocation} initPos={_initialPosition} ePos={e.GetPosition(Element.Editor.ItemsHost)}");
                    if (!_canBeDragging)
                        return;

                    // Enforce a minimum movement threshold before entering the dragging state,
                    // preventing micro-jitter from starting a drag.
                    //
                    // Use the current event's graph-space position instead of Editor.MouseLocation:
                    // the editor updates that property after the child container handles bubbled
                    // pointer events, which made drag threshold checks lag or no-op.
                    var currentPosition = Element.Editor.GetLocationInsideEditor(e);
                    double dragThreshold = NodifyEditor.MouseActionSuppressionThreshold
                                          * NodifyEditor.MouseActionSuppressionThreshold;
                    var displacement = currentPosition - _initialPosition;
                    double dragDistanceSq = displacement.X * displacement.X
                                           + displacement.Y * displacement.Y;

                    if (dragDistanceSq <= dragThreshold)
                        return;

                    // Apply the drag-time selection rule before entering Dragging. An unselected
                    // container is selected using GetSelectionTypeForDragging so that a
                    // Remove-modifier drag still selects (never removes) the dragged item.
                    if (!Element.IsSelected)
                    {
                        var selectionType = GetSelectionTypeForDragging(_selectionType);
                        Element.Select(selectionType);
                    }

                    DragTrace.Log($"  SelectingState.OnMouseMove: THRESHOLD CROSSED distSq={dragDistanceSq} -> PushState(Dragging)");
                    PushState(new Dragging(Stack));
                }

                private static SelectionHelper.SelectionType GetSelectionTypeForDragging(
                    SelectionHelper.SelectionType? selectionType)
                    => selectionType == SelectionHelper.SelectionType.Remove
                        ? SelectionHelper.SelectionType.Replace
                        : selectionType.GetValueOrDefault(SelectionHelper.SelectionType.Replace);
            }
        }
    }
}
