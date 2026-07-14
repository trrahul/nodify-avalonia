using System.Collections;
using Avalonia;
using Avalonia.Controls;

namespace Nodify.Avalonia.Playground.Views
{
    public partial class SettingsView : UserControl
    {
        public static readonly StyledProperty<IEnumerable?> ItemsProperty =
            AvaloniaProperty.Register<SettingsView, IEnumerable?>(nameof(Items));

        public IEnumerable? Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public SettingsView()
        {
            InitializeComponent();
        }
    }
}
