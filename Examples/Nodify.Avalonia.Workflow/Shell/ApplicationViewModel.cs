using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Common;
using Nodify.Avalonia.Workflow.Designer;
using Nodify.Avalonia.Workflow.Settings;
using I = Nodify.Avalonia.Workflow.Common.WorkflowIcon;

namespace Nodify.Avalonia.Workflow.Shell
{
    public sealed class ApplicationViewModel : ViewModelBase
    {
        public string Title => "Workflow Designer";

        public ObservableCollection<SubWorkflowDesignerViewModel> Workflows { get; } = new();

        private MainWorkflowDesignerViewModel _mainWorkflow;
        public MainWorkflowDesignerViewModel MainWorkflow
        {
            get => _mainWorkflow;
            set => SetProperty(ref _mainWorkflow, value);
        }

        private SubWorkflowDesignerViewModel? _selectedWorkflow;
        public SubWorkflowDesignerViewModel? SelectedWorkflow
        {
            get => _selectedWorkflow;
            set => SetProperty(ref _selectedWorkflow, value);
        }

        private bool _isZenMode;
        public bool IsZenMode
        {
            get => _isZenMode;
            set => SetProperty(ref _isZenMode, value);
        }

        public CommandViewModel RunWorkflowCommand { get; }
        public CommandViewModel SaveChangesCommand { get; }
        public ToggleCommandViewModel ToggleZenModeCommand { get; }
        public CommandViewModel OpenSettingsCommand { get; }

        public ApplicationViewModel()
        {
            _mainWorkflow = new MainWorkflowDesignerViewModel();

            CreateDefaultWorkflows();

            SelectedWorkflow = Workflows[0];

            RunWorkflowCommand = new CommandViewModel(new DelegateCommand(() => { }))
            {
                Label = "Run",
                ToolTip = "Run the main workflow",
                Icon = I.Play,
                IconVariant = IconVariant.Filled,
                IconColor = Colors.LawnGreen
            };

            SaveChangesCommand = new CommandViewModel(new DelegateCommand(() => { }))
            {
                Label = "Save",
                ToolTip = "Save all changes",
                Icon = I.Save,
                IconColor = Colors.LightSkyBlue
            };

            ToggleZenModeCommand = new ToggleCommandViewModel(new DelegateCommand(() => { }))
            {
                Label = "Zen",
                ToolTip = "Toggle zen mode",
                Icon = I.ArrowMaximize,
                IconChecked = I.ArrowMinimize
            };
            ToggleZenModeCommand.IsCheckedChanged += value => IsZenMode = value;

            OpenSettingsCommand = new CommandViewModel(new DelegateCommand(OpenAppSettingsDialog))
            {
                Icon = I.Settings,
                IconVariant = IconVariant.Filled,
                ToolTip = "Open application settings"
            };
        }

        private void OpenAppSettingsDialog()
        {
            var applicationSettings = new ApplicationSettingsViewModel(SelectedWorkflow?.EditorGestures);

            var window = new ApplicationSettingsWindow
            {
                DataContext = applicationSettings
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                && desktop.MainWindow is Window owner)
            {
                window.ShowDialog(owner);
            }
            else
            {
                window.Show();
            }
        }

        private void CreateDefaultWorkflows()
        {
            var runTestsWorkflow = CreateRunTestsWorkflow();
            var buildLibraryWorkflow = CreateBuildLibraryWorkflow();
            var publishToNugetWorkflow = CreatePublishToNugetWorkflow();

            Workflows.Add(runTestsWorkflow);
            Workflows.Add(buildLibraryWorkflow);
            Workflows.Add(publishToNugetWorkflow);

            foreach (var workflow in Workflows)
            {
                workflow.OnPostInitialize();
            }

            var runTestsStep = new WorkflowStepViewModel(runTestsWorkflow.Name)
            {
                IconColor = Colors.LimeGreen,
                Position = new Point(130, 150)
            };
            var buildLibraryStep = new WorkflowStepViewModel(buildLibraryWorkflow.Name)
            {
                IconColor = Colors.SteelBlue,
                Position = new Point(120, 300)
            };
            var publishToNugetStep = new WorkflowStepViewModel(publishToNugetWorkflow.Name)
            {
                IconColor = Colors.Tomato,
                Position = new Point(85, 450)
            };

            MainWorkflow.Steps.Add(runTestsStep);
            MainWorkflow.Steps.Add(buildLibraryStep);
            MainWorkflow.Steps.Add(publishToNugetStep);

            MainWorkflow.Connections.Add(new WorkflowStepConnectionViewModel(runTestsStep, buildLibraryStep));
            MainWorkflow.Connections.Add(new WorkflowStepConnectionViewModel(buildLibraryStep, publishToNugetStep));
            MainWorkflow.Name = "Release pipeline";

            MainWorkflow.OnPostInitialize();
        }

