using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Interactivity;
using static Nodify.Avalonia.Helpers.SelectionHelper;

namespace Nodify.Avalonia.Helpers;

internal static class EditorGesturesExtensions
{
    public static SelectionType GetSelectionType(this EditorGestures.SelectionGestures gestures, RoutedEventArgs e)
    {
        if (gestures.Append.Matches(e.Source, e))
            return SelectionType.Append;
        if (gestures.Invert.Matches(e.Source, e))
            return SelectionType.Invert;
        if (gestures.Remove.Matches(e.Source, e))
            return SelectionType.Remove;
        return SelectionType.Replace;
    }

    public static bool TryGetNavigationDirection(this EditorGestures.DirectionalNavigationGestures gestures, RoutedEventArgs e, out Vector direction)
    {
        double y = gestures.Up.Matches(e.Source, e) ? 1 : gestures.Down.Matches(e.Source, e) ? -1 : 0;
        double x = gestures.Left.Matches(e.Source, e) ? -1 : gestures.Right.Matches(e.Source, e) ? 1 : 0;
        direction = new Vector(x, y);
        return x != 0 || y != 0;
    }

    public static bool TryGetFocusDirection(this EditorGestures.DirectionalNavigationGestures gestures, RoutedEventArgs e, out FocusNavigationDirection direction)
    {
        if (gestures.Up.Matches(e.Source, e))    { direction = FocusNavigationDirection.Up;    return true; }
        if (gestures.Down.Matches(e.Source, e))  { direction = FocusNavigationDirection.Down;  return true; }
        if (gestures.Left.Matches(e.Source, e))  { direction = FocusNavigationDirection.Left;  return true; }
        if (gestures.Right.Matches(e.Source, e)) { direction = FocusNavigationDirection.Right; return true; }
        direction = default;
        return false;
    }
}
