using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia;
using Nodify.Avalonia.Connections;

namespace Nodify.StateMachine
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Connector.EnableStickyConnections = true;
            NodifyEditor.EnableCuttingLinePreview = true;

            EditorGestures.Mappings.Connection.Disconnect.Unbind();
            EditorGestures.Mappings.Editor.ZoomModifierKey = KeyModifiers.Control;
            EditorGestures.Mappings.Editor.PanWithMouseWheel = true;

            AddHandler(GotFocusEvent, OnPreviewGotFocus, RoutingStrategies.Tunnel, handledEventsToo: true);

            PART_ScrollViewer.AddHandler(PointerWheelChangedEvent, ScrollViewer_PointerWheelChanged, RoutingStrategies.Tunnel);
            PART_ScrollViewer.AddHandler(KeyDownEvent, ScrollViewer_PreviewKeyDown, RoutingStrategies.Tunnel);
        }

        private void OnPreviewGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.Source is not null)
            {
                Title = e.Source.ToString();
            }
        }

        private void ScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers != KeyModifiers.Shift)
                return;

            var scrollViewer = (ScrollViewer)sender!;

            if (e.Delta.Y < 0)
            {
                scrollViewer.LineRight();
            }
            else
            {
                scrollViewer.LineLeft();
            }

            e.Handled = true;
        }

        private void ScrollViewer_PreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyModifiers != KeyModifiers.Shift)
                return;

            var scrollViewer = (ScrollViewer)sender!;

            if (e.Key == Key.PageUp)
            {
                scrollViewer.PageLeft();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                scrollViewer.PageRight();
                e.Handled = true;
            }
        }
    }
}
