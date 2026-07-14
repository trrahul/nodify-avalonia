namespace Nodify.Avalonia.Helpers;

internal static class MathExtensions
{
    // Wraps into the half-open range [min, max).
    public static double WrapToRange(this double value, double min, double max)
    {
        double range = max - min;
        value = (value - min) % range;
        return value < 0 ? value + range + min : value + min;
    }
}
