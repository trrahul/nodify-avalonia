using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorViewportTests
{
    [AvaloniaFact]
    public async Task BringIntoView_OverlappingCalls_LatestOperationOwnsViewportAndCleanup()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());
        var firstCallbackCount = 0;
        var secondCallbackCount = 0;
        var firstTarget = new Point(800, 600);
        var secondTarget = new Point(1200, 900);

        Task first = editor.BringIntoView(firstTarget, onFinish: () => firstCallbackCount++);
        Task second = editor.BringIntoView(secondTarget, onFinish: () => secondCallbackCount++);

        await Task.WhenAll(first, second);

        Assert.Equal(ToViewportLocation(editor, secondTarget), editor.ViewportLocation);
        Assert.False(editor.IsPanning);
        Assert.False(editor.DisablePanning);
        Assert.False(editor.DisableZooming);
        Assert.Equal(0, firstCallbackCount);
        Assert.Equal(1, secondCallbackCount);
    }

    [AvaloniaFact]
    public async Task BringIntoView_InternalInteractionFlags_PreserveConsumerBindings()
    {
        var editor = CreateEditor();
        var source = new InteractionSettings();
        using var panningBinding = editor.Bind(
            NodifyEditor.DisablePanningProperty,
            source.GetObservable(InteractionSettings.DisablePanningProperty),
            BindingPriority.Style);
        using var zoomingBinding = editor.Bind(
            NodifyEditor.DisableZoomingProperty,
            source.GetObservable(InteractionSettings.DisableZoomingProperty),
            BindingPriority.Style);
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

        await editor.BringIntoView(new Point(800, 600));

        source.DisablePanning = true;
        source.DisableZooming = true;

        Assert.True(editor.DisablePanning);
        Assert.True(editor.DisableZooming);
    }

    [AvaloniaFact]
    public async Task BringIntoView_AnimatedThenImmediateNonAnimatedCall_ReleasesInheritedInteractionLock()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());
        var animatedTarget = new Point(800, 600);
        var immediateTarget = new Point(1200, 900);

        Task animated = editor.BringIntoView(animatedTarget);
        Task immediate = editor.BringIntoView(immediateTarget, animated: false);

        await Task.WhenAll(animated, immediate);

        Assert.Equal(ToViewportLocation(editor, immediateTarget), editor.ViewportLocation);
        Assert.False(editor.IsPanning);
        Assert.False(editor.DisablePanning);
        Assert.False(editor.DisableZooming);
    }

    [AvaloniaFact]
    public async Task BringIntoView_ViewportUpdatedReentrancy_LatestOperationRetainsOwnership()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());
        var lockTarget = new Point(800, 600);
        var reentrantTriggerTarget = new Point(1200, 900);
        var latestTarget = new Point(1600, 1200);
        var supersededCallbackCount = 0;
        var latestCallbackCount = 0;
        var reentered = false;
        Task? latest = null;

        editor.ViewportUpdated += (_, _) =>
        {
            if (!reentered && editor.ViewportLocation == ToViewportLocation(editor, reentrantTriggerTarget))
            {
                reentered = true;
                latest = editor.BringIntoView(latestTarget, onFinish: () => latestCallbackCount++);
            }
        };

        Task lockOwner = editor.BringIntoView(lockTarget);
        Task superseded = editor.BringIntoView(
            reentrantTriggerTarget,
            animated: false,
            onFinish: () => supersededCallbackCount++);

        Assert.NotNull(latest);
        Assert.True(editor.IsPanning);
        Assert.True(editor.DisablePanning);
        Assert.True(editor.DisableZooming);

        await Task.WhenAll(lockOwner, superseded, latest!);

        Assert.Equal(ToViewportLocation(editor, latestTarget), editor.ViewportLocation);
        Assert.Equal(0, supersededCallbackCount);
        Assert.Equal(1, latestCallbackCount);
        Assert.False(editor.IsPanning);
        Assert.False(editor.DisablePanning);
        Assert.False(editor.DisableZooming);
    }

    [AvaloniaFact]
    public async Task BringIntoView_PredecessorCancellationCallbackThrows_CurrentOwnerStillCleansUp()
    {
        var editor = CreateEditor();
        using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());
        Task predecessor = editor.BringIntoView(new Point(800, 600));
        CancellationTokenSource previousOperation = GetActiveOperation(editor)!;
        CancellationTokenSource? installedOperation = null;
        using var registration = previousOperation.Token.Register(() =>
        {
            installedOperation = GetActiveOperation(editor);
            throw new InvalidOperationException("Cancellation callback failed.");
        });

        Task current = editor.BringIntoView(new Point(1200, 900), animated: false);

        AggregateException exception = await Assert.ThrowsAsync<AggregateException>(() => current);
        await predecessor;

        Assert.Contains(exception.InnerExceptions, inner => inner is InvalidOperationException);
        Assert.False(editor.IsPanning);
        Assert.False(editor.DisablePanning);
        Assert.False(editor.DisableZooming);
        Assert.Null(GetActiveOperation(editor));
        Assert.NotNull(installedOperation);
        Assert.Throws<ObjectDisposedException>(() => _ = installedOperation!.Token);
    }

    private static NodifyEditor CreateEditor() => new()
    {
        Width = 400,
        Height = 300,
        BringIntoViewMaxDuration = 0.1
    };

    private static Point ToViewportLocation(NodifyEditor editor, Point target)
        => (Point)((Vector)target - new Vector(editor.ViewportSize.Width, editor.ViewportSize.Height) / 2);

    private static CancellationTokenSource? GetActiveOperation(NodifyEditor editor)
        => (CancellationTokenSource?)typeof(NodifyEditor)
            .GetField("_bringIntoViewCancellation", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(editor);

    private sealed class InteractionSettings : AvaloniaObject
    {
        public static readonly StyledProperty<bool> DisablePanningProperty =
            AvaloniaProperty.Register<InteractionSettings, bool>(nameof(DisablePanning));

        public static readonly StyledProperty<bool> DisableZoomingProperty =
            AvaloniaProperty.Register<InteractionSettings, bool>(nameof(DisableZooming));

        public bool DisablePanning
        {
            get => GetValue(DisablePanningProperty);
            set => SetValue(DisablePanningProperty, value);
        }

        public bool DisableZooming
        {
            get => GetValue(DisableZoomingProperty);
            set => SetValue(DisableZoomingProperty, value);
        }
    }
}
