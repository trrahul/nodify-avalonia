using Avalonia;
using Avalonia.Controls;

namespace Nodify.Avalonia.Calculator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            //this.AttachDevTools();
#endif
        }
    }
}