using Avalonia.Input;
using Nodify.Avalonia.Helpers;
using static Nodify.Avalonia.Helpers.SelectionHelper;

namespace Nodify.Avalonia.EditorStates
{
    /// <summary>The selecting state of the editor.</summary>
    public sealed class EditorSelectingState : EditorState
    {
        private readonly SelectionType _type;
        private bool _canceled;

        /// <summary>The selection helper.</summary>
        protected SelectionHelper Selection { get; }

        /// <summary>Constructs an instance of the <see cref="EditorSelectingState"/> state.</summary>
        /// <param name="editor">The owner of the state.</param>
        public EditorSelectingState(NodifyEditor editor, SelectionType type) : base(editor)
        {
            Selection = new SelectionHelper(editor);
            _type = type;
        }

        /// <inheritdoc />
        public override void Enter(EditorState? from)
        {
            base.Enter(from);
            _canceled = false;
            Selection.Start(Editor.MouseLocation, _type);
        }

        /// <inheritdoc />
        public override void Exit()
        {
            if (_canceled)
            {
                Selection.Abort();
            }
            else
            {
                Selection.End();
            }
        }

        /// <inheritdoc />
        public override void HandlePointerMove(PointerEventArgs e)
        {
            base.HandlePointerMove(e);
            Selection.Update(Editor.MouseLocation);
        }

        /// <inheritdoc />
        public override void HandlePointerPressed(PointerPressedEventArgs e)
        {
            base.HandlePointerPressed(e);
            if (!Editor.DisablePanning && EditorGestures.Pan.Matches(e.Source, e))
            {
                PushState(new EditorPanningState(Editor));
            }
        }

        /// <inheritdoc />

        public override void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            base.HandlePointerReleased(e);
            bool canCancel = EditorGestures.Selection.Cancel.Matches(e.Source, e);
            bool canComplete = EditorGestures.Select.Matches(e.Source, e);
            if (canCancel || canComplete)
            {
                _canceled = !canComplete && canCancel;
                PopState();
            }
        }

        /// <inheritdoc />
        public override void HandleAutoPanning()
        {
            HandlePointerMove(CurrentPointerArgs);
        }

        public override void HandleKeyUp(KeyEventArgs e)
        {
            base.HandleKeyUp(e);
            if (EditorGestures.Selection.Cancel.Matches(e.Source, e))
            {
                _canceled = true;
                PopState();
            }
        }
    }
}
