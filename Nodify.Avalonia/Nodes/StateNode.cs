using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Represents a control that acts as a <see cref="Connector"/>.
    /// </summary>
    [TemplatePart(Name = ElementContent, Type = typeof(Control))]
    public class StateNode : Connector
    {
        protected const string ElementContent = "PART_Content";

        #region Dependency Properties

        public static readonly StyledProperty<IBrush> HighlightBrushProperty = ItemContainer.HighlightBrushProperty.AddOwner<StateNode>();
        public static readonly StyledProperty<object?> ContentProperty = ContentPresenter.ContentProperty.AddOwner<StateNode>();
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = ContentPresenter.ContentTemplateProperty.AddOwner<StateNode>();
        //public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = TemplatedControl.CornerRadiusProperty.AddOwner<StateNode>();

        /// <summary>
        /// Gets or sets the brush used when the <see cref="PendingConnection.IsOverElementProperty"/> attached property is true for this <see cref="StateNode"/>.
        /// </summary>
        public IBrush HighlightBrush
        {
            get => GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the data for the control's content.
        /// </summary>
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display the content of the control's header.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }
        
        /// <summary>
        /// Gets or sets a value that represents the degree to which the corners of the <see cref="StateNode"/> are rounded.
        /// </summary>
        //public CornerRadius CornerRadius
        //{
        //    get => (CornerRadius)GetValue(CornerRadiusProperty);
        //    set => SetValue(CornerRadiusProperty, value);
        //}

        #endregion
        
        /// <summary>
        /// Gets the <see cref="ContentControl"/> control of this <see cref="StateNode"/>.
        /// </summary>
        protected Control? ContentControl { get; private set; }

        static StateNode()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(StateNode), new FrameworkPropertyMetadata(typeof(StateNode)));
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            ContentControl = e.NameScope.Find<Control>(ElementContent);
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            // Do not raise PendingConnection events if clicked on content
            if (e.Source is Visual visual && (!ContentControl?.IsVisualAncestorOf(visual) ?? true) && !Equals(e.Source, ContentControl))
            {
                base.OnPointerPressed(e);
            }
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            // Do not raise PendingConnection events if clicked on content
            if (e.Source is Visual visual && (!ContentControl?.IsVisualAncestorOf(visual) ?? true) && !Equals(e.Source, ContentControl))
            {
                base.OnPointerReleased(e);
            }
        }
    }
}
