using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Interactivity;
using static Nodify.Avalonia.Helpers.SelectionHelper;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// The container used by <see cref="ConnectionsMultiSelector"/> for each connection item.
    /// </summary>
    [PseudoClasses(IsSelectedPseudoClass)]
    public class ConnectionContainer : ContentPresenter, IKeyboardFocusTarget<ConnectionContainer>
    {
        private const string IsSelectedPseudoClass = ":isselected";

        #region Properties

        public static readonly StyledProperty<bool> IsSelectedProperty =
            SelectingItemsControl.IsSelectedProperty.AddOwner<ConnectionContainer>(
                new StyledPropertyMetadata<bool>(defaultBindingMode: BindingMode.TwoWay));

        public static readonly DirectProperty<ConnectionContainer, bool> IsSelectableProperty =
            AvaloniaProperty.RegisterDirect<ConnectionContainer, bool>(
                nameof(IsSelectable),
                o => o.IsSelectable,
                (o, v) => o.IsSelectable = v);

        #region Routed Events

        public static readonly RoutedEvent<RoutedEventArgs> SelectedEvent =
            RoutedEvent.Register<ConnectionContainer, RoutedEventArgs>(nameof(Selected), RoutingStrategies.Bubble);

        public static readonly RoutedEvent<RoutedEventArgs> UnselectedEvent =
            RoutedEvent.Register<ConnectionContainer, RoutedEventArgs>(nameof(Unselected), RoutingStrategies.Bubble);

        /// <summary>Occurs when this <see cref="ConnectionContainer"/> is selected.</summary>
        public event EventHandler<RoutedEventArgs> Selected
        {
            add => AddHandler(SelectedEvent, value);
            remove => RemoveHandler(SelectedEvent, value);
        }

        /// <summary>Occurs when this <see cref="ConnectionContainer"/> is unselected.</summary>
        public event EventHandler<RoutedEventArgs> Unselected
        {
            add => AddHandler(UnselectedEvent, value);
            remove => RemoveHandler(UnselectedEvent, value);
        }

        #endregion

        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="ConnectionContainer"/> is selected.
        /// Can only be set to true if <see cref="IsSelectable"/> is true.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        private bool _isSelectable;

        /// <summary>
        /// Gets or sets whether this <see cref="ConnectionContainer"/> can be selected.
        /// Delegates to <see cref="BaseConnection.IsSelectableProperty"/> on the inner connection.
        /// </summary>
        public bool IsSelectable
        {
            get => BaseConnection.GetIsSelectable(Connection ?? (Control)this);
            set
            {
                bool old = _isSelectable;
                BaseConnection.SetIsSelectable(Connection ?? (Control)this, value);
                SetAndRaise(IsSelectableProperty, ref old, value);
                _isSelectable = value;
            }
        }

        #endregion

        #region IKeyboardFocusTarget<ConnectionContainer>

        global::Avalonia.Rect IKeyboardFocusTarget<ConnectionContainer>.Bounds
            => ConnectionFocusTarget.Bounds;

        ConnectionContainer IKeyboardFocusTarget<ConnectionContainer>.Element => this;

        private IKeyboardFocusTarget<Control> ConnectionFocusTarget
            => Connection as IKeyboardFocusTarget<Control>
               ?? throw new NotSupportedException(
                   "Custom connections must implement IKeyboardFocusTarget<Control> for keyboard navigation; alternatively, keyboard navigation may be disabled for the connections layer.");

        #endregion

        private Control? _connection;

        /// <summary>
        /// Resolves the inner connection control used for selection-related operations.
        /// Prefers <see cref="BaseConnection"/> when <see cref="BaseConnection.PrioritizeBaseConnectionForSelection"/> is true.
        /// </summary>
        public Control? Connection
            => _connection ??= BaseConnection.PrioritizeBaseConnectionForSelection
                ? this.FindDescendantOfType<BaseConnection>() ?? this.FindDescendantOfType<Control>()
                : this.FindDescendantOfType<Control>();

        /// <summary>Gets the <see cref="ConnectionsMultiSelector"/> that owns this container.</summary>
        public ConnectionsMultiSelector Selector { get; }

        /// <summary>
        /// Gets the effective gesture mappings for this container (per-editor override or global fallback).
        /// </summary>
        public EditorGestures ActualGestures => Selector.Editor?.ActualGestures ?? EditorGestures.Mappings;

        private SelectionType? _pendingSelectionType;
        private IDisposable? _focusZIndexOverride;

        static ConnectionContainer()
        {
            FocusableProperty.OverrideMetadata<ConnectionContainer>(new StyledPropertyMetadata<bool>(true));
            IsSelectedProperty.Changed.AddClassHandler<ConnectionContainer, bool>(OnIsSelectedChanged);
        }

        internal ConnectionContainer(ConnectionsMultiSelector selector)
        {
            Selector = selector;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            // When this container gains/loses direct keyboard focus, repaint the inner connection so it can
            // draw/erase its focus outline.
            if (change.Property == IsFocusedProperty)
            {
                this.FindDescendantOfType<BaseConnection>()?.UpdateFocusVisual();
                UpdateFocusZIndex();
            }
        }

        private void UpdateFocusZIndex()
        {
            _focusZIndexOverride?.Dispose();
            _focusZIndexOverride = IsFocused
                ? SetValue(Panel.ZIndexProperty, int.MaxValue, BindingPriority.Animation)
                : null;
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Focus-trap: if this connection container is removed while it holds keyboard
            // focus, return focus to the editor so keyboard navigation keeps working.
            bool hadKeyboardFocus = IsKeyboardFocusWithin;

            _focusZIndexOverride?.Dispose();
            _focusZIndexOverride = null;

            base.OnDetachedFromVisualTree(e);

            if (hadKeyboardFocus)
            {
                Selector.Editor?.Focus();
            }
        }

        private static void OnIsSelectedChanged(ConnectionContainer container, AvaloniaPropertyChangedEventArgs<bool> e)
        {
            bool result = container.IsSelectable && e.NewValue.Value;
            // Enforce IsSelectable constraint (same pattern as ItemContainer).
            if (container.IsSelected != result)
            {
                container.IsSelected = result;
                return;
            }

            container.PseudoClasses.Set(IsSelectedPseudoClass, result);
            container.OnSelectedChanged(result);
        }

        private void OnSelectedChanged(bool newValue)
        {
            BaseConnection.SetIsSelected(Connection, newValue);
            RaiseEvent(new RoutedEventArgs(newValue ? SelectedEvent : UnselectedEvent, this));
        }

        /// <summary>
        /// Modifies the selection state based on the specified selection type.
        /// </summary>
        public void Select(SelectionType type)
        {
            switch (type)
            {
                case SelectionType.Append:
                    IsSelected = true;
                    break;
                case SelectionType.Remove:
                    IsSelected = false;
                    break;
                case SelectionType.Invert:
                    IsSelected = !IsSelected;
                    break;
                case SelectionType.Replace:
                    Selector.Select(this);
                    break;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            EditorGestures.ConnectionGestures gestures = ActualGestures.Connection;

            if (IsSelectable && gestures.Selection.Select.Matches(e.Source, e))
            {
                _pendingSelectionType = gestures.Selection.GetSelectionType(e);
            }
            // Context-menu right-click on an item that isn't being selected by the select gesture:
            // replace the selection (or append if already selected) so the menu targets the right set.
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed && Connection?.ContextMenu != null)
            {
                _pendingSelectionType = IsSelected ? SelectionType.Append : SelectionType.Replace;
            }

            // Focus + handle for either branch, so a right-click with a context menu still focuses
            // the container and doesn't bubble to the editor.
            if (_pendingSelectionType.HasValue)
            {
                Focus();
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_pendingSelectionType.HasValue)
            {
                SelectionType type = _pendingSelectionType.Value;
                _pendingSelectionType = null;

                // Right-click on an already-selected item with a context menu: don't change selection
                // so the context menu applies to the whole selection.
                bool isContextMenuRightClick = e.InitialPressMouseButton == MouseButton.Right
                                               && IsSelected
                                               && Connection?.ContextMenu != null;
                if (!isContextMenuRightClick)
                {
                    Select(type);
                }
            }
        }
    }
}
