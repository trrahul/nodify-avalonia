using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

public abstract class InputGesture
{
    public abstract bool Matches(object? source, RoutedEventArgs args);
}