using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Nodify.Avalonia.Calculator.ViewModels;

namespace Nodify.Avalonia.Calculator.Views
{
    public partial class OperationsMenuView : UserControl
    {
        // On open, focus the operations list so keyboard nav works immediately; restore this element on close.
        private readonly WeakReference<IInputElement?> _focusToRestore = new WeakReference<IInputElement?>(null);

        public OperationsMenuView()
        {
            InitializeComponent();

            var menuBorder = this.FindControl<Border>("MenuBorder")!;
            menuBorder.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsVisibleProperty)
                {
                    OnMenuVisibilityChanged(e.NewValue is true);
                }
            };
        }

        private void OnMenuVisibilityChanged(bool isVisible)
        {
            if (!IsLoaded)
            {
                return;
            }

            if (isVisible)
            {
                _focusToRestore.SetTarget(TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement());
                Dispatcher.UIThread.Post(() => this.FindControl<ItemsControl>("OperationsList")?.Focus(), DispatcherPriority.Input);
            }
            else if (_focusToRestore.TryGetTarget(out var elementToFocus) && elementToFocus is not null)
            {
                Dispatcher.UIThread.Post(() => elementToFocus.Focus(), DispatcherPriority.Input);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && DataContext is OperationsMenuViewModel menu)
            {
                menu.Close();
            }

            base.OnKeyDown(e);
        }
    }
}
