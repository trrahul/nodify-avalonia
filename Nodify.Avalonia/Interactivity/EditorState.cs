namespace Nodify.Avalonia.Interactivity
{
    /// <summary>
    /// Provides registration of the default input handler states for <see cref="NodifyEditor"/>.
    /// </summary>
    public static partial class EditorState
    {
        internal static void RegisterDefaultHandlers()
        {
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new Panning(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new PanningWithMouseWheel(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new Selecting(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new PushingItems(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new Cutting(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new Zooming(e));
            InputProcessor.Shared<NodifyEditor>.RegisterHandlerFactory(e => new KeyboardNavigation(e));
        }
    }
}
