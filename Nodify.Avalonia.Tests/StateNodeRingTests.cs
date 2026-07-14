using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Nodes;
using Xunit;

namespace Nodify.Avalonia.Tests;

public class StateNodeRingTests
{
    [AvaloniaFact]
    public void PointerOverContent_DoesNotHighlightOuterRing_ButPointerOverRingDoes()
    {
        var borderBrush = new SolidColorBrush(Colors.DarkSlateGray);
        var highlightBrush = new SolidColorBrush(Colors.DodgerBlue);
        var node = new StateNode
        {
            Width = 160,
            Height = 100,
            BorderBrush = borderBrush,
            HighlightBrush = highlightBrush,
            Content = new Border { Width = 80, Height = 40 },
        };
        var window = new Window { Width = 300, Height = 200, Content = node };

        window.Show();
        window.UpdateLayout();
        Border outerRing = FindOuterRing(node);
        Border content = node.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Name == "PART_Content");

        Point contentCenter = content.TranslatePoint(
            new Point(content.Bounds.Width / 2, content.Bounds.Height / 2),
            window)!.Value;
        window.MouseMove(contentCenter);
        Assert.Equal(GetColor(borderBrush), GetColor(outerRing.Background));

        Point ringPoint = outerRing.TranslatePoint(new Point(2, 2), window)!.Value;
        window.MouseMove(ringPoint);
        Assert.Equal(GetColor(highlightBrush), GetColor(outerRing.Background));
    }

    [AvaloniaFact]
    public void PendingConnectionTarget_HighlightsRingEvenWhenPointerIsOverContent()
    {
        var borderBrush = new SolidColorBrush(Colors.DarkSlateGray);
        var highlightBrush = new SolidColorBrush(Colors.DodgerBlue);
        var node = new StateNode
        {
            Width = 160,
            Height = 100,
            BorderBrush = borderBrush,
            HighlightBrush = highlightBrush,
            Content = new Border { Width = 80, Height = 40 },
        };
        var window = new Window { Width = 300, Height = 200, Content = node };

        window.Show();
        window.UpdateLayout();
        Border outerRing = FindOuterRing(node);
        Border content = node.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Name == "PART_Content");
        Point contentCenter = content.TranslatePoint(
            new Point(content.Bounds.Width / 2, content.Bounds.Height / 2),
            window)!.Value;
        window.MouseMove(contentCenter);

        PendingConnection.SetIsOverElement(node, true);

        Assert.Equal(GetColor(highlightBrush), GetColor(outerRing.Background));
    }

    private static Border FindOuterRing(StateNode node)
        => node.GetVisualDescendants()
            .OfType<Border>()
            .Single(border => border.Name == "Border");

    private static Color GetColor(IBrush? brush)
        => brush switch
        {
            SolidColorBrush solid => solid.Color,
            ImmutableSolidColorBrush solid => solid.Color,
            null => throw new InvalidOperationException("Expected a SolidColorBrush, but the brush was null."),
            _ => throw new InvalidOperationException($"Expected a SolidColorBrush, but got {brush.GetType().FullName}."),
        };
}
