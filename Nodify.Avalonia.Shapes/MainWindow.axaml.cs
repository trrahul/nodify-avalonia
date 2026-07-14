using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Shapes
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            PendingConnection.HotKeysDisplayMode = HotKeysDisplayMode.All;

            AddHandler(GotFocusEvent, OnPreviewGotFocus, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

        private void OnPreviewGotFocus(object? sender, FocusChangedEventArgs e)
        {
            if (e.Source is not null)
            {
                Title = e.Source.ToString();
            }
        }
    }
}
