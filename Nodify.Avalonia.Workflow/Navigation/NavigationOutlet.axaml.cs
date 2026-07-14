using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Nodify.Avalonia.Workflow.Common;

namespace Nodify.Avalonia.Workflow.Navigation
{
    public partial class NavigationOutlet : UserControl
    {
        private ContentControl _contentHost = null!;
        private long _transitionVersion;

        public static readonly StyledProperty<NavigationService?> NavigationServiceProperty =
            AvaloniaProperty.Register<NavigationOutlet, NavigationService?>(nameof(NavigationService));

        public NavigationService? NavigationService
        {
            get => GetValue(NavigationServiceProperty);
            set => SetValue(NavigationServiceProperty, value);
        }

        public NavigationOutlet()
        {
            InitializeComponent();
            _contentHost = this.FindControl<ContentControl>("ContentHost")!;
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == NavigationServiceProperty)
            {
                CancelPendingTransition();

                if (change.OldValue is NavigationService oldService)
                {
                    oldService.Navigated -= OnNavigated;
                }

                if (change.NewValue is NavigationService newService)
                {
                    newService.Navigated += OnNavigated;
                    if (newService.CurrentView is not null)
                    {
                        _contentHost.Content = newService.CurrentView;
                    }
                }
            }
        }

        private async void OnNavigated(object? sender, NavigationEventArgs args)
        {
            long version = NextTransitionVersion();
            await OnNavigatedAsync(args, version);
        }

        private async Task OnNavigatedAsync(NavigationEventArgs args, long version)
        {
            const double offset = 100d;
            var easeIn = new CubicEaseIn();
            var easeOut = new CubicEaseOut();

            if (args.IsSameLayer)
            {
                if (args.Direction is NavigationDirection.Forward)
                {
                    await _contentHost.FadeOut();
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }

                    _contentHost.Content = args.NewEntry.ViewModel;
                    await Task.WhenAll(
                        _contentHost.FadeIn(),
                        _contentHost.TranslateY(0, new AnimationOptions { From = offset, Easing = easeIn }));
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }
                }
                else
                {
                    await Task.WhenAll(
                        _contentHost.FadeOut(),
                        _contentHost.TranslateY(offset, new AnimationOptions { From = 0, Easing = easeIn }));
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }

                    _contentHost.Content = args.NewEntry.ViewModel;
                    await Task.WhenAll(
                        _contentHost.TranslateY(0, new AnimationOptions { Duration = System.TimeSpan.Zero }),
                        _contentHost.FadeIn());
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }
                }
            }
            else
            {
                if (args.Direction is NavigationDirection.Forward)
                {
                    await _contentHost.TranslateX(-offset, new AnimationOptions { From = 0, Easing = easeIn });
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }

                    _contentHost.Content = args.NewEntry.ViewModel;
                    await _contentHost.TranslateX(0, new AnimationOptions { From = offset * 2, Easing = easeOut });
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }
                }
                else
                {
                    await _contentHost.TranslateX(offset, new AnimationOptions { From = 0, Easing = easeIn });
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }

                    _contentHost.Content = args.NewEntry.ViewModel;
                    await _contentHost.TranslateX(0, new AnimationOptions { From = -offset, Easing = easeOut });
                    if (!IsCurrentTransition(version))
                    {
                        return;
                    }
                }
            }
        }

        private long NextTransitionVersion()
            => System.Threading.Interlocked.Increment(ref _transitionVersion);

        private void CancelPendingTransition()
            => NextTransitionVersion();

        private bool IsCurrentTransition(long version)
            => System.Threading.Volatile.Read(ref _transitionVersion) == version;
    }
}
