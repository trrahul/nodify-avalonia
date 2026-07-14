using System;

namespace Nodify.Avalonia.Playground.Views;

internal static class FrameRateEstimator
{
    public static double FromFrameDuration(TimeSpan frameDuration)
        => frameDuration <= TimeSpan.Zero
            ? 0d
            : TimeSpan.FromSeconds(1).TotalMilliseconds / frameDuration.TotalMilliseconds;
}
