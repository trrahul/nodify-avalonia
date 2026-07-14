using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Nodify.Avalonia.Workflow.Common
{
    /// <summary>
    /// With <c>ExtendClientAreaToDecorationsHint</c>, the drawn window decorations render a
    /// window <c>Title</c> overlay panel ABOVE client content (<c>PART_TitleTextPanel</c>,
    /// doubling the custom title bar text) and a fullscreen caption button
    /// (<c>PART_FullScreenButton</c>) that overflows the app's 140px caption reserve. This hides
    /// both — keeping minimize/maximize/close — while the window keeps a real <c>Title</c> for the
    /// taskbar and Alt-Tab.
    /// </summary>
    public static class WindowChromeInterop
    {
        public static void HideDrawnTitleText(Window window)
        {
            // The decorations layer attaches asynchronously after the window opens, so poll on
            // layout passes until both parts exist, then disarm.
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                // The decorations layers are hosted in TopLevelHost as SIBLINGS of the window's
                // content, so search from the outermost visual root.
                global::Avalonia.Visual root = window;
                while (root.GetVisualParent() is { } parent)
                {
                    root = parent;
                }

                var decorationVisuals = root.GetVisualDescendants().ToArray();
                var titleTextPanel = decorationVisuals
                    .OfType<Panel>()
                    .FirstOrDefault(panel => panel.Name == "PART_TitleTextPanel");
                var fullScreenButton = decorationVisuals
                    .OfType<Button>()
                    .FirstOrDefault(button => button.Name == "PART_FullScreenButton");

                if (titleTextPanel is not null && fullScreenButton is not null)
                {
                    // Local values override the template's bindings for good.
                    titleTextPanel.IsVisible = false;
                    fullScreenButton.IsVisible = false;
                    window.LayoutUpdated -= handler;
                }
            };

            window.LayoutUpdated += handler;
            handler(null, EventArgs.Empty);
        }
    }
}
