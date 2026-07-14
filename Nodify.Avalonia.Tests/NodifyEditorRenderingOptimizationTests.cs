using System;
using System.Collections.ObjectModel;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorRenderingOptimizationTests
{
    [AvaloniaFact]
    public void RenderingOptimization_BelowItemThreshold_DoesNotCache()
    {
        WithSettings(enabled: true, minimumContainers: 2, zoomOutPercent: 0.3, () =>
        {
            var editor = CreateEditor(new ObservableCollection<object> { new() }, viewportZoom: 0.2);
            using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

            Assert.Null(editor.ItemsHost.CacheMode);
        });
    }

    [AvaloniaFact]
    public void RenderingOptimization_ZoomedOutAtThreshold_UsesInverseZoomScale()
    {
        WithSettings(enabled: true, minimumContainers: 2, zoomOutPercent: 0.3, () =>
        {
            var items = new ObservableCollection<object> { new() };
            var editor = CreateEditor(items, viewportZoom: 0.2);
            using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

            items.Add(new object());
            Dispatcher.UIThread.RunJobs();

            var cache = Assert.IsType<BitmapCache>(editor.ItemsHost.CacheMode);
            Assert.Equal(5d, cache.RenderAtScale, 12);
        });
    }

    [AvaloniaFact]
    public void RenderingOptimization_DisabledOrZoomedIn_ClearsCache()
    {
        WithSettings(enabled: true, minimumContainers: 1, zoomOutPercent: 0.3, () =>
        {
            var editor = CreateEditor(new ObservableCollection<object> { new() }, viewportZoom: 0.2);
            using var fixture = TestWindowFixture.Show(editor, afterShow: _ => editor.ApplyTemplate());

            Assert.IsType<BitmapCache>(editor.ItemsHost.CacheMode);

            editor.ViewportZoom = 1;
            Assert.Null(editor.ItemsHost.CacheMode);

            editor.ViewportZoom = 0.2;
            Assert.IsType<BitmapCache>(editor.ItemsHost.CacheMode);

            NodifyEditor.EnableRenderingContainersOptimizations = false;
            editor.ViewportZoom = 0.19;
            Assert.Null(editor.ItemsHost.CacheMode);
        });
    }

    private static NodifyEditor CreateEditor(ObservableCollection<object> items, double viewportZoom)
        => new()
        {
            Width = 400,
            Height = 300,
            ItemsSource = items,
            ViewportZoom = viewportZoom
        };

    private static void WithSettings(
        bool enabled,
        uint minimumContainers,
        double zoomOutPercent,
        Action test)
    {
        bool oldEnabled = NodifyEditor.EnableRenderingContainersOptimizations;
        uint oldMinimum = NodifyEditor.OptimizeRenderingMinimumContainers;
        double oldPercent = NodifyEditor.OptimizeRenderingZoomOutPercent;

        try
        {
            NodifyEditor.EnableRenderingContainersOptimizations = enabled;
            NodifyEditor.OptimizeRenderingMinimumContainers = minimumContainers;
            NodifyEditor.OptimizeRenderingZoomOutPercent = zoomOutPercent;
            test();
        }
        finally
        {
            NodifyEditor.EnableRenderingContainersOptimizations = oldEnabled;
            NodifyEditor.OptimizeRenderingMinimumContainers = oldMinimum;
            NodifyEditor.OptimizeRenderingZoomOutPercent = oldPercent;
        }
    }

}
