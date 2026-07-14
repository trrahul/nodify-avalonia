using System.Linq;
using Avalonia.Input;
using Nodify.Avalonia.Helpers.Gestures;
using Nodify.Avalonia.Shared;

namespace Nodify.Avalonia.Workflow.Settings
{
    public abstract class GestureSelectorViewModel : ObservableObject
    {
        protected GestureSelectorViewModel(InputGestureRef gestureRef)
        {
            GestureRef = gestureRef;
        }

        public InputGestureRef GestureRef { get; }
    }

    public sealed class KeyGestureSelectorViewModel : GestureSelectorViewModel
    {
        private KeyModifiers _modifier;
        public KeyModifiers Modifier
        {
            get => _modifier;
            set
            {
                if (SetProperty(ref _modifier, value))
                {
                    Rebuild();
                }
            }
        }

        private Key _key;
        public Key Key
        {
            get => _key;
            set
            {
                if (SetProperty(ref _key, value))
                {
                    Rebuild();
                }
            }
        }

        private Key _comboTriggerKey;
        public Key ComboTriggerKey
        {
            get => _comboTriggerKey;
            set
            {
                if (SetProperty(ref _comboTriggerKey, value))
                {
                    Rebuild();
                }
            }
        }

        private bool _suppressRebuild;

        public KeyGestureSelectorViewModel(InputGestureRef gestureRef) : base(gestureRef)
        {
            _suppressRebuild = true;

            Key = gestureRef.Value switch
            {
                MultiGesture multi => multi.Gestures.OfType<KeyboardGesture>().FirstOrDefault()?.Key ?? Key.None,
                KeyboardGesture kb => kb.Key,
                _ => Key.None
            };

            Modifier = gestureRef.Value switch
            {
                MultiGesture multi => multi.Gestures.OfType<KeyboardGesture>().FirstOrDefault()?.Modifiers ?? KeyModifiers.None,
                KeyboardGesture kb => kb.Modifiers,
                _ => KeyModifiers.None
            };

            ComboTriggerKey = gestureRef.Value is KeyComboGesture combo ? combo.TriggerKey : Key.None;

            _suppressRebuild = false;
        }

        private void Rebuild()
        {
            if (_suppressRebuild)
            {
                return;
            }

            GestureRef.Value = ComboTriggerKey == Key.None
                ? new KeyboardGesture(Key, Modifier)
                : new KeyComboGesture(ComboTriggerKey, Key, Modifier);
        }
    }

    public sealed class MouseActionViewModel
    {
        public MouseActionViewModel(MouseAction action)
        {
            Action = action;
        }

        public MouseAction Action { get; }

        public string Name => Action switch
        {
            MouseAction.LeftClick => "Left Click",
            MouseAction.RightClick => "Right Click",
            MouseAction.MiddleClick => "Middle Click",
            _ => Action.ToString()
        };
    }

    public sealed class MouseGestureSelectorViewModel : GestureSelectorViewModel
    {
        private KeyModifiers _modifier;
        public KeyModifiers Modifier
        {
            get => _modifier;
            set
            {
                if (SetProperty(ref _modifier, value))
                {
                    Rebuild();
                }
            }
        }

        private Key _key;
        public Key Key
        {
            get => _key;
            set
            {
                if (SetProperty(ref _key, value))
                {
                    Rebuild();
                }
            }
        }

        private MouseAction _selectedMouseAction;
        public MouseAction SelectedMouseAction
        {
            get => _selectedMouseAction;
            set
            {
                if (SetProperty(ref _selectedMouseAction, value))
                {
                    Rebuild();
                }
            }
        }

        public static MouseActionViewModel[] AvailableOptions { get; } =
        {
            new(MouseAction.LeftClick),
            new(MouseAction.RightClick),
            new(MouseAction.MiddleClick),
        };

        private bool _suppressRebuild;

        public MouseGestureSelectorViewModel(InputGestureRef gestureRef) : base(gestureRef)
        {
            _suppressRebuild = true;

            Modifier = gestureRef.Value switch
            {
                PointerGesture pointer => pointer.Modifiers,
                MultiGesture multi => multi.Gestures.OfType<PointerGesture>().FirstOrDefault()?.Modifiers ?? KeyModifiers.None,
                _ => KeyModifiers.None
            };

            Key = gestureRef.Value switch
            {
                PointerGesture pointer => pointer.Key,
                MultiGesture multi => multi.Gestures.OfType<PointerGesture>().FirstOrDefault()?.Key ?? Key.None,
                _ => Key.None
            };

            SelectedMouseAction = gestureRef.Value switch
            {
                PointerGesture pointer => pointer.MouseAction,
                MultiGesture multi => multi.Gestures.OfType<PointerGesture>().FirstOrDefault()?.MouseAction ?? MouseAction.None,
                _ => MouseAction.None
            };

            _suppressRebuild = false;
        }

        private void Rebuild()
        {
            if (_suppressRebuild)
            {
                return;
            }

            GestureRef.Value = new PointerGesture(SelectedMouseAction, Modifier, Key);
        }
    }
}
