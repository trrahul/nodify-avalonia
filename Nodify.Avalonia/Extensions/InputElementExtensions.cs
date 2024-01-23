
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Nodify.Avalonia.Extensions;

public static class InputElementExtensions
{
    public static void CaptureMouseSafe(this IInputElement self, PointerEventArgs e)
    {
        if (e.Pointer.Captured == null || e.Pointer.Captured == self)
        {
            e.Pointer.Capture(self);
        }
    }

    public static bool IsMouseCaptured(this Visual self, PointerEventArgs e)
    {
        return e.Pointer.Captured == self || e.Pointer.Captured is Visual v && ReferenceEquals(v.TemplatedParent, self);
    }
    public static bool IsPointerCapturedWithin<T>(this T self, PointerEventArgs e) where T : Visual
    {
        var pt = e.GetCurrentPoint(self);
        var v = self.Bounds.Contains(pt.Position) && pt.Pointer.Captured is Visual vi &&
                (ReferenceEquals(self,vi) || ReferenceEquals(self,vi.TemplatedParent) || ReferenceEquals(self, vi.FindAncestorOfType<T>()));
        return v;
    }
    public static void ReleaseMouseCapture(this IInputElement self, PointerEventArgs e)
    {
        e.Pointer.Capture(null);
    }

    public static T? GetElementUnderPoint<T>(this Control container, Point point)
        where T : Visual
    {
        foreach (var child in container.GetVisualsAt(point))
        {
            if (child is T visual)
            {
                return visual;
            }
            else if (child.TemplatedParent is T parentVisual)
            {
                return parentVisual;
            }
        } 
        return default(T);
    }
}