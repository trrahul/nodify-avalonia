using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Nodify.Avalonia.Helpers;
using System;

namespace Nodify.Avalonia;

public partial class NodifyEditor : ILogicalScrollable
{
    /// <summary>
    /// The number of units scrolled per logical scroll step. Divided by
    /// <see cref="ViewportZoom"/> before use so the step is stable regardless of zoom.
    /// </summary>
    public static double ScrollIncrement { get; set; } = 60d;

    private double _extentWidth;
    private double _extentHeight;
    private double _horizontalOffset;
    private double _verticalOffset;

    // Guards against re-entrant scrollbar updates while a programmatic scroll is in progress.
    private bool _isScrolling;

    private Point? _viewportLocationBeforeScrolling;

    public Size Extent => new Size(_extentWidth, _extentHeight);
    public Size Viewport => ViewportSize;

    public Vector Offset
    {
        get => new Vector(_horizontalOffset, _verticalOffset);
        set
        {
            // Ignore infinite values (e.g. sent by ScrollViewer on initial layout when the
            // extent is not yet known).
            if (double.IsInfinity(value.X) || double.IsInfinity(value.Y))
                return;

            _horizontalOffset = value.X;
            _verticalOffset = value.Y;
            UpdateViewportLocationOnScroll();
        }
    }

    public bool CanHorizontallyScroll { get; set; }
    public bool CanVerticallyScroll { get; set; }
    public bool IsLogicalScrollEnabled => true;
    public Size ScrollSize => new Size(ScrollIncrement / ViewportZoom, ScrollIncrement / ViewportZoom);
    public Size PageScrollSize => ViewportSize;

    public event EventHandler? ScrollInvalidated;

    public void RaiseScrollInvalidated(EventArgs e) => ScrollInvalidated?.Invoke(this, e);

    /// <remarks>
    /// Returns false: the editor manages viewport position independently, so it does not
    /// auto-scroll to bring a child into view.
    /// </remarks>
    public bool BringIntoView(Control target, Rect targetRect) => false;

    public Control? GetControlInDirection(NavigationDirection direction, Control? from) => null;

    // The _isScrolling guard prevents re-entry while a programmatic scroll updates ViewportLocation.
    private void UpdateScrollbars()
    {
        if (_isScrolling)
            return;

        _viewportLocationBeforeScrolling = null;

        var (extent, offset) = ScrollingMath.ComputeScrollbars(ItemsExtent, ViewportLocation, ViewportSize);
        _extentWidth = extent.Width;
        _extentHeight = extent.Height;
        _horizontalOffset = offset.X;
        _verticalOffset = offset.Y;

        RaiseScrollInvalidated(EventArgs.Empty);
    }

    private void UpdateViewportLocationOnScroll()
    {
        if (!_viewportLocationBeforeScrolling.HasValue)
            _viewportLocationBeforeScrolling = ViewportLocation;

        _isScrolling = true;

        ViewportLocation = ScrollingMath.ComputeViewportLocationOnScroll(
            ItemsExtent,
            _viewportLocationBeforeScrolling.Value,
            new Vector(_horizontalOffset, _verticalOffset));

        // Clear the re-entrancy guard BEFORE raising invalidation so a synchronous
        // re-entry into Offset.set (should a listener raise one) starts cleanly.
        _isScrolling = false;
        RaiseScrollInvalidated(EventArgs.Empty);
    }
}
