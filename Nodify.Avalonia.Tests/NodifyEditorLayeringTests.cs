using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Xunit;
using ConnectionsMultiSelector = Nodify.Avalonia.Connections.ConnectionsMultiSelector;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorLayeringTests
{
    [AvaloniaFact]
    public void ConnectionsHost_IsBelowItemsByDefault_AndMovesOnTopWhenRequested()
    {
        var editor = new NodifyEditor
        {
            Width = 500,
            Height = 500
        };

        var window = new Window
        {
            Width = 500,
            Height = 500,
            Content = editor
        };

        try
        {
            window.Show();
            editor.ApplyTemplate();

            var host = editor.GetVisualDescendants()
                .OfType<ConnectionsMultiSelector>()
                .Single();

            Assert.False(editor.DisplayConnectionsOnTop);
            Assert.Equal(-1, host.ZIndex);

            editor.DisplayConnectionsOnTop = true;

            Assert.Equal(0, host.ZIndex);
        }
        finally
        {
            window.Close();
        }
    }
}
