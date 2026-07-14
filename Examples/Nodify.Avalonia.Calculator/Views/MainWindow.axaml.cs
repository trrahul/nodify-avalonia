using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia.Calculator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EditorGestures.Mappings.Editor.Cutting.Unbind();

            AddHandler(GotFocusEvent, OnAnyGotFocus, RoutingStrategies.Bubble, handledEventsToo: true);
#if DEBUG
            //this.AttachDevTools();
#endif
        }

        private void OnAnyGotFocus(object? sender, FocusChangedEventArgs e)
        {
            Title = e.Source?.ToString() ?? nameof(MainWindow);
        }
    }
}