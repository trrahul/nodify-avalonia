using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;

namespace Nodify.Avalonia
{
    /// <summary>
    /// Represents a single node inside a <see cref="Minimap"/>.
    /// </summary>
    public class MinimapItem : ContentControl
    {
        /// <summary>
        /// Gets or sets the location of this <see cref="MinimapItem"/> inside the <see cref="Minimap"/>.
        /// </summary>
        public static readonly StyledProperty<Point> LocationProperty =
            ItemContainer.LocationProperty.AddOwner<MinimapItem>(
                new StyledPropertyMetadata<Point>(default, BindingMode.TwoWay));

        /// <summary>
        /// Gets or sets the location of this <see cref="MinimapItem"/> in graph space.
        /// </summary>
        public Point Location
        {
            get => (Point)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        static MinimapItem()
        {
            // Invalidate MinimapPanel's arrange when Location changes.
            // (ContentControl does not inherit Panel.AffectsParentArrange; calling
            // InvalidateArrange on the parent panel achieves the same effect.)
            LocationProperty.Changed.AddClassHandler<MinimapItem>(
                (item, _) => (item.Parent as Layoutable)?.InvalidateMeasure());
        }
    }
}
