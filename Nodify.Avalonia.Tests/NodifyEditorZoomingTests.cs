using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorZoomingTests
{
    [AvaloniaFact]
    public void PointerWheel_HorizontalDelta_DoesNotChangeViewportZoom()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());
        bool? handled = null;
        fixture.Window.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (_, args) => handled = args.Handled,
            RoutingStrategies.Bubble,
            handledEventsToo: true);

        Wheel(fixture.Window, editor, new Vector(1, 0), RawInputModifiers.Control);

        Assert.Equal(1d, editor.ViewportZoom);
        Assert.False(handled);
    }

    [AvaloniaFact]
    public void PointerWheel_DiagonalDelta_UsesVerticalDirection()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

        Wheel(fixture.Window, editor, new Vector(2, -1), RawInputModifiers.Control);

        Assert.Equal(Math.Pow(2.0, -1.0 / 3.0), editor.ViewportZoom, 12);
    }

    [AvaloniaFact]
    public void PointerWheel_VerticalDelta_ZoomsEditor()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

        Wheel(fixture.Window, editor, new Vector(0, 1), RawInputModifiers.Control);

        Assert.Equal(Math.Pow(2.0, 1.0 / 3.0), editor.ViewportZoom, 12);
    }

    private static NodifyEditor CreateEditor()
    {
        var gestures = new EditorGestures();
        gestures.Editor.ZoomModifierKey = KeyModifiers.Control;

        return new NodifyEditor
        {
            Width = 400,
            Height = 300,
            InputGestures = gestures
        };
    }

    private static void Wheel(
        Window window,
        NodifyEditor editor,
        Vector delta,
        RawInputModifiers modifiers)
    {
        Point? center = editor.TranslatePoint(
            new Point(editor.Bounds.Width / 2, editor.Bounds.Height / 2),
            window);

        Assert.True(center.HasValue, "Could not translate the editor to the test window.");

        window.MouseWheel(center.Value, delta, modifiers);
        Dispatcher.UIThread.RunJobs();
    }

}
