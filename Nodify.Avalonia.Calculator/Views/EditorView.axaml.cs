using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.VisualBasic;
using Nodify.Avalonia.Calculator.ViewModels;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Calculator.Views
{
    public partial class EditorView : UserControl
    {
        private Size _calcSize;

        public EditorView()
        {
            InitializeComponent();
            AddHandler(PointerPressedEvent,CloseOperationsMenuFromPointer);
            AddHandler(ItemContainer.DragStartedEvent,CloseOperationsMenu);
            AddHandler(PointerReleasedEvent,OpenOperationsMenu);
        }

        private void CloseOperationsMenuFromPointer(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetChangedButton() == MouseButton.Left)
            {
                CloseOperationsMenu(sender,e);
            }
        }

        private async void OpenOperationsMenu(object? sender, PointerReleasedEventArgs e)
        {
            if (e.GetChangedButton() == MouseButton.Right && e is
                {
                    Handled: false, Source: NodifyEditor
                    {
                        IsPanning: false,
                        DataContext: CalculatorViewModel calculator
                    } editor
                })
            {
                e.Handled = true;
                _calcSize = _calcSize == default ? calculator.OperationsMenu.Bounds.Size : _calcSize;
                var calcRectangle = new Rect(editor.State.CurrentPointerArgs.GetPosition(editor), _calcSize);
                
                if (!editor.Bounds.Contains(calcRectangle))
                {
                    await editor.BringIntoView(calcRectangle);
                }
                calculator.OperationsMenu.OpenAt(editor.MouseLocation);
            }
        }

        private void CloseOperationsMenu(object? sender, RoutedEventArgs e)
        {
            object? dc = null;
            if (sender is EditorView ev)
            {
                dc = ev.Editor.DataContext;
            }
            else if (sender is ItemContainer ic)
            {
                dc = ic.Editor.DataContext;
            }

            if (!e.Handled && dc is CalculatorViewModel calculator)
            {
                calculator.OperationsMenu.Close();
            }
        }
    }
}
