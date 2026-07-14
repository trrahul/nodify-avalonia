using System;
using Avalonia;

namespace Nodify.Avalonia.Helpers;

internal static class ScrollingMath
{
    public static (Size Extent, Vector Offset) ComputeScrollbars(Rect itemsExtent, Point viewportLocation, Size viewportSize)
    {
        var extent = itemsExtent.Union(new Rect(viewportLocation, viewportSize));
        double offsetX = Math.Max(0d, viewportLocation.X - itemsExtent.X);
        double offsetY = Math.Max(0d, viewportLocation.Y - itemsExtent.Y);
        return (extent.Size, new Vector(offsetX, offsetY));
    }

    // Translates a raw ScrollViewer offset back to a graph-space ViewportLocation, anchored at min(items-extent edge, viewport location when the scroll began).
    public static Point ComputeViewportLocationOnScroll(Rect itemsExtent, Point viewportLocationBeforeScrolling, Vector offset)
    {
        double locationX = Math.Min(itemsExtent.Left, viewportLocationBeforeScrolling.X) + offset.X;
        double locationY = Math.Min(itemsExtent.Top, viewportLocationBeforeScrolling.Y) + offset.Y;
        return new Point(locationX, locationY);
    }
}
