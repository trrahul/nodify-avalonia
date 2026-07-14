using System;
using Avalonia;

namespace Nodify.Avalonia.Connections;

internal static class StepConnectionGeometry
{
    public static (Point P0, Point P1, Point P2, Point P3) GetLinePoints(
        Point source,
        Point target,
        ConnectorPosition sourcePosition,
        ConnectorPosition targetPosition,
        double spacing)
    {
        var sourceDir = GetConnectorDirection(sourcePosition);
        var targetDir = GetConnectorDirection(targetPosition);

        Point startPoint = source + new Vector(spacing * sourceDir.X, spacing * sourceDir.Y);
        Point endPoint   = target + new Vector(spacing * targetDir.X, spacing * targetDir.Y);

        var connectionDir   = GetConnectionDirection(startPoint, sourcePosition, endPoint);
        bool horizontalConn = connectionDir.X != 0;

        if (IsOppositePosition(sourcePosition, targetPosition))
        {
            var (p1, p2) = GetOppositePositionPoints(
                startPoint, endPoint, sourceDir, connectionDir, horizontalConn);
            return (startPoint, p1, p2, endPoint);
        }

        if (sourcePosition == targetPosition)
        {
            var p = GetSamePositionPoint(
                startPoint, endPoint, sourceDir, connectionDir, horizontalConn);
            return (startPoint, p, p, endPoint);
        }

        bool isSameDir = horizontalConn
            ? sourceDir.X == targetDir.Y
            : sourceDir.Y == targetDir.X;
        bool startGreaterThanEnd = horizontalConn
            ? startPoint.Y > endPoint.Y
            : startPoint.X > endPoint.X;

        bool positiveDir = horizontalConn ? sourceDir.X == 1 : sourceDir.Y == 1;
        bool shouldFlip = positiveDir
            ? isSameDir ? !startGreaterThanEnd : startGreaterThanEnd
            : isSameDir ?  startGreaterThanEnd : !startGreaterThanEnd;

        if (shouldFlip)
        {
            var sourceTarget = new Point(startPoint.X, endPoint.Y);
            var targetSource = new Point(endPoint.X,   startPoint.Y);
            var pf = horizontalConn ? sourceTarget : targetSource;
            return (startPoint, pf, pf, endPoint);
        }

        var pp = GetSamePositionPoint(
            startPoint, endPoint, sourceDir, connectionDir, horizontalConn);
        return (startPoint, pp, pp, endPoint);
    }

    private static (Point P1, Point P2) GetOppositePositionPoints(
        Point startPoint, Point endPoint,
        Point sourceDir, Point connectionDir, bool horizontalConn)
    {
        var center = startPoint + (endPoint - startPoint) / 2;

        (Point P1, Point P2) verticalSplit   = (new Point(center.X, startPoint.Y), new Point(center.X, endPoint.Y));
        (Point P1, Point P2) horizontalSplit = (new Point(startPoint.X, center.Y), new Point(endPoint.X, center.Y));

        if (horizontalConn)
            return sourceDir.X == connectionDir.X ? verticalSplit : horizontalSplit;

        return sourceDir.Y == connectionDir.Y ? horizontalSplit : verticalSplit;
    }

    private static Point GetSamePositionPoint(
        Point startPoint, Point endPoint,
        Point sourceDir, Point connectionDir, bool horizontalConn)
    {
        var sourceTarget = new Point(startPoint.X, endPoint.Y);
        var targetSource = new Point(endPoint.X,   startPoint.Y);

        if (horizontalConn)
            return sourceDir.X == connectionDir.X ? targetSource : sourceTarget;

        return sourceDir.Y == connectionDir.Y ? sourceTarget : targetSource;
    }

    internal static Point GetConnectionDirection(
        in Point source, ConnectorPosition sourcePosition, in Point target)
    {
        return sourcePosition == ConnectorPosition.Left || sourcePosition == ConnectorPosition.Right
            ? new Point(Math.Sign(target.X - source.X), 0)
            : new Point(0, Math.Sign(target.Y - source.Y));
    }

    internal static Point GetConnectorDirection(ConnectorPosition position)
        => position switch
        {
            ConnectorPosition.Top    => new Point(0, -1),
            ConnectorPosition.Left   => new Point(-1, 0),
            ConnectorPosition.Bottom => new Point(0,  1),
            ConnectorPosition.Right  => new Point(1,  0),
            _                        => default,
        };

    internal static bool IsOppositePosition(
        ConnectorPosition sourcePosition, ConnectorPosition targetPosition)
    {
        return (sourcePosition == ConnectorPosition.Left   && targetPosition == ConnectorPosition.Right)
            || (sourcePosition == ConnectorPosition.Right  && targetPosition == ConnectorPosition.Left)
            || (sourcePosition == ConnectorPosition.Top    && targetPosition == ConnectorPosition.Bottom)
            || (sourcePosition == ConnectorPosition.Bottom && targetPosition == ConnectorPosition.Top);
    }

    internal static double CopySign(double value, double signSource)
        => signSource < 0 ? -Math.Abs(value) : Math.Abs(value);

    internal static Vector FlipSourceOffset(Vector baseSourceOffset, ConnectorPosition sourcePosition)
    {
        var dir = GetConnectorDirection(sourcePosition);
        return new Vector(CopySign(baseSourceOffset.X, dir.X), CopySign(baseSourceOffset.Y, dir.Y));
    }
}
