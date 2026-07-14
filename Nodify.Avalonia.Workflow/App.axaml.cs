using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Workflow.Shell;

namespace Nodify.Avalonia.Workflow
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            // The main workflow renders its connections through an explicit LineConnection
            // template, so the auto-registered connections layer is disabled.
            NodifyEditor.AutoRegisterConnectionsLayer = false;

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new ApplicationViewModel()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
