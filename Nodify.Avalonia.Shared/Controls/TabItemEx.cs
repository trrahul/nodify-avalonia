using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace Nodify.Avalonia.Shared.Controls
{
    public class TabItemEx : TabItem
    {
        public static readonly StyledProperty<ICommand?> CloseTabCommandProperty = AvaloniaProperty.Register<TabItemEx,ICommand?>(nameof(CloseTabCommand));
        public static readonly StyledProperty<object?> CloseTabCommandParameterProperty = AvaloniaProperty.Register<TabItemEx,object?>(nameof(CloseTabCommandParameter));
        
        public ICommand? CloseTabCommand
        {
            get => GetValue(CloseTabCommandProperty);
            set => SetValue(CloseTabCommandProperty, value);
        }

        public object? CloseTabCommandParameter
        {
            get => GetValue(CloseTabCommandParameterProperty);
            set => SetValue(CloseTabCommandParameterProperty, value);
        }

        static TabItemEx()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(TabItemEx), new FrameworkPropertyMetadata(typeof(TabItemEx)));
        }
    }
}
