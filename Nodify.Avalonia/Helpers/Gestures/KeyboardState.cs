using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodify.Avalonia.Helpers.Gestures;

/// <summary>
/// Tracks currently-held keys from class-level key events, so <see cref="PointerGesture"/>
/// can match gestures that require a held key.
/// State is kept per <see cref="TopLevel"/>: key events only reach the window that owns focus,
/// and a window that goes away must not leak "held" keys into other windows.
/// </summary>
public static class KeyboardState
{
    private static readonly ConditionalWeakTable<TopLevel, HashSet<Key>> _pressedKeys = new ConditionalWeakTable<TopLevel, HashSet<Key>>();

    // Windows whose Deactivated event we already hooked to drop stale key state
    // (a key released while another app is focused never produces a KeyUp here).
    private static readonly ConditionalWeakTable<Window, object> _hookedWindows = new ConditionalWeakTable<Window, object>();

    static KeyboardState()
    {
        // KeyDown/KeyUp route Tunnel | Bubble; handledEventsToo so gesture-consumed keys stay tracked.
        InputElement.KeyDownEvent.AddClassHandler<InputElement>(
            OnKeyDown,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);

        InputElement.KeyUpEvent.AddClassHandler<InputElement>(
            (sender, e) => KeysOf(sender)?.Remove(e.Key),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private static HashSet<Key>? KeysOf(InputElement sender)
        => TopLevel.GetTopLevel(sender as Visual) is { } topLevel
            ? _pressedKeys.GetValue(topLevel, _ => new HashSet<Key>())
            : null;

    private static void OnKeyDown(InputElement sender, KeyEventArgs e)
    {
        if (TopLevel.GetTopLevel(sender as Visual) is not { } topLevel)
            return;

        _pressedKeys.GetValue(topLevel, _ => new HashSet<Key>()).Add(e.Key);

        if (topLevel is Window window && !_hookedWindows.TryGetValue(window, out _))
        {
            _hookedWindows.Add(window, new object());
            window.Deactivated += (_, _) =>
            {
                if (_pressedKeys.TryGetValue(window, out var keys))
                    keys.Clear();
            };
        }
    }

    private static HashSet<Key>? Resolve(Visual? context)
        => context is not null && TopLevel.GetTopLevel(context) is { } topLevel
            && _pressedKeys.TryGetValue(topLevel, out var keys)
            ? keys
            : null;

    /// <summary>Whether the specified key is currently held down in <paramref name="context"/>'s window.</summary>
    public static bool IsKeyDown(Key key, Visual? context)
        => Resolve(context)?.Contains(key) == true;

    // The non-modifier keys whose being held suppresses Key.None mouse gestures.
    private static readonly Key[] _allKeys = new[]
    {
        Key.A, Key.B, Key.C, Key.D, Key.E, Key.F, Key.G, Key.H, Key.I, Key.J,
        Key.K, Key.L, Key.M, Key.N, Key.O, Key.P, Key.Q, Key.R, Key.S, Key.T,
        Key.U, Key.V, Key.W, Key.X, Key.Y, Key.Z,
        Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,

        Key.Oem3, Key.OemMinus, Key.OemPlus, Key.OemOpenBrackets, Key.OemCloseBrackets,
        Key.Oem5, Key.Oem1, Key.OemQuotes, Key.OemComma, Key.OemPeriod, Key.Oem2,

        Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6, Key.F7, Key.F8,
        Key.F9, Key.F10, Key.F11, Key.F12,

        Key.Left, Key.Right, Key.Up, Key.Down,
        Key.PageUp, Key.PageDown, Key.Home, Key.End,

        Key.Back, Key.Delete, Key.Insert,

        Key.Space, Key.Return, Key.Escape, Key.Tab,

        Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4,
        Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
        Key.Multiply, Key.Add, Key.Subtract, Key.Divide, Key.Decimal
    };

    /// <summary>Whether any non-modifier key is currently held down in <paramref name="context"/>'s window.</summary>
    public static bool IsAnyKeyPressed(Visual? context)
    {
        if (Resolve(context) is not { Count: > 0 } pressed)
            return false;

        foreach (var key in _allKeys)
        {
            if (pressed.Contains(key))
                return true;
        }

        return false;
    }
}
