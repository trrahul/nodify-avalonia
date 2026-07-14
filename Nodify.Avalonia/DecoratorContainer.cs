using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia
{
    /// <summary>
    /// The container for all the items generated from the <see cref="NodifyEditor.Decorators"/> collection.
    /// </summary>
    public class DecoratorContainer : ContentControl, INodifyCanvasItem, IKeyboardFocusTarget<DecoratorContainer>
    {
        #region Dependency Properties

        public static readonly StyledProperty<Point> LocationProperty = ItemContainer.LocationProperty.AddOwner<DecoratorContainer>();
        public static readonly StyledProperty<Size> ActualSizeProperty = ItemContainer.ActualSizeProperty.AddOwner<DecoratorContainer>();

        /// <summary>
        /// Gets or sets the location of this <see cref="DecoratorContainer"/> inside the <see cref="DecoratorsControl"/>.
        /// </summary>
        public Point Location
        {
            get => (Point)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets the actual size of this <see cref="DecoratorContainer"/>.
        /// </summary>
        public Size ActualSize
        {
            get => (Size)GetValue(ActualSizeProperty);
            set => SetValue(ActualSizeProperty, value);
        }

        private static void OnLocationChanged(DecoratorContainer decoratorContainer, AvaloniaPropertyChangedEventArgs<Point> avaloniaPropertyChangedEventArgs)
        {
            decoratorContainer.OnLocationChanged();
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent LocationChangedEvent = RoutedEvent.Register<DecoratorContainer,RoutedEventArgs>(nameof(LocationChanged),RoutingStrategies.Bubble); 

        /// <summary>
        /// Occurs when the <see cref="Location"/> of this <see cref="DecoratorContainer"/> is changed.
        /// </summary>
        public event EventHandler<RoutedEventArgs> LocationChanged
        {
            add => AddHandler(LocationChangedEvent, value);
            remove => RemoveHandler(LocationChangedEvent, value);
        }

        /// <summary>
        /// Raises the <see cref="LocationChangedEvent"/>.
        /// </summary>
        protected void OnLocationChanged()
        {
            RaiseEvent(new RoutedEventArgs(LocationChangedEvent, this));
        }

        #endregion

        #region IKeyboardFocusTarget<DecoratorContainer>

        // Visual.Bounds and IKeyboardFocusTarget.Bounds would clash if both were named "Bounds".
        // Use explicit interface implementation to expose the graph-space focus bounds without a conflict.
        global::Avalonia.Rect IKeyboardFocusTarget<DecoratorContainer>.Bounds
            => new global::Avalonia.Rect(Location, ActualSize);

        DecoratorContainer IKeyboardFocusTarget<DecoratorContainer>.Element => this;

        #endregion

        private DecoratorsControl? _owner;

        /// <summary>
        /// Gets the <see cref="DecoratorsControl"/> that owns this <see cref="DecoratorContainer"/>,
        /// resolved lazily by walking the visual tree.
        /// </summary>
        public DecoratorsControl? Owner => _owner ??= this.FindAncestorOfType<DecoratorsControl>();

        static DecoratorContainer()
        {
            LocationProperty.Changed.AddClassHandler<DecoratorContainer, Point>(OnLocationChanged);

            // Needed for keyboard focus navigation (DecoratorsControl.TryMoveFocus).
            FocusableProperty.OverrideDefaultValue<DecoratorContainer>(true);
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<DecoratorContainer>(KeyboardNavigationMode.Cycle);
        }

        public DecoratorContainer()
        {
            SizeChanged += OnRenderSizeChanged;
        }

        protected void OnRenderSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            SetCurrentValue(ActualSizeProperty, ((DecoratorContainer)sender!).Bounds.Size);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            // Resolve and cache Owner while still attached — items placed directly in the
            // Decorators collection (as their own container, see DecoratorsControl's
            // NeedsContainerOverride) never go through a parameterized ctor, so Owner must be
            // captured here to still be available from OnDetachedFromVisualTree below (by then
            // the visual parent link is already gone and FindAncestorOfType would return null).
            _owner ??= this.FindAncestorOfType<DecoratorsControl>();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            // Focus-trap: if this decorator container is removed while it (or a descendant) holds
            // keyboard focus, return focus to the editor so keyboard navigation keeps working.
            bool hadKeyboardFocus = IsKeyboardFocusWithin;

            base.OnDetachedFromVisualTree(e);

            if (hadKeyboardFocus)
            {
                Owner?.Editor?.Focus();
            }
        }
    }
}
