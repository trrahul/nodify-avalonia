using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Nodify.Avalonia.Helpers;
using Nodify.Avalonia.Helpers.Gestures;

namespace Nodify.Avalonia;

public class RoutedCommand : ICommand
{
    public string Name { get; }
    public InputGestureCollection? Gesture { get; }

    public RoutedCommand(string name, InputGestureCollection? keyGesture = null)
    {
        Name = name;
        Gesture = keyGesture;
    }

    static RoutedCommand()
    {
        CanExecuteEvent.AddClassHandler<Interactive>(CanExecuteEventHandler);
        ExecutedEvent.AddClassHandler<Interactive>(ExecutedEventHandler);
        // handledEventsToo: button clicks and consumed gesture keys must still requery.
        InputElement.GotFocusEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        InputElement.KeyDownEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        InputElement.KeyUpEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
    }

    private static void CanExecuteEventHandler(Interactive control, CanExecuteRoutedEventArgs args)
    {
        if (control is IRoutedCommandBindable bindable)
        {
            var binding = bindable.CommandBindings.Where(c => c != null)
           .FirstOrDefault(c => c.Command == args.Command && c.DoCanExecute(control, args));
            args.CanExecute = binding != null;
        }
    }

    private static void ExecutedEventHandler(Interactive control, ExecutedRoutedEventArgs args)
    {
        if (control is IRoutedCommandBindable bindable)
        {
            // ReSharper disable once UnusedVariable
            var binding = bindable.CommandBindings.Where(c => c != null)
                .FirstOrDefault(c => c.Command == args.Command && c.DoExecuted(control, args));

        }
    }

    public static RoutedEvent<CanExecuteRoutedEventArgs> CanExecuteEvent { get; } = RoutedEvent.Register<CanExecuteRoutedEventArgs>(nameof(CanExecuteEvent), RoutingStrategies.Bubble, typeof(RoutedCommand));

    public bool CanExecute(object? parameter, IInputElement? target)
    {
        if (target == null) return false;

        var args = new CanExecuteRoutedEventArgs(this, parameter);
        target.RaiseEvent(args);

        return args.CanExecute;
    }

    bool ICommand.CanExecute(object? parameter)
    {
        return CanExecute(parameter,GetFocusedElement());
    }

    public static RoutedEvent<ExecutedRoutedEventArgs> ExecutedEvent { get; } = RoutedEvent.Register<ExecutedRoutedEventArgs>(nameof(ExecutedEvent), RoutingStrategies.Bubble, typeof(RoutedCommand));

    public void Execute(object? parameter, IInputElement? target)
    {
        if (target == null) return;

        var args = new ExecutedRoutedEventArgs(this, parameter);
        target.RaiseEvent(args);
    }

    internal static TopLevel? ResolveTopLevel(IApplicationLifetime? lifetime)
    {
        return lifetime switch
        {
            IClassicDesktopStyleApplicationLifetime desktop =>
                desktop.Windows.LastOrDefault(window => window.IsActive)
                ?? desktop.MainWindow,
            ISingleViewApplicationLifetime singleView when singleView.MainView is { } mainView =>
                TopLevel.GetTopLevel(mainView),
            _ => null
        };
    }

    private static IInputElement? GetFocusedElement() =>
        ResolveTopLevel(Application.Current?.ApplicationLifetime)
            ?.FocusManager
            ?.GetFocusedElement();

    void ICommand.Execute(object? parameter)
    {
        Execute(parameter, GetFocusedElement());
    }

    private static readonly WeakEventHandlerStore RequeryHandlers = new();
    private static bool _requeryPending;

    /// <summary>
    /// Raises <see cref="ICommand.CanExecuteChanged"/> on all subscribers. Call this whenever
    /// application state changes that might affect whether a command can execute.
    /// </summary>
    public static void InvalidateRequerySuggested()
    {
        RequeryHandlers.Raise(null, EventArgs.Empty);
    }

    internal static void RequestRequerySuggested()
    {
        if (_requeryPending)
        {
            return;
        }

        _requeryPending = true;
        Dispatcher.UIThread.Post(() =>
        {
            _requeryPending = false;
            InvalidateRequerySuggested();
        });
    }

    event EventHandler? ICommand.CanExecuteChanged
    {
        add
        {
            if (value is not null)
            {
                RequeryHandlers.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                RequeryHandlers.Remove(value);
            }
        }
    }

