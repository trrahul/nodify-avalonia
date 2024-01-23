using Avalonia;

namespace Nodify.Avalonia.Extensions;

public static class SizeExtensions
{
    public static Vector ToVector(this Size size)
    {
        return new Vector(size.Width, size.Height);
    }
}