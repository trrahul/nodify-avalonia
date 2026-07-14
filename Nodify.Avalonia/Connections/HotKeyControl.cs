using Avalonia;
using Avalonia.Controls.Primitives;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Specifies how hotkeys are displayed for a pending connection.
    /// </summary>
    public enum HotKeysDisplayMode
    {
        /// <summary>
        /// No hotkeys will be displayed for the pending connection.
        /// </summary>
        None,

        /// <summary>
        /// Display hotkeys for keyboard only.
        /// </summary>
        Keyboard,

        /// <summary>
        /// Display hotkeys for both mouse and keyboard.
        /// </summary>
        All
    }

    /// <summary>
    /// Displays a numbered badge over a potential target <see cref="Connector"/> during a pending connection.
    /// </summary>
    public class HotKeyControl : TemplatedControl
    {
        public static readonly StyledProperty<int> NumberProperty =
            AvaloniaProperty.Register<HotKeyControl, int>(nameof(Number));

        /// <summary>Gets or sets the hotkey number (1–9) shown on the badge.</summary>
        public int Number
        {
            get => GetValue(NumberProperty);
            set => SetValue(NumberProperty, value);
        }

        static HotKeyControl()
        {
            IsHitTestVisibleProperty.OverrideDefaultValue<HotKeyControl>(false);
        }
    }
}
