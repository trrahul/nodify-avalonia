
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Extensions;

public static class InputElementExtensions
{
    public static void CaptureMouseSafe(this IInputElement self, PointerEventArgs e)
    {
        if (e.Pointer.Captured == null
            || e.Pointer.Captured == self
            || IsCapturedByVisualAncestor(self, e.Pointer.Captured)
            || IsCapturedByNonConnectorDescendant(self, e.Pointer.Captured))
        {
            e.Pointer.Capture(self);
        }
    }

    public static bool IsMouseCaptured(this Visual self, PointerEventArgs e)
    {
        if (e.Pointer.Captured is not Visual v)
        {
            return false;
        }

        // The pointer may be captured on press by controls and presenters in the same visual
        // chain (for example a ScrollViewer presenter around an editor). Treat those implicit
        // captures as capturable by the interaction target.
        return ReferenceEquals(v, self)
            || ReferenceEquals(v.TemplatedParent, self)
            || v.GetVisualAncestors().Contains(self)
            || self.GetVisualAncestors().Contains(v);
    }

    private static bool IsCapturedByVisualAncestor(IInputElement self, IInputElement? captured)
        => self is Visual selfVisual
           && captured is Visual capturedVisual
           && selfVisual.GetVisualAncestors().Contains(capturedVisual);

    private static bool IsCapturedByNonConnectorDescendant(IInputElement self, IInputElement? captured)
    {
        if (self is not Visual selfVisual || captured is not Visual capturedVisual)
        {
            return false;
        }

        bool capturedWithinSelf = ReferenceEquals(capturedVisual.TemplatedParent, self)
                                  || capturedVisual.GetVisualAncestors().Contains(selfVisual);
        if (!capturedWithinSelf)
        {
            return false;
        }

        if (self is ItemContainer
            && (capturedVisual is Connector
                || capturedVisual.TemplatedParent is Connector
                || capturedVisual.GetVisualAncestors().OfType<Connector>().Any()))
        {
            return false;
        }

        return true;
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
        foreach (Visual child in container.GetVisualsAt(point))
        {
            for (Visual? current = child; current != null; current = current.GetVisualParent())
            {
                if (current is T visual)
                {
                    return visual;
                }

                if (current.TemplatedParent is T parentVisual)
                {
                    return parentVisual;
                }

                if (ReferenceEquals(current, container))
                {
                    break;
                }
            }
        }

        return default;
    }
}
