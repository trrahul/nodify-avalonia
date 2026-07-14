using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace Nodify.Avalonia.Workflow.Common
{
    public sealed class AnimationOptions
    {
        public double? From { get; set; }
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(200);
        public Easing Easing { get; set; } = new CubicEaseOut();
    }

    /// <summary>
    /// A minimal DispatcherTimer-based tweening helper for double-property, column-width, fade and
    /// translate animations. Re-invoking a tween on the same target/key cancels the previous one.
    /// </summary>
    public static class AnimationExtensions
    {
        private static readonly ConcurrentDictionary<(object, string), long> _running = new();

        private static Task Tween(object owner, string key, double from, double to, AnimationOptions? options, Action<double> apply)
        {
            options ??= new AnimationOptions();
            double start = options.From ?? from;

            var token = DateTime.UtcNow.Ticks;
            _running[(owner, key)] = token;

            var tcs = new TaskCompletionSource();

            if (options.Duration <= TimeSpan.Zero)
            {
                apply(to);
                _running.TryRemove((owner, key), out _);
                tcs.TrySetResult();
                return tcs.Task;
            }

            var startTime = DateTime.UtcNow;
            var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };

            timer.Tick += (_, _) =>
            {
                // A newer tween superseded this one.
                if (!_running.TryGetValue((owner, key), out var current) || current != token)
                {
                    timer.Stop();
                    tcs.TrySetResult();
                    return;
                }

                double t = (DateTime.UtcNow - startTime).TotalMilliseconds / options.Duration.TotalMilliseconds;
                if (t >= 1d)
                {
                    apply(to);
                    timer.Stop();
                    _running.TryRemove((owner, key), out _);
                    tcs.TrySetResult();
                    return;
                }

                double eased = options.Easing.Ease(t);
                apply(start + (to - start) * eased);
            };

            apply(start);
            timer.Start();
            return tcs.Task;
        }

        public static Task AnimateColumnWidth(this ColumnDefinition column, double to, AnimationOptions? options = null)
            => Tween(column, "Width", column.Width.IsAbsolute ? column.Width.Value : to, to, options,
                v => column.Width = new GridLength(v));

        public static Task FadeTo(this Visual el, double to, AnimationOptions? options = null)
            => Tween(el, "Opacity", el.Opacity, to, options, v => el.Opacity = v);

        public static Task FadeIn(this Visual el, AnimationOptions? options = null) => el.FadeTo(1d, options);
        public static Task FadeOut(this Visual el, AnimationOptions? options = null) => el.FadeTo(0d, options);

        private static TranslateTransform GetTranslate(Visual el)
        {
            if (el.RenderTransform is TranslateTransform t)
            {
                return t;
            }

            var nt = new TranslateTransform();
            el.RenderTransform = nt;
            return nt;
        }

        public static Task TranslateX(this Visual el, double to, AnimationOptions? options = null)
        {
            var t = GetTranslate(el);
            return Tween(el, "TranslateX", t.X, to, options, v => t.X = v);
        }

        public static Task TranslateY(this Visual el, double to, AnimationOptions? options = null)
        {
            var t = GetTranslate(el);
            return Tween(el, "TranslateY", t.Y, to, options, v => t.Y = v);
        }
    }
}
