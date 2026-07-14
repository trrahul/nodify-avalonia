using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nodify.Avalonia.Events;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class MinimapZoomingTests
{
    [AvaloniaFact]
    public void PointerWheel_HorizontalDelta_DoesNotRaiseMinimapZoomAndRemainsUnhandled()
    {
        var minimap = CreateMinimap();
        using var fixture = TestWindowFixture.Show(
            minimap,
            width: 300,
            height: 200,
            afterShow: _ => minimap.ApplyTemplate());
        ZoomEventArgs? zoom = null;
        bool? handled = null;
        minimap.Zoom += (_, args) => zoom = args;
        fixture.Window.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, args) => handled = args.Handled,
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        Wheel(fixture.Window, minimap, new Vector(1, 0));

        Assert.Null(zoom);
        Assert.False(handled);
    }

    [AvaloniaFact]
    public void PointerWheel_VerticalDelta_UsesSignedVerticalDirectionInMinimap()
    {
        var minimap = CreateMinimap();
        using var fixture = TestWindowFixture.Show(
            minimap,
            width: 300,
            height: 200,
            afterShow: _ => minimap.ApplyTemplate());
        ZoomEventArgs? zoom = null;
        minimap.Zoom += (_, args) => zoom = args;

        Wheel(fixture.Window, minimap, new Vector(4, -1));

        Assert.NotNull(zoom);
        Assert.Equal(Math.Pow(2d, -1d / 3d), zoom.Zoom, 12);
    }

    private static Minimap CreateMinimap()
        => new()
        {
            Width = 300,
            Height = 200,
            Extent = new Rect(0, 0, 300, 200),
            ItemsExtent = new Rect(0, 0, 300, 200),
            ViewportSize = new Size(100, 80)
        };

    private static void Wheel(Window window, Minimap minimap, Vector delta)
    {
        Point? center = minimap.TranslatePoint(
            new Point(minimap.Bounds.Width / 2, minimap.Bounds.Height / 2),
            window);
        Assert.True(center.HasValue);

        window.MouseWheel(center.Value, delta, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
    }

}
