using Nodify.Avalonia.Workflow.Common;
using I = Nodify.Avalonia.Workflow.Common.WorkflowIcon;

namespace Nodify.Avalonia.Workflow.Settings
{
    public sealed class EditorKeybindingsListViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Editor";

        public EditorKeybindingsListViewModel(EditorGestures gestures)
        {
            Title = "Editor";

            Keybindings.Add(new KeybindingsGroupViewModel("Selection")
            {
                new KeybindingViewModel("Replace", "This gesture will replace the current selection", gestures.Editor.Selection.Replace, I.SquareHint),
                new KeybindingViewModel("Remove", "This gesture will remove items from the current selection", gestures.Editor.Selection.Remove, I.SquareEraser),
                new KeybindingViewModel("Append", "This gesture will append items to the current selection", gestures.Editor.Selection.Append, I.SquaresNested),
                new KeybindingViewModel("Invert", "This gesture will invert the current selection", gestures.Editor.Selection.Invert, I.SquareShadow),
                new KeybindingViewModel("Toggle selected", "This gesture will toggle selection for the focused item", gestures.Editor.Keyboard.ToggleSelected, I.Square),
                new KeybindingViewModel("Deselect all", "This gesture will deselect all the selected items", gestures.Editor.Keyboard.DeselectAll, I.SquareDismiss),
                new KeybindingViewModel("Select all", "This gesture will select all the items in the viewport", gestures.Editor.SelectAll, I.SelectObject),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Navigation")
            {
                new KeybindingViewModel("Navigate up", "This gesture will move the focus up", gestures.Editor.Keyboard.NavigateSelection.Up, I.ArrowUp),
                new KeybindingViewModel("Navigate down", "This gesture will move the focus down", gestures.Editor.Keyboard.NavigateSelection.Down, I.ArrowDown),
                new KeybindingViewModel("Navigate left", "This gesture will move the focus left", gestures.Editor.Keyboard.NavigateSelection.Left, I.ArrowLeft),
                new KeybindingViewModel("Navigate right", "This gesture will move the focus right", gestures.Editor.Keyboard.NavigateSelection.Right, I.ArrowRight),
                new KeybindingViewModel("Next navigation layer", "This gesture will move focus to the next navigation layer", gestures.Editor.Keyboard.NextNavigationLayer, I.ArrowNext),
                new KeybindingViewModel("Previous navigation layer", "This gesture will move focus to the previous navigation layer", gestures.Editor.Keyboard.PrevNavigationLayer, I.ArrowPrevious),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Panning")
            {
                new KeybindingViewModel("Pan", "This gesture will move the viewport in the editor", gestures.Editor.Pan, I.HandWave),
                new KeybindingViewModel("Pan up", "This gesture will move the viewport up", gestures.Editor.Keyboard.Pan.Up, I.HandWave),
                new KeybindingViewModel("Pan down", "This gesture will move the viewport down", gestures.Editor.Keyboard.Pan.Down, I.HandWave),
                new KeybindingViewModel("Pan left", "This gesture will move the viewport left", gestures.Editor.Keyboard.Pan.Left, I.HandWave),
                new KeybindingViewModel("Pan right", "This gesture will move the viewport right", gestures.Editor.Keyboard.Pan.Right, I.HandWave),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Dragging")
            {
                new KeybindingViewModel("Up", "This gesture will move the current selection up", gestures.Editor.Keyboard.DragSelection.Up, I.ArrowUp),
                new KeybindingViewModel("Down", "This gesture will move the current selection down", gestures.Editor.Keyboard.DragSelection.Down, I.ArrowDown),
                new KeybindingViewModel("Left", "This gesture will move the current selection left", gestures.Editor.Keyboard.DragSelection.Left, I.ArrowLeft),
                new KeybindingViewModel("Right", "This gesture will move the current selection right", gestures.Editor.Keyboard.DragSelection.Right, I.ArrowRight),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Others")
            {
                new KeybindingViewModel("Zoom in", "This gesture will zoom in the viewport", gestures.Editor.ZoomIn, I.ZoomIn),
                new KeybindingViewModel("Zoom out", "This gesture will zoom out the viewport", gestures.Editor.ZoomOut, I.ZoomOut),
                new KeybindingViewModel("Reset viewport", "This gesture will reset the viewport location and scale", gestures.Editor.ResetViewport, I.ArrowMoveInward),
                new KeybindingViewModel("Fit to screen", "This gesture will scale and reposition the viewport to fit as many items as possible", gestures.Editor.FitToScreen, I.ZoomFit),
                new KeybindingViewModel("Cut connections", "This gesture cut the intersecting connections", gestures.Editor.Cutting, I.LineDashes),
                new KeybindingViewModel("Push items", "This gesture will push items in the viewport", gestures.Editor.PushItems, I.PaddingRight),
                new KeybindingViewModel("Cancel action", "This gesture will cancel the current action", gestures.Editor.CancelAction, I.DismissCircle),
            });
        }
    }

    public sealed class ItemContainerKeybindingsViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Item container";

        public ItemContainerKeybindingsViewModel(EditorGestures gestures)
        {
            Title = "Item container";

            Keybindings.Add(new KeybindingsGroupViewModel("Selection")
            {
                new KeybindingViewModel("Replace", "This gesture will replace the current selection with the current item", gestures.ItemContainer.Selection.Replace, I.SquareHint),
                new KeybindingViewModel("Remove", "This gesture will remove the current item from the selection", gestures.ItemContainer.Selection.Remove, I.SquareEraser),
                new KeybindingViewModel("Append", "This gesture will append the current item to the selection", gestures.ItemContainer.Selection.Append, I.SquaresNested),
                new KeybindingViewModel("Invert", "This gesture will invert the current selection of the item", gestures.ItemContainer.Selection.Invert, I.SquareShadow),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Others")
            {
                new KeybindingViewModel("Start dragging", "This gesture will start moving the current selected item", gestures.ItemContainer.Drag, I.ArrowMove),
                new KeybindingViewModel("Cancel action", "This gesture will cancel the current action", gestures.ItemContainer.CancelAction, I.DismissCircle),
            });
        }
    }

    public sealed class ConnectorKeybindingsViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Connector";

        public ConnectorKeybindingsViewModel(EditorGestures gestures)
        {
            Title = "Connector";

            Keybindings.Add(new KeybindingsGroupViewModel("General")
            {
                new KeybindingViewModel("Start connecting", "This gesture will start connecting the current connector", gestures.Connector.Connect, I.PlugConnectedAdd),
                new KeybindingViewModel("Disconnect", "This gesture will disconnect the current connector", gestures.Connector.Disconnect, I.PlugDisconnected),
                new KeybindingViewModel("Cancel action", "This gesture will cancel the current action", gestures.Connector.CancelAction, I.DismissCircle),
            });
        }
    }

    public sealed class ConnectionKeybindingsViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Connection";

        public ConnectionKeybindingsViewModel(EditorGestures gestures)
        {
            Title = "Connection";

            Keybindings.Add(new KeybindingsGroupViewModel("Selection")
            {
                new KeybindingViewModel("Replace", "This gesture will replace the current selection with the current item", gestures.Connection.Selection.Replace, I.SquareHint),
                new KeybindingViewModel("Remove", "This gesture will remove the current item from the selection", gestures.Connection.Selection.Remove, I.SquareEraser),
                new KeybindingViewModel("Append", "This gesture will append the current item to the selection", gestures.Connection.Selection.Append, I.SquaresNested),
                new KeybindingViewModel("Invert", "This gesture will invert the current selection of the item", gestures.Connection.Selection.Invert, I.SquareShadow),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Others")
            {
                new KeybindingViewModel("Split", "This gesture will split the current connection", gestures.Connection.Split, I.LineHorizontal1DashDotDash),
                new KeybindingViewModel("Disconnect", "This gesture will remove the current connection", gestures.Connection.Disconnect, I.PlugDisconnected),
            });
        }
    }

    public sealed class GroupingNodeKeybindingsViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Grouping node";

        public GroupingNodeKeybindingsViewModel(EditorGestures gestures)
        {
            Title = "Grouping node";

            Keybindings.Add(new KeybindingsGroupViewModel("General")
            {
                new KeybindingViewModel("Toggle content selection", "This gesture will toggle the content selection", gestures.GroupingNode.ToggleContentSelection, I.PlugConnectedAdd),
            });
        }
    }

    public sealed class MinimapKeybindingsViewModel : KeybindingsListViewModel, INavigatable
    {
        public static string RouteKey => $"{KeybindingsSettingsViewModel.RouteKey}/Minimap";

        public MinimapKeybindingsViewModel(EditorGestures gestures)
        {
            Title = "Minimap";

            Keybindings.Add(new KeybindingsGroupViewModel("General")
            {
                new KeybindingViewModel("Start connecting", "This gesture will start connecting the current connector", gestures.Connector.Connect, I.PlugConnectedAdd),
                new KeybindingViewModel("Disconnect", "This gesture will disconnect the current connector", gestures.Connector.Disconnect, I.PlugDisconnected),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Panning")
            {
                new KeybindingViewModel("Pan", "This gesture will move the viewport inside the minimap", gestures.Minimap.DragViewport, I.HandWave),
                new KeybindingViewModel("Pan up", "This gesture will move the viewport up", gestures.Minimap.Pan.Up, I.HandWave),
                new KeybindingViewModel("Pan down", "This gesture will move the viewport down", gestures.Minimap.Pan.Down, I.HandWave),
                new KeybindingViewModel("Pan left", "This gesture will move the viewport left", gestures.Minimap.Pan.Left, I.HandWave),
                new KeybindingViewModel("Pan right", "This gesture will move the viewport right", gestures.Minimap.Pan.Right, I.HandWave),
            });

            Keybindings.Add(new KeybindingsGroupViewModel("Others")
            {
                new KeybindingViewModel("Zoom in", "This gesture will zoom in the viewport", gestures.Minimap.ZoomIn, I.ZoomIn),
                new KeybindingViewModel("Zoom out", "This gesture will zoom out the viewport", gestures.Minimap.ZoomOut, I.ZoomOut),
                new KeybindingViewModel("Reset viewport", "This gesture will reset the viewport location and scale", gestures.Minimap.ResetViewport, I.ArrowMoveInward),
                new KeybindingViewModel("Fit to screen", "This gesture will scale and reposition the viewport to fit as many items as possible", gestures.Editor.FitToScreen, I.ZoomFit),
                new KeybindingViewModel("Cancel action", "This gesture will cancel the current action", gestures.Minimap.CancelAction, I.DismissCircle),
            });
        }
    }
}
