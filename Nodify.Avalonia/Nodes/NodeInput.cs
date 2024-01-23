﻿using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Represents the default control for the <see cref="Node.InputConnectorTemplate"/>.
    /// </summary>
    public class NodeInput : Connector
    {
        #region Dependency Properties

        public static readonly StyledProperty<object?> HeaderProperty = HeaderedContentControl.HeaderProperty.AddOwner<NodeInput>();
        public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = HeaderedContentControl.HeaderTemplateProperty.AddOwner<NodeInput>();
        public static readonly StyledProperty<ControlTemplate> ConnectorTemplateProperty = AvaloniaProperty.Register<NodeInput,ControlTemplate>(nameof(ConnectorTemplate));

        /// <summary>
        /// Gets of sets the data used for the control's header.
        /// </summary>
        public object? Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display the content of the control's header.
        /// </summary>
        public IDataTemplate? HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the template used to display the connecting point of this <see cref="Connector"/>.
        /// </summary>
        public ControlTemplate ConnectorTemplate
        {
            get => GetValue(ConnectorTemplateProperty);
            set => SetValue(ConnectorTemplateProperty, value);
        }

        #endregion

        static NodeInput()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(NodeInput), new FrameworkPropertyMetadata(typeof(NodeInput)));
        }
    }
}
