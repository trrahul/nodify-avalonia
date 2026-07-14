using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Nodify.Avalonia.Events;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia.Connections
{
    /// <summary>
    /// Represents a pending connection usually started by a <see cref="Connector"/> which invokes the <see cref="CompletedCommand"/> when completed.
    /// </summary>
    public class PendingConnection : ContentControl
    {
        internal const string IsOverElementPseudoClass = ":isoverelement";
        #region Dependency Properties

        public static readonly StyledProperty<Point> SourceAnchorProperty = AvaloniaProperty.Register<PendingConnection,Point>(nameof(SourceAnchor));
        public static readonly StyledProperty<Point> TargetAnchorProperty = AvaloniaProperty.Register<PendingConnection,Point>(nameof(TargetAnchor));
        public static readonly StyledProperty<object?> SourceProperty = AvaloniaProperty.Register<PendingConnection,object?>(nameof(Source));
        public static readonly StyledProperty<object?> TargetProperty = AvaloniaProperty.Register<PendingConnection,object?>(nameof(Target));
        public static readonly StyledProperty<object?> PreviewTargetProperty = AvaloniaProperty.Register<PendingConnection,object?>(nameof(PreviewTarget));
        public static readonly StyledProperty<bool> EnablePreviewProperty = AvaloniaProperty.Register<PendingConnection, bool>(nameof(EnablePreview));
        public static readonly StyledProperty<double> StrokeThicknessProperty = Shape.StrokeThicknessProperty.AddOwner<PendingConnection>();
        public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty = Shape.StrokeDashArrayProperty.AddOwner<PendingConnection>();
        public static readonly StyledProperty<IBrush?> StrokeProperty = Shape.StrokeProperty.AddOwner<PendingConnection>();
        public static readonly StyledProperty<bool> AllowOnlyConnectorsProperty = AvaloniaProperty.Register<PendingConnection, bool>(nameof(AllowOnlyConnectors),true);
        public static readonly StyledProperty<bool> EnableSnappingProperty = AvaloniaProperty.Register<PendingConnection, bool>(nameof(EnableSnapping));
        public static readonly StyledProperty<ConnectionDirection> DirectionProperty = BaseConnection.DirectionProperty.AddOwner<PendingConnection>();
        public new static readonly StyledProperty<bool> IsVisibleProperty = AvaloniaProperty.Register<PendingConnection, bool>(nameof(IsVisible), defaultBindingMode: BindingMode.TwoWay);

        private static void OnVisibilityChanged(PendingConnection pendingConnection,
            AvaloniaPropertyChangedEventArgs<bool> args) => pendingConnection.OnVisibilityChanged(args.NewValue.Value);

        private void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                Opacity = 1;
            }
            else
            {
                Opacity = 0;
            }
        }

        /// <summary>
        /// Gets or sets the starting point for the connection.
        /// </summary>
        public Point SourceAnchor
        {
            get => (Point)GetValue(SourceAnchorProperty);
            set => SetValue(SourceAnchorProperty, value);
        }

        public new bool IsVisible
        {
            get => GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point for the connection.
        /// </summary>
        public Point TargetAnchor
        {
            get => (Point)GetValue(TargetAnchorProperty);
            set => SetValue(TargetAnchorProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/> that started this pending connection.
        /// </summary>
        public object? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/> (or potentially an <see cref="ItemContainer"/>'s <see cref="StyledElement.DataContext"/> if <see cref="AllowOnlyConnectors"/> is false) that the <see cref="Source"/> can connect to.
        /// Only set when the connection is completed (see <see cref="CompletedCommand"/>).
        /// </summary>
        public object? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// <see cref="PreviewTarget"/> will be updated with a potential <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/> if this is true.
        /// </summary>
        public bool EnablePreview
        {
            get => (bool)GetValue(EnablePreviewProperty);
            set => SetValue(EnablePreviewProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Connector"/> or the <see cref="ItemContainer"/> (if <see cref="AllowOnlyConnectors"/> is false) that we're previewing.
        /// </summary>
        public object? PreviewTarget
        {
            get => GetValue(PreviewTargetProperty);
            set => SetValue(PreviewTargetProperty, value);
        }

        /// <summary>
        /// Enables snapping the <see cref="TargetAnchor"/> to a possible <see cref="Target"/> connector.
        /// </summary>
        public bool EnableSnapping
        {
            get => (bool)GetValue(EnableSnappingProperty);
            set => SetValue(EnableSnappingProperty, value);
        }

        /// <summary>
        /// If true will preview and connect only to <see cref="Connector"/>s, otherwise will also enable <see cref="ItemContainer"/>s.
        /// </summary>
        public bool AllowOnlyConnectors
        {
            get => (bool)GetValue(AllowOnlyConnectorsProperty);
            set => SetValue(AllowOnlyConnectorsProperty, value);
        }

        /// <summary>
        /// Gets or set the connection thickness.
        /// </summary>
        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the pattern of dashes and gaps that is used to outline the connection.
        /// </summary>
        public AvaloniaList<double>? StrokeDashArray
        {
            get => GetValue(StrokeDashArrayProperty);
            set => SetValue(StrokeDashArrayProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color of the connection.
        /// </summary>
        public IBrush? Stroke
        {
            get => GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the direction of this connection.
        /// </summary>
        public ConnectionDirection Direction
        {
            get => (ConnectionDirection)GetValue(DirectionProperty);
            set => SetValue(DirectionProperty, value);
        }

        #endregion

        #region Attached Properties

        private static readonly AttachedProperty<bool> AllowOnlyConnectorsAttachedProperty = AvaloniaProperty.RegisterAttached<PendingConnection,Control,bool>("AllowOnlyConnectorsAttached",true);
        /// <summary>
        /// Will be set for <see cref="Connector"/>s and <see cref="ItemContainer"/>s when the pending connection is over the element if <see cref="EnablePreview"/> or <see cref="EnableSnapping"/> is true.
        /// </summary>
        public static readonly AttachedProperty<bool> IsOverElementProperty = AvaloniaProperty.RegisterAttached<PendingConnection,Control,bool>("IsOverElement");

        internal static bool GetAllowOnlyConnectorsAttached(Control elem)
            => (bool)elem.GetValue(AllowOnlyConnectorsAttachedProperty);

        internal static void SetAllowOnlyConnectorsAttached(Control elem, bool value)
            => elem.SetValue(AllowOnlyConnectorsAttachedProperty, value);

        public static bool GetIsOverElement(Control elem)
            => (bool)elem.GetValue(IsOverElementProperty);

        public static void SetIsOverElement(Control elem, bool value)
            => elem.SetValue(IsOverElementProperty, value);

        private static void OnAllowOnlyConnectorsChanged(PendingConnection pendingConnection, AvaloniaPropertyChangedEventArgs<bool> args)
        {
            NodifyEditor? editor = pendingConnection.Editor;

            if (editor != null)
            {
                SetAllowOnlyConnectorsAttached(editor, args.NewValue.Value);
            }
        }

        #endregion

        #region Commands

        public static readonly StyledProperty<ICommand?> StartedCommandProperty = AvaloniaProperty.Register<PendingConnection,ICommand?>(nameof(StartedCommand));
        public static readonly StyledProperty<ICommand?> CompletedCommandProperty = AvaloniaProperty.Register<PendingConnection,ICommand?>(nameof(CompletedCommand));

        /// <summary>
        /// Gets or sets the command to invoke when the pending connection is started.
        /// Will not be invoked if <see cref="NodifyEditor.ConnectionStartedCommand"/> is used.
        /// <see cref="Source"/> will be set to the <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/> that started this connection and will also be the command's parameter.
        /// </summary>
        public ICommand? StartedCommand
        {
            get => (ICommand?)GetValue(StartedCommandProperty);
            set => SetValue(StartedCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command to invoke when the pending connection is completed.
        /// Will not be invoked if <see cref="NodifyEditor.ConnectionCompletedCommand"/> is used.
        /// <see cref="Target"/> will be set to the desired <see cref="Connector"/>'s <see cref="StyledElement.DataContext"/> and will also be the command's parameter.
        /// </summary>
        public ICommand? CompletedCommand
        {
            get => (ICommand?)GetValue(CompletedCommandProperty);
            set => SetValue(CompletedCommandProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets or sets whether hit testing is enabled for pending connections.
        /// </summary>
        public static bool EnableHitTesting { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of hotkeys that can be displayed for a pending connection.
        /// </summary>
        /// <remarks>The maximum value can be 9.</remarks>
        public static uint MaxHotKeys { get; set; } = 9;

        /// <summary>
        /// Gets or sets whether hotkeys are enabled for pending connections.
        /// </summary>
        public static HotKeysDisplayMode HotKeysDisplayMode { get; set; } = HotKeysDisplayMode.Keyboard;

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="PendingConnection"/>.
        /// </summary>
        protected NodifyEditor? Editor { get; private set; }

        private Control? _previousConnector;

        private Connector? _hotKeysSource;
        private readonly List<(HotKeyControl Control, Connector Connector)> _hotKeys = new List<(HotKeyControl, Connector)>();

        #endregion

        static PendingConnection()
        {
            AllowOnlyConnectorsProperty.Changed.AddClassHandler<PendingConnection, bool>(OnAllowOnlyConnectorsChanged);
            IsVisibleProperty.Changed.AddClassHandler<PendingConnection,bool>(OnVisibilityChanged);
            PendingConnection.IsOverElementProperty.Changed.AddClassHandler<Connector, bool>(Connector.OnIsOverElementChanged);
            AffectsRender<PendingConnection>(SourceAnchorProperty,TargetAnchorProperty);
            AffectsArrange<PendingConnection>(SourceAnchorProperty,TargetAnchorProperty);
        }

        public PendingConnection()
        {
            Opacity = 0;
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (Editor != null)
            {
                Editor.RemoveHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnPendingConnectionStarted));
                Editor.RemoveHandler(Connector.PendingConnectionDragEvent, new PendingConnectionEventHandler(OnPendingConnectionDrag));
                Editor.RemoveHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnPendingConnectionCompleted));
                Editor.RemoveHandler(InputElement.KeyUpEvent, new EventHandler<KeyEventArgs>(OnKeyUp));
            }

            Editor = this.FindAncestorOfType<NodifyEditor>();

            if (Editor != null)
            {
                Editor.AddHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnPendingConnectionStarted));
                Editor.AddHandler(Connector.PendingConnectionDragEvent, new PendingConnectionEventHandler(OnPendingConnectionDrag));
                Editor.AddHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnPendingConnectionCompleted));
                Editor.AddHandler(InputElement.KeyUpEvent, new EventHandler<KeyEventArgs>(OnKeyUp), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
                SetAllowOnlyConnectorsAttached(Editor, AllowOnlyConnectors);
            }
        }

        #region Event Handlers

        protected virtual void OnPendingConnectionStarted(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Handled && !e.Canceled)
            {
                e.Handled = true;
                e.Canceled = !StartedCommand?.CanExecute(e.SourceConnector) ?? false;

                Target = null;
                IsVisible = !e.Canceled;
                SourceAnchor = e.Anchor;
                TargetAnchor = new Point(e.Anchor.X + e.OffsetX, e.Anchor.Y + e.OffsetY);
                Source = e.SourceConnector;
                if (e.Source is Connector c)
                {
                    Stroke = c.BorderBrush;
                }
                if (!e.Canceled)
                {
                    StartedCommand?.Execute(Source);

                    if (e.Source is Connector connector)
                    {
                        ShowHotKeys(connector);
                    }
                }

                if (EnablePreview)
                {
                    PreviewTarget = e.SourceConnector;
                }
            }
        }

        protected virtual void OnPendingConnectionDrag(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Handled && IsVisible)
            {
                e.Handled = true;
                TargetAnchor = new Point(e.Anchor.X + e.OffsetX, e.Anchor.Y + e.OffsetY);

                if (!EnableHitTesting)
                {
                    return;
                }

                if (Editor != null)
                {
                    Control? connector = GetPotentialConnector(Editor, TargetAnchor, AllowOnlyConnectors);

                    if (EnableSnapping && connector is Connector target)
                    {
                        target.UpdateAnchor();
                        TargetAnchor = target.Anchor;
                    }

                    SetConnectionTarget(connector);

                    if (EnablePreview)
                    {
                        PreviewTarget = connector?.DataContext;
                    }
                }
            }
        }

        private void SetConnectionTarget(Control? target)
        {
            if (target == _previousConnector)
            {
                return;
            }

            if (_previousConnector != null)
            {
                SetIsOverElement(_previousConnector, false);
            }

            if (target != null)
            {
                SetIsOverElement(target, true);
            }

            _previousConnector = target;
        }

        protected virtual void OnPendingConnectionCompleted(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Handled && IsVisible)
            {
                e.Handled = true;
                IsVisible = false;

                SetConnectionTarget(null);
                HideHotKeys();

                if (!e.Canceled)
                {
                    Target = e.TargetConnector;

                    if (CompletedCommand?.CanExecute(Target) ?? false)
                    {
                        CompletedCommand?.Execute(Target);
                    }
                }

                if (EnablePreview)
                {
                    PreviewTarget = null;
                }
            }
        }

        #endregion

        #region Hot Keys

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (_hotKeysSource is { IsPendingConnection: true })
            {
                int hotKey = GetHotKey(e.Key);
                var entry = _hotKeys.Find(x => x.Control.Number == hotKey);
                if (entry.Control != null)
                {
                    var tempSource = _hotKeysSource;
                    tempSource.EndConnecting(entry.Connector);
                    tempSource.ReleaseMouseCapture();
                    HideHotKeys();
                    e.Handled = true;
                }
            }
        }

        internal static int GetHotKey(Key key)
        {
            if (key >= Key.D1 && key <= Key.D9)
            {
                return key - Key.D0;
            }

            if (key >= Key.NumPad1 && key <= Key.NumPad9)
            {
                return key - Key.NumPad0;
            }

            return -1;
        }

        private void ShowHotKeys(Connector sourceConnector)
        {
            if (Editor == null
                || HotKeysDisplayMode == HotKeysDisplayMode.None
                || (HotKeysDisplayMode == HotKeysDisplayMode.Keyboard && !Editor.LastInputWasKeyboard))
            {
                return;
            }

            var adornerLayer = AdornerLayer.GetAdornerLayer(Editor);
            if (adornerLayer == null)
            {
                return;
            }

            _hotKeysSource = sourceConnector;

            var connectCommand = Editor.ConnectionCompletedCommand ?? CompletedCommand;
            if (connectCommand != null)
            {
                bool isEditorConnect = connectCommand == Editor.ConnectionCompletedCommand;
                var connectorsInViewport = Editor.GetConnectorsInViewport();

                var possibleTargets = connectorsInViewport
                    .Where(x => isEditorConnect
                        ? connectCommand.CanExecute((sourceConnector.DataContext, x.DataContext))
                        : connectCommand.CanExecute(x.DataContext))
                    .OrderBy(x => sourceConnector.Anchor.VectorSubtract(x.Anchor).SquaredLength)
                    .Take((int)Math.Min(MaxHotKeys, 9));

                int index = 1;
                foreach (var connector in possibleTargets)
                {
                    var hotKeyControl = new HotKeyControl
                    {
                        Number = index,
                        DataContext = connector.DataContext
                    };

                    adornerLayer.Children.Add(hotKeyControl);
                    AdornerLayer.SetAdornedElement(hotKeyControl, connector);

                    _hotKeys.Add((hotKeyControl, connector));
                    index++;
                }
            }
        }

        private void HideHotKeys()
        {
            _hotKeysSource = null;

            if (_hotKeys.Count > 0)
            {
                var adornerLayer = Editor != null ? AdornerLayer.GetAdornerLayer(Editor) : null;
                if (adornerLayer != null)
                {
                    foreach (var (ctrl, _) in _hotKeys)
                    {
                        adornerLayer.Children.Remove(ctrl);
                    }
                }
                _hotKeys.Clear();
            }
        }

        #endregion

        #region Helpers

        // Returns a Connector, an ItemContainer (only if allowOnlyConnectors is false), the editor itself as fallback, or null.
        internal static Control? GetPotentialConnector(NodifyEditor editor, Point point,
            bool allowOnlyConnectors)
        {
            Connector? connector = editor.ItemsHost.GetElementUnderPoint<Connector>(point);
            if (connector != null && connector.Editor == editor)
                return connector;

            if (allowOnlyConnectors)
                return null;

            var itemContainer = editor.ItemsHost.GetElementUnderPoint<ItemContainer>(point);
            if (itemContainer != null && itemContainer.Editor == editor)
                return itemContainer;

            return editor;
        }

        #endregion
    }
}
