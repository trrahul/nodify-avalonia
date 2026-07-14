using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Interactivity
{
    public partial class InputElementStateStack<TElement> where TElement : Control
    {
        /// <summary>
        /// Represents a specialized state for handling drag interactions.
        /// </summary>
        public abstract class DragState : DragState<TElement>, IInputElementState, IInputHandler
        {
            /// <summary>
            /// Gets the state stack managing this state.
            /// </summary>
            public InputElementStateStack<TElement> Stack { get; }

            // A plain RoutedEventArgs is used here instead of a synthesized PointerEventArgs. All DragState
            // code paths that inspect the event (CanCaptureInput, GetInitialPosition, CaptureInput,
            // IsInputEvent*) gracefully no-op on a non-pointer event, while HandleEvent dispatch sees an
            // unrecognized RoutedEvent and only runs OnEvent.
            private readonly RoutedEventArgs _mouseEventArgs = new RoutedEventArgs(NodifyEditor.ViewportUpdatedEvent);

            /// <summary>
            /// Initializes a new instance of the <see cref="DragState"/> class.
            /// </summary>
            /// <param name="stack">The state stack managing this state.</param>
            public DragState(InputElementStateStack<TElement> stack) : base(stack.Element)
            {
                PositionElement = stack.Element;
                Stack = stack;
            }

            public void Enter(IInputElementState? from)
                => BeginDrag(_mouseEventArgs);

            public void Exit()
            {
            }

            /// <summary>
            /// Pushes a new state onto the stack.
            /// </summary>
            /// <param name="newState">The new state to push.</param>
            public void PushState(IInputElementState newState)
                => Stack.PushState(newState);

            /// <summary>
            /// Pops the current state from the stack.
            /// </summary>
            public void PopState()
                => Stack.PopState();

            protected override void OnCancel(RoutedEventArgs e)
                => PopState();

            protected override void OnEnd(RoutedEventArgs e)
                => PopState();
        }
    }
}
