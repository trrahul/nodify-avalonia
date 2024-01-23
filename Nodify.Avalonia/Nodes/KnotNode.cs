using Avalonia.Controls;
using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Nodes
{
    /// <summary>
    /// Represents a control that owns a <see cref="Connector"/>.
    /// </summary>
    public class KnotNode : ContentControl
    {
        static KnotNode()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(KnotNode), new FrameworkPropertyMetadata(typeof(KnotNode)));
        }
    }
}
