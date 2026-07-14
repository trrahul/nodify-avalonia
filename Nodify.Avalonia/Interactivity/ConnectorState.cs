using Nodify.Avalonia.Connections;

namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Provides registration of the default input handler states for <see cref="Connector"/>.
    /// </summary>
    public static partial class ConnectorState
    {
        internal static void RegisterDefaultHandlers()
        {
            InputProcessor.Shared<Connector>.RegisterHandlerFactory(elem => new Disconnect(elem));
            InputProcessor.Shared<Connector>.RegisterHandlerFactory(elem => new Connecting(elem));
            InputProcessor.Shared<Connector>.RegisterHandlerFactory(elem => new Default(elem));
        }
    }
}
