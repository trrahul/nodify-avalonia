using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Nodify.Avalonia.Helpers.Gestures;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Settings
{
    public sealed class KeybindingViewModel
    {
        public KeybindingViewModel(string label, string description, InputGestureRef gestureRef, WorkflowIcon icon = WorkflowIcon.None)
        {
            Label = label;
            Description = description;
            Icon = icon;

            GestureEditor = gestureRef.Value switch
            {
                PointerGesture => new MouseGestureSelectorViewModel(gestureRef),
                MultiGesture multi when multi.Gestures.OfType<PointerGesture>().Any()
                    => new MouseGestureSelectorViewModel(gestureRef),
                _ => new KeyGestureSelectorViewModel(gestureRef)
            };
        }

        public string Label { get; }
        public string Description { get; }
        public WorkflowIcon Icon { get; }

        public GestureSelectorViewModel GestureEditor { get; }
    }

    public sealed class KeybindingsGroupViewModel : ObservableCollection<KeybindingViewModel>
    {
        public KeybindingsGroupViewModel(string label)
        {
            Label = label;
        }

        public string Label { get; }
    }

    public abstract class KeybindingsListViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ObservableCollection<KeybindingsGroupViewModel> Keybindings { get; } = new();
    }

    public sealed class KeybindingCategoryViewModel
    {
        public KeybindingCategoryViewModel(string name, string description, WorkflowIcon icon, ICommand selectCommand)
        {
            Name = name;
            Description = description;
            Icon = icon;
            SelectCommand = selectCommand;
        }

        public string Name { get; }
        public string Description { get; }
        public WorkflowIcon Icon { get; }
        public ICommand SelectCommand { get; }
    }
}
