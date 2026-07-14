using Avalonia.Input;
using Avalonia.Interactivity;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Helpers.Gestures;

/// <summary>
/// Represents a keyboard gesture that requires a trigger key to be held down
/// before pressing a combo key. For example, press and hold Space, then press Left arrow.
/// </summary>
public class KeyComboGesture : KeyboardGesture
{
    private static readonly WeakReferenceCollection<KeyComboGesture> _allCombos = new WeakReferenceCollection<KeyComboGesture>(16);

    private bool _isTriggerDown;
    private int _comboCounter;

    private bool HasBeenPerformedAtLeastOnce => _comboCounter > 0;

    /// <summary>
    /// Gets or sets the key that must be pressed first to activate this combo gesture.
    /// </summary>
    public Key TriggerKey { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the combo key can be repeatedly triggered
    /// without releasing the trigger key.
    /// </summary>
    public bool AllowRepeatingComboKey { get; set; }

    static KeyComboGesture()
    {
        // KeyUpEvent routes Tunnel | Bubble; subscribe to both so the combo is reset even when a
        // tunneling handler marks the key handled first.
        InputElement.KeyUpEvent.AddClassHandler<InputElement>(
            (_, e) => HandleKeyUp(e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);

        // LostFocusEvent routes Bubble only.
        InputElement.LostFocusEvent.AddClassHandler<InputElement>(
            (_, _) => ResetAll(),
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyComboGesture"/> class with the specified trigger and combo keys.
    /// </summary>
    /// <param name="triggerKey">The key that must be pressed first.</param>
    /// <param name="comboKey">The combo key pressed while the trigger key is held.</param>
    public KeyComboGesture(Key triggerKey, Key comboKey) : base(comboKey)
    {
        TriggerKey = triggerKey;
        _allCombos.Add(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyComboGesture"/> class with the specified trigger and combo keys and modifiers.
    /// </summary>
    /// <param name="triggerKey">The key that must be pressed first.</param>
    /// <param name="comboKey">The combo key pressed while the trigger key is held.</param>
    /// <param name="modifiers">Any modifier keys required for the combo key.</param>
    public KeyComboGesture(Key triggerKey, Key comboKey, KeyModifiers modifiers) : base(comboKey, modifiers)
    {
        TriggerKey = triggerKey;
        _allCombos.Add(this);
    }

    private static void HandleKeyUp(KeyEventArgs e)
    {
        foreach (var combo in _allCombos)
        {
            if (e.Key == combo.TriggerKey)
            {
                // We don't want to handle the event if only the trigger key was pressed.
                if (combo.HasBeenPerformedAtLeastOnce)
                {
                    e.Handled = true;
                }
                combo.Reset();
            }
        }
    }

    private static void ResetAll()
    {
        foreach (var combo in _allCombos)
        {
            combo.Reset();
        }
    }

    private void Reset()
    {
        _isTriggerDown = false;
        _comboCounter = 0;
    }

    public override bool Matches(object? source, RoutedEventArgs args)
    {
        // Only act on key-down events; detect key-down by checking the routed event identity.
        if (args is KeyEventArgs ke && ke.RoutedEvent == InputElement.KeyDownEvent)
        {
            if (ke.Key == TriggerKey)
            {
                _isTriggerDown = true;
            }

            bool matches = _isTriggerDown && base.Matches(source, args);
            if (!matches)
            {
                return false;
            }

            _comboCounter++;

            if (!AllowRepeatingComboKey)
            {
                _isTriggerDown = false;
            }

            return true;
        }

        return false;
    }
}
