using Avalonia.Input;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia
{
    /// <summary>Gestures used by built-in controls inside the <see cref="NodifyEditor"/>.</summary>
    public class EditorGestures
    {
        /// <summary>The global default gesture mappings singleton.</summary>
        public static readonly EditorGestures Mappings = new EditorGestures();

        /// <summary>Gestures for the selection.</summary>
        public class SelectionGestures
        {
            /// <summary>Disable selection gestures.</summary>
            public static readonly SelectionGestures None = new SelectionGestures(MouseAction.None);

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectionGestures"/> class with specified mouse action
            /// and a flag indicating whether modifier keys should be ignored when releasing the mouse button.
            /// </summary>
            public SelectionGestures(MouseAction mouseAction, bool ignoreModifierKeysOnRelease)
            {
                Replace = new PointerGesture(mouseAction);
                Remove = new PointerGesture(mouseAction, KeyModifiers.Alt, ignoreModifierKeysOnRelease);
                Append = new PointerGesture(mouseAction, KeyModifiers.Shift, ignoreModifierKeysOnRelease);
                Invert = new PointerGesture(mouseAction, KeyModifiers.Control, ignoreModifierKeysOnRelease);
                Select = new AnyGesture(Replace, Remove, Append, Invert);
                Cancel = new KeyboardGesture(Key.Escape);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectionGestures"/> class with a specified mouse action.
            /// Modifier keys will be ignored when releasing the mouse button.
            /// </summary>
            public SelectionGestures(MouseAction mouseAction)
                : this(mouseAction, true)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectionGestures"/> class with a flag indicating
            /// whether modifier keys should be ignored when releasing the mouse button.
            /// The default mouse action is <see cref="MouseAction.LeftClick"/>.
            /// </summary>
            public SelectionGestures(bool ignoreModifierKeysOnRelease)
                : this(MouseAction.LeftClick, ignoreModifierKeysOnRelease)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SelectionGestures"/> class with default values.
            /// </summary>
            public SelectionGestures() : this(true)
            {
            }

            /// <summary>Gesture to replace previous selection with the selected items.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Replace { get; }

            /// <summary>Gesture to remove the selected items from the previous selection.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Alt"/>+<see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Remove { get; }

            /// <summary>Gesture to add the new selected items to the previous selection.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Shift"/>+<see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Append { get; }

            /// <summary>Gesture to invert the selected items.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Invert { get; }

            /// <summary>Cancel the current selection operation reverting to the previous selection.</summary>
            /// <remarks>Defaults to <see cref="Key.Escape"/>.</remarks>
            public InputGestureRef Cancel { get; }

            /// <summary>Gesture used to start selecting using a <see cref="SelectionGestures"/> strategy.</summary>
            public InputGestureRef Select { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(SelectionGestures gestures)
            {
                Replace.Value = gestures.Replace.Value;
                Remove.Value = gestures.Remove.Value;
                Append.Value = gestures.Append.Value;
                Invert.Value = gestures.Invert.Value;
                Select.Value = gestures.Select.Value;
                Cancel.Value = gestures.Cancel.Value;
            }

            /// <summary>Unbinds all the gestures used for selection.</summary>
            public void Unbind()
                => Apply(None);
        }

        /// <summary>Gestures for the item containers.</summary>
        public class ItemContainerGestures
        {
            public ItemContainerGestures()
            {
                Selection = new SelectionGestures();
                Selection.Select.Value = new AnyGesture(
                    Selection.Replace, Selection.Remove, Selection.Append, Selection.Invert);

                Drag = new AnyGesture(Selection.Replace, Selection.Remove, Selection.Append, Selection.Invert);
                CancelAction = new AnyGesture(new PointerGesture(MouseAction.RightClick), new KeyboardGesture(Key.Escape));
            }

            /// <summary>Gesture to select the container using a <see cref="SelectionGestures"/> strategy.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or any of the <see cref="SelectionGestures"/> gestures.</remarks>
            public SelectionGestures Selection { get; }

            /// <summary>Gesture to start and complete a dragging operation.</summary>
            /// <remarks>Using a <see cref="SelectionGestures"/> strategy to drag from a new selection.
            /// <br /> Defaults to any of the <see cref="SelectionGestures"/> gestures.
            /// </remarks>
            public InputGestureRef Drag { get; }

            /// <summary>Gesture to cancel the dragging operation.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or <see cref="Key.Escape"/>.</remarks>
            public InputGestureRef CancelAction { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(ItemContainerGestures gestures)
            {
                Selection.Apply(gestures.Selection);
                Drag.Value = gestures.Drag.Value;
                CancelAction.Value = gestures.CancelAction.Value;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Selection.Unbind();
                Drag.Unbind();
                CancelAction.Unbind();
            }
        }

        /// <summary>Keyboard gestures used for navigating the editor and moving selected items.</summary>
        public class DirectionalNavigationGestures
        {
            public DirectionalNavigationGestures(KeyModifiers modifierKeys = KeyModifiers.None)
            {
                Up = new KeyboardGesture(Key.Up, modifierKeys);
                Left = new KeyboardGesture(Key.Left, modifierKeys);
                Down = new KeyboardGesture(Key.Down, modifierKeys);
                Right = new KeyboardGesture(Key.Right, modifierKeys);
            }

            public DirectionalNavigationGestures(Key triggerKey, KeyModifiers modifierKeys = KeyModifiers.None, bool repeated = false)
            {
                Up = new KeyComboGesture(triggerKey, Key.Up, modifierKeys) { AllowRepeatingComboKey = repeated };
                Left = new KeyComboGesture(triggerKey, Key.Left, modifierKeys) { AllowRepeatingComboKey = repeated };
                Down = new KeyComboGesture(triggerKey, Key.Down, modifierKeys) { AllowRepeatingComboKey = repeated };
                Right = new KeyComboGesture(triggerKey, Key.Right, modifierKeys) { AllowRepeatingComboKey = repeated };
            }

            /// <summary>Gesture used for navigating or moving upward.</summary>
            public InputGestureRef Up { get; }

            /// <summary>Gesture used for navigating or moving left.</summary>
            public InputGestureRef Left { get; }

            /// <summary>Gesture used for navigating or moving downward.</summary>
            public InputGestureRef Down { get; }

            /// <summary>Gesture used for navigating or moving right.</summary>
            public InputGestureRef Right { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(DirectionalNavigationGestures gestures)
            {
                Up.Value = gestures.Up.Value;
                Left.Value = gestures.Left.Value;
                Down.Value = gestures.Down.Value;
                Right.Value = gestures.Right.Value;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Up.Unbind();
                Left.Unbind();
                Down.Unbind();
                Right.Unbind();
            }
        }

        /// <summary>Gestures for the editor.</summary>
        public class NodifyEditorGestures
        {
            /// <summary>Keyboard gestures used for navigation, selection, and manipulation in the editor.</summary>
            public class KeyboardGestures
            {
                public KeyboardGestures()
                {
                    Pan = new DirectionalNavigationGestures(Key.Space, repeated: true);
                    DragSelection = new DirectionalNavigationGestures(KeyModifiers.Control);
                    NavigateSelection = new DirectionalNavigationGestures(KeyModifiers.None);
                    ToggleSelected = new AnyGesture(new KeyboardGesture(Key.Space), new KeyboardGesture(Key.Enter));
                    DeselectAll = new KeyboardGesture(Key.Escape);
                    NextNavigationLayer = new KeyboardGesture(Key.OemCloseBrackets, KeyModifiers.Control);
                    PrevNavigationLayer = new KeyboardGesture(Key.OemOpenBrackets, KeyModifiers.Control);
                }

                /// <summary>Directional gestures used for panning the viewport.</summary>
                /// <remarks>Defaults to <see cref="Key.Space"/>+arrow keys.</remarks>
                public DirectionalNavigationGestures Pan { get; }

                /// <summary>Directional gestures used for dragging the selected items.</summary>
                /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+arrow keys.</remarks>
                public DirectionalNavigationGestures DragSelection { get; }

                /// <summary>Directional gestures used to navigate the selection focus (e.g., between nodes).</summary>
                /// <remarks>Defaults to arrow keys.</remarks>
                public DirectionalNavigationGestures NavigateSelection { get; }

                /// <summary>Gesture used to toggle the selected state of the currently focused item.</summary>
                /// <remarks>Defaults to <see cref="Key.Space"/>.</remarks>
                public InputGestureRef ToggleSelected { get; }

                /// <summary>Gesture used to clear the current selection.</summary>
                /// <remarks>Defaults to <see cref="Key.Escape"/>.</remarks>
                public InputGestureRef DeselectAll { get; }

                /// <summary>Gesture used to activate the next keyboard navigation layer.</summary>
                /// <remarks><see cref="KeyModifiers.Control"/>+<see cref="Key.OemCloseBrackets"/>.</remarks>
                public InputGestureRef NextNavigationLayer { get; }

                /// <summary>Gesture used to activate the previous keyboard navigation layer.</summary>
                /// <remarks><see cref="KeyModifiers.Control"/>+<see cref="Key.OemOpenBrackets"/>.</remarks>
                public InputGestureRef PrevNavigationLayer { get; }

                /// <summary>Copies from the specified gestures.</summary>
                public void Apply(KeyboardGestures gestures)
                {
                    Pan.Apply(gestures.Pan);
                    DragSelection.Apply(gestures.DragSelection);
                    NavigateSelection.Apply(gestures.NavigateSelection);
                    ToggleSelected.Value = gestures.ToggleSelected.Value;
                    DeselectAll.Value = gestures.DeselectAll.Value;
                    NextNavigationLayer.Value = gestures.NextNavigationLayer.Value;
                    PrevNavigationLayer.Value = gestures.PrevNavigationLayer.Value;
                }

                /// <summary>Unbinds all the gestures.</summary>
                public void Unbind()
                {
                    Pan.Unbind();
                    DragSelection.Unbind();
                    NavigateSelection.Unbind();
                    ToggleSelected.Unbind();
                    DeselectAll.Unbind();
                    NextNavigationLayer.Unbind();
                    PrevNavigationLayer.Unbind();
                }
            }

            public NodifyEditorGestures()
            {
                Keyboard = new KeyboardGestures();
                Selection = new SelectionGestures();
                SelectAll = new KeyboardGesture(Key.A, KeyModifiers.Control);
                Cutting = new PointerGesture(MouseAction.LeftClick, KeyModifiers.Alt | KeyModifiers.Shift, true);
                PushItems = new PointerGesture(MouseAction.LeftClick, KeyModifiers.Control | KeyModifiers.Shift, true);
                Pan = new AnyGesture(new PointerGesture(MouseAction.RightClick), new PointerGesture(MouseAction.MiddleClick));
                ZoomModifierKey = KeyModifiers.None;
                ZoomIn = new AnyGesture(new KeyboardGesture(Key.OemPlus, KeyModifiers.Control), new KeyboardGesture(Key.Add, KeyModifiers.Control));
                ZoomOut = new AnyGesture(new KeyboardGesture(Key.OemMinus, KeyModifiers.Control), new KeyboardGesture(Key.Subtract, KeyModifiers.Control));
                ResetViewport = new KeyboardGesture(Key.Home);
                FitToScreen = new KeyboardGesture(Key.Home, KeyModifiers.Shift);
                CancelAction = new AnyGesture(new PointerGesture(MouseAction.RightClick), new KeyboardGesture(Key.Escape));
                PanWithMouseWheel = false;
                PanHorizontalModifierKey = KeyModifiers.Shift;
                PanVerticalModifierKey = KeyModifiers.None;
            }

            public KeyboardGestures Keyboard { get; }

            /// <summary>Gesture used to start selecting using a <see cref="SelectionGestures"/> strategy.</summary>
            public SelectionGestures Selection { get; }

            /// <summary>Gesture used to select all <see cref="ItemContainer"/>s in the editor.</summary>
            public InputGestureRef SelectAll { get; }

            /// <summary>Gesture used to start cutting connections.</summary>
            public InputGestureRef Cutting { get; }

            /// <summary>Gesture used to start pushing.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="KeyModifiers.Shift"/>+<see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef PushItems { get; }

            /// <summary>Gesture used to start panning.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or <see cref="MouseAction.MiddleClick"/>.</remarks>
            public InputGestureRef Pan { get; }

            /// <summary>Whether panning using mouse wheel is allowed.</summary>
            /// <remarks>Set the <see cref="ZoomModifierKey"/> to allow zooming using the mouse wheel.</remarks>
            public bool PanWithMouseWheel { get; set; }

            /// <summary>The modifier key required to start panning vertically with the mouse wheel.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.None"/>.</remarks>
            public KeyModifiers PanVerticalModifierKey { get; set; }

            /// <summary>The modifier key required to start panning horizontally with the mouse wheel.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Shift"/>.</remarks>
            public KeyModifiers PanHorizontalModifierKey { get; set; }

            /// <summary>The key modifier required to start zooming by mouse wheel.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.None"/>.</remarks>
            public KeyModifiers ZoomModifierKey { get; set; }

            /// <summary>Gesture used to zoom in.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="Key.OemPlus"/>.</remarks>
            public InputGestureRef ZoomIn { get; }

            /// <summary>Gesture used to zoom out.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="Key.OemMinus"/>.</remarks>
            public InputGestureRef ZoomOut { get; }

            /// <summary>Gesture used to move the editor's viewport location to (0, 0) and set the zoom to 1.</summary>
            /// <remarks>Defaults to <see cref="Key.Home"/>.</remarks>
            public InputGestureRef ResetViewport { get; }

            /// <summary>Gesture used to fit as many containers as possible into the viewport.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Shift"/>+<see cref="Key.Home"/>.</remarks>
            public InputGestureRef FitToScreen { get; }

            /// <summary>Gesture to cancel the current operation.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or <see cref="Key.Escape"/>.</remarks>
            public InputGestureRef CancelAction { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(NodifyEditorGestures gestures)
            {
                Keyboard.Apply(gestures.Keyboard);
                Selection.Apply(gestures.Selection);
                SelectAll.Value = gestures.SelectAll.Value;
                Cutting.Value = gestures.Cutting.Value;
                PushItems.Value = gestures.PushItems.Value;
                Pan.Value = gestures.Pan.Value;
                ZoomModifierKey = gestures.ZoomModifierKey;
                ZoomIn.Value = gestures.ZoomIn.Value;
                ZoomOut.Value = gestures.ZoomOut.Value;
                ResetViewport.Value = gestures.ResetViewport.Value;
                FitToScreen.Value = gestures.FitToScreen.Value;
                CancelAction.Value = gestures.CancelAction.Value;
                PanWithMouseWheel = gestures.PanWithMouseWheel;
                PanHorizontalModifierKey = gestures.PanHorizontalModifierKey;
                PanVerticalModifierKey = gestures.PanVerticalModifierKey;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Keyboard.Unbind();
                Selection.Unbind();
                SelectAll.Unbind();
                Cutting.Unbind();
                Pan.Unbind();
                PushItems.Unbind();
                ZoomIn.Unbind();
                ZoomOut.Unbind();
                ResetViewport.Unbind();
                FitToScreen.Unbind();
                CancelAction.Unbind();
            }
        }

        /// <summary>Gestures used by the <see cref="Connections.Connector"/>.</summary>
        public class ConnectorGestures
        {
            public ConnectorGestures()
            {
                Disconnect = new AnyGesture(new PointerGesture(MouseAction.LeftClick, KeyModifiers.Alt), new KeyboardGesture(Key.Delete));
                Connect = new AnyGesture(new PointerGesture(MouseAction.LeftClick), new KeyboardGesture(Key.Space));
                CancelAction = new AnyGesture(new PointerGesture(MouseAction.RightClick), new KeyboardGesture(Key.Escape));
            }

            /// <summary>Gesture to call the <see cref="Connections.Connector.DisconnectCommand"/>.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Alt"/>+<see cref="MouseAction.LeftClick"/> or <see cref="Key.Delete"/>.</remarks>
            public InputGestureRef Disconnect { get; }

            /// <summary>Gesture to start and complete a pending connection.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Connect { get; }

            /// <summary>Gesture to cancel the pending connection.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or <see cref="Key.Escape"/>.</remarks>
            public InputGestureRef CancelAction { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(ConnectorGestures gestures)
            {
                Disconnect.Value = gestures.Disconnect.Value;
                Connect.Value = gestures.Connect.Value;
                CancelAction.Value = gestures.CancelAction.Value;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Disconnect.Unbind();
                Connect.Unbind();
                CancelAction.Unbind();
            }
        }

        /// <summary>Gestures used by the <see cref="Connections.BaseConnection"/>.</summary>
        public class ConnectionGestures
        {
            public ConnectionGestures()
            {
                Split = new PointerGesture(MouseAction.LeftDoubleClick);
                Selection = new SelectionGestures(MouseAction.LeftClick);
                Disconnect = new PointerGesture(MouseAction.LeftClick, KeyModifiers.Alt);
            }

            /// <summary>Gesture to call the <see cref="Connections.BaseConnection.SplitCommand"/> command.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.LeftDoubleClick"/>.</remarks>
            public InputGestureRef Split { get; }

            /// <summary>Gesture used to start selecting using a <see cref="SelectionGestures"/> strategy.</summary>
            public SelectionGestures Selection { get; }

            /// <summary>Gesture to call the <see cref="Connections.BaseConnection.DisconnectCommand"/> command.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Alt"/>+<see cref="MouseAction.LeftClick"/>.</remarks>
            public InputGestureRef Disconnect { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(ConnectionGestures gestures)
            {
                Split.Value = gestures.Split.Value;
                Disconnect.Value = gestures.Disconnect.Value;
                Selection.Apply(gestures.Selection);
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Split.Unbind();
                Selection.Unbind();
                Disconnect.Unbind();
            }
        }

        /// <summary>Gestures for the <see cref="Nodes.GroupingNode"/>.</summary>
        public class GroupingNodeGestures
        {
            public GroupingNodeGestures()
            {
                SwitchMovementMode = KeyModifiers.Shift;
                ToggleContentSelection = new AnyGesture(new KeyboardGesture(Key.Space, KeyModifiers.Control), new KeyboardGesture(Key.Enter, KeyModifiers.Control));
            }

            /// <summary>The key modifier that will toggle between <see cref="Nodes.GroupingMovementMode"/>s.</summary>
            /// <remarks>The modifier must be allowed by the <see cref="ItemContainerGestures.Drag"/> gesture.
            /// <br /> Defaults to <see cref="KeyModifiers.Shift"/>.
            /// </remarks>
            public KeyModifiers SwitchMovementMode { get; set; }

            /// <summary>Gesture to toggle the content selection of the <see cref="Nodes.GroupingNode"/> when it is selected.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="Key.Space"/>.</remarks>
            public InputGestureRef ToggleContentSelection { get; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(GroupingNodeGestures gestures)
            {
                SwitchMovementMode = gestures.SwitchMovementMode;
                ToggleContentSelection.Value = gestures.ToggleContentSelection.Value;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                ToggleContentSelection.Unbind();
            }
        }

        /// <summary>Gestures used by the Minimap control.</summary>
        public class MinimapGestures
        {
            public MinimapGestures()
            {
                Pan = new DirectionalNavigationGestures();
                DragViewport = new PointerGesture(MouseAction.LeftClick);
                ResetViewport = new KeyboardGesture(Key.Home);
                CancelAction = new AnyGesture(new PointerGesture(MouseAction.RightClick), new KeyboardGesture(Key.Escape));
                ZoomIn = new AnyGesture(new KeyboardGesture(Key.OemPlus, KeyModifiers.Control), new KeyboardGesture(Key.Add, KeyModifiers.Control));
                ZoomOut = new AnyGesture(new KeyboardGesture(Key.OemMinus, KeyModifiers.Control), new KeyboardGesture(Key.Subtract, KeyModifiers.Control));
                ZoomModifierKey = KeyModifiers.None;
            }

            /// <summary>Directional gestures used for panning the viewport.</summary>
            /// <remarks>Defaults to arrow keys.</remarks>
            public DirectionalNavigationGestures Pan { get; }

            /// <summary>Gesture to move the viewport inside the Minimap.</summary>
            public InputGestureRef DragViewport { get; }

            /// <summary>Gesture to reset the viewport inside the Minimap.</summary>
            public InputGestureRef ResetViewport { get; }

            /// <summary>Gesture to cancel the panning operation.</summary>
            /// <remarks>Defaults to <see cref="MouseAction.RightClick"/> or <see cref="Key.Escape"/>.</remarks>
            public InputGestureRef CancelAction { get; }

            /// <summary>Gesture used to zoom in.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="Key.OemPlus"/>.</remarks>
            public InputGestureRef ZoomIn { get; }

            /// <summary>Gesture used to zoom out.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.Control"/>+<see cref="Key.OemMinus"/>.</remarks>
            public InputGestureRef ZoomOut { get; }

            /// <summary>The key modifier required to start zooming by mouse wheel.</summary>
            /// <remarks>Defaults to <see cref="KeyModifiers.None"/>.</remarks>
            public KeyModifiers ZoomModifierKey { get; set; }

            /// <summary>Copies from the specified gestures.</summary>
            public void Apply(MinimapGestures gestures)
            {
                Pan.Apply(gestures.Pan);
                ZoomIn.Value = gestures.ZoomIn.Value;
                ZoomOut.Value = gestures.ZoomOut.Value;
                ResetViewport.Value = gestures.ResetViewport.Value;
                DragViewport.Value = gestures.DragViewport.Value;
                CancelAction.Value = gestures.CancelAction.Value;
                ZoomModifierKey = gestures.ZoomModifierKey;
            }

            /// <summary>Unbinds all the gestures.</summary>
            public void Unbind()
            {
                Pan.Unbind();
                ZoomIn.Unbind();
                ZoomOut.Unbind();
                ResetViewport.Unbind();
                DragViewport.Unbind();
                CancelAction.Unbind();
            }
        }

        /// <summary>Gestures for the editor.</summary>
        public NodifyEditorGestures Editor { get; } = new NodifyEditorGestures();

        /// <summary>Gestures for the item container.</summary>
        public ItemContainerGestures ItemContainer { get; } = new ItemContainerGestures();

        /// <summary>Gestures for the connector.</summary>
        public ConnectorGestures Connector { get; } = new ConnectorGestures();

        /// <summary>Gestures for the connection.</summary>
        public ConnectionGestures Connection { get; } = new ConnectionGestures();

        /// <summary>Gestures for the grouping node.</summary>
        public GroupingNodeGestures GroupingNode { get; } = new GroupingNodeGestures();

        /// <summary>Gestures for the minimap.</summary>
        public MinimapGestures Minimap { get; } = new MinimapGestures();

        /// <summary>Copies from the specified gestures.</summary>
        public void Apply(EditorGestures gestures)
        {
            Editor.Apply(gestures.Editor);
            ItemContainer.Apply(gestures.ItemContainer);
            Connector.Apply(gestures.Connector);
            Connection.Apply(gestures.Connection);
            GroupingNode.Apply(gestures.GroupingNode);
            Minimap.Apply(gestures.Minimap);
        }

        /// <summary>Unbinds all the gestures used by the editor and its controls.</summary>
        public void Unbind()
        {
            Editor.Unbind();
            ItemContainer.Unbind();
            Connector.Unbind();
            Connection.Unbind();
            Minimap.Unbind();
        }
    }
}
