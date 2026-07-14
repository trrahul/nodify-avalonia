using System;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Specifies the possible movement modes of a <see cref="GroupingNode"/>.
    /// </summary>
    public enum GroupingMovementMode
    {
        /// <summary>
        /// The <see cref="GroupingNode"/> will move its content when moved.
        /// </summary>
        Group,

        /// <summary>
        /// The <see cref="GroupingNode"/> will not move its content when moved.
        /// </summary>
        Self
    }

    /// <summary>
    /// Defines a panel with a header that groups <see cref="ItemContainer"/>s inside it and can be resized.
    /// </summary>
    [TemplatePart(Name = ElementResizeThumb, Type = typeof(Control))]
    [TemplatePart(Name = ElementHeader, Type = typeof(Control))]
    [TemplatePart(Name = ElementContent, Type = typeof(Control))]
    public class GroupingNode : HeaderedContentControl
    {
        protected const string ElementResizeThumb = "PART_ResizeThumb";
        protected const string ElementHeader = "PART_Header";
        protected const string ElementContent = "PART_Content";

        #region Routed Events

        public static readonly RoutedEvent ResizeStartedEvent = RoutedEvent.Register<GroupingNode,ResizeEventArgs>(nameof(ResizeStarted), RoutingStrategies.Bubble);
        public static readonly RoutedEvent ResizeCompletedEvent = RoutedEvent.Register<GroupingNode,ResizeEventArgs>(nameof(ResizeCompleted), RoutingStrategies.Bubble);

        /// <summary>
        /// Occurs when the node finished resizing.
        /// </summary>
        public event EventHandler<ResizeEventArgs> ResizeCompleted
        {
            add => AddHandler(ResizeCompletedEvent, value);
            remove => RemoveHandler(ResizeCompletedEvent, value);
        }

        /// <summary>
        /// Occurs when the node started resizing.
        /// </summary>
        public event EventHandler<ResizeEventArgs> ResizeStarted
        {
            add => AddHandler(ResizeStartedEvent, value);
            remove => RemoveHandler(ResizeStartedEvent, value);
        }

        #endregion

        #region Dependency Properties

        public static readonly StyledProperty<IBrush> HeaderBrushProperty = Node.HeaderBrushProperty.AddOwner<GroupingNode>();
        public static readonly StyledProperty<bool> CanResizeProperty = AvaloniaProperty.Register<GroupingNode,bool>(nameof(CanResize), true);
        public static readonly DirectProperty<GroupingNode, Size> ActualSizeProperty = AvaloniaProperty.RegisterDirect<GroupingNode, Size>(nameof(ActualSize),
            o => o.ActualSize, (o, v) => o.ActualSize = v, defaultBindingMode: BindingMode.TwoWay);
        public static readonly StyledProperty<GroupingMovementMode> MovementModeProperty = AvaloniaProperty.Register<GroupingNode, GroupingMovementMode>(nameof(MovementMode), GroupingMovementMode.Group);
        public static readonly StyledProperty<ICommand?> ResizeCompletedCommandProperty = AvaloniaProperty.Register<GroupingNode,ICommand?>(nameof(ResizeCompletedCommand));
        public static readonly StyledProperty<ICommand?> ResizeStartedCommandProperty = AvaloniaProperty.Register<GroupingNode,ICommand?>(nameof(ResizeStartedCommand));
        public static readonly StyledProperty<IControlTemplate?> ResizeThumbTemplateProperty = AvaloniaProperty.Register<GroupingNode, IControlTemplate?>(nameof(ResizeThumbTemplate));
        public static readonly StyledProperty<bool> IsContentHitTestVisibleProperty = AvaloniaProperty.Register<GroupingNode, bool>(nameof(IsContentHitTestVisible), true);

        private static void OnActualSizeChanged(GroupingNode node, AvaloniaPropertyChangedEventArgs<Size> args)
        {
            var newSize = args.NewValue.Value;
            node.Width = newSize.Width;
            node.Height = newSize.Height;
        }

        /// <summary>
        /// Gets or sets the brush used for the background of the <see cref="HeaderedContentControl.Header"/> of this <see cref="GroupingNode"/>.
        /// </summary>
        public IBrush HeaderBrush
        {
            get => GetValue(HeaderBrushProperty);
            set => SetValue(HeaderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="GroupingNode"/> can be resized.
        /// </summary>
        public bool CanResize
        {
            get => (bool)GetValue(CanResizeProperty);
            set => SetValue(CanResizeProperty, value);
        }

        private Size _actualSize;

        /// <summary>
        /// Gets or sets the actual size of this <see cref="GroupingNode"/>.
        /// </summary>
        /// <remarks>
        /// A direct property using per-component <see cref="double.Equals(double)"/> equality, so NaN
        /// compares equal to NaN. With value equality (<c>==</c>) NaN never equals itself, which makes a
        /// TwoWay binding echo an auto-size (NaN, NaN) sentinel back and forth forever.
        /// </remarks>
        public Size ActualSize
        {
            get => _actualSize;
            set
            {
                if (_actualSize.Width.Equals(value.Width) && _actualSize.Height.Equals(value.Height))
                {
                    return;
                }

                SetAndRaise(ActualSizeProperty, ref _actualSize, value);
            }
        }

        /// <summary>
        /// Gets or sets the default movement mode which can be temporarily changed by holding the
        /// <see cref="EditorGestures.GroupingNodeGestures.SwitchMovementMode"/> modifier key while dragging by the header.
        /// </summary>
        public GroupingMovementMode MovementMode
        {
            get => (GroupingMovementMode)GetValue(MovementModeProperty);
            set => SetValue(MovementModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used for the resize thumb of this <see cref="GroupingNode"/>.
        /// </summary>
        public IControlTemplate? ResizeThumbTemplate
        {
            get => GetValue(ResizeThumbTemplateProperty);
            set => SetValue(ResizeThumbTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the content area of this <see cref="GroupingNode"/> is hit-test visible.
        /// When false, pointer input passes through the content to the items underneath the group.
        /// </summary>
        public bool IsContentHitTestVisible
        {
            get => GetValue(IsContentHitTestVisibleProperty);
            set => SetValue(IsContentHitTestVisibleProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="ResizeCompleted"/> event is not handled.
        /// Parameter is the <see cref="ItemContainer.ActualSize"/> of the container.
        /// </summary>
        public ICommand? ResizeCompletedCommand
        {
            get => GetValue(ResizeCompletedCommandProperty);
            set => SetValue(ResizeCompletedCommandProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="ResizeStarted"/> event is not handled.
        /// Parameter is the <see cref="ItemContainer.ActualSize"/> of the container.
        /// </summary>
        public ICommand? ResizeStartedCommand
        {
            get => GetValue(ResizeStartedCommandProperty);
            set => SetValue(ResizeStartedCommandProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="GroupingNode"/>.
        /// </summary>
        protected NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets the effective gesture mappings for this grouping node (per-editor override or global fallback).
        /// </summary>
        public EditorGestures ActualGestures => Editor?.ActualGestures ?? EditorGestures.Mappings;

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        protected ItemContainer? Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="Control"/> used to resize this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? ResizeThumb;

        /// <summary>
        /// Gets the <see cref="HeaderedContentControl.Header"/> control of this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? HeaderControl;

        /// <summary>
        /// Gets the <see cref="global::Avalonia.Controls.ContentControl"/> control of this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? ContentControl;

        private double _minHeight = 30;
        private double _minWidth = 30;
        #endregion

        static GroupingNode()
        {
            Panel.ZIndexProperty.OverrideMetadata<GroupingNode>(new StyledPropertyMetadata<int>(-1));
            Panel.ZIndexProperty.Changed.AddClassHandler<GroupingNode, int>(OnZIndexPropertyChanged);
            ActualSizeProperty.Changed.AddClassHandler<GroupingNode,Size>(OnActualSizeChanged);
        }

        private static void OnZIndexPropertyChanged(GroupingNode node, AvaloniaPropertyChangedEventArgs<int> e)
        {
            // Propagate to the owning ItemContainer — the container is what the editor's items panel stacks.
            node.Container?.SetValue(Panel.ZIndexProperty, e.NewValue.Value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupingNode"/> class.
        /// </summary>
        public GroupingNode()
        {
            AddHandler(Thumb.DragDeltaEvent, OnResize);
            AddHandler(Thumb.DragCompletedEvent, OnResizeCompleted);
            AddHandler(Thumb.DragStartedEvent, OnResizeStarted);

            Loaded += OnNodeLoaded;
            Unloaded += OnNodeUnloaded;
        }

        private void OnNodeLoaded(object? sender, RoutedEventArgs e)
        {
            if (HeaderControl != null)
            {
                HeaderControl.PointerPressed += OnHeaderPointerPressed;
                HeaderControl.SizeChanged += OnHeaderSizeChanged;
                CalculateDesiredHeaderSize();
            }
        }

        private void OnNodeUnloaded(object? sender, RoutedEventArgs e)
        {
            if (HeaderControl != null)
            {
                HeaderControl.PointerPressed -= OnHeaderPointerPressed;
                HeaderControl.SizeChanged -= OnHeaderSizeChanged;
            }
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (Container != null && Editor != null && ActualGestures.ItemContainer.Drag.Matches(e.Source, e))
            {
                var prevMovementMode = MovementMode;

                if (e.KeyModifiers == ActualGestures.GroupingNode.SwitchMovementMode)
                {
                    MovementMode = MovementMode == GroupingMovementMode.Group ? GroupingMovementMode.Self : GroupingMovementMode.Group;
                }

                if (ActualGestures.ItemContainer.Selection.Append.Matches(e.Source, e))
                {
                    Editor.SelectArea(new Rect(Container.Location, Bounds.Size), append: true, fit: true);
                }
                else if (ActualGestures.ItemContainer.Selection.Remove.Matches(e.Source, e))
                {
                    Editor.UnselectArea(new Rect(Container.Location, Bounds.Size), fit: true);
                }
                else if (ActualGestures.ItemContainer.Selection.Invert.Matches(e.Source, e))
                {
                    if (Container.IsSelected)
                    {
                        Editor.UnselectArea(new Rect(Container.Location, Bounds.Size), fit: true);
                        Container.IsSelected = true;
                    }
                    else
                    {
                        Editor.SelectArea(new Rect(Container.Location, Bounds.Size), append: true, fit: true);
                    }
                }
                else if (ActualGestures.ItemContainer.Selection.Replace.Matches(e.Source, e) || ActualGestures.ItemContainer.Drag.Matches(e.Source, e))
                {
                    Editor.SelectArea(new Rect(Container.Location, Bounds.Size), append: Container.IsSelected, fit: true);
                }

                if (MovementMode == GroupingMovementMode.Self)
                {
                    Editor.UnselectArea(new Rect(Container.Location, Bounds.Size), fit: true);
                    Container.IsSelected = true;
                }

                MovementMode = prevMovementMode;
            }
        }

        /// <summary>
        /// Toggles the selection of the <see cref="ItemContainer"/>s inside this group.
        /// If any contained container is selected, all are unselected (and only this group's
        /// container stays selected); otherwise all contained containers are selected.
        /// </summary>
        /// <remarks>
        /// This method is public so it can be invoked directly (for example bound to a command).
        /// </remarks>
        public void ToggleContentSelection()
        {
            if (Editor != null && Container != null)
            {
                var groupBounds = new Rect(Container.Location, Bounds.Size);
                bool hasSelection = Editor.SelectedContainers.Any(x => x != Container && x.IsSelectableInArea(groupBounds, true));
                if (hasSelection)
                {
                    Editor.UnselectArea(groupBounds, fit: true);
                    Container.IsSelected = true;
                }
                else
                {
                    Editor.SelectArea(groupBounds, append: true, fit: true);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            ResizeThumb = e.NameScope.Find<Control>(ElementResizeThumb);
            HeaderControl = e.NameScope.Find<Control>(ElementHeader);
            ContentControl = e.NameScope.Find<Control>(ElementContent);

            Container = this.FindAncestorOfType<ItemContainer>();
            Editor = Container?.Editor ?? this.FindAncestorOfType<NodifyEditor>();

            if (Container != null)
            {
                Container.SetValue(ZIndexProperty, GetValue(ZIndexProperty));
            }
        }

        private void OnResize(object? sender, VectorEventArgs e)
        {
            if (CanResize && ReferenceEquals(e.Source, ResizeThumb))
            {
                double resultWidth = Bounds.Width + e.Vector.X;
                double resultHeight = Bounds.Height + e.Vector.Y;

                if (Editor != null)
                {
                    uint cellSize = Editor.GridCellSize;
                    resultWidth = (int)resultWidth / cellSize * cellSize;
                    resultHeight = (int)resultHeight / cellSize * cellSize;
                }

                Width = Math.Max(_minWidth, resultWidth);
                Height = Math.Max(_minHeight, resultHeight);

                e.Handled = true;
            }
        }

        private void OnResizeStarted(object? sender, VectorEventArgs e)
        {
            ActualSize = new Size( Bounds.Width, Bounds.Height);
            var args = new ResizeEventArgs(ActualSize, ActualSize)
            {
                RoutedEvent = ResizeStartedEvent,
                Source = this
            };

            RaiseEvent(args);

            if (!args.Handled && (ResizeStartedCommand?.CanExecute(ActualSize) ?? false))
            {
                ResizeStartedCommand.Execute(ActualSize);
            }
        }

        private void OnResizeCompleted(object? sender, VectorEventArgs e)
        {
            Size previousSize = ActualSize;
            var newSize = new Size(Bounds.Width, Bounds.Height);
            ActualSize = newSize;

            var args = new ResizeEventArgs(previousSize, newSize)
            {
                RoutedEvent = ResizeCompletedEvent,
                Source = this
            };

            RaiseEvent(args);

            if (!args.Handled && (ResizeCompletedCommand?.CanExecute(newSize) ?? false))
            {
                ResizeCompletedCommand.Execute(newSize);
            }
        }

        private void OnHeaderSizeChanged(object? sender, SizeChangedEventArgs e)
            => CalculateDesiredHeaderSize();

        private void CalculateDesiredHeaderSize()
        {
            if (HeaderControl != null && ResizeThumb != null)
            {
                _minHeight = Math.Max(HeaderControl.Bounds.Height + ResizeThumb.Bounds.Height, MinHeight);
                _minWidth = Math.Max(ResizeThumb.Bounds.Width, MinWidth);

                if (ContentControl != null)
                {
                    _minWidth = Math.Max(_minWidth, ContentControl.DesiredSize.Width);
                    _minHeight = Math.Max(_minHeight, _minHeight + ContentControl.DesiredSize.Height);
                }
            }

            // Allow selecting only by the header
            if (Container != null)
            {
                Container.DesiredSizeForSelection = new Size(Bounds.Width, Math.Max(HeaderControl?.Bounds.Height ?? _minHeight, MinHeight));
            }
        }
    }
}
