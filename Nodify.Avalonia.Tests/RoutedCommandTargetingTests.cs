using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class RoutedCommandTargetingTests
{
    [AvaloniaFact]
    public void RoutedCommand_AutomaticallyRequeriesAfterInput()
    {
        bool allowed = false;
        var editor = new NodifyEditor();
        var command = new RoutedCommand(nameof(RoutedCommand_AutomaticallyRequeriesAfterInput));
        editor.CommandBindings.Add(new RoutedCommandBinding(
            command,
            (_, _) => { },
            (_, args) => args.CanExecute = allowed));
        var targeted = new TargetedRoutedCommand { Command = command, Target = editor };
        var button = new Button { Command = targeted };
        var window = new Window
        {
            Content = new StackPanel { Children = { editor, button } }
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.False(button.IsEffectivelyEnabled);

            allowed = true;
            Assert.True(editor.Focus());
            window.KeyPress(Key.F1, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.F1, RawInputModifiers.None, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(button.IsEffectivelyEnabled);
        }
        finally
        {
            targeted.Command = null;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RoutedCommand_RequeriesAfterHandledInput()
    {
        bool allowed = false;
        var editor = new NodifyEditor();
        var command = new RoutedCommand(nameof(RoutedCommand_RequeriesAfterHandledInput));
        editor.CommandBindings.Add(new RoutedCommandBinding(
            command,
            (_, _) => { },
            (_, args) => args.CanExecute = allowed));
        var targeted = new TargetedRoutedCommand { Command = command, Target = editor };
        var commandButton = new Button { Command = targeted };
        // Marks its key events handled before they bubble, like a Button click or an
        // editor gesture — the requery pump must still see this input.
        var keyConsumer = new KeyConsumingControl(() => allowed = true);
        var window = new Window
        {
            Content = new StackPanel { Children = { keyConsumer, commandButton, editor } }
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // Focus before the assertion so the unhandled GotFocus requery is drained here.
            Assert.True(keyConsumer.Focus());
            Dispatcher.UIThread.RunJobs();
            Assert.False(commandButton.IsEffectivelyEnabled);

            window.KeyPress(Key.F2, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.F2, RawInputModifiers.None, PhysicalKey.None, null);
            Dispatcher.UIThread.RunJobs();

            Assert.True(allowed);
            Assert.True(commandButton.IsEffectivelyEnabled);
        }
        finally
        {
            targeted.Command = null;
            window.Close();
        }
    }

    private sealed class KeyConsumingControl : Control
    {
        private readonly Action _onKeyDown;

        public KeyConsumingControl(Action onKeyDown)
        {
            _onKeyDown = onKeyDown;
            Focusable = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            _onKeyDown();
        }

        protected override void OnKeyUp(KeyEventArgs e) => e.Handled = true;
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_ProgrammaticEditorStateChangeUpdatesButton()
    {
        var editor = new NodifyEditor();
        editor.ViewportZoom = editor.MaxViewportZoom;
        var targeted = new TargetedRoutedCommand
        {
            Command = EditorCommands.ZoomIn,
            Target = editor
        };
        var button = new Button { Command = targeted };
        var window = new Window
        {
            Content = new StackPanel { Children = { editor, button } }
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.False(button.IsEffectivelyEnabled);

            editor.ViewportZoom = 1d;
            Dispatcher.UIThread.RunJobs();

            Assert.True(button.IsEffectivelyEnabled);
        }
        finally
        {
            targeted.Command = null;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RoutedCommand_DirectSubscriberIsNotRetained()
    {
        WeakReference weakSubscriber = CreateWeakDirectSubscriber();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakSubscriber.IsAlive);
        RoutedCommand.InvalidateRequerySuggested();
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_UsesExplicitTargetInsteadOfFocusedElement()
    {
        var explicitTarget = new NodifyEditor();
        var focusedEditor = new NodifyEditor();
        var command = new RoutedCommand(nameof(TargetedRoutedCommand_UsesExplicitTargetInsteadOfFocusedElement));
        var explicitInvocationCount = 0;
        var focusedInvocationCount = 0;
        explicitTarget.CommandBindings.Add(new RoutedCommandBinding(command, (_, _) => explicitInvocationCount++));
        focusedEditor.CommandBindings.Add(new RoutedCommandBinding(command, (_, _) => focusedInvocationCount++));
        TargetedRoutedCommand? targeted = null;
        var window = new Window
        {
            Content = new StackPanel { Children = { explicitTarget, focusedEditor } }
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.True(focusedEditor.Focus());
            targeted = new TargetedRoutedCommand
            {
                Command = command,
                Target = explicitTarget
            };

            Assert.True(targeted.CanExecute(null));
            targeted.Execute(null);

            Assert.Equal(1, explicitInvocationCount);
            Assert.Equal(0, focusedInvocationCount);
        }
        finally
        {
            if (targeted is not null)
            {
                targeted.Command = null;
            }

            window.Close();
        }
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_ForwardsCanExecuteChanged()
    {
        var firstCommand = new RoutedCommand(nameof(TargetedRoutedCommand_ForwardsCanExecuteChanged));
        var secondCommand = new RoutedCommand($"{nameof(TargetedRoutedCommand_ForwardsCanExecuteChanged)}Replacement");
        var targeted = new TargetedRoutedCommand { Command = firstCommand };
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        targeted.CanExecuteChanged += handler;

        try
        {
            RoutedCommand.InvalidateRequerySuggested();
            Assert.Equal(1, raised);

            targeted.Command = secondCommand;
            Assert.Equal(2, raised);

            RoutedCommand.InvalidateRequerySuggested();
            Assert.Equal(3, raised);
        }
        finally
        {
            targeted.CanExecuteChanged -= handler;
            targeted.Command = null;
        }
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_WithNullTargetCannotExecute()
    {
        var command = new RoutedCommand(nameof(TargetedRoutedCommand_WithNullTargetCannotExecute));
        var targeted = new TargetedRoutedCommand { Command = command };

        try
        {
            Assert.False(targeted.CanExecute(null));
        }
        finally
        {
            targeted.Command = null;
        }
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_TargetChangeRaisesCanExecuteChanged()
    {
        var enabledEditor = new NodifyEditor();
        var disabledEditor = new NodifyEditor();
        var command = new RoutedCommand(nameof(TargetedRoutedCommand_TargetChangeRaisesCanExecuteChanged));
        enabledEditor.CommandBindings.Add(new RoutedCommandBinding(command, (_, _) => { }));
        var targeted = new TargetedRoutedCommand { Command = command };
        var raised = 0;
        EventHandler handler = (_, _) => raised++;
        targeted.CanExecuteChanged += handler;

        try
        {
            targeted.Target = enabledEditor;
            Assert.Equal(1, raised);
            Assert.True(targeted.CanExecute(null));

            targeted.Target = disabledEditor;
            Assert.Equal(2, raised);
            Assert.False(targeted.CanExecute(null));
        }
        finally
        {
            targeted.CanExecuteChanged -= handler;
            targeted.Command = null;
        }
    }

    [AvaloniaFact]
    public void TargetedRoutedCommand_RequerySubscriptionDoesNotRetainWrapper()
    {
        var weakTargetedCommand = CreateWeakTargetedCommand();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weakTargetedCommand.TryGetTarget(out _));
        RoutedCommand.InvalidateRequerySuggested();
    }

    [AvaloniaFact]
    public void ResolveTopLevel_PrefersActiveSecondaryWindow()
    {
        var mainWindow = new Window();
        var secondaryWindow = new Window();

        try
        {
            mainWindow.Show();
            secondaryWindow.Show();
            secondaryWindow.Activate();
            Dispatcher.UIThread.RunJobs();
            var lifetime = FakeDesktopLifetime.Create(mainWindow, mainWindow, secondaryWindow);

            Assert.True(secondaryWindow.IsActive);
            Assert.Same(secondaryWindow, RoutedCommand.ResolveTopLevel(lifetime));
        }
        finally
        {
            secondaryWindow.Close();
            mainWindow.Close();
        }
    }

    [AvaloniaFact]
    public void ICommandExecute_UsesResolvedFocusedElement()
    {
        var mainEditor = new NodifyEditor { Width = 400, Height = 300 };
        var secondaryEditor = new NodifyEditor { Width = 400, Height = 300 };
        var mainWindow = new Window { Width = 400, Height = 300, Content = mainEditor };
        var secondaryWindow = new Window { Width = 400, Height = 300, Content = secondaryEditor };
        var originalLifetime = Application.Current?.ApplicationLifetime;

        try
        {
            mainWindow.Show();
            secondaryWindow.Show();
            secondaryWindow.Activate();
            Dispatcher.UIThread.RunJobs();
            Assert.True(secondaryEditor.Focus());
            Assert.NotNull(Application.Current);
            SetApplicationLifetime(
                Application.Current,
                FakeDesktopLifetime.Create(mainWindow, mainWindow, secondaryWindow));
            ICommand command = EditorCommands.ZoomIn;

            command.Execute(null);

            Assert.Equal(1d, mainEditor.ViewportZoom);
            Assert.True(secondaryEditor.ViewportZoom > 1d);
        }
        finally
        {
            if (Application.Current is { } application)
            {
                SetApplicationLifetime(application, originalLifetime);
            }

            secondaryWindow.Close();
            mainWindow.Close();
        }
    }

    private static void SetApplicationLifetime(
        Application application,
        IApplicationLifetime? lifetime)
    {
        var lifetimeField = typeof(Application).GetField(
            "_applicationLifetime",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(lifetimeField);
        lifetimeField.SetValue(application, lifetime);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<TargetedRoutedCommand> CreateWeakTargetedCommand()
    {
        var targeted = new TargetedRoutedCommand
        {
            Command = new RoutedCommand(nameof(CreateWeakTargetedCommand)),
            Target = new NodifyEditor()
        };
        return new WeakReference<TargetedRoutedCommand>(targeted);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateWeakDirectSubscriber()
    {
        var subscriber = new RequerySubscriber();
        ICommand command = new RoutedCommand(nameof(CreateWeakDirectSubscriber));
        command.CanExecuteChanged += subscriber.OnCanExecuteChanged;
        return new WeakReference(subscriber);
    }

    private sealed class RequerySubscriber
    {
        public void OnCanExecuteChanged(object? sender, EventArgs e) { }
    }

    private class FakeDesktopLifetime : DispatchProxy
    {
        private Window? _mainWindow;
        private IReadOnlyList<Window> _windows = [];

        public static IClassicDesktopStyleApplicationLifetime Create(
            Window mainWindow,
            params Window[] windows)
        {
            var lifetime = Create<IClassicDesktopStyleApplicationLifetime, FakeDesktopLifetime>();
            var proxy = (FakeDesktopLifetime)(object)lifetime;
            proxy._mainWindow = mainWindow;
            proxy._windows = windows;
            return lifetime;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Args" => Array.Empty<string>(),
                "get_ShutdownMode" => ShutdownMode.OnExplicitShutdown,
                "get_MainWindow" => _mainWindow,
                "set_MainWindow" => SetMainWindow(args),
                "get_Windows" => _windows,
                "TryShutdown" => true,
                _ => null
            };
        }

        private object? SetMainWindow(object?[]? args)
        {
            _mainWindow = args?[0] as Window;
            return null;
        }
    }
}
