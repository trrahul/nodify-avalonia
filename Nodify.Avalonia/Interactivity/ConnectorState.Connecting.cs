using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Interactivity
{
    public static partial class ConnectorState
    {
        /// <summary>
        /// Represents the state for handling a connector's connecting operation in the editor,
        /// enabling drag-based creation of connections between connectors.
        /// </summary>
        /// <remarks>
        /// Toggled (two-step) connecting is driven by <see cref="Connector.EnableStickyConnections"/>.
        /// </remarks>
        public class Connecting : DragState<Connector>
        {
            protected override bool HasContextMenu => Element.HasContextMenu;
            protected override bool CanCancel => Connector.AllowPendingConnectionCancellation;
            protected override bool IsToggle => Connector.EnableStickyConnections;

            protected override InputGesture DragGesture => Element.ActualGestures.Connector.Connect;
            protected override InputGesture? CancelGesture => Element.ActualGestures.Connector.CancelAction;

            /// <summary>
            /// Initializes a new instance of the <see cref="Connecting"/> class.
            /// </summary>
            /// <param name="connector">The connector associated with this state.</param>
            public Connecting(Connector connector) : base(connector)
            {
                PositionElement = Element.Editor ?? (Visual)Element;
            }

            protected override void OnBegin(RoutedEventArgs e)
                => Element.BeginConnecting();

            protected override void OnEnd(RoutedEventArgs e)
                => Element.EndConnecting();

            protected override void OnCancel(RoutedEventArgs e)
                => Element.CancelConnecting();

            protected override void OnMouseMove(PointerEventArgs e)
            {
                Point editorPosition = Element.GetLocationInsideEditor(e);
                Element.UpdatePendingConnection(editorPosition);
            }
        }
    }
}
