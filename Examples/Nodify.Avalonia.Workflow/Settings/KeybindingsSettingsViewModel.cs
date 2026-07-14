using System.Collections.ObjectModel;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Navigation;
using I = Nodify.Avalonia.Workflow.Common.WorkflowIcon;

namespace Nodify.Avalonia.Workflow.Settings
{
    public sealed class KeybindingsSettingsViewModel : ViewModelBase, INavigatable
    {
        public static string RouteKey => "Keybindings";

        private readonly NavigationService _navigationService;

        public ObservableCollection<KeybindingCategoryViewModel> Categories { get; } = new();

        public KeybindingsSettingsViewModel(NavigationService navigationService)
        {
            _navigationService = navigationService;

            Categories.Add(new KeybindingCategoryViewModel("Editor", "Configure keybindings for editor actions", I.FlowDot, new DelegateCommand(() => NavigateTo(EditorKeybindingsListViewModel.RouteKey))));
            Categories.Add(new KeybindingCategoryViewModel("Item container", "Configure keybindings for item container actions", I.RectangleLandscapeSparkle, new DelegateCommand(() => NavigateTo(ItemContainerKeybindingsViewModel.RouteKey))));
            Categories.Add(new KeybindingCategoryViewModel("Connector", "Configure keybindings for connector actions", I.PlugDisconnected, new DelegateCommand(() => NavigateTo(ConnectorKeybindingsViewModel.RouteKey))));
            Categories.Add(new KeybindingCategoryViewModel("Connection", "Configure keybindings for connection actions", I.ArrowTurnBidirectionalDownRight, new DelegateCommand(() => NavigateTo(ConnectionKeybindingsViewModel.RouteKey))));
            Categories.Add(new KeybindingCategoryViewModel("Grouping Node", "Configure keybindings for grouping node actions", I.SquaresNested, new DelegateCommand(() => NavigateTo(GroupingNodeKeybindingsViewModel.RouteKey))));
            Categories.Add(new KeybindingCategoryViewModel("Minimap", "Configure keybindings for minimap actions", I.Map, new DelegateCommand(() => NavigateTo(MinimapKeybindingsViewModel.RouteKey))));
        }

        private void NavigateTo(string routeKey) => _navigationService.Navigate(routeKey, layer: 1);
    }

    public interface INavigatable
    {
        static abstract string RouteKey { get; }
    }
}
