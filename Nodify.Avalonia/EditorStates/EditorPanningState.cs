using Avalonia;
using Avalonia.Input;
using Nodify.Avalonia.Extensions;

namespace Nodify.Avalonia.EditorStates
{
    /// <summary>The panning state of the editor.</summary>
    public sealed class EditorPanningState : EditorState
    {
        private Point _initialMousePosition;
        private Point _previousMousePosition;
        private Point _currentPointerPosition;
        //private Point _currentMousePosition; //use

        /// <summary>Constructs an instance of the <see cref="EditorPanningState"/> state.</summary>
        /// <param name="editor">The owner of the state.</param>
        public EditorPanningState(NodifyEditor editor) : base(editor)
        {
        }

        /// <inheritdoc />
        public override void Exit()
            => Editor.IsPanning = false;

        /// <inheritdoc />
        public override void Enter(EditorState? from)
        {
            base.Enter(from);
            _currentPointerPosition = CurrentPointerArgs.GetPosition(Editor);
            _initialMousePosition = _currentPointerPosition;
            _previousMousePosition = _currentPointerPosition;
            Editor.IsPanning = true;
        }

        /// <inheritdoc />
        public override void HandlePointerMove(PointerEventArgs e)
        {
            base.HandlePointerMove(e);
            _currentPointerPosition = e.GetPosition(Editor);
            Editor.ViewportLocation -= (_currentPointerPosition - _previousMousePosition) / Editor.ViewportZoom;
            _previousMousePosition = _currentPointerPosition;
        }

        /// <inheritdoc />
        public override void HandlePointerReleased(PointerReleasedEventArgs e)
        {
            base.HandlePointerReleased(e);
            if (EditorGestures.Pan.Matches(e.Source, e))
            {
                // Handle right click if panning and moved the mouse more than threshold so context menu doesn't open
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    double contextMenuTreshold = NodifyEditor.HandleRightClickAfterPanningThreshold * NodifyEditor.HandleRightClickAfterPanningThreshold;
                    if (_currentPointerPosition.VectorSubtract(_initialMousePosition).SquaredLength > contextMenuTreshold)
                    {
                        e.Handled = true;
                    }
                }

                PopState();
            }
            else if (EditorGestures.Select.Matches(e.Source, e) && Editor.IsSelecting)
            {
                PopState();
                // Cancel selection and continue panning
                if (Editor.State is EditorSelectingState && !Editor.DisablePanning)
                {
                    PopState();
                    PushState(new EditorPanningState(Editor));
                }
            }
        }
    }
}
