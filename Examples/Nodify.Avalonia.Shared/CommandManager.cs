using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Nodify.Avalonia.Shared
{
    /// <summary>
    /// A minimal command-requery mechanism that pumps <see cref="RequerySuggested"/> so that
    /// <see cref="RequeryCommand"/>/<see cref="RequeryCommand{T}"/> can re-evaluate
    /// <c>CanExecute</c> after common input events: handled input still requeries, subscribers
    /// are held weakly, and an input burst coalesces into one deferred broadcast per dispatcher pass.
    /// </summary>
    public static class CommandManager
    {
        private static readonly WeakEventHandlerStore RequeryHandlers = new();
        private static bool _requeryPending;

        public static event EventHandler? RequerySuggested
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

        public static void InvalidateRequerySuggested()
            => RequeryHandlers.Raise(null, EventArgs.Empty);

        private static void RequestRequerySuggested()
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

        static CommandManager()
        {
            // handledEventsToo: handled input (button clicks, consumed gesture keys) must still requery.
            InputElement.GotFocusEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
            InputElement.LostFocusEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
            InputElement.PointerPressedEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
            InputElement.PointerReleasedEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
            InputElement.KeyUpEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
            InputElement.KeyDownEvent.AddClassHandler<Interactive>((_, _) => RequestRequerySuggested(), handledEventsToo: true);
        }

        // Subscribers are held weakly, so abandoning a command source must not root it for
        // process lifetime.
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
}
