using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Represents an abstract base class for managing drag interactions within a UI element.
    /// Provides a framework for handling input gestures such as starting, canceling, and completing drag interactions.
    /// </summary>
    /// <typeparam name="TElement">The type of <see cref="Control"/> that owns the state.</typeparam>
    public abstract class DragState<TElement> : InputElementState<TElement>, IInputHandler
        where TElement : Control
    {
        private enum InteractionState
        {
            Ready,
            InProgress,
            Ending
        }

        /// <summary>
        /// Gets the gesture used to cancel the drag interaction, if defined.
        /// </summary>
        protected virtual InputGesture? CancelGesture { get; }

        /// <summary>
        /// Gets the gesture used to begin and complete the drag interaction.
        /// </summary>
        protected abstract InputGesture DragGesture { get; }

        /// <summary>
        /// Indicates whether the element has a context menu associated with it.
        /// </summary>
        /// <remarks>This property is used to suppress the context menu when a drag interaction is performed using the right mouse button.</remarks>
        protected virtual bool HasContextMenu => Element.ContextMenu != null;

        /// <summary>
        /// Determines if the drag interaction can begin (see <see cref="OnBegin(RoutedEventArgs)"/>).
        /// </summary>
        protected virtual bool CanBegin { get; } = true;

        /// <summary>
        /// Determines if the drag interaction can be canceled (see <see cref="OnCancel(RoutedEventArgs)"/>).
        /// </summary>
        protected virtual bool CanCancel { get; } = true;

        /// <summary>
        /// Indicates if the drag gesture is a toggle, meaning the same gesture can be used to both start and stop the interaction.
        /// </summary>
        protected virtual bool IsToggle { get; }

        /// <summary>
        /// Gets or sets the UI element used to calculate the mouse position during the drag interaction.
        /// </summary>
        protected Visual PositionElement { get; set; }

        private InteractionState _interactionState;
        private Point _initialPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="DragState{TElement}"/> class with a begin gesture.
        /// </summary>
        /// <param name="element">The element associated with this state.</param>
        public DragState(TElement element) : base(element)
        {
            PositionElement = element;
        }

        void IInputHandler.HandleEvent(RoutedEventArgs e)
        {
            if (_interactionState == InteractionState.Ready && TryBeginDragging(e))
            {
                return;
            }

            if (_interactionState == InteractionState.Ending && TryEndDragging(e))
            {
                return;
            }

            if (_interactionState == InteractionState.InProgress)
            {
                if (TryEndDragging(e) || TryCancelDragging(e) || TrySuppressContextMenu(e))
                {
                    return;
                }
            }

            TryHandleEvent(e);
        }

        #region Interaction logic

        private bool TryBeginDragging(RoutedEventArgs e)
        {
            if (IsInputEventPressed(e) && CanBegin && DragGesture.Matches(e.Source, e))
            {
                BeginDrag(e);
                return true;
            }

            return false;
        }

        private bool TryEndDragging(RoutedEventArgs e)
        {
            if (IsInputCaptureLost(e))
            {
                EndDrag(e);
                return true;
            }

            if (IsToggle && _interactionState == InteractionState.InProgress)
            {
                return TryDeferToggleInteractionEnd(e);
            }

            return TryEndInteraction(e);
        }

        // Delay ending toggle interaction until the gesture is released
        private bool TryDeferToggleInteractionEnd(RoutedEventArgs e)
        {
            if (IsInputEventPressed(e) && DragGesture.Matches(e.Source, e))
            {
                _interactionState = InteractionState.Ending;
                HandleEvent(e);
                return true;
            }

            return false;
        }

        private bool TryEndInteraction(RoutedEventArgs e)
        {
            if (IsInputEventReleased(e) && DragGesture.Matches(e.Source, e))
            {
                EndDrag(e);
                return true;
            }

            return false;
        }

        private bool TryCancelDragging(RoutedEventArgs e)
        {
            if (CanCancel && IsInputEventReleased(e) && CancelGesture?.Matches(e.Source, e) is true)
            {
                CancelDrag(e);
                return true;
            }

            return false;
        }

        private bool TrySuppressContextMenu(RoutedEventArgs e)
        {
            if (IsToggle && IsRightMouseButton(e))
            {
                e.Handled = true;
                HandleEvent(e);
                return true;
            }

            return false;
        }

        private void TryHandleEvent(RoutedEventArgs e)
        {
            if (_interactionState == InteractionState.InProgress || _interactionState == InteractionState.Ending)
            {
                HandleEvent(e);
            }
        }

        internal void BeginDrag(RoutedEventArgs e)
        {
            // Avoid stealing mouse capture from other elements
            if (CanCaptureInput(e))
            {
                RequiresInputCapture = IsToggle;

                _interactionState = InteractionState.InProgress;
                _initialPosition = GetInitialPosition(e);

                HandleEvent(e); // Handle the event, otherwise CaptureMouse will send a MouseMove event and the current event will be handled out of order
                OnBegin(e);

                e.Handled = true;

                Element.Focus();
                CaptureInput(e);
            }
        }

        private void EndDrag(RoutedEventArgs e)
        {
            _interactionState = InteractionState.Ready;
            HandleEvent(e);

            // Suppress the context menu if the mouse moved beyond the defined drag threshold
            if (HasContextMenu && e is PointerReleasedEventArgs pre && pre.InitialPressMouseButton == MouseButton.Right)
            {
                double dragThreshold = NodifyEditor.MouseActionSuppressionThreshold * NodifyEditor.MouseActionSuppressionThreshold;
                double dragDistance = pre.GetPosition(PositionElement).VectorSubtract(_initialPosition).SquaredLength;

                if (dragDistance > dragThreshold)
                {
                    OnEnd(e);
                    e.Handled = true;
                }
                else
                {
                    OnCancel(e);
                }
            }
            else
            {
                OnEnd(e);
                e.Handled = true;
            }

            RequiresInputCapture = false;
        }

        private void CancelDrag(RoutedEventArgs e)
        {
            _interactionState = InteractionState.Ready;
            HandleEvent(e);
            OnCancel(e);

            e.Handled = true;
            RequiresInputCapture = false;
        }

        #endregion

        /// <summary>
        /// Retrieves the initial position of the input event relative to the <see cref="PositionElement"/>.
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> representing the input event.</param>
        /// <remarks>
        /// This position is used to calculate the drag distance, to determine whether
        /// the context menu can appear or if the action is considered a drag operation. The behavior is influenced
        /// by the <see cref="NodifyEditor.MouseActionSuppressionThreshold"/>.
        /// </remarks>
        protected virtual Point GetInitialPosition(RoutedEventArgs e)
        {
            if (e is PointerEventArgs pe)
            {
                return pe.GetPosition(PositionElement);
            }

            return default;
        }

        /// <summary>
        /// Determines whether input capture can be acquired for the <see cref="InputElementState{TElement}.Element" />.
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> representing the input event.</param>
        /// <remarks>Must return true if the input is already captured by the current element.</remarks>
        protected virtual bool CanCaptureInput(RoutedEventArgs e)
        {
            // Avoid stealing capture from other elements; non-pointer events (e.g. the
            // synthetic stack event) can always begin.
            if (e is PointerEventArgs pe)
                return pe.Pointer.Captured == null || this.Element.IsMouseCaptured(pe);
            return true;
        }

        /// <summary>
        /// Captures input for the element.
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> representing the input event.</param>
        protected virtual void CaptureInput(RoutedEventArgs e)
        {
            if (e is PointerEventArgs pe)
                this.Element.CaptureMouseSafe(pe);
        }

        /// <summary>
        /// Determines whether input capture has been lost.
        /// </summary>
        /// <param name="e">The <see cref="RoutedEventArgs"/> representing the input event.</param>
        protected virtual bool IsInputCaptureLost(RoutedEventArgs e)
            => e.RoutedEvent == InputElement.PointerCaptureLostEvent;

        /// <summary>
        /// Determines if the given input event represents the release of an input gesture.
        /// </summary>
        /// <param name="e">The input event to evaluate.</param>
        /// <returns>True if the event represents the release of a gesture; otherwise, false.</returns>
        protected virtual bool IsInputEventReleased(RoutedEventArgs e)
        {
            if (e is PointerReleasedEventArgs)
                return true;

            if (e is KeyEventArgs && e.RoutedEvent == InputElement.KeyUpEvent)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if the given input event represents the press of an input gesture.
        /// </summary>
        /// <param name="e">The input event to evaluate.</param>
        /// <returns>True if the event represents the press of a gesture; otherwise, false.</returns>
        protected virtual bool IsInputEventPressed(RoutedEventArgs e)
        {
            if (e is PointerPressedEventArgs)
                return true;

            if (e is KeyEventArgs && e.RoutedEvent == InputElement.KeyDownEvent)
                return true;

            return false;
        }

        private static bool IsRightMouseButton(RoutedEventArgs e)
        {
            if (e is PointerPressedEventArgs pp)
                return pp.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed;
            if (e is PointerReleasedEventArgs pr)
                return pr.InitialPressMouseButton == MouseButton.Right;
            return false;
        }

        /// <summary>
        /// Called when the drag interaction begins. Override to provide custom behavior.
        /// </summary>
        /// <param name="e">The input event that started the interaction.</param>
        protected virtual void OnBegin(RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the drag interaction ends. Override to provide custom behavior.
        /// </summary>
        /// <param name="e">The input event that ended the interaction.</param>
        protected virtual void OnEnd(RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Called when the drag interaction is canceled. Override to provide custom behavior.
        /// </summary>
        /// <param name="e">The input event that canceled the interaction.</param>
        protected virtual void OnCancel(RoutedEventArgs e)
        {
        }
    }
}
