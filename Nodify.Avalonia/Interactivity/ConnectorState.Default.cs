using Avalonia.Input;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class ConnectorState
    {
        /// <summary>
        /// Represents the default input state for a <see cref="Connector"/>.
        /// </summary>
        public class Default : InputElementState<Connector>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Default"/> class.
            /// </summary>
            /// <param name="connector">The <see cref="Connector"/> element associated with this state.</param>
            public Default(Connector connector) : base(connector)
            {
            }

            protected override void OnMouseDown(PointerPressedEventArgs e)
            {
                if (e.GetCurrentPoint(null).Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed
                    && Element.HasContextMenu)
                {
                    Element.Focus();
                    e.Handled = true;   // prevents the editor capturing the mouse
                }
            }
        }
    }
}