        private static SubWorkflowDesignerViewModel CreateRunTestsWorkflow()
        {
            var workflow = new SubWorkflowDesignerViewModel
            {
                Name = "Run tests",
                ViewportPosition = new Point(-100, -200)
            };

            workflow.Steps.Add(new WorkflowStepViewModel("Checkout repository") { Icon = I.BranchFork, IconColor = Colors.Orange, Position = new Point(50, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Setup .NET") { Icon = I.Settings, IconColor = Colors.MediumPurple, Position = new Point(250, 120) });
            workflow.Steps.Add(new WorkflowStepViewModel("Install dependencies") { Icon = I.ArrowDownload, IconColor = Colors.DodgerBlue, Position = new Point(450, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Build solution") { Icon = I.Building, IconColor = Colors.SteelBlue, Position = new Point(650, 120) });
            workflow.Steps.Add(new WorkflowStepViewModel("Run unit tests") { Icon = I.Beaker, IconColor = Colors.LimeGreen, Position = new Point(850, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Publish test results") { Icon = I.ClipboardTask, IconColor = Colors.MediumSeaGreen, Position = new Point(1050, 120) });

            AddSequentialConnections(workflow);
            return workflow;
        }

        private static SubWorkflowDesignerViewModel CreateBuildLibraryWorkflow()
        {
            var workflow = new SubWorkflowDesignerViewModel
            {
                Name = "Build library",
                ViewportPosition = new Point(-100, -200)
            };

            workflow.Steps.Add(new WorkflowStepViewModel("Checkout repository") { Icon = I.BranchFork, IconColor = Colors.Orange, Position = new Point(50, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Setup .NET") { Icon = I.Settings, IconColor = Colors.MediumPurple, Position = new Point(250, 120) });
            workflow.Steps.Add(new WorkflowStepViewModel("Install dependencies") { Icon = I.ArrowDownload, IconColor = Colors.DodgerBlue, Position = new Point(450, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Build solution") { Icon = I.Building, IconColor = Colors.SteelBlue, Position = new Point(650, 120) });
            workflow.Steps.Add(new WorkflowStepViewModel("Run static code analysis") { Icon = I.Search, IconColor = Colors.Goldenrod, Position = new Point(850, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Publish build artifacts") { Icon = I.CloudArrowUp, IconColor = Colors.LightSkyBlue, Position = new Point(1050, 120) });

            AddSequentialConnections(workflow);
            return workflow;
        }

        private static SubWorkflowDesignerViewModel CreatePublishToNugetWorkflow()
        {
            var workflow = new SubWorkflowDesignerViewModel
            {
                Name = "Publish NuGet package",
                ViewportPosition = new Point(-100, -200)
            };

            workflow.Steps.Add(new WorkflowStepViewModel("Download build artifacts") { Icon = I.CloudArrowDown, IconColor = Colors.LightSkyBlue, Position = new Point(50, 50) });
            workflow.Steps.Add(new WorkflowStepViewModel("Pack NuGet package") { Icon = I.Box, IconColor = Colors.Coral, Position = new Point(250, 120) });
            workflow.Steps.Add(new WorkflowStepViewModel("Publish to NuGet.org") { Icon = I.Rocket, IconColor = Colors.Tomato, Position = new Point(450, 50) });

            AddSequentialConnections(workflow);
            return workflow;
        }

        private static void AddSequentialConnections(SubWorkflowDesignerViewModel workflow)
        {
            for (int i = 0; i < workflow.Steps.Count - 1; i++)
            {
                workflow.Connections.Add(new WorkflowStepConnectionViewModel(workflow.Steps[i], workflow.Steps[i + 1]));
            }
        }
    }
}
