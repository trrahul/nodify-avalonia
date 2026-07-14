using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Nodify.Avalonia
{
    public partial class NodifyEditor
    {
        #region Multiple-Item Selection

        /// <summary>
        /// Identifies the <see cref="CanSelectMultipleItems"/> property, which wraps the underlying
        /// <see cref="SelectingItemsControl.SelectionMode"/>. When set to <see langword="false"/>,
        /// rubber-band selection is suppressed.
        /// </summary>
        public static readonly StyledProperty<bool> CanSelectMultipleItemsProperty =
            AvaloniaProperty.Register<NodifyEditor, bool>(nameof(CanSelectMultipleItems), true);

        private static void OnCanSelectMultipleItemsChanged(NodifyEditor editor, AvaloniaPropertyChangedEventArgs<bool> e)
        {
            editor.SelectionMode = e.NewValue.Value ? SelectionMode.Multiple : SelectionMode.Single;
        }

        /// <summary>
        /// Gets or sets whether multiple items can be selected simultaneously.
        /// </summary>
        public bool CanSelectMultipleItems
        {
            get => GetValue(CanSelectMultipleItemsProperty);
            set => SetValue(CanSelectMultipleItemsProperty, value);
        }

        #endregion

        #region Selection API

        /// <summary>
        /// Clears the current selection and selects the specified <paramref name="container"/> within
        /// the same selection transaction, then clears the connection selection.
        /// </summary>
        public void Select(ItemContainer container)
        {
            var items = (IList)Items;
            Selection.BeginBatchUpdate();
            Selection.Clear();
            for (var i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(ContainerFromIndex(i), container))
                {
                    Selection.Select(i);
                    break;
                }
            }
            Selection.EndBatchUpdate();

            UnselectAllConnections();
        }

        /// <summary>
        /// Selects all connections in the editor.
        /// </summary>
        public void SelectAllConnections()
        {
            (ConnectionsHost as Connections.ConnectionsMultiSelector)?.SelectAll();
        }

        #endregion

        #region Connection Selection Properties

        /// <summary>
        /// Gets or sets the list of currently selected connections.
        /// Two-way-synced with the <see cref="Connections.ConnectionsMultiSelector"/> hosted in the editor template.
        /// </summary>
        public static readonly StyledProperty<IList?> SelectedConnectionsProperty =
            AvaloniaProperty.Register<NodifyEditor, IList?>(nameof(SelectedConnections),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Gets or sets the currently selected connection (single-selection convenience property).
        /// </summary>
        public static readonly StyledProperty<object?> SelectedConnectionProperty =
            AvaloniaProperty.Register<NodifyEditor, object?>(nameof(SelectedConnection),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Gets or sets whether multiple connections can be selected simultaneously.
        /// </summary>
        public static readonly StyledProperty<bool> CanSelectMultipleConnectionsProperty =
            AvaloniaProperty.Register<NodifyEditor, bool>(nameof(CanSelectMultipleConnections),
                defaultValue: true);

        /// <summary>
        /// Gets or sets the list of currently selected connections.
        /// </summary>
        public IList? SelectedConnections
        {
            get => GetValue(SelectedConnectionsProperty);
            set => SetValue(SelectedConnectionsProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently selected connection.
        /// </summary>
        public object? SelectedConnection
        {
            get => GetValue(SelectedConnectionProperty);
            set => SetValue(SelectedConnectionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether multiple connections can be selected simultaneously.
        /// </summary>
        public bool CanSelectMultipleConnections
        {
            get => GetValue(CanSelectMultipleConnectionsProperty);
            set => SetValue(CanSelectMultipleConnectionsProperty, value);
        }

        #endregion
    }
}
