using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Settings
{
    public partial class EditKeybinding : UserControl
    {
        private Button _editButton = null!;
        private Path _editIcon = null!;
        private bool _isEditing;
        private bool _isRecording;
        private TopLevel? _dismissScope;

        public EditKeybinding()
        {
            InitializeComponent();
            _editButton = this.FindControl<Button>("EditButton")!;
            _editIcon = this.FindControl<Path>("EditIcon")!;
            Focusable = true;
            LostFocus += (_, _) => StopEditing();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void OnEditClick(object? sender, RoutedEventArgs e) => ToggleEditing();

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_isEditing && DataContext is KeyGestureSelectorViewModel vm)
            {
                if (!_isRecording)
                {
                    vm.Modifier = KeyModifiers.None;
                    vm.Key = Key.None;
                    _isRecording = true;
                }

                vm.Modifier = e.KeyModifiers;

                if (!IsModifierKey(e.Key))
                {
                    vm.Key = e.Key;
                }

                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _isRecording = false;
            base.OnKeyUp(e);
        }

        private void ToggleEditing()
        {
            if (_isEditing)
            {
                StopEditing();
            }
            else
            {
                StartEditing();
            }
        }

        private void StartEditing()
        {
            _editIcon.Data = TryGetIcon("Icon.PenDismiss");
            if (this.TryFindResource("AccentFillColorDefaultBrush", out var accent) && accent is IBrush b)
            {
                _editButton.BorderBrush = b;
            }
            _isEditing = true;

            // There is no persistent mouse capture, so tunnel-handle presses at the TopLevel to
            // catch the click that should dismiss recording.
            _dismissScope = TopLevel.GetTopLevel(this);
            _dismissScope?.AddHandler(PointerPressedEvent, OnDismissScopePointerPressed, RoutingStrategies.Tunnel);

            Focus();
        }

        private void StopEditing()
        {
            _editIcon.Data = TryGetIcon("Icon.Edit");
            _editButton.BorderBrush = null;
            _isEditing = false;

            if (_dismissScope is not null)
            {
                _dismissScope.RemoveHandler(PointerPressedEvent, OnDismissScopePointerPressed);
                _dismissScope = null;
            }

            if (_isRecording && DataContext is KeyGestureSelectorViewModel vm)
            {
                vm.Modifier = KeyModifiers.None;
                vm.Key = Key.None;
                _isRecording = false;
            }
        }

        private void OnDismissScopePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is Visual source && (source == this || this.IsVisualAncestorOf(source)))
            {
                return;
            }

            StopEditing();
            e.Handled = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            StopEditing();
            base.OnDetachedFromVisualTree(e);
        }

        private Geometry? TryGetIcon(string key)
            => this.TryFindResource(key, out var res) && res is Geometry g ? g : null;

        private static bool IsModifierKey(Key key)
            => key is Key.LeftCtrl or Key.RightCtrl
                   or Key.LeftShift or Key.RightShift
                   or Key.LeftAlt or Key.RightAlt
                   or Key.LWin or Key.RWin;
    }
}
