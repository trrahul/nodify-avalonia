using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorItemContainerTests
{
    [AvaloniaFact]
    public void ControlItem_IsWrappedInItemContainer_AndArrangesWithoutInvalidCast()
    {
        var button = new Button { Content = "Control item" };
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300,
            ItemsSource = new[] { button }
        };
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = editor
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var container = Assert.IsType<ItemContainer>(editor.ContainerFromIndex(0));
            Assert.Same(button, container.Content);

            var exception = Record.Exception(() =>
            {
                editor.Measure(new Size(400, 300));
                editor.Arrange(new Rect(0, 0, 400, 300));
                Dispatcher.UIThread.RunJobs();
            });

            Assert.Null(exception);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ItemContainerItem_RemainsItsOwnContainer()
    {
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300
        };
        var item = new ItemContainer(editor);
        editor.ItemsSource = new[] { item };

        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = editor
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(item, editor.ContainerFromIndex(0));
        }
        finally
        {
            window.Close();
        }
    }
}
