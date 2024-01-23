﻿using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
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
        public static readonly StyledProperty<Size> ActualSizeProperty = AvaloniaProperty.Register<GroupingNode,Size>(nameof(ActualSize), defaultBindingMode: BindingMode.TwoWay);
        public static readonly StyledProperty<GroupingMovementMode> MovementModeProperty = AvaloniaProperty.Register<GroupingNode, GroupingMovementMode>(nameof(MovementMode), GroupingMovementMode.Group);
        public static readonly DirectProperty<GroupingNode,ICommand?> ResizeCompletedCommandProperty = AvaloniaProperty.RegisterDirect<GroupingNode,ICommand?>(nameof(ResizeCompletedCommand), o => o.ResizeCompletedCommand,(o,v)=>o.ResizeCompletedCommand = v);
        public static readonly DirectProperty<GroupingNode, ICommand?> ResizeStartedCommandProperty = AvaloniaProperty.RegisterDirect<GroupingNode,ICommand?>(nameof(ResizeStartedCommand), o => o.ResizeStartedCommand, (o,v)=>o.ResizeStartedCommand = v);

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

        /// <summary>
        /// Gets or sets the actual size of this <see cref="GroupingNode"/>.
        /// </summary>
        public Size ActualSize
        {
            get => (Size)GetValue(ActualSizeProperty);
            set => SetValue(ActualSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the default movement mode which can be temporarily changed by holding the <see cref="SwitchMovementModeModifierKey"/> while dragging by the header.
        /// </summary>
        public GroupingMovementMode MovementMode
        {
            get => (GroupingMovementMode)GetValue(MovementModeProperty);
            set => SetValue(MovementModeProperty, value);
        }

        /// <summary>
        /// Invoked when the <see cref="ResizeCompleted"/> event is not handled.
        /// Parameter is the <see cref="ItemContainer.ActualSize"/> of the container.
        /// </summary>
        public ICommand? ResizeCompletedCommand
        {
            get => _resizeCompletedCommand;
            set => SetAndRaise(ResizeCompletedCommandProperty,ref _resizeCompletedCommand, value);
        }

        /// <summary>
        /// Invoked when the <see cref="ResizeStarted"/> event is not handled.
        /// Parameter is the <see cref="ItemContainer.ActualSize"/> of the container.
        /// </summary>
        public ICommand? ResizeStartedCommand
        {
            get => _resizeStartedCommand;
            set => SetAndRaise(ResizeStartedCommandProperty,ref _resizeStartedCommand, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="GroupingNode"/>.
        /// </summary>
        protected NodifyEditor? Editor { get; private set; }

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="Container"/>.
        /// </summary>
        protected ItemContainer? Container { get; private set; }

        /// <summary>
        /// Gets the <see cref="FrameworkElement"/> used to resize this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? ResizeThumb;

        /// <summary>
        /// Gets the <see cref="HeaderedContentControl.Header"/> control of this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? HeaderControl;

        /// <summary>
        /// Gets the <see cref="Avalonia.Controls.ContentControl"/> control of this <see cref="GroupingNode"/>.
        /// </summary>
        protected Control? ContentControl;

        private double _minHeight = 30;
        private double _minWidth = 30;
        private ICommand? _resizeCompletedCommand;
        private ICommand? _resizeStartedCommand;

        #endregion

        static GroupingNode()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(GroupingNode), new FrameworkPropertyMetadata(typeof(GroupingNode)));
            Panel.ZIndexProperty.OverrideMetadata<GroupingNode>(new StyledPropertyMetadata<int>(-1));
            Panel.ZIndexProperty.Changed.AddClassHandler<GroupingNode, int>(OnZIndexPropertyChanged);
            ActualSizeProperty.Changed.AddClassHandler<GroupingNode,Size>(OnActualSizeChanged);
        }

        private static void OnZIndexPropertyChanged(GroupingNode node, AvaloniaPropertyChangedEventArgs<int> e)
        {
            if (node.Container != null)
            {
                node.SetValue(Panel.ZIndexProperty, e.NewValue.Value);
            }
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

        private void OnNodeLoaded(object sender, RoutedEventArgs e)
        {
            if (HeaderControl != null)
            {
                HeaderControl.PointerPressed += OnHeaderPointerPressed;
                HeaderControl.SizeChanged += OnHeaderSizeChanged;
                CalculateDesiredHeaderSize();
            }
        }

        private void OnNodeUnloaded(object sender, RoutedEventArgs e)
        {
            if (HeaderControl != null)
            {
                HeaderControl.PointerPressed -= OnHeaderPointerPressed;
                HeaderControl.SizeChanged -= OnHeaderSizeChanged;
            }
        }

        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (Container != null && Editor != null && EditorGestures.ItemContainer.Drag.Matches(e.Source, e)) 
            {
                // Switch the default movement mode if necessary
                var prevMovementMode = MovementMode;
                
                if (Editor.State.CurrentKeyModifiers == EditorGestures.GroupingNode.SwitchMovementMode)
                {
                    MovementMode = MovementMode == GroupingMovementMode.Group ? GroupingMovementMode.Self : GroupingMovementMode.Group;
                }

                // Select the content and move with it
                if (EditorGestures.Selection.Append.Matches(e.Source, e))
                {
                    Editor.SelectArea(new Rect(Container.Location, Bounds.Size), append: true, fit: true);
                }
                else if (EditorGestures.Selection.Remove.Matches(e.Source, e))
                {
                    Editor.UnselectArea(new Rect(Container.Location, Bounds.Size), fit: true);
                }
                else if (EditorGestures.Selection.Invert.Matches(e.Source, e))
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
                else if (EditorGestures.Selection.Replace.Matches(e.Source, e) || EditorGestures.ItemContainer.Drag.Matches(e.Source, e))
                {
                    Editor.SelectArea(new Rect(Container.Location, Bounds.Size), append: Container.IsSelected, fit: true);
                }

                // Deselect content
                if (MovementMode == GroupingMovementMode.Self)
                {
                    Editor.UnselectArea(new Rect(Container.Location, Bounds.Size), fit: true);
                    Container.IsSelected = true;
                }

                // Switch the default movement mode back
                MovementMode = prevMovementMode;
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

                // Snap to grid
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

            // Raise ResizeStartedCommand if ResizeStartedEvent event is not handled
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

            // Raise ResizeCompletedCommand if ResizeCompletedEvent event is not handled
            if (!args.Handled && (ResizeCompletedCommand?.CanExecute(newSize) ?? false))
            {
                ResizeCompletedCommand.Execute(newSize);
            }
        }

        private void OnHeaderSizeChanged(object sender, SizeChangedEventArgs e)
            => CalculateDesiredHeaderSize();

        private void CalculateDesiredHeaderSize()
        {
            if (HeaderControl != null && ResizeThumb != null)
            {
                _minHeight = Math.Max(HeaderControl.Bounds.Height + ResizeThumb.Bounds.Height, MinHeight);
                _minWidth = Math.Max(ResizeThumb.Bounds.Width, MinWidth);

                // If there's content don't resize it
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
