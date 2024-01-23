using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Nodify.Avalonia
{
    /// <summary>
    /// The container for all the items generated from the <see cref="NodifyEditor.Decorators"/> collection.
    /// </summary>
    public class DecoratorContainer : ContentControl, INodifyCanvasItem
    {
        #region Dependency Properties

        public static readonly StyledProperty<Point> LocationProperty = ItemContainer.LocationProperty.AddOwner<DecoratorContainer>();
        public static readonly StyledProperty<Size> ActualSizeProperty = ItemContainer.ActualSizeProperty.AddOwner<DecoratorContainer>();

        /// <summary>
        /// Gets or sets the location of this <see cref="DecoratorContainer"/> inside the <see cref="NodifyEditor.DecoratorsHost"/>.
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

        static DecoratorContainer()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(DecoratorContainer), new FrameworkPropertyMetadata(typeof(DecoratorContainer)));
            LocationProperty.Changed.AddClassHandler<DecoratorContainer, Point>(OnLocationChanged);
        }

        public DecoratorContainer()
        {
            SizeChanged += OnRenderSizeChanged;
        }

        /// <inheritdoc />
        protected void OnRenderSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            ActualSize = ((DecoratorContainer)sender).Bounds.Size;
        }

    }
}
