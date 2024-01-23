using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Represents a control that has a list of <see cref="Input"/> <see cref="Connector"/>s and a list of <see cref="Output"/> <see cref="Connector"/>s.
    /// </summary>
    public class Node : HeaderedContentControl
    {
        #region Dependency Properties

        public static readonly StyledProperty<IBrush> ContentBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(ContentBrush));
        public static readonly StyledProperty<IBrush> HeaderBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(HeaderBrush));
        public static readonly StyledProperty<IBrush> FooterBrushProperty = AvaloniaProperty.Register<Node,IBrush>(nameof(FooterBrush));
        public static readonly StyledProperty<object?> FooterProperty = AvaloniaProperty.Register<Node, object?>(nameof(Footer));
        public static readonly StyledProperty<DataTemplate> FooterTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(FooterTemplate));
        public static readonly StyledProperty<DataTemplate> InputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(InputConnectorTemplate));
        public static readonly StyledProperty<DataTemplate> FlowInputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(FlowInputConnectorTemplate));
        public static readonly DirectProperty<Node,bool> HasFooterProperty =
            AvaloniaProperty.RegisterDirect<Node, bool>(nameof(HasFooter), o => o.HasFooter);
        public static readonly DirectProperty<Node,bool> HasHeaderProperty =
            AvaloniaProperty.RegisterDirect<Node, bool>(nameof(HasHeader), o => o.HasHeader);
        public static readonly StyledProperty<DataTemplate> OutputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(OutputConnectorTemplate));
        public static readonly StyledProperty<DataTemplate> FlowOutputConnectorTemplateProperty = AvaloniaProperty.Register<Node,DataTemplate>(nameof(FlowOutputConnectorTemplate));
        public static readonly StyledProperty<IEnumerable> InputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(Input));
        public static readonly StyledProperty<IEnumerable> FlowInputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(FlowInput));
        public static readonly StyledProperty<IEnumerable> OutputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(Output));
        public static readonly StyledProperty<IEnumerable> FlowOutputProperty = AvaloniaProperty.Register<Node,IEnumerable>(nameof(FlowOutput));
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

        public DataTemplate FlowInputConnectorTemplate
        {
            get => GetValue(FlowInputConnectorTemplateProperty);
            set => SetValue(FlowInputConnectorTemplateProperty, value);
        }

        public DataTemplate FlowOutputConnectorTemplate
        {
            get => GetValue(FlowOutputConnectorTemplateProperty);
            set => SetValue(FlowOutputConnectorTemplateProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the data for the input <see cref="Connector"/>s of this control.
        /// </summary>
        public IEnumerable Input
        {
            get => GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        public IEnumerable FlowInput
        {
            get => GetValue(FlowInputProperty);
            set => SetValue(FlowInputProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the data for the output <see cref="Connector"/>s of this control.
        /// </summary>
        public IEnumerable Output
        {
            get => GetValue(OutputProperty);
            set => SetValue(OutputProperty, value);
        }

        public IEnumerable FlowOutput
        {
            get => GetValue(FlowOutputProperty);
            set => SetValue(FlowOutputProperty, value);
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

        #endregion

        static Node()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(Node), new FrameworkPropertyMetadata(typeof(Node)));
            FooterProperty.Changed.AddClassHandler<Node, object?>((o,e)=> o.HasFooter = e.NewValue.Value != null);
            HeaderProperty.Changed.AddClassHandler<Node, object?>((o, e) => o.HasHeader = e.NewValue.Value != null);
        }

        public Node()
        {
            
        }
    }
}
