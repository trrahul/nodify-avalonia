using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorCommandGestureTests
{
    [AvaloniaFact]
    public void ControlA_SelectsAllItems()
    {
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300,
            ItemsSource = new[] { "one", "two" }
        };

        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));

        PressAndRelease(fixture.Window, Key.A, RawInputModifiers.Control);

        Assert.Equal(2, editor.SelectedItems?.Count);
    }

    [AvaloniaFact]
    public void ControlPlus_IncreasesViewportZoom()
    {
        var editor = new NodifyEditor { Width = 400, Height = 300 };
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));
        var initialZoom = editor.ViewportZoom;

        PressAndRelease(fixture.Window, Key.OemPlus, RawInputModifiers.Control);

        Assert.True(editor.ViewportZoom > initialZoom);
    }

    [AvaloniaFact]
    public void ControlMinus_DecreasesViewportZoom()
    {
        var editor = new NodifyEditor { Width = 400, Height = 300 };
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));
        var initialZoom = editor.ViewportZoom;

        PressAndRelease(fixture.Window, Key.OemMinus, RawInputModifiers.Control);

        Assert.True(editor.ViewportZoom < initialZoom);
    }

    [AvaloniaFact]
    public void Home_ResetsViewport()
    {
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300,
            ViewportZoom = 1.5,
            BringIntoViewMaxDuration = 0
        };
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));

        PressAndRelease(fixture.Window, Key.Home);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(default, editor.ViewportLocation);
        Assert.Equal(1d, editor.ViewportZoom);
    }

    [AvaloniaFact]
    public void ShiftHome_FitsItemsToScreen()
    {
        var editor = new NodifyEditor { Width = 400, Height = 300 };
        editor.ItemsSource = new[]
        {
            new ItemContainer(editor) { Width = 100, Height = 100, Location = new Point(0, 0) },
            new ItemContainer(editor) { Width = 100, Height = 100, Location = new Point(600, 400) }
        };
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));

        PressAndRelease(fixture.Window, Key.Home, RawInputModifiers.Shift);

        Assert.True(editor.ViewportZoom < 1d);
    }

    [AvaloniaFact]
    public void DisabledCommandGesture_DoesNotConsumeKeyEvent()
    {
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300,
            MaxViewportZoom = 1,
            ViewportZoom = 1
        };
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => Assert.True(editor.Focus()));
        var reachedWindowUnhandled = false;
        fixture.Window.AddHandler(
            InputElement.KeyDownEvent,
            (_, e) => reachedWindowUnhandled = !e.Handled,
            RoutingStrategies.Bubble);

        PressAndRelease(fixture.Window, Key.OemPlus, RawInputModifiers.Control);

        Assert.True(reachedWindowUnhandled);
        Assert.Equal(1d, editor.ViewportZoom);
    }

    private static void PressAndRelease(
        Window window,
        Key key,
        RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Dispatcher.UIThread.RunJobs();
    }

}
