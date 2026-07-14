using System;
using Avalonia;
using Avalonia.Controls;

namespace Nodify.Avalonia
{
    internal class MinimapPanel : Panel
    {
        public static readonly StyledProperty<Point> ViewportLocationProperty =
            NodifyEditor.ViewportLocationProperty.AddOwner<MinimapPanel>();

        public static readonly StyledProperty<Size> ViewportSizeProperty =
            NodifyEditor.ViewportSizeProperty.AddOwner<MinimapPanel>();

        public static readonly StyledProperty<Rect> ExtentProperty =
            NodifyCanvas.ExtentProperty.AddOwner<MinimapPanel>();

        public static readonly StyledProperty<Rect> ItemsExtentProperty =
            Minimap.ItemsExtentProperty.AddOwner<MinimapPanel>();

        public static readonly StyledProperty<bool> ResizeToViewportProperty =
            Minimap.ResizeToViewportProperty.AddOwner<MinimapPanel>();

        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }

        public Size ViewportSize
        {
            get => (Size)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        public Rect Extent
        {
            get => (Rect)GetValue(ExtentProperty);
            set => SetValue(ExtentProperty, value);
        }

        public Rect ItemsExtent
        {
            get => (Rect)GetValue(ItemsExtentProperty);
            set => SetValue(ItemsExtentProperty, value);
        }

        public bool ResizeToViewport
        {
            get => (bool)GetValue(ResizeToViewportProperty);
            set => SetValue(ResizeToViewportProperty, value);
        }

        static MinimapPanel()
        {
            AffectsMeasure<MinimapPanel>(ViewportSizeProperty, ViewportLocationProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            var children = Children;
            for (int i = 0; i < children.Count; i++)
            {
                var item = (MinimapItem)children[i];
                item.Measure(availableSize);

                Size size = item.DesiredSize;

                if (item.Location.X < minX)
                    minX = item.Location.X;

                if (item.Location.Y < minY)
                    minY = item.Location.Y;

                double sizeX = item.Location.X + size.Width;
                if (sizeX > maxX)
                    maxX = sizeX;

                double sizeY = item.Location.Y + size.Height;
                if (sizeY > maxY)
                    maxY = sizeY;
            }

            var itemsExtent = minX == double.MaxValue
                ? new Rect(0, 0, 0, 0)
                : new Rect(minX, minY, maxX - minX, maxY - minY);

            SetCurrentValue(ItemsExtentProperty, itemsExtent);

            if (ResizeToViewport)
            {
                itemsExtent = itemsExtent.Union(new Rect(ViewportLocation, ViewportSize));
            }

            SetCurrentValue(ExtentProperty, itemsExtent);

            double width = Math.Max(itemsExtent.Size.Width, ViewportSize.Width);
            double height = Math.Max(itemsExtent.Height, ViewportSize.Height);
            return new Size(width, height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var children = Children;
            for (int i = 0; i < children.Count; i++)
            {
                var item = (MinimapItem)children[i];
                item.Arrange(new Rect(item.Location - (Vector)Extent.Position, item.DesiredSize));
            }

            return finalSize;
        }
    }
}
