using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures
{
    /// <summary>Combines multiple input gestures.</summary>
    public class MultiGesture : InputGesture
    {
        /// <summary>A gesture that never matches. Used as the default for unbound gestures.</summary>
        public static readonly MultiGesture None = new MultiGesture(Match.Any);

        /// <summary>The strategy used by <see cref="Matches(object, RoutedEventArgs)"/>.</summary>
        public enum Match
        {
            /// <summary>Any gesture can match.</summary>
            Any,
            /// <summary>All gestures must match.</summary>
            All
        }

        private readonly InputGesture[] _gestures;
        private readonly Match _match;

        /// <summary>Gets the gestures combined by this instance.</summary>
        public InputGesture[] Gestures => _gestures;

        /// <summary>Constructs an instance of a <see cref="MultiGesture"/>.</summary>
        /// <param name="match">The matching strategy.</param>
        /// <param name="gestures">The input gestures.</param>
        public MultiGesture(Match match, params InputGesture[] gestures)
        {
            _gestures = gestures;
            _match = match;
        }

        /// <inheritdoc />
        public override bool Matches(object? targetElement, RoutedEventArgs inputEventArgs)
        {
            if (_match == Match.Any)
            {
                return MatchesAny(targetElement, inputEventArgs);
            }

            return MatchesAll(targetElement, inputEventArgs);
        }

        private bool MatchesAll(object? targetElement, RoutedEventArgs inputEventArgs)
        {
            for (int i = 0; i < _gestures.Length; i++)
            {
                if (!_gestures[i].Matches(targetElement, inputEventArgs))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesAny(object? targetElement, RoutedEventArgs inputEventArgs)
        {
            for (int i = 0; i < _gestures.Length; i++)
            {
                if (_gestures[i].Matches(targetElement, inputEventArgs))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
