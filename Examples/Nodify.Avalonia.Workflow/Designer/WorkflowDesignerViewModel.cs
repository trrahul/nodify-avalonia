using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Designer
{
    /// <summary>
    /// Base view model for a workflow designer surface. Owns the steps, connections, selection,
    /// editor gestures, and a viewport position that is clamped to the steps' bounding box.
    /// </summary>
    public abstract class WorkflowDesignerViewModel : ViewModelBase
    {
        public ObservableCollection<WorkflowStepViewModel> Steps { get; } = new();
        public ObservableCollection<WorkflowStepConnectionViewModel> Connections { get; } = new();

        public EditorGestures EditorGestures { get; } = new();

        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private WorkflowStepViewModel? _selectedStep;
        public WorkflowStepViewModel? SelectedStep
        {
            get => _selectedStep;
            set => SetProperty(ref _selectedStep, value);
        }

        private Size _viewportSize;
        public Size ViewportSize
        {
            get => _viewportSize;
            set => SetProperty(ref _viewportSize, value);
        }

        // Clamping happens only when the viewport is assigned (the next pan/zoom); step or size
        // changes never snap the viewport on their own.
        private Point _viewportPosition;
        public Point ViewportPosition
        {
            get => _viewportPosition;
            set => SetProperty(ref _viewportPosition, ClampViewport(value));
        }

        /// <summary>Extra padding (in graph units) around the steps' bounding box.</summary>
        public double EdgePadding { get; set; } = 50;

        private bool _boundsInitialized;

        protected WorkflowDesignerViewModel()
        {
            ConfigureDefaultGestures();
        }

        public virtual void OnPostInitialize()
        {
            // Arms clamping; the step bounds are computed live on each viewport assignment.
            _boundsInitialized = true;
        }

        protected virtual void ConfigureDefaultGestures()
        {
            EditorGestures.Editor.PanWithMouseWheel = true;
            EditorGestures.Editor.ZoomModifierKey = KeyModifiers.Control;
        }

        private Point ClampViewport(Point value)
        {
            if (!_boundsInitialized || Steps.Count == 0)
            {
                return value;
            }

            if (ViewportSize.Width == 0 || ViewportSize.Height == 0)
            {
                return value;
            }

            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (var step in Steps)
            {
                var pos = step.Position;
                var size = step.Size;
                minX = System.Math.Min(minX, pos.X);
                minY = System.Math.Min(minY, pos.Y);
                maxX = System.Math.Max(maxX, pos.X + size.Width);
                maxY = System.Math.Max(maxY, pos.Y + size.Height);
            }

            double minBoundX = minX - EdgePadding;
            double minBoundY = minY - EdgePadding;
            double maxBoundX = maxX + EdgePadding - ViewportSize.Width;
            double maxBoundY = maxY + EdgePadding - ViewportSize.Height;

            double lowX = System.Math.Min(minBoundX, maxBoundX);
            double highX = System.Math.Max(minBoundX, maxBoundX);
            double lowY = System.Math.Min(minBoundY, maxBoundY);
            double highY = System.Math.Max(minBoundY, maxBoundY);

            return new Point(
                System.Math.Clamp(value.X, lowX, highX),
                System.Math.Clamp(value.Y, lowY, highY));
        }
    }

    public sealed class MainWorkflowDesignerViewModel : WorkflowDesignerViewModel
    {
        protected override void ConfigureDefaultGestures()
        {
            base.ConfigureDefaultGestures();
            EditorGestures.LockEditing();
        }
    }

    public sealed class SubWorkflowDesignerViewModel : WorkflowDesignerViewModel
    {
        private readonly EditorGestures _backupGestures = new();

        public ToggleCommandViewModel LockViewCommand { get; }

        public SubWorkflowDesignerViewModel()
        {
            LockViewCommand = new ToggleCommandViewModel(new DelegateCommand(() => { }))
            {
                Icon = WorkflowIcon.LockOpen,
                IconChecked = WorkflowIcon.LockClosed,
                ToolTip = "Lock editing"
            };

            LockViewCommand.IsCheckedChanged += OnLockChanged;

            ConfigureDefaultGestures();
        }

        private void OnLockChanged(bool value)
        {
            if (value)
            {
                LockViewCommand.ToolTip = "Unlock editing";
                _backupGestures.Apply(EditorGestures);
                EditorGestures.LockEditing();
            }
            else
            {
                LockViewCommand.ToolTip = "Lock editing";
                EditorGestures.Apply(_backupGestures);
            }
        }

        public override void OnPostInitialize()
        {
            base.OnPostInitialize();
            EdgePadding = 200;
        }

        protected override void ConfigureDefaultGestures()
        {
            base.ConfigureDefaultGestures();
            EditorGestures.Editor.PanVerticalModifierKey = KeyModifiers.Shift;
            EditorGestures.Editor.PanHorizontalModifierKey = KeyModifiers.None;
            _backupGestures.Apply(EditorGestures);
        }
    }
}
