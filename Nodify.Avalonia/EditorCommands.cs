using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia
{
    public static class EditorCommands
    {
        /// <summary>
        /// Zoom in relative to the editor's viewport center.
        /// </summary>
        public static RoutedUICommand ZoomIn { get; } = new RoutedUICommand("Zoom in", nameof(ZoomIn), typeof(EditorCommands), new InputGestureCollection
        {
           EditorGestures.Mappings.Editor.ZoomIn
        });

        /// <summary>
        /// Zoom out relative to the editor's viewport center.
        /// </summary>
        public static RoutedUICommand ZoomOut { get; } = new RoutedUICommand("Zoom out", nameof(ZoomOut), typeof(EditorCommands), new InputGestureCollection
        {
            EditorGestures.Mappings.Editor.ZoomOut
        });

        /// <summary>
        /// Select all <see cref="ItemContainer"/>s in the <see cref="NodifyEditor"/>.
        /// </summary>
        public static RoutedUICommand SelectAll { get; } = ApplicationCommands.SelectAll;

        /// <summary>
        /// Moves the <see cref="NodifyEditor.ViewportLocation"/> to the specified location.
        /// Parameter is a <see cref="Point"/> or a string that can be converted to a point.
        /// </summary>
        public static RoutedUICommand BringIntoView { get; } = new RoutedUICommand("Bring location into view", nameof(BringIntoView), typeof(EditorCommands), new InputGestureCollection
        {
            EditorGestures.Mappings.Editor.ResetViewport
        });

        /// <summary>
        /// Scales the editor's viewport to fit all the <see cref="ItemContainer"/>s if that's possible.
        /// </summary>
        public static RoutedUICommand FitToScreen { get; } = new RoutedUICommand("Fit to screen", nameof(FitToScreen), typeof(EditorCommands), new InputGestureCollection
        {
            EditorGestures.Mappings.Editor.FitToScreen
        });

        /// <summary>
        /// Aligns <see cref="NodifyEditor.SelectedItems"/> using the specified alignment method.
        /// Parameter is of type <see cref="Nodify.Avalonia.Alignment"/> or a string that can be converted to an alignment.
        /// </summary>
        public static RoutedUICommand Align { get; } = new RoutedUICommand("Align", nameof(Align), typeof(EditorCommands));

        /// <summary>
        /// Locks the position of the <see cref="NodifyEditor.SelectedContainers"/>.
        /// </summary>
        public static RoutedUICommand LockSelection { get; } = new RoutedUICommand("Lock selection", nameof(LockSelection), typeof(EditorCommands));

        /// <summary>
        /// Unlocks the position of the <see cref="NodifyEditor.SelectedContainers"/>.
        /// </summary>
        public static RoutedUICommand UnlockSelection { get; } = new RoutedUICommand("Unlock selection", nameof(UnlockSelection), typeof(EditorCommands));

        internal static void Register(IRoutedCommandBindable routedCommandBindable)
        {
            RegisterClassCommandBinding(routedCommandBindable,new RoutedCommandBinding(ZoomIn, OnZoomIn, OnQueryStatusZoomIn));
            RegisterClassCommandBinding(routedCommandBindable,new RoutedCommandBinding(ZoomOut, OnZoomOut, OnQueryStatusZoomOut));
            RegisterClassCommandBinding(routedCommandBindable,new RoutedCommandBinding(SelectAll, OnSelectAll, OnQuerySelectAllStatus));
            RegisterClassCommandBinding(routedCommandBindable,new RoutedCommandBinding(BringIntoView, OnBringIntoView, OnQueryBringIntoViewStatus));
            RegisterClassCommandBinding(routedCommandBindable, new RoutedCommandBinding(FitToScreen, OnFitToScreen, OnQueryFitToScreenStatus));
            RegisterClassCommandBinding(routedCommandBindable, new RoutedCommandBinding(Align, OnAlign, OnQueryAlignStatus));
            RegisterClassCommandBinding(routedCommandBindable, new RoutedCommandBinding(LockSelection, OnLockSelection, OnQueryLockSelectionStatus));
            RegisterClassCommandBinding(routedCommandBindable, new RoutedCommandBinding(UnlockSelection, OnUnlockSelection, OnQueryUnlockSelectionStatus));
        }

        public static void RegisterClassCommandBinding(IRoutedCommandBindable routedCommandBindable,RoutedCommandBinding commandBinding)
        {
            lock (((ICollection)routedCommandBindable.CommandBindings).SyncRoot)
            {
                if (routedCommandBindable.CommandBindings.Contains(commandBinding))
                {
                    throw new InvalidOperationException("Command binding already exists");
                }
                routedCommandBindable.CommandBindings.Add(commandBinding);
            }
        }


        private static void OnQueryAlignStatus(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = (editor.SelectedItems?.Count ?? 0) > 1;
            }
        }

        private static void OnAlign(object? sender,ExecutedRoutedEventArgs e)
        {
            // Delegates to NodifyEditor.AlignSelection, which guards on IsDragging and toggles it
            // itself — so ItemsDragStartedCommand/ItemsDragCompletedCommand fire via the IsDragging
            // property transition. This handler must NOT fire those commands manually (that would double-fire).
            if (sender is NodifyEditor editor && editor.SelectedContainersCount > 0)
            {
                if (e.Parameter is Alignment alignment)
                {
                    editor.AlignSelection(alignment, e.Source as ItemContainer);
                }
                else if (e.Parameter is string str && Enum.TryParse(str, true, out alignment))
                {
                    editor.AlignSelection(alignment, e.Source as ItemContainer);
                }
                else
                {
                    editor.AlignSelection(Alignment.Top, e.Source as ItemContainer);
                }
            }
        }

        private static void OnQueryLockSelectionStatus(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = editor.SelectedContainers.Any(c => c.IsDraggable);
            }
        }

        private static void OnLockSelection(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.LockSelection();
            }
        }

        private static void OnQueryUnlockSelectionStatus(object? sender, CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = editor.SelectedContainers.Any(c => !c.IsDraggable);
            }
        }

        private static void OnUnlockSelection(object? sender, ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.UnlockSelection();
            }
        }

        private static void OnQueryBringIntoViewStatus(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = !editor.DisablePanning;
            }
        }

        private static async void OnBringIntoView(object? sender,ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                switch (e.Parameter)
                {
                    case Point location:
                        await editor.BringIntoView(location);
                        break;
                    case Rect rect:
                        await editor.BringIntoView(rect, animated: true);
                        break;
                    case string str:
                        await editor.BringIntoView(Point.Parse(str));
                        break;
                    default:
                        await editor.ResetViewport();
                        break;
                }
            }
        }

        private static void OnQueryFitToScreenStatus(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = editor.Items.Count != 0;
            }
        }

        private static void OnFitToScreen(object? sender,ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.FitToScreen();
            }
        }

        private static void OnQuerySelectAllStatus(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = !editor.IsSelecting && editor.CanSelectMultipleItems;
            }
        }

        private static void OnSelectAll(object? sender,ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.SelectAll();
            }
        }

        private static void OnQueryStatusZoomIn(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = editor.ViewportZoom < editor.MaxViewportZoom;
            }
        }

        private static void OnZoomIn(object? sender,ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.ZoomIn();
            }
        }

        private static void OnQueryStatusZoomOut(object? sender,CanExecuteRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                e.CanExecute = editor.ViewportZoom > editor.MinViewportZoom;
            }
        }

        private static void OnZoomOut(object? sender,ExecutedRoutedEventArgs e)
        {
            if (sender is NodifyEditor editor)
            {
                editor.ZoomOut();
            }
        }
    }
}
