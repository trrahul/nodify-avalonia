using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

public class PointerGesture : InputGesture
{
    private readonly MouseAction _action;
    private readonly KeyModifiers _modifiers;
    private readonly bool _ignoreModifierKeysOnRelease;
    private readonly Key _key;

    /// <summary>The mouse action this gesture matches.</summary>
    public MouseAction MouseAction => _action;

    /// <summary>The modifier keys required by this gesture.</summary>
    public KeyModifiers Modifiers => _modifiers;

    /// <summary>The key that must be held down to match this gesture. When <see cref="Key.None"/>,
    /// the gesture only matches while no non-modifier key is held.</summary>
    public Key Key => _key;

    public PointerGesture(MouseAction action, KeyModifiers modifiers)
    {
        _action = action;
        _modifiers = modifiers;
    }

    public PointerGesture(MouseAction action, KeyModifiers modifiers, bool ignoreModifierKeysOnRelease)
    {
        _action = action;
        _modifiers = modifiers;
        _ignoreModifierKeysOnRelease = ignoreModifierKeysOnRelease;
    }

    public PointerGesture(MouseAction action, KeyModifiers modifiers, Key key)
    {
        _action = action;
        _modifiers = modifiers;
        _key = key;
    }

    public PointerGesture(MouseAction action, Key key)
    {
        _action = action;
        _key = key;
    }

    public PointerGesture(MouseAction action)
    {
        _action = action;
    }

    private bool MatchesKeyboard(object? source, RoutedEventArgs args)
    {
        var context = source as Visual ?? args.Source as Visual;

        if (_key is Key.None)
        {
            return !KeyboardState.IsAnyKeyPressed(context);
        }

        return KeyboardState.IsKeyDown(_key, context);
    }

    public override bool Matches(object? source, RoutedEventArgs args)
    {
        if (args is PointerPressedEventArgs pArgs )
        {
            var buttonKind = pArgs.GetCurrentPoint(null).Properties.PointerUpdateKind;
            if (pArgs.KeyModifiers == _modifiers && MatchesKeyboard(source, args))
            {
                switch (_action)
                {
                    case MouseAction.LeftClick:
                        return buttonKind == PointerUpdateKind.LeftButtonPressed && pArgs.ClickCount == 1;
                    case MouseAction.RightClick:
                        return buttonKind == PointerUpdateKind.RightButtonPressed && pArgs.ClickCount == 1;
                    case MouseAction.MiddleClick:
                        return buttonKind == PointerUpdateKind.MiddleButtonPressed && pArgs.ClickCount == 1;
                    case MouseAction.LeftDoubleClick:
                        return buttonKind == PointerUpdateKind.LeftButtonPressed && pArgs.ClickCount == 2;
                    case MouseAction.RightDoubleClick:
                        return buttonKind == PointerUpdateKind.RightButtonPressed && pArgs.ClickCount == 2;
                    case MouseAction.MiddleDoubleClick:
                        return buttonKind == PointerUpdateKind.MiddleButtonPressed && pArgs.ClickCount == 2 ;
                    default:
                        // MouseAction.None (set by Unbind()), WheelClick, etc. never match a press.
                        return false;
                }
            }
        }
        else if (args is PointerReleasedEventArgs rArgs )
        {
            var buttonKind = rArgs.GetCurrentPoint(null).Properties.PointerUpdateKind;
            bool modifiersMatch = rArgs.KeyModifiers == _modifiers
                || (_ignoreModifierKeysOnRelease && rArgs.KeyModifiers == KeyModifiers.None);
            if (modifiersMatch && MatchesKeyboard(source, args))
            {
                switch (_action)
                {
                    case MouseAction.LeftClick:
                        return buttonKind == PointerUpdateKind.LeftButtonReleased;
                    case MouseAction.RightClick:
                        return buttonKind == PointerUpdateKind.RightButtonReleased;
                    case MouseAction.MiddleClick:
                        return buttonKind == PointerUpdateKind.MiddleButtonReleased;
                    case MouseAction.LeftDoubleClick:
                    case MouseAction.RightDoubleClick:
                    case MouseAction.MiddleDoubleClick:
                        // A double-click is matched on press (ClickCount==2), never on release.
                        return false;
                    default:
                        // MouseAction.None (set by Unbind()), WheelClick, etc. never match a release.
                        return false;
                }
            }
        }

        return false;
    }
}