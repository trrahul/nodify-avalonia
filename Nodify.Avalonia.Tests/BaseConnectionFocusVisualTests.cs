using System;
using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Nodify.Avalonia.Connections;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class BaseConnectionFocusVisualTests
{
    [AvaloniaFact]
    public void FocusGeometry_IncludesDirectionalArrow()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.None;
        connection.DirectionalArrowsCount = 1;

        Geometry renderedGeometry = connection.RenderedGeometry;
        Geometry focusGeometry = connection.CreateFocusVisualGeometry()
            ?? throw new InvalidOperationException("Focus visual geometry was not created.");

        var directionalArrowInterior = new Point(44, 0);

        Assert.True(renderedGeometry.FillContains(directionalArrowInterior));
        Assert.True(focusGeometry.FillContains(directionalArrowInterior));
    }

    [AvaloniaFact]
    public void FocusVisualDrawingPen_UsesConnectionArrowJoin()
    {
        var connection = CreateLineConnection();
        connection.StrokeJoin = PenLineJoin.Round;
        var resourcePen = new Pen(Brushes.DodgerBlue, 1d)
        {
            DashStyle = new DashStyle(new[] { 3d, 3d }, 0d),
            LineJoin = PenLineJoin.Miter,
        };

        IPen drawingPen = connection.CreateFocusVisualDrawingPen(resourcePen);

        Assert.Equal(PenLineJoin.Round, drawingPen.LineJoin);
        Assert.Same(resourcePen.DashStyle, drawingPen.DashStyle);
    }

    [AvaloniaFact]
    public void FocusGeometry_IncludesTerminalArrowhead()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.End;
        connection.DirectionalArrowsCount = 1;

        Geometry focusGeometry = connection.CreateFocusVisualGeometry()
            ?? throw new InvalidOperationException("Focus visual geometry was not created.");

        var terminalArrowInterior = new Point(94, 0);

        Assert.True(focusGeometry.FillContains(terminalArrowInterior));
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_WidensConnectionSymmetrically()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.None;
        connection.DirectionalArrowsCount = 0;
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Rect bounds = outline.Bounds;

        Assert.InRange(bounds.Top, -3.6d, -3.4d);
        Assert.InRange(bounds.Bottom, 3.4d, 3.6d);
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_UsesParallelSideRails()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.None;
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Assert.True(outline.StrokeContains(focusPen, new Point(50, 3.5d)));
        Assert.False(outline.StrokeContains(focusPen, new Point(50, 0d)));
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_UnitesTerminalArrowheadInterior()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.End;
        connection.ArrowSize = new Size(30, 30);
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Assert.True(outline.FillContains(new Point(80, 5)));
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_UnitesDirectionalArrowInterior()
    {
        var connection = CreateLineConnection();
        connection.ArrowEnds = ArrowHeadEnds.None;
        connection.DirectionalArrowsCount = 1;
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Assert.True(outline.FillContains(new Point(44, 0)));
        Assert.False(outline.StrokeContains(focusPen, new Point(44, 0)));
        Assert.True(outline.StrokeContains(focusPen, new Point(42, 7.5)));
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_DoesNotCloseOpenStepPath()
    {
        var connection = new StepConnection
        {
            Source = new Point(0, 0),
            Target = new Point(100, 50),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            SourcePosition = ConnectorPosition.Right,
            TargetPosition = ConnectorPosition.Left,
            Spacing = 20,
            ArrowEnds = ArrowHeadEnds.None,
            StrokeThickness = 3
        };
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Assert.False(outline.StrokeContains(focusPen, new Point(40, 20)));
    }

    [AvaloniaFact]
    public void FocusVisualOutlineGeometry_DoesNotCloseOpenCircuitPath()
    {
        var connection = new CircuitConnection
        {
            Source = new Point(0, 0),
            Target = new Point(100, 100),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            Spacing = 20,
            ArrowEnds = ArrowHeadEnds.None,
            StrokeThickness = 3
        };
        var focusPen = new Pen(Brushes.DodgerBlue, 2d);

        Geometry outline = connection.CreateFocusVisualOutlineGeometry(focusPen)
            ?? throw new InvalidOperationException("Focus visual outline was not created.");

        Assert.False(outline.StrokeContains(focusPen, new Point(40, 40)));
    }

    private static InspectableLineConnection CreateLineConnection()
        => new()
        {
            Source = new Point(0, 0),
            Target = new Point(100, 0),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            ArrowSize = new Size(8, 8),
            StrokeThickness = 3
        };

    private sealed class InspectableLineConnection : LineConnection
    {
        public Geometry RenderedGeometry
            => CreateDefiningGeometry()
               ?? throw new InvalidOperationException("Rendered geometry was not created.");
    }
}
