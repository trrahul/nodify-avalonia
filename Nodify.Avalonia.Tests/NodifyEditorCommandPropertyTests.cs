using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodifyEditorCommandPropertyTests
{
    [AvaloniaFact]
    public void CommandHooks_CanAllBeProvidedByControlThemeAtStylePriority()
    {
        var commands = new TestCommand[10];
        for (int i = 0; i < commands.Length; i++)
        {
            commands[i] = new TestCommand(_ => { });
        }

        var theme = new ControlTheme(typeof(NodifyEditor))
        {
            Setters =
            {
                new Setter(NodifyEditor.ConnectionStartedCommandProperty, commands[0]),
                new Setter(NodifyEditor.ConnectionCompletedCommandProperty, commands[1]),
                new Setter(NodifyEditor.DisconnectConnectorCommandProperty, commands[2]),
                new Setter(NodifyEditor.RemoveConnectionCommandProperty, commands[3]),
                new Setter(NodifyEditor.ItemsDragStartedCommandProperty, commands[4]),
                new Setter(NodifyEditor.ItemsDragCompletedCommandProperty, commands[5]),
                new Setter(NodifyEditor.ItemsSelectStartedCommandProperty, commands[6]),
                new Setter(NodifyEditor.ItemsSelectCompletedCommandProperty, commands[7]),
                new Setter(NodifyEditor.CuttingStartedCommandProperty, commands[8]),
                new Setter(NodifyEditor.CuttingCompletedCommandProperty, commands[9])
            }
        };
        var editor = new NodifyEditor { Theme = theme };
        var properties = new AvaloniaProperty[]
        {
            NodifyEditor.ConnectionStartedCommandProperty,
            NodifyEditor.ConnectionCompletedCommandProperty,
            NodifyEditor.DisconnectConnectorCommandProperty,
            NodifyEditor.RemoveConnectionCommandProperty,
            NodifyEditor.ItemsDragStartedCommandProperty,
            NodifyEditor.ItemsDragCompletedCommandProperty,
            NodifyEditor.ItemsSelectStartedCommandProperty,
            NodifyEditor.ItemsSelectCompletedCommandProperty,
            NodifyEditor.CuttingStartedCommandProperty,
            NodifyEditor.CuttingCompletedCommandProperty
        };
        var readers = new Func<ICommand?>[]
        {
            () => editor.ConnectionStartedCommand,
            () => editor.ConnectionCompletedCommand,
            () => editor.DisconnectConnectorCommand,
            () => editor.RemoveConnectionCommand,
            () => editor.ItemsDragStartedCommand,
            () => editor.ItemsDragCompletedCommand,
            () => editor.ItemsSelectStartedCommand,
            () => editor.ItemsSelectCompletedCommand,
            () => editor.CuttingStartedCommand,
            () => editor.CuttingCompletedCommand
        };

        editor.Measure(new Size(400, 300));
        editor.Arrange(new Rect(0, 0, 400, 300));

        for (int i = 0; i < properties.Length; i++)
        {
            Assert.Same(commands[i], readers[i]());
            Assert.Equal(BindingPriority.Style, editor.GetDiagnostic(properties[i]).Priority);
        }
    }

    [AvaloniaFact]
    public void ThemedSelectionAndCuttingCommands_KeepLifecycleParameters()
    {
        object dataContext = new();
        var invocations = new List<(string Name, object? Parameter)>();
        var theme = new ControlTheme(typeof(NodifyEditor))
        {
            Setters =
            {
                new Setter(
                    NodifyEditor.ItemsSelectStartedCommandProperty,
                    new TestCommand(parameter => invocations.Add(("selection-started", parameter)))),
                new Setter(
                    NodifyEditor.ItemsSelectCompletedCommandProperty,
                    new TestCommand(parameter => invocations.Add(("selection-completed", parameter)))),
                new Setter(
                    NodifyEditor.CuttingStartedCommandProperty,
                    new TestCommand(parameter => invocations.Add(("cutting-started", parameter)))),
                new Setter(
                    NodifyEditor.CuttingCompletedCommandProperty,
                    new TestCommand(parameter => invocations.Add(("cutting-completed", parameter))))
            }
        };
        var editor = new NodifyEditor
        {
            Width = 400,
            Height = 300,
            Theme = theme,
            DataContext = dataContext
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
            editor.ApplyTemplate();

            editor.BeginSelecting(new Point(10, 10));
            editor.EndSelecting();
            editor.BeginCutting(new Point(10, 10));
            editor.CancelCutting();

            Assert.Equal(
                new (string Name, object? Parameter)[]
                {
                    ("selection-started", dataContext),
                    ("selection-completed", dataContext),
                    ("cutting-started", dataContext),
                    ("cutting-completed", dataContext)
                },
                invocations);
        }
        finally
        {
            window.Close();
        }
    }

}
