using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia
{
    /// <summary>
    /// A minimap control that can position the viewport, and zoom in and out.
    /// </summary>
    public class Minimap : ItemsControl
    {
        #region Properties

        public static readonly StyledProperty<Point> ViewportLocationProperty =
            NodifyEditor.ViewportLocationProperty.AddOwner<Minimap>(
                new StyledPropertyMetadata<Point>(default, BindingMode.TwoWay));

        public static readonly StyledProperty<Size> ViewportSizeProperty =
            NodifyEditor.ViewportSizeProperty.AddOwner<Minimap>();

        public static readonly StyledProperty<ControlTheme?> ViewportStyleProperty =
            AvaloniaProperty.Register<Minimap, ControlTheme?>(nameof(ViewportStyle));

        public static readonly StyledProperty<Rect> ExtentProperty =
            NodifyCanvas.ExtentProperty.AddOwner<Minimap>();

        public static readonly StyledProperty<Rect> ItemsExtentProperty =
            AvaloniaProperty.Register<Minimap, Rect>(nameof(ItemsExtent));

        public static readonly StyledProperty<Size> MaxViewportOffsetProperty =
            AvaloniaProperty.Register<Minimap, Size>(nameof(MaxViewportOffset), new Size(2000, 2000));

        public static readonly StyledProperty<bool> ResizeToViewportProperty =
            AvaloniaProperty.Register<Minimap, bool>(nameof(ResizeToViewport));

        public static readonly StyledProperty<bool> IsReadOnlyProperty =
            AvaloniaProperty.Register<Minimap, bool>(nameof(IsReadOnly));

        /// <summary>Gets or sets the custom <see cref="EditorGestures.MinimapGestures"/> used by this minimap instance.</summary>
        public static readonly StyledProperty<EditorGestures.MinimapGestures?> InputGesturesProperty =
            AvaloniaProperty.Register<Minimap, EditorGestures.MinimapGestures?>(nameof(InputGestures));

        /// <inheritdoc cref="NodifyEditor.ViewportLocation" />
        public Point ViewportLocation
        {
            get => (Point)GetValue(ViewportLocationProperty);
            set => SetValue(ViewportLocationProperty, value);
        }

        /// <inheritdoc cref="NodifyEditor.ViewportSize" />
        public Size ViewportSize
        {
            get => (Size)GetValue(ViewportSizeProperty);
            set => SetValue(ViewportSizeProperty, value);
        }

        /// <summary>Gets or sets the style to use for the viewport rectangle.</summary>
        public ControlTheme? ViewportStyle
        {
            get => (ControlTheme?)GetValue(ViewportStyleProperty);
            set => SetValue(ViewportStyleProperty, value);
        }

        /// <summary>The area covered by the items and the viewport rectangle in graph space.</summary>
        public Rect Extent
        {
            get => (Rect)GetValue(ExtentProperty);
            set => SetValue(ExtentProperty, value);
        }

        /// <summary>The area covered by the <see cref="MinimapItem"/>s in graph space.</summary>
        public Rect ItemsExtent
        {
            get => (Rect)GetValue(ItemsExtentProperty);
            set => SetValue(ItemsExtentProperty, value);
        }

        /// <summary>The max position from the <see cref="NodifyEditor.ItemsExtent"/> that the viewport can move to.</summary>
        public Size MaxViewportOffset
        {
            get => (Size)GetValue(MaxViewportOffsetProperty);
            set => SetValue(MaxViewportOffsetProperty, value);
        }

        /// <summary>Whether the minimap should resize to also display the whole viewport.</summary>
        public bool ResizeToViewport
        {
            get => (bool)GetValue(ResizeToViewportProperty);
            set => SetValue(ResizeToViewportProperty, value);
        }

        /// <summary>Whether the minimap can move and zoom the viewport.</summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom <see cref="EditorGestures.MinimapGestures"/> used by the minimap.
        /// </summary>
        public EditorGestures.MinimapGestures? InputGestures
        {
            get => GetValue(InputGesturesProperty);
            set => SetValue(InputGesturesProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="EditorGestures.MinimapGestures"/> currently used by the minimap.
        /// Returns <see cref="InputGestures"/> if set, otherwise the default gestures from
        /// <see cref="EditorGestures.Mappings"/>.
        /// </summary>
        public EditorGestures.MinimapGestures ActualGestures => InputGestures ?? EditorGestures.Mappings.Minimap;

        #endregion

        #region Routed Events

        /// <summary>Triggered when zooming in or out using the mouse wheel.</summary>
        public static readonly RoutedEvent<ZoomEventArgs> ZoomEvent =
            RoutedEvent.Register<Minimap, ZoomEventArgs>(nameof(Zoom), RoutingStrategies.Bubble);

        /// <summary>Triggered when zooming in or out using the mouse wheel.</summary>
        public event ZoomEventHandler Zoom
        {
            add => AddHandler(ZoomEvent, value);
            remove => RemoveHandler(ZoomEvent, value);
        }

        #endregion

        /// <summary>Gets the <see cref="ItemsPresenter"/> that hosts the <see cref="MinimapItem"/>s.</summary>
        protected internal ItemsPresenter? ItemsHost { get; private set; }

        /// <summary>Whether the user is currently dragging (panning) the viewport.</summary>
        protected bool IsDragging { get; private set; }

        /// <summary>
        /// Gets the current mouse location in graph space coordinates (relative to the
        /// <see cref="ItemsHost"/>). Updated on every pointer press/move/release/wheel event.
        /// </summary>
        public Point MouseLocation { get; private set; }

        /// <summary>
        /// Gets or sets whether panning cancellation is allowed (see
        /// <see cref="EditorGestures.MinimapGestures.CancelAction"/>). Default true.
        /// </summary>
        public static bool AllowPanningCancellation { get; set; } = true;

        /// <summary>
        /// Determines whether toggled panning mode is enabled, allowing the user to start and end
        /// the drag-viewport interaction in two clicks instead of a press-hold-release.
        /// </summary>
        public static bool EnableToggledPanningMode { get; set; }

        /// <summary>
        /// Defines the distance to pan when using directional input (such as arrow keys).
        /// </summary>
        public static double NavigationStepSize { get; set; } = 50d;

        // Despite the name, this stores the raw pan-start pointer location (graph space), not a
        // resolved viewport — cancel re-derives the viewport via SetViewportLocation (transform + clamp).
        private Point _initialViewportLocation;

        // The pointer that captured the drag, so a keyboard cancel (Escape) can release capture too.
        private IPointer? _capturedPointer;

        static Minimap()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Minimap>(true);

            // Needed so a drag can take keyboard focus and receive the Escape cancel gesture
            // (see OnPointerPressed/OnKeyUp below).
            FocusableProperty.OverrideDefaultValue<Minimap>(true);

            // Focusable-by-click but not part of Tab traversal. This does not block the explicit
            // Focus() call used by drag/keyboard-nav below.
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<Minimap>(KeyboardNavigationMode.None);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ItemsHost = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            MouseLocation = GetLocationInsideMinimap(e);
            var gestures = ActualGestures;

            if (!IsReadOnly && gestures.DragViewport.Matches(this, e))
            {
                if (EnableToggledPanningMode && IsDragging)
                {
                    // Second click of a toggled interaction ends it — the same gesture both
                    // starts and stops the interaction.
                    EndPanning();
                    ReleaseDragCapture();
                    e.Handled = true;
                    return;
                }

                // Use direct Pointer.Capture instead of PropagateMouseCapturedWithin —
                // our editor's auto-pan gate already uses IsPointerCapturedWithin.
                e.Pointer.Capture(this);
                _capturedPointer = e.Pointer;

                // Take keyboard focus so the Escape cancel gesture reaches OnKeyUp below.
                Focus();

                BeginPanning(MouseLocation);

                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            MouseLocation = GetLocationInsideMinimap(e);

            if (IsDragging)
            {
                UpdatePanning(MouseLocation);
            }
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            MouseLocation = GetLocationInsideMinimap(e);
            var gestures = ActualGestures;

            // Cancel gesture (right-click by default) is checked while a drag is in progress and
            // takes priority over a normal end; cancel is matched on gesture release. Assumes
            // CancelAction and DragViewport are not the same gesture.
            if (IsDragging && AllowPanningCancellation && gestures.CancelAction.Matches(this, e))
            {
                CancelPanning();
                ReleaseDragCapture();
                e.Handled = true;
            }
            else if (IsDragging && gestures.DragViewport.Matches(this, e) && !EnableToggledPanningMode)
            {
                // In toggled panning mode, releasing the gesture must NOT end the interaction —
                // it continues until the second click (see OnPointerPressed above).
                EndPanning();
            }

            var props = e.GetCurrentPoint(this).Properties;
            if (!IsDragging && !props.IsRightButtonPressed && !props.IsLeftButtonPressed && !props.IsMiddleButtonPressed)
            {
                // Release capture — no PropagateMouseCapturedWithin needed (see note in OnPointerPressed).
                e.Pointer.Capture(null);
                _capturedPointer = null;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Keyboard navigation (arrow-key pan, Home reset, Ctrl+Plus/Minus zoom). Only acts
            // while the minimap itself has keyboard focus — a drag in progress does not gate this;
            // keyboard navigation and zooming run independently of panning.
            if (!IsFocused)
            {
                return;
            }

            var gestures = ActualGestures;

            if (gestures.Pan.TryGetNavigationDirection(e, out var panDirection))
            {
                var panning = new Vector(-panDirection.X * NavigationStepSize, panDirection.Y * NavigationStepSize);
                UpdatePanning(panning);
                e.Handled = true;
            }
            else if (gestures.ResetViewport.Matches(this, e))
            {
                ResetViewport();
                e.Handled = true;
            }
            else if (!IsReadOnly && gestures.ZoomIn.Matches(this, e))
            {
                ZoomIn();
                e.Handled = true;
            }
            else if (!IsReadOnly && gestures.ZoomOut.Matches(this, e))
            {
                ZoomOut();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            // Escape-to-cancel (see EditorGestures.MinimapGestures.CancelAction). Matched on key-up,
            // since KeyUp is treated as the release event for keyboard gestures.
            if (IsDragging && AllowPanningCancellation)
            {
                var gestures = ActualGestures;
                if (gestures.CancelAction.Matches(this, e))
                {
                    CancelPanning();
                    ReleaseDragCapture();
                    e.Handled = true;
                }
            }
        }

        // A right-click cancel can arrive while the drag button is still held, and an Escape cancel
        // has no pointer-release event at all — so release capture explicitly instead of waiting on
        // OnPointerReleased's no-buttons-held check.
        private void ReleaseDragCapture()
        {
            _capturedPointer?.Capture(null);
            _capturedPointer = null;
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            MouseLocation = GetLocationInsideMinimap(e);

            if (!IsReadOnly && !e.Handled && ActualGestures.ZoomModifierKey == e.KeyModifiers)
            {
                double delta = e.Delta.Y;
                if (delta == 0d)
                {
                    return;
                }

                double zoom = Math.Pow(2.0, delta / 3.0);

                // Zoom AT the cursor, not the current viewport center.
                ZoomAtPosition(zoom, MouseLocation);

                e.Handled = true;
            }
        }

        /// <summary>
        /// Zooms by <paramref name="zoom"/> focused at <paramref name="location"/> (graph space).
        /// When <see cref="ResizeToViewport"/> is false the viewport is first panned so the focus
        /// point is centered, then a <see cref="ZoomEvent"/> is raised at the new center.
        /// </summary>
        public void ZoomAtPosition(double zoom, Point location)
        {
            if (IsReadOnly)
            {
                return;
            }

            if (!ResizeToViewport)
            {
                SetViewportLocation(location);
            }

            var viewportLocation = ViewportLocation + ViewportSize.ToVector() / 2;
            var args = new ZoomEventArgs(zoom, viewportLocation)
            {
                RoutedEvent = ZoomEvent,
                Source = this
            };
            RaiseEvent(args);
        }

        /// <summary>
        /// Pans the viewport by the specified amount. Used by keyboard navigation (arrow keys) as
        /// opposed to the drag-pan path (<see cref="SetViewportLocation"/>).
        /// </summary>
        public void UpdatePanning(Vector amount)
        {
            if (IsReadOnly)
            {
                return;
            }

            ViewportLocation -= amount;
        }

        /// <summary>
        /// Resets the viewport to the origin and raises a <see cref="ZoomEvent"/> with a zoom
        /// factor of 1, centered on the viewport. Triggered by the
        /// <see cref="EditorGestures.MinimapGestures.ResetViewport"/> gesture (Home).
        /// </summary>
        public void ResetViewport()
        {
            SetCurrentValue(ViewportLocationProperty, default);

            var args = new ZoomEventArgs(1d, new Point(ViewportSize.Width / 2, ViewportSize.Height / 2))
            {
                RoutedEvent = ZoomEvent,
                Source = this
            };
            RaiseEvent(args);
        }

        /// <summary>Zooms in at the viewport's center.</summary>
        public void ZoomIn() => SetZoom(Math.Pow(2.0, 120.0 / 3.0 / PointerHelper.PointerWheelDeltaForOneLine));

        /// <summary>Zooms out at the viewport's center.</summary>
        public void ZoomOut() => SetZoom(Math.Pow(2.0, -120.0 / 3.0 / PointerHelper.PointerWheelDeltaForOneLine));

        private void SetZoom(double zoom)
        {
            var viewportLocation = ViewportLocation + ViewportSize.ToVector() / 2;
            var args = new ZoomEventArgs(zoom, viewportLocation)
            {
                RoutedEvent = ZoomEvent,
                Source = this
            };
            RaiseEvent(args);
        }

        /// <summary>
        /// Starts the panning operation from the current <see cref="MouseLocation"/>. Call
        /// <see cref="EndPanning"/> to end the panning operation.
        /// </summary>
        /// <remarks>This method has no effect if a panning operation is already in progress.</remarks>
        public void BeginPanning() => BeginPanning(MouseLocation);

        /// <summary>
        /// Starts the panning operation from the specified <paramref name="location"/> (graph
        /// space coordinates). Call <see cref="EndPanning"/> to end the panning operation.
        /// </summary>
        /// <param name="location">The initial location where panning starts, in graph space coordinates.</param>
        /// <remarks>This method has no effect if a panning operation is already in progress or if <see cref="IsReadOnly"/> is true.</remarks>
        public void BeginPanning(Point location)
        {
            if (IsDragging || IsReadOnly)
            {
                return;
            }

            IsDragging = true;

            // Capture the raw pan-start POINTER location (not the resolved ViewportLocation) so a
            // cancel can re-derive the viewport via SetViewportLocation.
            _initialViewportLocation = location;

            SetViewportLocation(location);
        }

        /// <summary>
        /// Sets the viewport location to the specified location (graph space coordinates). Used by
        /// the drag-pan path (as opposed to <see cref="UpdatePanning(Vector)"/>, used by keyboard navigation).
        /// </summary>
        /// <param name="location">The location to pan the viewport to.</param>
        /// <remarks>This method has no effect if there's no panning operation in progress.</remarks>
        public void UpdatePanning(Point location)
        {
            if (!IsDragging)
            {
                return;
            }

            SetViewportLocation(location);
        }

        /// <summary>
        /// Ends the current panning operation, retaining the current <see cref="ViewportLocation"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no panning operation in progress.</remarks>
        public void EndPanning()
        {
            if (!IsDragging)
            {
                return;
            }

            // Re-arm _initialViewportLocation from the current pointer location so a subsequent
            // BeginPanning()/CancelPanning() pair (e.g. toggled panning mode) has a sane fallback.
            _initialViewportLocation = MouseLocation;
            IsDragging = false;
        }

        /// <summary>
        /// Cancels the current panning operation and reverts the viewport to its location before
        /// panning began if <see cref="AllowPanningCancellation"/> is true. Otherwise, ends the
        /// panning operation by calling <see cref="EndPanning"/>.
        /// </summary>
        /// <remarks>This method has no effect if there's no panning operation in progress.</remarks>
        public void CancelPanning()
        {
            if (!AllowPanningCancellation)
            {
                EndPanning();
                return;
            }

            if (IsDragging)
            {
                // Re-run the transform + clamp instead of restoring a stale ViewportLocation.
                SetViewportLocation(_initialViewportLocation);
                IsDragging = false;
            }
        }

        /// <summary>
        /// Translates the event location to graph space coordinates (relative to the <see cref="ItemsHost"/>).
        /// </summary>
        /// <param name="e">The pointer event.</param>
        /// <returns>A location inside the minimap.</returns>
        public Point GetLocationInsideMinimap(PointerEventArgs e) => e.GetPosition(ItemsHost);

        private void SetViewportLocation(Point location)
        {
            var position = location - new Vector(ViewportSize.Width / 2, ViewportSize.Height / 2) + (Vector)Extent.Position;

            if (MaxViewportOffset.Width != 0 || MaxViewportOffset.Height != 0)
            {
                double maxRight = ResizeToViewport
                    ? ItemsExtent.Right
                    : Math.Max(ItemsExtent.Right, ItemsExtent.Left + ViewportSize.Width);
                double maxBottom = ResizeToViewport
                    ? ItemsExtent.Bottom
                    : Math.Max(ItemsExtent.Bottom, ItemsExtent.Top + ViewportSize.Height);

                position = new Point(
                    Math.Clamp(position.X,
                        ItemsExtent.Left - ViewportSize.Width / 2 - MaxViewportOffset.Width,
                        maxRight - ViewportSize.Width / 2 + MaxViewportOffset.Width),
                    Math.Clamp(position.Y,
                        ItemsExtent.Top - ViewportSize.Height / 2 - MaxViewportOffset.Height,
                        maxBottom - ViewportSize.Height / 2 + MaxViewportOffset.Height));
            }

            ViewportLocation = position;
        }

        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
            => new MinimapItem();

        /// <inheritdoc />
        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = null;
            return item is not MinimapItem;
        }
    }
}
