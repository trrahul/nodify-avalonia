using System.ComponentModel;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Shell
{
    public partial class MainWindow : Window
    {
        private const double DefaultMainWorkflowWidth = 350;
        private const double DefaultWorkflowsPanelWidth = 220;

        private ColumnDefinition _workflowsPanelColumn = null!;
        private ColumnDefinition _selectedWorkflowColumn = null!;
        private ColumnDefinition _mainWorkflowColumn = null!;
        private GridSplitter _columnSplitter = null!;
        private WorkflowsPanel _workflowsPanel = null!;
        private Border _mainWorkflowChrome = null!;

        private double _lastMainWorkflowWidth = DefaultMainWorkflowWidth;
        private bool _isZenMode;
        private bool _hasSelectedWorkflow;
        private ApplicationViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var layoutGrid = this.FindControl<Grid>("LayoutGrid")!;
            _workflowsPanelColumn = layoutGrid.ColumnDefinitions[0];
            _selectedWorkflowColumn = layoutGrid.ColumnDefinitions[1];
            _mainWorkflowColumn = layoutGrid.ColumnDefinitions[3];
            _columnSplitter = this.FindControl<GridSplitter>("ColumnSplitter")!;
            _workflowsPanel = this.FindControl<WorkflowsPanel>("WorkflowsPanelControl")!;
            _mainWorkflowChrome = this.FindControl<Border>("MainWorkflowChrome")!;

            DataContextChanged += OnDataContextChanged;
            Opened += OnOpened;
            UpdateMainWindowVisuals();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == WindowStateProperty)
            {
                UpdateMainWindowVisuals();
            }
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            WindowChromeInterop.HideDrawnTitleText(this);
            UpdateMainWindowVisuals();
            AnimateWorkflowPanelOpacity(_isZenMode ? 0 : 1, System.TimeSpan.FromMilliseconds(200), new CubicEaseOut());
            AnimateMainWorkflowOpacity(1, System.TimeSpan.FromMilliseconds(200), new CubicEaseOut());
        }

        private void OnDataContextChanged(object? sender, System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = DataContext as ApplicationViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                OnSelectedWorkflowChanged(_viewModel.SelectedWorkflow);
                OnZenModeChanged(_viewModel.IsZenMode);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_viewModel is null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(ApplicationViewModel.IsZenMode):
                    OnZenModeChanged(_viewModel.IsZenMode);
                    break;
                case nameof(ApplicationViewModel.SelectedWorkflow):
                    OnSelectedWorkflowChanged(_viewModel.SelectedWorkflow);
                    break;
            }
        }

        private void OnZenModeChanged(bool isZenMode)
        {
            _isZenMode = isZenMode;
            UpdateSplitterVisibility();

            if (isZenMode)
            {
                if (_mainWorkflowColumn.Width.IsAbsolute)
                {
                    _lastMainWorkflowWidth = _mainWorkflowColumn.Width.Value;
                }

                if (_hasSelectedWorkflow)
                {
                    AnimateColumn(_mainWorkflowColumn, 0);
                }
                AnimateColumn(_workflowsPanelColumn, 0);
                AnimateWorkflowPanelOpacity(0, System.TimeSpan.FromMilliseconds(150), new CubicEaseIn());
            }
            else
            {
                if (_hasSelectedWorkflow)
                {
                    AnimateColumn(_mainWorkflowColumn, _lastMainWorkflowWidth);
                }
                AnimateColumn(_workflowsPanelColumn, DefaultWorkflowsPanelWidth);
                AnimateWorkflowPanelOpacity(1, System.TimeSpan.FromMilliseconds(200), new CubicEaseOut());
            }
        }

        private void OnSelectedWorkflowChanged(object? selectedWorkflow)
        {
            _hasSelectedWorkflow = selectedWorkflow is not null;
            UpdateSplitterVisibility();

            if (_hasSelectedWorkflow)
            {
                _selectedWorkflowColumn.Width = new GridLength(1, GridUnitType.Star);
                _mainWorkflowColumn.Width = new GridLength(DefaultMainWorkflowWidth);
            }
            else
            {
                if (_mainWorkflowColumn.Width.IsAbsolute)
                {
                    _lastMainWorkflowWidth = _mainWorkflowColumn.Width.Value;
                }
                _selectedWorkflowColumn.Width = new GridLength(0);
                _mainWorkflowColumn.Width = new GridLength(1, GridUnitType.Star);
            }
        }

        private void UpdateSplitterVisibility()
            => _columnSplitter.IsVisible = _hasSelectedWorkflow && !_isZenMode;

        private static void AnimateColumn(ColumnDefinition column, double to)
        {
            _ = column.AnimateColumnWidth(to, new AnimationOptions
            {
                Duration = System.TimeSpan.FromMilliseconds(250),
                Easing = to == 0 ? new CubicEaseIn() : (Easing)new CubicEaseOut()
            });
        }

        private void AnimateWorkflowPanelOpacity(double to, System.TimeSpan duration, Easing easing)
        {
            _ = _workflowsPanel.FadeTo(to, new AnimationOptions
            {
                Duration = duration,
                Easing = easing
            });
        }

        private void AnimateMainWorkflowOpacity(double to, System.TimeSpan duration, Easing easing)
        {
            _ = _mainWorkflowChrome.FadeTo(to, new AnimationOptions
            {
                Duration = duration,
                Easing = easing
            });
        }

        private void UpdateMainWindowVisuals()
        {
            BorderThickness = WindowState is WindowState.Maximized
                ? new Thickness(8)
                : new Thickness(0);
        }
    }
}
