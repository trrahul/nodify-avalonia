using System;

namespace Nodify.Avalonia.Shapes.Canvas;

internal static class CursorMotion
{
    private const double ReferenceStep = 0.015d;
    private static readonly TimeSpan ReferenceInterval = TimeSpan.FromSeconds(1d / 30d);

    internal static double GetInterpolationFactor(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return 0d;
        }

        double referenceIntervals = elapsed.TotalSeconds / ReferenceInterval.TotalSeconds;
        return 1d - Math.Pow(1d - ReferenceStep, referenceIntervals);
    }
}
