using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Represents a base class for handling input events in a specific state for a framework element.
    /// </summary>
    /// <typeparam name="TElement">The type of the framework element that owns this state.</typeparam>
    public abstract class InputElementState<TElement> : IInputHandler
        where TElement : Control
    {
        /// <summary>
        /// Gets the owner of the state.
        /// </summary>
        protected TElement Element { get; }

        public bool RequiresInputCapture { get; protected set; }
        public bool ProcessHandledEvents { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputElementState{TElement}"/> class.
        /// </summary>
        /// <param name="element">The framework element that owns this state.</param>
        protected InputElementState(TElement element)
        {
            Element = element;
        }

        /// <inheritdoc cref="InputElement.PointerPressedEvent"/>
        protected virtual void OnMouseDown(PointerPressedEventArgs e) { }

        /// <inheritdoc cref="InputElement.PointerReleasedEvent"/>
        protected virtual void OnMouseUp(PointerReleasedEventArgs e) { }

        /// <inheritdoc cref="InputElement.PointerMovedEvent"/>
        protected virtual void OnMouseMove(PointerEventArgs e) { }

        /// <inheritdoc cref="InputElement.PointerWheelChangedEvent"/>
        protected virtual void OnMouseWheel(PointerWheelEventArgs e) { }

        /// <inheritdoc cref="InputElement.KeyUpEvent"/>
        protected virtual void OnKeyUp(KeyEventArgs e) { }

        /// <inheritdoc cref="InputElement.KeyDownEvent"/>
        protected virtual void OnKeyDown(KeyEventArgs e) { }

        /// <inheritdoc cref="InputElement.PointerCaptureLostEvent"/>
        protected virtual void OnLostMouseCapture(PointerCaptureLostEventArgs e) { }

        /// <summary>
        /// Called for any input event that is not explicitly handled by other methods.
        /// </summary>
        /// <param name="e">The input event arguments.</param>
        protected virtual void OnEvent(RoutedEventArgs e) { }

        /// <summary>
        /// Processes the input event by invoking the appropriate handler method based on the routed event.
        /// </summary>
        /// <param name="e">The input event arguments.</param>
        public void HandleEvent(RoutedEventArgs e)
        {
            if (e.RoutedEvent == InputElement.PointerMovedEvent)
            {
                OnMouseMove((PointerEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.PointerPressedEvent)
            {
                OnMouseDown((PointerPressedEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.PointerReleasedEvent)
            {
                OnMouseUp((PointerReleasedEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.PointerWheelChangedEvent)
            {
                OnMouseWheel((PointerWheelEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.PointerCaptureLostEvent)
            {
                OnLostMouseCapture((PointerCaptureLostEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.KeyDownEvent)
            {
                OnKeyDown((KeyEventArgs)e);
            }
            else if (e.RoutedEvent == InputElement.KeyUpEvent)
            {
                OnKeyUp((KeyEventArgs)e);
            }

            OnEvent(e);
        }
    }
}
