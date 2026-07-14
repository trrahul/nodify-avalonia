namespace Nodify.Avalonia.Helpers.Gestures;

/// <inheritdoc cref="MultiGesture.Match.All" />
public sealed class AllGestures : MultiGesture
{
    public AllGestures(params InputGesture[] gestures) : base(Match.All, gestures)
    {
    }
}
