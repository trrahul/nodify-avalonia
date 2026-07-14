using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Settings
{
    public partial class ApplicationSettingsWindow : Window
    {
        public ApplicationSettingsWindow()
        {
            InitializeComponent();
            Opened += (_, _) => WindowChromeInterop.HideDrawnTitleText(this);
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // The header strip is the window's caption: WindowDecorationProperties.ElementRole="TitleBar"
        // provides the native drag/maximize behavior in the extended client area.
    }
}
