using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

public class KeyboardGesture : InputGesture
{
    private readonly KeyGesture _internal;
    public KeyboardGesture(Key key)
    {
        _internal = new KeyGesture(key);
    }

    public KeyboardGesture(Key key, KeyModifiers modifiers)
    {
        _internal = new KeyGesture(key, modifiers);
    }

    public override bool Matches(object? source, RoutedEventArgs args)
    {
        return args is KeyEventArgs kArgs && _internal.Matches(kArgs);
    }
}