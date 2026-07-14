using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// An <see cref="SelectingItemsControl"/> whose items are wrapped in <see cref="ConnectionContainer"/>s,
    /// providing multi-select support for connections.
    /// </summary>
    public class ConnectionsMultiSelector : SelectingItemsControl, IKeyboardNavigationLayer
    {
        #region Properties

        /// <summary>
        /// Mirrors <see cref="NodifyEditor.SelectedConnectionsProperty"/> — the external IList that is
        /// two-way-synced with the internal <see cref="SelectingItemsControl.Selection"/> model.
        /// </summary>
        public static new readonly StyledProperty<IList?> SelectedItemsProperty =
            AvaloniaProperty.Register<ConnectionsMultiSelector, IList?>(nameof(SelectedItems),
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> CanSelectMultipleItemsProperty =
            AvaloniaProperty.Register<ConnectionsMultiSelector, bool>(nameof(CanSelectMultipleItems),
                defaultValue: true);

        /// <summary>
        /// Gets or sets the external list that is kept in sync with the internal selection model.
        /// Bind this to <see cref="NodifyEditor.SelectedConnections"/>.
        /// </summary>
        public new IList? SelectedItems
        {
            get => GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        /// <summary>Gets or sets whether multiple connections can be selected simultaneously.</summary>
        public bool CanSelectMultipleItems
        {
            get => GetValue(CanSelectMultipleItemsProperty);
            set => SetValue(CanSelectMultipleItemsProperty, value);
        }

        #endregion

        /// <summary>Gets the <see cref="NodifyEditor"/> that owns this selector.</summary>
        public NodifyEditor? Editor { get; private set; }

        // Prevents re-entrant sync loops between the internal Selection model and the external SelectedItems list.
        private bool _isSyncingSelection;

        public ConnectionsMultiSelector()
        {
            SelectionMode = SelectionMode.Multiple;
            Selection.SelectionChanged += OnSelectionModelChanged;
            ItemsView.CollectionChanged += OnItemsViewChanged;
            _connectionsNavigator = new StatefulFocusNavigator<ConnectionContainer>(
                getCurrentFocus: () => global::Avalonia.Controls.TopLevel.GetTopLevel(this)
                    ?.FocusManager?.GetFocusedElement() as ConnectionContainer,
                onFocus: OnConnectionElementFocused);
        }

        static ConnectionsMultiSelector()
        {
            SelectedItemsProperty.Changed.AddClassHandler<ConnectionsMultiSelector, IList?>(OnSelectedItemsSourceChanged);
            CanSelectMultipleItemsProperty.Changed.AddClassHandler<ConnectionsMultiSelector, bool>(OnCanSelectMultipleItemsChanged);
        }

        #region StyleKeyOverride

        // Report as ItemsControl so the default ItemsControl theme applies (no selection-highlight chrome).
        protected override Type StyleKeyOverride => typeof(ItemsControl);

        #endregion

        #region Template

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            Editor = this.FindAncestorOfType<NodifyEditor>();

            if (NodifyEditor.AutoRegisterConnectionsLayer)
            {
                Editor?.RegisterNavigationLayer(this);
            }
        }

        #endregion

        #region Keyboard Navigation (IKeyboardNavigationLayer)

        private readonly StatefulFocusNavigator<ConnectionContainer> _connectionsNavigator;

        /// <inheritdoc />
        public KeyboardNavigationLayerId Id => KeyboardNavigationLayerId.Connections;

        /// <inheritdoc />
        public IKeyboardFocusTarget<Control>? LastFocusedElement => _connectionsNavigator.LastFocusedElement;

        /// <inheritdoc />
        public bool TryMoveFocus(TraversalRequest request)
            => _connectionsNavigator.TryMoveFocus(request, TryFindContainerToFocus);

        /// <inheritdoc />
        public bool TryRestoreFocus()
            => _connectionsNavigator.TryRestoreFocus();

        /// <inheritdoc />
        public void OnActivated() => TryRestoreFocus();

        /// <inheritdoc />
        public void OnDeactivated() { }

        /// <summary>
        /// Iterates all realized <see cref="ConnectionContainer"/>s.
        /// </summary>
        protected internal IReadOnlyCollection<ConnectionContainer> ConnectionContainers
        {
            get
            {
                var list = new List<ConnectionContainer>(Items.Count);
                for (int i = 0; i < Items.Count; i++)
                {
                    if (ContainerFromIndex(i) is ConnectionContainer cc)
                        list.Add(cc);
                }
                return list;
            }
        }

        private bool TryFindContainerToFocus(ConnectionContainer? currentElement, TraversalRequest request, out ConnectionContainer? containerToFocus)
        {
            containerToFocus = null;

            if (currentElement is ConnectionContainer focusedContainer)
            {
                containerToFocus = FindNextFocusTarget(focusedContainer, request);
            }
            else if (currentElement is Control elem && elem.FindAncestorOfType<ConnectionContainer>() is ConnectionContainer parentContainer)
            {
                containerToFocus = parentContainer;
            }
            else if (Items.Count > 0 && Editor != null)
            {
                var viewport = new Rect(Editor.ViewportLocation, Editor.ViewportSize);
                var containers = ConnectionContainers;
                containerToFocus = containers.FirstOrDefault(c =>
                    viewport.Intersects(((IKeyboardFocusTarget<ConnectionContainer>)c).Bounds))
                    ?? containers.FirstOrDefault();
            }

            return containerToFocus != null;
        }

        /// <summary>
        /// Finds the next connection container to focus using directional navigation.
        /// </summary>
        protected virtual ConnectionContainer? FindNextFocusTarget(ConnectionContainer currentContainer, TraversalRequest request)
        {
            var nav = new DirectionalFocusNavigator<ConnectionContainer>(ConnectionContainers);
            return nav.FindNextFocusTarget(currentContainer, request)?.Element;
        }

        /// <summary>
        /// Called when a connection container is keyboard-focused.
        /// </summary>
        protected virtual void OnConnectionElementFocused(IKeyboardFocusTarget<ConnectionContainer> target)
        {
            if (NodifyEditor.AutoPanOnNodeFocus)
            {
                Editor?.BringIntoView(target.Bounds, NodifyEditor.BringIntoViewEdgeOffset);
            }
        }

        #endregion

        #region UnselectAll

        /// <summary>
        /// Clears all connection selections, keeping the external <see cref="SelectedItems"/> in sync.
        /// </summary>
        public void UnselectAll()
        {
            if (_isSyncingSelection) return;

            _isSyncingSelection = true;
            try
            {
                Selection.BeginBatchUpdate();
                Selection.Clear();
                Selection.EndBatchUpdate();

                SelectedItems?.Clear();
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        #endregion

        #region SelectAll

        /// <summary>
        /// Selects all connections, keeping the external <see cref="SelectedItems"/> in sync.
        /// Backs <see cref="NodifyEditor.SelectAllConnections"/>.
        /// </summary>
        public void SelectAll()
        {
            if (_isSyncingSelection) return;

            _isSyncingSelection = true;
            try
            {
                Selection.BeginBatchUpdate();
                Selection.SelectAll();
                Selection.EndBatchUpdate();

                // The guard suppresses the model's own internal->external sync, so mirror the
                // select-all into the external list explicitly (mirrors Select(ConnectionContainer)).
                IList? ext = SelectedItems;
                if (ext != null)
                {
                    ext.Clear();
                    for (int i = 0; i < Items.Count; i++)
                    {
                        ext.Add(Items[i]);
                    }
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        #endregion

        #region Container Overrides

        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            => new ConnectionContainer(this);

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = null;
            return item is not ConnectionContainer;
        }

        #endregion

        #region CanSelectMultipleItems

        private static void OnCanSelectMultipleItemsChanged(ConnectionsMultiSelector selector, AvaloniaPropertyChangedEventArgs<bool> e)
        {
            selector.SelectionMode = e.NewValue.Value ? SelectionMode.Multiple : SelectionMode.Single;
        }

        #endregion

        #region Selection sync: internal Selection model ↔ external SelectedItems list

        /// <summary>
        /// Selects a single <paramref name="container"/>, clearing all other selections, and then
        /// calls <see cref="NodifyEditor.UnselectAll"/> to deselect all nodes, so that clicking a
        /// connection clears the node selection.
        /// </summary>
        public void Select(ConnectionContainer container)
        {
            if (_isSyncingSelection) return;

            int idx = IndexFromContainer(container);
            if (idx < 0) return;

            _isSyncingSelection = true;
            try
            {
                Selection.BeginBatchUpdate();
                Selection.Clear();
                Selection.Select(idx);
                Selection.EndBatchUpdate();

                // The guard suppresses the model's own internal->external sync, so mirror the
                // single replace-selection into the external list explicitly.
                IList? ext = SelectedItems;
                if (ext != null)
                {
                    ext.Clear();
                    ext.Add(Items[idx]);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }

            Editor?.UnselectAll();
        }

        private int IndexFromContainer(ConnectionContainer container)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (ContainerFromIndex(i) == container) return i;
            }
            return -1;
        }

        // Internal selection model changed → push changes to external SelectedItems list.
        private void OnSelectionModelChanged(object? sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;
            IList? selected = SelectedItems;
            if (selected == null) return;

            _isSyncingSelection = true;
            try
            {
                var added = e.SelectedItems;
                for (int i = 0; i < added.Count; i++)
                {
                    if (!selected.Contains(added[i]))
                        selected.Add(added[i]);
                }

                var removed = e.DeselectedItems;
                for (int i = 0; i < removed.Count; i++)
                {
                    selected.Remove(removed[i]);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        // External SelectedItems property replaced → re-subscribe and rebuild internal selection.
        private static void OnSelectedItemsSourceChanged(ConnectionsMultiSelector selector,
            AvaloniaPropertyChangedEventArgs<IList?> e)
        {
            if (e.OldValue.Value is INotifyCollectionChanged oldNcc)
                oldNcc.CollectionChanged -= selector.OnExternalSelectedItemsChanged;

            if (e.NewValue.Value is INotifyCollectionChanged newNcc)
                newNcc.CollectionChanged += selector.OnExternalSelectedItemsChanged;

            selector.RebuildSelectionFromExternalList(e.NewValue.Value);
        }

        // External SelectedItems collection modified → push change to internal Selection model.
        private void OnExternalSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isSyncingSelection) return;
            if (!CanSelectMultipleItems) return;

            _isSyncingSelection = true;
            try
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        Selection.BeginBatchUpdate();
                        Selection.Clear();
                        Selection.EndBatchUpdate();
                        break;

                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems != null)
                        {
                            Selection.BeginBatchUpdate();
                            for (int i = 0; i < e.NewItems.Count; i++)
                            {
                                int idx = IndexOfItem(e.NewItems[i]);
                                if (idx >= 0) Selection.Select(idx);
                            }
                            Selection.EndBatchUpdate();
                        }
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null)
                        {
                            Selection.BeginBatchUpdate();
                            for (int i = 0; i < e.OldItems.Count; i++)
                            {
                                int idx = IndexOfItem(e.OldItems[i]);
                                if (idx >= 0) Selection.Deselect(idx);
                            }
                            Selection.EndBatchUpdate();
                        }
                        break;
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        // Items collection changed (e.g. ItemsSource assigned/populated after SelectedItems was
        // already set) → re-attempt matching so pre-populated selections aren't dropped when the
        // items simply weren't realized yet at sync time.
        private void OnItemsViewChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_isSyncingSelection && SelectedItems is { Count: > 0 } selected)
            {
                // Full Clear+rebuild is O(selected) per items-change, not O(delta); acceptable
                // because selections here are small (interactive multi-select) and items-changes
                // are user-driven, so this never runs in a hot loop.
                RebuildSelectionFromExternalList(selected);
            }
        }

        private void RebuildSelectionFromExternalList(IList? newList)
        {
            if (_isSyncingSelection) return;

            _isSyncingSelection = true;
            try
            {
                Selection.BeginBatchUpdate();
                Selection.Clear();
                if (newList != null)
                {
                    for (int i = 0; i < newList.Count; i++)
                    {
                        int idx = IndexOfItem(newList[i]);
                        if (idx >= 0) Selection.Select(idx);
                    }
                }
                Selection.EndBatchUpdate();
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private int IndexOfItem(object? item)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Equals(Items[i], item)) return i;
            }
            return -1;
        }

        #endregion
    }
}
