using Avalonia;

namespace Nodify.Avalonia.Extensions;

public static class PointExtensions
{
    public static Vector VectorSubtract(this Point self, Point other)
    {
        return new Vector(self.X - other.X, self.Y - other.Y);
    }
}