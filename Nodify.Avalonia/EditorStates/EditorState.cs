using Avalonia;
using Avalonia.Input;

namespace Nodify.Avalonia.EditorStates
{
    /// <summary>The base class for editor states.</summary>
    public abstract class EditorState
    {
        /// <summary>Constructs a new <see cref="EditorState"/>.</summary>
        /// <param name="editor">The owner of the state.</param>
        public EditorState(NodifyEditor editor)
        {
            Editor = editor;
        }

        /// <summary>The owner of the state.</summary>
        protected NodifyEditor Editor { get; }

        /// <inheritdoc cref="NodifyEditor.OnMouseDown(MouseButtonEventArgs)"/>
        public virtual void HandlePointerPressed(PointerPressedEventArgs e)
        {
            CurrentPointerArgs = e;
        }



        /// <inheritdoc cref="NodifyEditor.OnMouseUp(MouseButtonEventArgs)"/>
        public virtual void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            CurrentPointerArgs = e;
        }

        /// <inheritdoc cref="NodifyEditor.OnMouseMove(MouseEventArgs)"/>
        public virtual void HandlePointerMove(PointerEventArgs e)
        {
            CurrentPointerArgs = e;
        }

        /// <inheritdoc cref="NodifyEditor.OnMouseWheel(MouseWheelEventArgs)"/>
        public virtual void HandlePointerWheel(PointerWheelEventArgs e)
        {
            CurrentPointerArgs = e;
        }

        /// <summary>Handles auto panning when mouse is outside the editor.</summary>
        /// <param name="e">The <see cref="MouseEventArgs"/> that contains the event data.</param>
        public virtual void HandleAutoPanning()
        {
            
        }

        /// <inheritdoc cref="NodifyEditor.OnKeyUp(KeyEventArgs)"/>
        public virtual void HandleKeyUp(KeyEventArgs e)
        {
            if (CurrentKey == e.Key)
            {
                CurrentKey = default;
            }

            CurrentKeyModifiers = CurrentKeyModifiers & ~(e.KeyModifiers);

        }

        public KeyModifiers CurrentKeyModifiers { get; private set; }

        public PointerEventArgs? CurrentPointerArgs { get; private set; }
        //public Point CurrentPointerPosition { get; private set; }

        public Key CurrentKey { get; private set; }

        /// <inheritdoc cref="NodifyEditor.OnKeyDown(KeyEventArgs)"/>
        public virtual void HandleKeyDown(KeyEventArgs e)
        {
            CurrentKey = e.Key;
            CurrentKeyModifiers = e.KeyModifiers;
        }

        /// <summary>Called when <see cref="NodifyEditor.PushState(EditorState)"/> is called.</summary>
        /// <param name="from">The state we enter from (is null for root state).</param>
        public virtual void Enter(EditorState? from)
        {
            if (from != null)
            {
                CurrentPointerArgs = from.CurrentPointerArgs;
            }
        }

        /// <summary>Called when <see cref="NodifyEditor.PopState"/> is called.</summary>
        public virtual void Exit() { }

        /// <summary>Called when <see cref="NodifyEditor.PopState"/> is called.</summary>
        /// <param name="from">The state we re-enter from.</param>
        public virtual void ReEnter(EditorState from) { }

        /// <summary>Pushes a new state into the stack.</summary>
        /// <param name="newState">The new state.</param>
        public virtual void PushState(EditorState newState) => Editor.PushState(newState);

        /// <summary>Pops the current state from the stack.</summary>
        public virtual void PopState() => Editor.PopState();
    }
}
