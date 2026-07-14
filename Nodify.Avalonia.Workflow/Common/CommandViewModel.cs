using System.Windows.Input;
using Avalonia.Media;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Workflow.Common
{
    /// <summary>
    /// A presentable command: pairs an <see cref="ICommand"/> with a label, tooltip and icon.
    /// Rendered by the <c>CommandViewModel</c> data template in <c>Common/CommonDataTemplates.axaml</c>.
    /// </summary>
    public class CommandViewModel : ObservableObject
    {
        public CommandViewModel(ICommand command)
        {
            Command = command;
        }

        public ICommand Command { get; }

        private string? _label;
        public string? Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        private string? _toolTip;
        public string? ToolTip
        {
            get => _toolTip;
            set => SetProperty(ref _toolTip, value);
        }

        private WorkflowIcon _icon = WorkflowIcon.None;
        public WorkflowIcon Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private Color? _iconColor;
        public Color? IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        private IconVariant _iconVariant = IconVariant.Regular;
        public IconVariant IconVariant
        {
            get => _iconVariant;
            set => SetProperty(ref _iconVariant, value);
        }
    }

    /// <summary>A <see cref="CommandViewModel"/> that maintains a toggled state and a checked icon.</summary>
    public class ToggleCommandViewModel : CommandViewModel
    {
        public ToggleCommandViewModel(ICommand command) : base(command)
        {
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (SetProperty(ref _isChecked, value))
                {
                    IsCheckedChanged?.Invoke(value);
                }
            }
        }

        private WorkflowIcon _iconChecked = WorkflowIcon.None;
        public WorkflowIcon IconChecked
        {
            get => _iconChecked;
            set => SetProperty(ref _iconChecked, value);
        }

        public event System.Action<bool>? IsCheckedChanged;
    }
}
