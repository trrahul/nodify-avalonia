namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Specifies the direction of focus traversal for keyboard navigation.
    /// </summary>
    public enum FocusNavigationDirection
    {
        Next,
        Previous,
        First,
        Last,
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Carries the direction and wrap state for a single focus-traversal request.
    /// </summary>
    public class TraversalRequest
    {
        public TraversalRequest(FocusNavigationDirection direction) => FocusNavigationDirection = direction;

        /// <summary>Gets the requested focus navigation direction.</summary>
        public FocusNavigationDirection FocusNavigationDirection { get; }

        /// <summary>
        /// Set to <see langword="true"/> by a navigator when it wraps around the end of its list.
        /// </summary>
        public bool Wrapped { get; set; }
    }

    /// <summary>
    /// Extension methods for <see cref="FocusNavigationDirection"/>.
    /// </summary>
    public static class FocusNavigationDirectionExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="direction"/> is the
        /// geometric or sequential opposite of <paramref name="other"/>.
        /// Pairs: Left↔Right, Up↔Down, Next↔Previous, First↔Last.
        /// </summary>
        public static bool IsOppositeOf(this FocusNavigationDirection direction, FocusNavigationDirection other)
        {
            return (direction == FocusNavigationDirection.Left && other == FocusNavigationDirection.Right)
                || (direction == FocusNavigationDirection.Right && other == FocusNavigationDirection.Left)
                || (direction == FocusNavigationDirection.Up && other == FocusNavigationDirection.Down)
                || (direction == FocusNavigationDirection.Down && other == FocusNavigationDirection.Up)
                || (direction == FocusNavigationDirection.Next && other == FocusNavigationDirection.Previous)
                || (direction == FocusNavigationDirection.Previous && other == FocusNavigationDirection.Next)
                || (direction == FocusNavigationDirection.First && other == FocusNavigationDirection.Last)
                || (direction == FocusNavigationDirection.Last && other == FocusNavigationDirection.First);
        }
    }
}
