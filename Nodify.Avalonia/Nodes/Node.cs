using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Represents a control that has a list of <see cref="Input"/> <see cref="Connector"/>s and a list of <see cref="Output"/> <see cref="Connector"/>s.
    /// </summary>
    /// <remarks>
    /// Grouped collection views and group-container styling for the input/output connectors
    /// are not supported.
    /// </remarks>
    public class Node : HeaderedContentControl
    {
        #region Dependency Properties

        public static readonly StyledProperty<IBrush> ContentBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(ContentBrush));
        public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<Node,Thickness>(nameof(ContentPadding), new Thickness(16, 0, 16, 0));
        public static readonly StyledProperty<IBrush> HeaderBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(HeaderBrush));
        public static readonly StyledProperty<IBrush> FooterBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(FooterBrush));
        public static readonly StyledProperty<object?> FooterProperty = AvaloniaProperty.Register<Node, object?>(nameof(Footer));
        public static readonly StyledProperty<DataTemplate> FooterTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(FooterTemplate));
        public static readonly StyledProperty<DataTemplate> InputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(InputConnectorTemplate));
        public static readonly DirectProperty<Node,bool> HasFooterProperty =
            AvaloniaProperty.RegisterDirect<Node, bool>(nameof(HasFooter), o => o.HasFooter);
        public static readonly DirectProperty<Node,bool> HasHeaderProperty =
            AvaloniaProperty.RegisterDirect<Node, bool>(nameof(HasHeader), o => o.HasHeader);
        public static readonly StyledProperty<DataTemplate> OutputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(OutputConnectorTemplate));
        public static readonly StyledProperty<IEnumerable> InputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(Input));
        public static readonly StyledProperty<IEnumerable> OutputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(Output));

        /// <summary>Identifies the <see cref="HeaderContainerStyle"/> property.</summary>
        public static readonly StyledProperty<ControlTheme?> HeaderContainerStyleProperty =
            AvaloniaProperty.Register<Node, ControlTheme?>(nameof(HeaderContainerStyle));

        /// <summary>Identifies the <see cref="ContentContainerStyle"/> property.</summary>
        public static readonly StyledProperty<ControlTheme?> ContentContainerStyleProperty =
            AvaloniaProperty.Register<Node, ControlTheme?>(nameof(ContentContainerStyle));

        /// <summary>Identifies the <see cref="FooterContainerStyle"/> property.</summary>
        public static readonly StyledProperty<ControlTheme?> FooterContainerStyleProperty =
            AvaloniaProperty.Register<Node, ControlTheme?>(nameof(FooterContainerStyle));

        private bool _hasHeader;
        private bool _hasFooter;

        /// <summary>
        /// Gets or sets the brush used for the background of the <see cref="ContentControl.Content"/> of this <see cref="Node"/>.
        /// </summary>
        public IBrush ContentBrush
        {
            get => GetValue(ContentBrushProperty);
            set => SetValue(ContentBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding applied to the <see cref="ContentControl.Content"/> area of this <see cref="Node"/>.
        /// </summary>
        public Thickness ContentPadding
        {
            get => GetValue(ContentPaddingProperty);
            set => SetValue(ContentPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the background of the <see cref="HeaderedContentControl.Header"/> of this <see cref="Node"/>.
        /// </summary>
        public IBrush HeaderBrush
        {
            get => GetValue(HeaderBrushProperty);
            set => SetValue(HeaderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the background of the <see cref="Node.Footer"/> of this <see cref="Node"/>.
        /// </summary>
        public IBrush FooterBrush
        {
            get => GetValue(FooterBrushProperty);
            set => SetValue(FooterBrushProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the data for the footer of this control.
        /// </summary>
        public object? Footer
        {
            get => GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display the content of the control's footer.
        /// </summary>
        public DataTemplate FooterTemplate
        {
            get => GetValue(FooterTemplateProperty);
            set => SetValue(FooterTemplateProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the template used to display the content of the control's <see cref="Input"/> connectors.
        /// </summary>
        public DataTemplate InputConnectorTemplate
        {
            get => GetValue(InputConnectorTemplateProperty);
            set => SetValue(InputConnectorTemplateProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the template used to display the content of the control's <see cref="Output"/> connectors.
        /// </summary>
        public DataTemplate OutputConnectorTemplate
        {
            get => GetValue(OutputConnectorTemplateProperty);
            set => SetValue(OutputConnectorTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the data for the input <see cref="Connector"/>s of this control.
        /// </summary>
        public IEnumerable Input
        {
            get => GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        /// <summary>
        /// Gets or sets the data for the output <see cref="Connector"/>s of this control.
        /// </summary>
        public IEnumerable Output
        {
            get => GetValue(OutputProperty);
            set => SetValue(OutputProperty, value);
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="Footer"/> is <see langword="null" />.
        /// </summary>
        public bool HasFooter
        {
            get => _hasFooter;
            private set => this.SetAndRaise(HasFooterProperty, ref _hasFooter, value);
        }

        public bool HasHeader
        {
            get => _hasHeader;
            private set => this.SetAndRaise(HasHeaderProperty, ref _hasHeader, value);
        }

        /// <summary>
        /// Gets or sets the theme applied to the header container (the <c>PART_HeaderContainer</c>
        /// <see cref="Border"/> in the control template). Use a <see cref="ControlTheme"/> whose
        /// <see cref="ControlTheme.TargetType"/> is <see cref="Border"/>.
        /// </summary>
        /// <remarks>
        /// Consumer setters override the template's default <c>CornerRadius</c> and <c>Padding</c>.
        /// <c>Background</c> does not override the header fill: the border has
        /// <c>Background="{TemplateBinding HeaderBrush}"</c> in the control template, which is set at
        /// Template binding priority — higher than an unconditional theme setter (Animation
        /// &gt; LocalValue &gt; StyleTrigger &gt; Template &gt; Style &gt; Inherited &gt; Default), so a
        /// background setter here is inert. Use <see cref="HeaderBrush"/> to control the header fill
        /// color instead.
        /// </remarks>
        public ControlTheme? HeaderContainerStyle
        {
            get => GetValue(HeaderContainerStyleProperty);
            set => SetValue(HeaderContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the theme applied to the content container (the <c>PART_ContentContainer</c>
        /// <see cref="Border"/> in the control template) that wraps the complete input/content/output
        /// body. Use a <see cref="ControlTheme"/> whose <see cref="ControlTheme.TargetType"/> is
        /// <see cref="Border"/>. The
        /// <see cref="ContentBrush"/> and <see cref="ContentPadding"/> properties remain scoped to the
        /// center content cell and do not affect the connector columns.
        /// </summary>
        public ControlTheme? ContentContainerStyle
        {
            get => GetValue(ContentContainerStyleProperty);
            set => SetValue(ContentContainerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the theme applied to the footer container (the <c>PART_FooterContainer</c>
        /// <see cref="Border"/> in the control template). Use a <see cref="ControlTheme"/> whose
        /// <see cref="ControlTheme.TargetType"/> is <see cref="Border"/>. Consumer setters override
        /// the template's default
        /// <c>CornerRadius</c> and <c>Padding</c>. The footer fill remains controlled by
        /// <see cref="FooterBrush"/> because the template binding has higher priority than an
        /// unconditional theme setter.
        /// </summary>
        public ControlTheme? FooterContainerStyle
        {
            get => GetValue(FooterContainerStyleProperty);
            set => SetValue(FooterContainerStyleProperty, value);
        }

        #endregion

        static Node()
        {
            FooterProperty.Changed.AddClassHandler<Node, object?>((o,e)=> o.HasFooter = e.NewValue.Value != null);
            HeaderProperty.Changed.AddClassHandler<Node, object?>((o, e) => o.HasHeader = e.NewValue.Value != null);
        }

        public Node()
        {

        }

    }
}
