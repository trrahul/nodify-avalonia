using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nodify.Avalonia.Workflow.Shell
{
    public partial class WorkflowsPanel : UserControl
    {
        public WorkflowsPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    }
}
