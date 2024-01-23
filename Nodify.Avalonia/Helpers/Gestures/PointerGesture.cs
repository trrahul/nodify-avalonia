using System;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

public class PointerGesture : InputGesture
{
    private readonly MouseAction _action;
    private readonly KeyModifiers _modifiers;

    public PointerGesture(MouseAction action, KeyModifiers modifiers)
    {
        _action = action;
        _modifiers = modifiers;
    }
    public PointerGesture(MouseAction action)
    {
        _action = action;
    }

    public override bool Matches(object? source, RoutedEventArgs args)
    {
        if (args is PointerPressedEventArgs pArgs )
        {
            var buttonKind = pArgs.GetCurrentPoint(null).Properties.PointerUpdateKind;
            if (pArgs.KeyModifiers == _modifiers)
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
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        else if (args is PointerReleasedEventArgs rArgs )
        {
            var buttonKind = rArgs.GetCurrentPoint(null).Properties.PointerUpdateKind;
            if (rArgs.KeyModifiers == _modifiers)
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
                        throw new ArgumentOutOfRangeException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return false;
    }
}