namespace Nodify.Avalonia.Helpers.Gestures;

/// <inheritdoc cref="MultiGesture.Match.Any" />
public sealed class AnyGesture : MultiGesture
{
    public AnyGesture(params InputGesture[] gestures) : base(Match.Any, gestures)
    {
    }
}
