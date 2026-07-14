using System.Collections.ObjectModel;
using System.Windows.Input;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Common;
using Nodify.Avalonia.Workflow.Navigation;
using I = Nodify.Avalonia.Workflow.Common.WorkflowIcon;

namespace Nodify.Avalonia.Workflow.Settings
{
    public sealed class ApplicationSettingsViewModel : ViewModelBase
    {
        public EditorGestures? EditorGestures { get; }

        public NavigationService NavigationService { get; }

        public NavigationBreadcrumbsViewModel Breadcrumbs { get; }

        public ObservableCollection<SettingsEntryViewModel> SettingsList { get; } = new();

        private SettingsEntryViewModel? _selectedCategory;
        public SettingsEntryViewModel? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value) && value != null)
                {
                    NavigationService.Navigate(value.Name);
                }
            }
        }

        public CommandViewModel NavigateBackCommand { get; }

        public ApplicationSettingsViewModel(EditorGestures? gestures)
        {
            EditorGestures = gestures;

            NavigationService = new NavigationService(routeKey =>
            {
                // TODO: replace this hardcoded routing table with DI-backed view model resolution.
                if (routeKey == KeybindingsSettingsViewModel.RouteKey)
                    return new KeybindingsSettingsViewModel(NavigationService!);
                if (routeKey == EditorKeybindingsListViewModel.RouteKey)
                    return new EditorKeybindingsListViewModel(EditorGestures!);
                if (routeKey == ItemContainerKeybindingsViewModel.RouteKey)
                    return new ItemContainerKeybindingsViewModel(EditorGestures!);
                if (routeKey == ConnectorKeybindingsViewModel.RouteKey)
                    return new ConnectorKeybindingsViewModel(EditorGestures!);
                if (routeKey == ConnectionKeybindingsViewModel.RouteKey)
                    return new ConnectionKeybindingsViewModel(EditorGestures!);
                if (routeKey == GroupingNodeKeybindingsViewModel.RouteKey)
                    return new GroupingNodeKeybindingsViewModel(EditorGestures!);
                if (routeKey == MinimapKeybindingsViewModel.RouteKey)
                    return new MinimapKeybindingsViewModel(EditorGestures!);

                return routeKey;
            });

            Breadcrumbs = new NavigationBreadcrumbsViewModel(NavigationService);

            SettingsList.Add(new SettingsEntryViewModel("General", I.Settings));
            if (EditorGestures != null)
            {
                SettingsList.Add(new SettingsEntryViewModel(KeybindingsSettingsViewModel.RouteKey, I.Keyboard));
            }
            SettingsList.Add(new SettingsEntryViewModel("Appearance", I.Color));
            SettingsList.Add(new SettingsEntryViewModel("About", I.Info));

            NavigateBackCommand = new CommandViewModel(NavigationService.NavigateBackCommand)
            {
                Icon = I.ArrowLeft,
                ToolTip = "Back"
            };

            SelectedCategory = SettingsList.Count > 1 ? SettingsList[1] : SettingsList[0];
        }
    }

    public sealed class SettingsEntryViewModel
    {
        public SettingsEntryViewModel(string name, WorkflowIcon icon)
        {
            Name = name;
            Icon = icon;
        }

        public string Name { get; }
        public WorkflowIcon Icon { get; }
    }
}
