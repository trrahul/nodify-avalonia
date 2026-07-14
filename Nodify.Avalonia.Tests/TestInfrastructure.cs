using System;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Nodify.Avalonia.Tests;

internal sealed class TestWindowFixture : IDisposable
{
    private TestWindowFixture(Window window)
    {
        Window = window;
    }

    public Window Window { get; }

    public static TestWindowFixture Show(
        Control content,
        double width = 400,
        double height = 300,
        Action<Window>? afterShow = null)
    {
        var window = new Window
        {
            Width = width,
            Height = height,
            Content = content
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            afterShow?.Invoke(window);
            Dispatcher.UIThread.RunJobs();
            return new TestWindowFixture(window);
        }
        catch
        {
            window.Close();
            throw;
        }
    }

    public void Dispose() => Window.Close();
}

internal sealed class TestCommand(Action<object?> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => execute(parameter);
}
