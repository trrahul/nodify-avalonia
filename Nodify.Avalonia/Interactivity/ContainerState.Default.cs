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

                    if (!Element.IsSelectableLocation(e.GetPosition(Element)))
                    {
                        return;
                    }

                    var gestures = Element.ActualGestures.ItemContainer;

                    if (gestures.Drag.Matches(e.Source, e))
                    {
                        // Only arm the drag when we can take capture; a child Connector may already own the pointer.
                        _canBeDragging = Element.IsDraggable && CanCaptureMouse(e);
                    }

                    if (gestures.Selection.Select.Matches(e.Source, e))
                    {
                        _selectionType = gestures.Selection.GetSelectionType(e);
                    }
                    else if (e.GetPointerUpdateKind() == PointerUpdateKind.RightButtonPressed
                             && PreserveSelectionOnRightClick)
                    {
                        _selectionType = Element.IsSelected
                            ? SelectionHelper.SelectionType.Append
                            : SelectionHelper.SelectionType.Replace;
                    }

                    _initialPosition = Element.Editor.GetLocationInsideEditor(e);

                    if (_canBeDragging || _selectionType.HasValue)
                    {
                        e.Handled = true;
                        CaptureMouseSafe(e);
                    }
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
                    // Selection type was captured at press time; never re-derive it from the release event.
                    if (!_canceled && _selectionType.HasValue)
                    {
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
                    if (!_canBeDragging)
                        return;

                    // Require a minimum movement before dragging to avoid micro-jitter.
                    var currentPosition = Element.Editor.GetLocationInsideEditor(e);
                    double dragThreshold = NodifyEditor.MouseActionSuppressionThreshold
                                          * NodifyEditor.MouseActionSuppressionThreshold;
                    var displacement = currentPosition - _initialPosition;
                    double dragDistanceSq = displacement.X * displacement.X
                                           + displacement.Y * displacement.Y;

                    if (dragDistanceSq <= dragThreshold)
                        return;

                    // Select an unselected container before dragging; a Remove-modifier drag still selects it.
                    if (!Element.IsSelected)
                    {
                        var selectionType = GetSelectionTypeForDragging(_selectionType);
                        Element.Select(selectionType);
                    }

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
