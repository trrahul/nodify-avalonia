namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Provides registration of the default input handler states for <see cref="ItemContainer"/>.
    /// </summary>
    public static partial class ContainerState
    {
        /// <summary>
        /// Determines whether toggled dragging mode is enabled, allowing the user to start and end
        /// the drag interaction in two steps with the same input gesture.
        /// </summary>
        public static bool EnableToggledDraggingMode { get; set; } = false;

        internal static void RegisterDefaultHandlers()
        {
            InputProcessor.Shared<ItemContainer>.RegisterHandlerFactory(e => new Default(e));
        }
    }
}
