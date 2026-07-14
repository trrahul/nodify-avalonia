using Avalonia;
using Nodify.Avalonia.Shared.Controls;

namespace Nodify.Avalonia.Shapes.Controls
{
    internal class ResizableContainer : ResizablePanel
    {
        public static readonly StyledProperty<uint> GridCellSizeProperty = NodifyEditor.GridCellSizeProperty.AddOwner<ResizableContainer>();

        public uint GridCellSize
        {
            get => GetValue(GridCellSizeProperty);
            set => SetValue(GridCellSizeProperty, value);
        }

        protected override void OnMove(double x, double y)
        {
            // we can't use the default behavior because we are not inside a Canvas
            if (TemplatedParent is ItemContainer item)
            {
                item.Location = new Point(item.Location.X + x, item.Location.Y + y);
            }
        }

        protected override void OnProcessDelta(ref double dx, ref double dy)
        {
            dx = (int)dx / (int)GridCellSize * GridCellSize;
            dy = (int)dy / (int)GridCellSize * GridCellSize;
        }
    }
}
