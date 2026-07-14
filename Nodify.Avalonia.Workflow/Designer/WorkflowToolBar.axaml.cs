using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nodify.Avalonia.Workflow.Designer
{
    public partial class WorkflowToolBar : UserControl
    {
        public static readonly StyledProperty<NodifyEditor?> EditorProperty =
            AvaloniaProperty.Register<WorkflowToolBar, NodifyEditor?>(nameof(Editor));

        public NodifyEditor? Editor
        {
            get => GetValue(EditorProperty);
            set => SetValue(EditorProperty, value);
        }

        public WorkflowToolBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
