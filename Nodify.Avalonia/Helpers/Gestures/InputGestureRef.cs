using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

/// <summary>
/// An input gesture that allows changing its logic at runtime without changing its reference.
/// Useful for classes that capture the object reference without the possibility of updating it.
/// </summary>
public sealed class InputGestureRef : InputGesture
{
    /// <summary>The referenced gesture.</summary>
    public InputGesture Value { get; set; } = MultiGesture.None;

    private InputGestureRef() { }

    internal InputGestureRef(InputGesture gesture)
    {
        Value = gesture;
    }

    public override bool Matches(object? source, RoutedEventArgs args) => Value.Matches(source, args);

    public static implicit operator InputGestureRef(PointerGesture gesture) => new InputGestureRef { Value = gesture };
    public static implicit operator InputGestureRef(KeyboardGesture gesture) => new InputGestureRef { Value = gesture };
    public static implicit operator InputGestureRef(MultiGesture gesture) => new InputGestureRef { Value = gesture };

    /// <summary>Unbinds the current gesture.</summary>
    public void Unbind() => Value = MultiGesture.None;
}

/// <summary>Extension methods for the <see cref="InputGestureRef"/> class.</summary>
public static class InputGestureRefExtensions
{
    /// <summary>Creates a new <see cref="InputGestureRef"/> from the specified gesture.</summary>
    public static InputGestureRef AsRef(this InputGesture gesture) => new InputGestureRef(gesture);
}
