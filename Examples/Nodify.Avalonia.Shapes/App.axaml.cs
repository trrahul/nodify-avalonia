using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia.Shapes
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            NodifyEditor.EnableDraggingContainersOptimizations = false;
            NodifyEditor.EnableCuttingLinePreview = true;

            EditorGestures.Mappings.Connection.Disconnect.Value = new AnyGesture(
                new PointerGesture(MouseAction.LeftClick, KeyModifiers.Alt),
                new PointerGesture(MouseAction.RightClick));

            EditorGestures.Mappings.Editor.Pan.Value = new AnyGesture(
                EditorGestures.Mappings.Editor.Pan.Value,
                new PointerGesture(MouseAction.LeftClick, Key.Space));

            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
