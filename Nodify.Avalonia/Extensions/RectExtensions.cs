using Avalonia;

namespace Nodify.Avalonia.Extensions;

public static class RectExtensions
{
    public static bool IntersectsWith(this Rect self,Rect rect)
    {
        if (self == default || rect == default)
        {
            return false;
        }

        return (rect.Left <= self.Right) &&
               (rect.Right >= self.Left) &&
               (rect.Top <= self.Bottom) &&
               (rect.Bottom >= self.Top);
    }

    /// <summary>
    /// Inflate - inflate the bounds by the size provided, in all directions.
    /// If -width is > Width / 2 or -height is > Height / 2, this Rect becomes Empty
    /// If this is Empty, this method is illegal.
    /// </summary>
    public static Rect Inflate(this Rect r, double width, double height)
    {
        if (r == default)
        {
            throw new System.InvalidOperationException("Rect cannot be empty");
        }

        var x = r.X - width;
        var y = r.Y - height;
            
        // Do two additions rather than multiplication by 2 to avoid spurious overflow
        // That is: (A + 2 * B) != ((A + B) + B) if 2*B overflows.
        // Note that multiplication by 2 might work in this case because A should start
        // positive & be "clamped" to positive after, but consider A = Inf & B = -MAX.

        var w = r.Width + width;
        w += width;
        var h = r.Height + height;
        h += height;

        // We catch the case of inflation by less than -width/2 or -height/2 here.  This also
        // maintains the invariant that either the Rect is Empty or _width and _height are
        // non-negative, even if the user parameters were NaN, though this isn't strictly maintained
        // by other methods.
        if ( !(w >= 0 && h >= 0) )
        {
            return default;
        }
        return new Rect(x, y, w, h);
    }
}