    private sealed class WeakEventHandlerStore
    {
        private readonly object _gate = new();
        private readonly List<WeakHandler> _handlers = [];

        public void Add(EventHandler handler)
        {
            lock (_gate)
            {
                _handlers.Add(new WeakHandler(handler));
            }
        }

        public void Remove(EventHandler handler)
        {
            lock (_gate)
            {
                for (int i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i].Matches(handler))
                    {
                        _handlers.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void Raise(object? sender, EventArgs args)
        {
            List<EventHandler> liveHandlers = [];
            lock (_gate)
            {
                for (int i = _handlers.Count - 1; i >= 0; i--)
                {
                    if (_handlers[i].TryCreateDelegate(out EventHandler handler))
                    {
                        liveHandlers.Add(handler);
                    }
                    else
                    {
                        _handlers.RemoveAt(i);
                    }
                }
            }

            for (int i = liveHandlers.Count - 1; i >= 0; i--)
            {
                liveHandlers[i](sender, args);
            }
        }

        private sealed class WeakHandler
        {
            private readonly MethodInfo _method;
            private readonly WeakReference<object>? _target;
            private readonly EventHandler? _staticHandler;

            public WeakHandler(EventHandler handler)
            {
                _method = handler.Method;
                if (handler.Target is { } target)
                {
                    _target = new WeakReference<object>(target);
                }
                else
                {
                    _staticHandler = handler;
                }
            }

            public bool Matches(EventHandler handler)
            {
                if (_method != handler.Method)
                {
                    return false;
                }

                if (_staticHandler is not null)
                {
                    return handler.Target is null;
                }

                return handler.Target is { } target
                    && _target?.TryGetTarget(out object? current) == true
                    && ReferenceEquals(current, target);
            }

            public bool TryCreateDelegate(out EventHandler handler)
            {
                if (_staticHandler is not null)
                {
                    handler = _staticHandler;
                    return true;
                }

                if (_target?.TryGetTarget(out object? target) == true)
                {
                    handler = (EventHandler)_method.CreateDelegate(typeof(EventHandler), target);
                    return true;
                }

                handler = null!;
                return false;
            }
        }
    }
}

public interface IRoutedCommandBindable
{
    IList<RoutedCommandBinding> CommandBindings { get; }
}

public class RoutedCommandBinding
{
    public RoutedCommandBinding(RoutedCommand command,
        EventHandler<ExecutedRoutedEventArgs>? executed = null,
        EventHandler<CanExecuteRoutedEventArgs>? canExecute = null)
    {
        Command = command;
        if (executed != null) Executed += executed;
        if (canExecute != null) CanExecute += canExecute;
    }

    public RoutedCommand Command { get; }

    public event EventHandler<CanExecuteRoutedEventArgs>? CanExecute;

    public event EventHandler<ExecutedRoutedEventArgs>? Executed;

    internal bool DoCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Handled) return true;

        var canExecute = CanExecute;
        if (canExecute == null)
        {
            if (Executed != null)
            {
                e.Handled = true;
                e.CanExecute = true;
            }
        }
        else
        {
            canExecute(sender, e);

            if (e.CanExecute)
            {
                e.Handled = true;
            }
        }

        return e.CanExecute;
    }

    internal bool DoExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (!e.Handled)
        {
            var executed = Executed;

            if (executed != null)
            {
                if (DoCanExecute(sender, new CanExecuteRoutedEventArgs(e.Command, e.Parameter)))
                {
                    executed(sender, e);
                    e.Handled = true;
                    return true;
                }
            }
        }

        return false;
    }
}

public sealed class CanExecuteRoutedEventArgs : RoutedEventArgs
{
    public ICommand Command { get; }

    public object? Parameter { get; }

    public bool CanExecute { get; set; }

    internal CanExecuteRoutedEventArgs(ICommand command, object? parameter)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Parameter = parameter;
        RoutedEvent = RoutedCommand.CanExecuteEvent;
    }
}

public sealed class ExecutedRoutedEventArgs : RoutedEventArgs
{
    public ICommand Command { get; }

    public object? Parameter { get; }

    internal ExecutedRoutedEventArgs(ICommand command, object? parameter)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Parameter = parameter;
        RoutedEvent = RoutedCommand.ExecutedEvent;
    }
}
