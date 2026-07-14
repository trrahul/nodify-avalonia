using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Playground.ViewModels;
using Nodify.Avalonia.Playground.Views;

namespace Nodify.Avalonia.Playground
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}