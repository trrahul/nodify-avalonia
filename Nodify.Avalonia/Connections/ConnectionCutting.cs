using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Nodify.Avalonia.Connections;

internal static class ConnectionCutting
{
    internal static bool Intersects(
        Geometry? geometry,
        double strokeThickness,
        PenLineCap lineCap,
        PenLineJoin lineJoin,
        Point start,
        Point end,
        double tolerance)
    {
        if (geometry is null)
        {
            return false;
        }

        double hitWidth = Math.Max(1d, strokeThickness) + tolerance * 2d;
        var hitPen = new Pen(Brushes.Black, hitWidth, lineCap: lineCap, lineJoin: lineJoin);
        var segmentBounds = new Rect(
            Math.Min(start.X, end.X),
            Math.Min(start.Y, end.Y),
            Math.Abs(end.X - start.X),
            Math.Abs(end.Y - start.Y));

        Rect renderBounds = geometry.GetRenderBounds(hitPen);
        if (renderBounds.Right < segmentBounds.Left
            || renderBounds.Left > segmentBounds.Right
            || renderBounds.Bottom < segmentBounds.Top
            || renderBounds.Top > segmentBounds.Bottom)
        {
            return false;
        }

        Vector segment = end - start;
        int sampleCount = Math.Max(1, (int)Math.Ceiling(segment.Length / (hitWidth / 2d)));
        for (int i = 0; i <= sampleCount; i++)
        {
            Point point = start + segment * (i / (double)sampleCount);
            if (geometry.FillContains(point) || geometry.StrokeContains(hitPen, point))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool Intersects(
        Control connection,
        Visual coordinateSpace,
        Point start,
        Point end,
        double tolerance)
    {
        foreach (Visual candidate in GetShapeCandidates(connection))
        {
            (Geometry? Geometry, double Thickness, PenLineCap Cap, PenLineJoin Join) rendered =
                candidate switch
                {
                    LayeredShape shape =>
                        (shape.GeometryForHitTesting, shape.StrokeThickness, shape.StrokeLineCap, shape.StrokeJoin),
                    Shape shape =>
                        (shape.RenderedGeometry, shape.StrokeThickness, shape.StrokeLineCap, shape.StrokeJoin),
                    _ => default
                };

            Point? localStart = coordinateSpace.TranslatePoint(start, candidate);
            Point? localEnd = coordinateSpace.TranslatePoint(end, candidate);
            if (localStart is not Point translatedStart || localEnd is not Point translatedEnd)
            {
                continue;
            }

            if (Intersects(
                    rendered.Geometry,
                    rendered.Thickness,
                    rendered.Cap,
                    rendered.Join,
                    translatedStart,
                    translatedEnd,
                    tolerance))
            {
                return true;
            }
        }

        return false;
    }

    internal static IEnumerable<Visual> GetShapeCandidates(Control connection)
    {
        foreach (Visual candidate in connection.GetSelfAndVisualDescendants())
        {
            if (candidate is LayeredShape)
            {
                yield return candidate;
            }
            else if (candidate is Shape
                     && (candidate.GetVisualParent() is not LayeredShape owner || !owner.IsGeometryHost(candidate)))
            {
                yield return candidate;
            }
        }
    }
}
