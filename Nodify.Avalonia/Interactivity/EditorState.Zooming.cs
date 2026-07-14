using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class EditorState
    {
        /// <summary>
        /// Handles zooming of the <see cref="NodifyEditor"/> viewport via the pointer wheel.
        /// </summary>
        public class Zooming : InputElementState<NodifyEditor>
        {
            /// <summary>Initializes a new instance of <see cref="Zooming"/>.</summary>
            public Zooming(NodifyEditor editor) : base(editor)
            {
            }

            /// <inheritdoc />
            protected override void OnMouseWheel(PointerWheelEventArgs e)
            {
                if (Element.ActualGestures.Editor.ZoomModifierKey == e.KeyModifiers && Element.IsZoomingAllowed())
                {
                    double delta = e.Delta.Y;
                    if (delta == 0d)
                    {
                        return;
                    }

                    double zoom = System.Math.Pow(2.0, delta / 3.0);
                    Element.ZoomAtPosition(zoom, e.GetPosition(Element.ItemsHost));

                    if (e.Source is NodifyEditor
                        || (e.Source is Visual v && ReferenceEquals(v.FindAncestorOfType<NodifyEditor>(), Element)))
                    {
                        e.Handled = true;
                    }
                }
            }
        }
    }
}
