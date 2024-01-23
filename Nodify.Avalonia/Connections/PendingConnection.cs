using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
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
        public static readonly StyledProperty<object> SourceProperty = AvaloniaProperty.Register<PendingConnection,object>(nameof(Source));
        public static readonly StyledProperty<object> TargetProperty = AvaloniaProperty.Register<PendingConnection,object>(nameof(Target));
        public static readonly StyledProperty<object> PreviewTargetProperty = AvaloniaProperty.Register<PendingConnection,object>(nameof(PreviewTarget));
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
        /// Gets or sets the <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/> that started this pending connection.
        /// </summary>
        public object? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/> (or potentially an <see cref="ItemContainer"/>'s <see cref="FrameworkElement.DataContext"/> if <see cref="AllowOnlyConnectors"/> is false) that the <see cref="Source"/> can connect to.
        /// Only set when the connection is completed (see <see cref="CompletedCommand"/>).
        /// </summary>
        public object? Target
        {
            get => GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// <see cref="PreviewTarget"/> will be updated with a potential <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/> if this is true.
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
        /// <see cref="Source"/> will be set to the <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/> that started this connection and will also be the command's parameter.
        /// </summary>
        public ICommand? StartedCommand
        {
            get => (ICommand?)GetValue(StartedCommandProperty);
            set => SetValue(StartedCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command to invoke when the pending connection is completed.
        /// Will not be invoked if <see cref="NodifyEditor.ConnectionCompletedCommand"/> is used.
        /// <see cref="Target"/> will be set to the desired <see cref="Connector"/>'s <see cref="FrameworkElement.DataContext"/> and will also be the command's parameter.
        /// </summary>
        public ICommand? CompletedCommand
        {
            get => (ICommand?)GetValue(CompletedCommandProperty);
            set => SetValue(CompletedCommandProperty, value);
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets the <see cref="NodifyEditor"/> that owns this <see cref="PendingConnection"/>.
        /// </summary>
        protected NodifyEditor? Editor { get; private set; }

        private Control? _previousConnector;

        #endregion

        static PendingConnection()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(PendingConnection), new FrameworkPropertyMetadata(typeof(PendingConnection)));
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
            Editor = this.FindAncestorOfType<NodifyEditor>();

            if (Editor != null)
            {
                Editor.AddHandler(Connector.PendingConnectionStartedEvent, new PendingConnectionEventHandler(OnPendingConnectionStarted));
                Editor.AddHandler(Connector.PendingConnectionDragEvent, new PendingConnectionEventHandler(OnPendingConnectionDrag));
                Editor.AddHandler(Connector.PendingConnectionCompletedEvent, new PendingConnectionEventHandler(OnPendingConnectionCompleted));
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
                }
            }
        }

        protected virtual void OnPendingConnectionDrag(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Handled && IsVisible)
            {
                e.Handled = true;
                TargetAnchor = new Point(e.Anchor.X + e.OffsetX, e.Anchor.Y + e.OffsetY);
                if (Editor != null && (EnablePreview || EnableSnapping))
                {
                    // Look for a potential connector
                    Control? connector = GetPotentialConnector(Editor,TargetAnchor, AllowOnlyConnectors);

                    // Update the connector's anchor and snap to it if snapping is enabled
                    if (EnableSnapping && connector is Connector target)
                    {
                        target.UpdateAnchor();
                        TargetAnchor = target.Anchor;
                    }

                    // If it's not the same connector
                    if (connector != _previousConnector)
                    {
                        if (_previousConnector != null)
                        {
                            SetIsOverElement(_previousConnector, false);
                        }

                        // And we have a connector
                        if (connector != null)
                        {
                            SetIsOverElement(connector, true);

                            // Update the preview target if enabled
                            if (EnablePreview)
                            {
                                PreviewTarget = connector.DataContext;
                            }
                        }

                        _previousConnector = connector;
                    }
                }
            }
        }

        protected virtual void OnPendingConnectionCompleted(object sender, PendingConnectionEventArgs e)
        {
            if (!e.Handled && IsVisible)
            {
                e.Handled = true;
                IsVisible = false;

                if (_previousConnector != null)
                {
                    SetIsOverElement(_previousConnector, false);
                    _previousConnector = null;
                }

                if (!e.Canceled)
                {
                    Target = e.TargetConnector;

                    // Invoke the CompletedCommand if event is not handled
                    if (CompletedCommand?.CanExecute(Target) ?? false)
                    {
                        CompletedCommand?.Execute(Target);
                    }
                }
            }
        }

        #endregion

        #region Helpers

        /// <summary>Searches for a potential connector prioritizing <see cref="Connector"/>s</summary>
        /// <param name="editor">The editor to scan for connectors or item containers.</param>
        /// <param name="pointerEventArgs"></param>
        /// <param name="allowOnlyConnectors">Will also look for <see cref="ItemContainer"/>s if false.</param>
        /// <returns>A connector, an item container, the editor or null.</returns>
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
