using Avalonia;

namespace Nodify.Avalonia.Shared
{
    /// <summary>
    /// Captures a data context as a resource so detached visuals (e.g. context menus in
    /// item container themes) can bind to it.
    /// </summary>
    public class BindingProxy : AvaloniaObject
    {
        public static readonly StyledProperty<object?> DataContextProperty =
            AvaloniaProperty.Register<BindingProxy, object?>(nameof(DataContext));

        public object? DataContext
        {
            get => GetValue(DataContextProperty);
            set => SetValue(DataContextProperty, value);
        }
    }
}
