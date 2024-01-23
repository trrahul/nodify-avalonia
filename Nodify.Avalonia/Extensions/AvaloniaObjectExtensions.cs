using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using Avalonia.Win32;
using VisualExtensions = Avalonia.VisualTree.VisualExtensions;

namespace Nodify.Avalonia.Extensions
{
    internal static class AvaloniaObjectExtensions
    {
        //public static T? GetParentOfType<T>(this AvaloniaObject child)
        //    where T : AvaloniaObject
        //{
        //    Control? current = child;
        //    current.FindAncestorOfType<>()
        //    do
        //    {
        //        current = VisualTreeHelper.GetParent(current);
        //        if (current == default)
        //        {
        //            return default;
        //        }

        //    } while (!(current is T));

        //    return (T)current;
        //}

        

        public static Window? GetWindow(this Control control)
        {
            return control.FindAncestorOfType<Window>();
        }

        public static IMouseDevice GetMouseDevice(this Control control)
        {
            var window = control.FindAncestorOfType<Window>();
            return null; //todo
        }

        //public static bool CaptureMouseSafe(this UIElement elem)
        //{
        //    if (Mouse.Captured == null || elem.IsMouseCaptured)
        //    {
        //        return elem.CaptureMouse();
        //    }

        //    return false;
        //}

        #region Animation

        //public static void StartAnimation(this UIElement animatableElement, DependencyProperty dependencyProperty, Point toValue, double animationDurationSeconds, EventHandler? completedEvent = null)
        //{
        //    var fromValue = (Point)animatableElement.GetValue(dependencyProperty);

        //    PointAnimation animation = new PointAnimation
        //    {
        //        From = fromValue,
        //        To = toValue,
        //        Duration = TimeSpan.FromSeconds(animationDurationSeconds)
        //    };

        //    animation.Completed += delegate (object? sender, EventArgs e)
        //    {
        //        animatableElement.SetValue(dependencyProperty, animatableElement.GetValue(dependencyProperty));
        //        animatableElement.CancelAnimation(dependencyProperty);

        //        completedEvent?.Invoke(sender, e);
        //    };

        //    animation.Freeze();

        //    animatableElement.BeginAnimation(dependencyProperty, animation);
        //}

        //public static void CancelAnimation(this UIElement animatableElement, DependencyProperty dependencyProperty)
        //    => animatableElement.BeginAnimation(dependencyProperty, null);

        #endregion
    }
}
