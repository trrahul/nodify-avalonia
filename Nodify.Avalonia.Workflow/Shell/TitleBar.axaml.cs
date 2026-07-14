using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nodify.Avalonia.Workflow.Shell
{
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // Caption behavior (drag to move, double-click to toggle maximize, snap layouts) comes
        // from WindowDecorationProperties.ElementRole="TitleBar" on the root Grid. Interactive
        // children keep their input.
    }
}
