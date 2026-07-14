using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Input;

namespace Nodify.Avalonia;

/// <summary>
/// Adapts a <see cref="RoutedCommand"/> to an <see cref="ICommand"/> with an explicit routed target.
/// </summary>
public sealed class TargetedRoutedCommand : AvaloniaObject, ICommand
{
    private WeakCanExecuteChangedSubscription? _canExecuteChangedSubscription;

    public static readonly StyledProperty<RoutedCommand?> CommandProperty =
        AvaloniaProperty.Register<TargetedRoutedCommand, RoutedCommand?>(nameof(Command));

    public static readonly StyledProperty<IInputElement?> TargetProperty =
        AvaloniaProperty.Register<TargetedRoutedCommand, IInputElement?>(nameof(Target));

    static TargetedRoutedCommand()
    {
        CommandProperty.Changed.AddClassHandler<TargetedRoutedCommand, RoutedCommand?>(OnCommandChanged);
        TargetProperty.Changed.AddClassHandler<TargetedRoutedCommand, IInputElement?>(OnTargetChanged);
    }

    public RoutedCommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public IInputElement? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public bool CanExecute(object? parameter) => Command?.CanExecute(parameter, Target) == true;

    public void Execute(object? parameter) => Command?.Execute(parameter, Target);

    public event EventHandler? CanExecuteChanged;

    private static void OnCommandChanged(
        TargetedRoutedCommand targetedCommand,
        AvaloniaPropertyChangedEventArgs<RoutedCommand?> args)
    {
        targetedCommand._canExecuteChangedSubscription?.Dispose();
        targetedCommand._canExecuteChangedSubscription = null;

        if (args.NewValue.Value is ICommand newCommand)
        {
            targetedCommand._canExecuteChangedSubscription =
                new WeakCanExecuteChangedSubscription(targetedCommand, newCommand);
        }

        targetedCommand.CanExecuteChanged?.Invoke(targetedCommand, EventArgs.Empty);
    }

    private static void OnTargetChanged(
        TargetedRoutedCommand targetedCommand,
        AvaloniaPropertyChangedEventArgs<IInputElement?> args)
        => targetedCommand.CanExecuteChanged?.Invoke(targetedCommand, EventArgs.Empty);

    private void OnInnerCanExecuteChanged(object? sender, EventArgs e)
        => CanExecuteChanged?.Invoke(this, e);

    private sealed class WeakCanExecuteChangedSubscription : IDisposable
    {
        private readonly WeakReference<TargetedRoutedCommand> _targetedCommand;
        private ICommand? _command;

        public WeakCanExecuteChangedSubscription(TargetedRoutedCommand targetedCommand, ICommand command)
        {
            _targetedCommand = new WeakReference<TargetedRoutedCommand>(targetedCommand);
            _command = command;
            command.CanExecuteChanged += OnCanExecuteChanged;
        }

        public void Dispose()
        {
            if (_command is { } command)
            {
                command.CanExecuteChanged -= OnCanExecuteChanged;
                _command = null;
            }
        }

        private void OnCanExecuteChanged(object? sender, EventArgs e)
        {
            if (_targetedCommand.TryGetTarget(out var targetedCommand))
            {
                targetedCommand.OnInnerCanExecuteChanged(sender, e);
            }
            else
            {
                Dispose();
            }
        }
    }
}
