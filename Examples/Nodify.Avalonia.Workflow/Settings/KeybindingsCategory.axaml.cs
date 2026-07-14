using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Settings
{
    public partial class KeybindingsCategory : UserControl
    {
        public static readonly StyledProperty<WorkflowIcon> IconProperty =
            AvaloniaProperty.Register<KeybindingsCategory, WorkflowIcon>(nameof(Icon), WorkflowIcon.Warning);

        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<KeybindingsCategory, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<string> DescriptionProperty =
            AvaloniaProperty.Register<KeybindingsCategory, string>(nameof(Description), string.Empty);

        public static readonly StyledProperty<ICommand?> CommandProperty =
            AvaloniaProperty.Register<KeybindingsCategory, ICommand?>(nameof(Command));

        public WorkflowIcon Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public KeybindingsCategory()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
