using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Workflow.Navigation
{
    public sealed class NavigationBreadcrumbsViewModel
    {
        public ObservableCollection<NavigationBreadcrumbViewModel> Breadcrumbs { get; } = new();

        public NavigationBreadcrumbsViewModel(NavigationService navigationService)
        {
            var defaultEntry = navigationService.CurrentEntry;
            if (defaultEntry != null)
            {
                foreach (var b in GetBreadcrumbsForEntry(defaultEntry, navigationService))
                {
                    Breadcrumbs.Add(b);
                }
            }

            navigationService.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NavigationService.CurrentEntry))
                {
                    UpdateBreadcrumbsFromEntry(
                        navigationService.CurrentEntry ?? new NavigationEntry(new object(), string.Empty, 0),
                        navigationService);
                }
            };
        }

        private void UpdateBreadcrumbsFromEntry(NavigationEntry value, NavigationService navigationService)
        {
            Breadcrumbs.Clear();
            foreach (var b in GetBreadcrumbsForEntry(value, navigationService))
            {
                Breadcrumbs.Add(b);
            }
        }

        private static NavigationBreadcrumbViewModel[] GetBreadcrumbsForEntry(NavigationEntry entry, NavigationService navigationService)
        {
            var splits = entry.RouteKey.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var routes = splits.Select((_, i) => string.Join("/", splits.Take(i + 1))).ToArray();

            return routes.Select((route, index) => new NavigationBreadcrumbViewModel
            {
                RouteKey = GetLabel(route),
                Command = index == splits.Length - 1
                    ? null
                    : new DelegateCommand(() => navigationService.Navigate(route, layer: entry.Layer)),
                IsRoot = index == 0,
                IsCurrent = index == splits.Length - 1
            }).ToArray();
        }

        private static string GetLabel(string route) => route.Split('/').Last();
    }

    public sealed class NavigationBreadcrumbViewModel
    {
        public bool IsRoot { get; init; }
        public bool IsCurrent { get; init; }
        public required string RouteKey { get; init; }
        public ICommand? Command { get; init; }
    }
}
