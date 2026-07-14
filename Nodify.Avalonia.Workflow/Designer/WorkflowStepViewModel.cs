using Avalonia;
using Avalonia.Media;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Designer
{
    public sealed class WorkflowStepViewModel : ObservableObject
    {
        public WorkflowStepViewModel(string name)
        {
            _name = name;
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private WorkflowIcon _icon = WorkflowIcon.None;
        public WorkflowIcon Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        private Color _iconColor = Colors.White;
        public Color IconColor
        {
            get => _iconColor;
            set => SetProperty(ref _iconColor, value);
        }

        private Point _position;
        public Point Position
        {
            get => _position;
            set => SetProperty(ref _position, value);
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        private Point _inAnchorPosition;
        public Point InAnchorPosition
        {
            get => _inAnchorPosition;
            set => SetProperty(ref _inAnchorPosition, value);
        }

        private Point _outAnchorPosition;
        public Point OutAnchorPosition
        {
            get => _outAnchorPosition;
            set => SetProperty(ref _outAnchorPosition, value);
        }
    }
}
