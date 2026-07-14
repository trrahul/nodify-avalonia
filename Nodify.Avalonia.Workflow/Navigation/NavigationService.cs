using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Workflow.Navigation
{
    public enum NavigationDirection
    {
        Backward,
        Forward,
    }

    public sealed class NavigationEntry
    {
        public NavigationEntry(object viewModel, string routeKey, int layer)
        {
            ViewModel = viewModel;
            RouteKey = routeKey;
            Layer = layer;
        }

        public object ViewModel { get; }
        public string RouteKey { get; }
        public int Layer { get; }
    }

    public sealed class NavigationEventArgs : EventArgs
    {
        public NavigationEventArgs(NavigationEntry? oldEntry, NavigationEntry newEntry, NavigationDirection direction)
        {
            OldEntry = oldEntry;
            NewEntry = newEntry;
            Direction = direction;
        }

        public NavigationEntry? OldEntry { get; }
        public NavigationEntry NewEntry { get; }
        public NavigationDirection Direction { get; }
        public bool IsSameLayer => OldEntry?.Layer == NewEntry?.Layer;
    }

    public delegate object ViewModelFactory(string routeKey);

    public sealed class NavigationService : ObservableObject
    {
        private readonly ObservableCollection<NavigationEntry> _stack = new();
        private readonly ViewModelFactory _viewModelFactory;

        public IReadOnlyList<NavigationEntry> Stack => _stack;

        private object? _currentView;
        public object? CurrentView
        {
            get => _currentView;
            private set => SetProperty(ref _currentView, value);
        }

        private NavigationEntry? _currentEntry;
        public NavigationEntry? CurrentEntry
        {
            get => _currentEntry;
            private set => SetProperty(ref _currentEntry, value);
        }

        private bool _canNavigateBack;
        public bool CanNavigateBack
        {
            get => _canNavigateBack;
            private set
            {
                if (SetProperty(ref _canNavigateBack, value))
                {
                    (NavigateBackCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand NavigateBackCommand { get; }

        public event EventHandler<NavigationEventArgs>? Navigating;
        public event EventHandler<NavigationEventArgs>? Navigated;

        public NavigationService(ViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
            NavigateBackCommand = new DelegateCommand(NavigateBack, () => CanNavigateBack);
        }

        public void Navigate(string routeKey, int layer = 0, bool replace = false)
        {
            if (CanNavigateBack && routeKey == _stack[^1].RouteKey)
            {
                return;
            }

            var oldEntry = GetCurrentEntry();
            var newEntry = _stack.FirstOrDefault(e => e.RouteKey == routeKey)
                ?? new NavigationEntry(_viewModelFactory(routeKey), routeKey, layer);

            OnNavigating(oldEntry, newEntry, NavigationDirection.Forward);

            if (replace && _stack.Count > 0)
            {
                _stack.RemoveAt(_stack.Count - 1);
            }

            _stack.Add(newEntry);
            UpdateCurrentView();
            OnNavigated(oldEntry, newEntry, NavigationDirection.Forward);
        }

        public void NavigateBack()
        {
            if (!CanNavigateBack)
            {
                return;
            }

            var oldEntry = _stack[^1];
            var newEntry = _stack[^2];

            OnNavigating(oldEntry, newEntry, NavigationDirection.Backward);
            _stack.RemoveAt(_stack.Count - 1);
            UpdateCurrentView();
            OnNavigated(oldEntry, newEntry, NavigationDirection.Backward);
        }

        public void Clear()
        {
            _stack.Clear();
            UpdateCurrentView();
        }

        private NavigationEntry? GetCurrentEntry() => _stack.Count > 0 ? _stack[^1] : null;

        private void UpdateCurrentView()
        {
            var entry = GetCurrentEntry();
            CurrentEntry = entry;
            CurrentView = entry?.ViewModel;
            CanNavigateBack = _stack.Count > 1;
        }

        private void OnNavigating(NavigationEntry? oldEntry, NavigationEntry newEntry, NavigationDirection direction)
            => Navigating?.Invoke(this, new NavigationEventArgs(oldEntry, newEntry, direction));

        private void OnNavigated(NavigationEntry? oldEntry, NavigationEntry newEntry, NavigationDirection direction)
            => Navigated?.Invoke(this, new NavigationEventArgs(oldEntry, newEntry, direction));
    }
}
