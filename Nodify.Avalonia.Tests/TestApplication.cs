using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Skia;

[assembly: AvaloniaTestApplication(typeof(Nodify.Avalonia.Tests.TestAppBuilder))]

namespace Nodify.Avalonia.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<TestApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false
            })
            .UseSkia();
}

public sealed class TestApplication : Application
{
    public override void Initialize()
    {
        Styles.Add(new StyleInclude(new Uri("avares://Nodify.Avalonia/"))
        {
            Source = new Uri("avares://Nodify.Avalonia/Themes/Controls.xaml")
        });
    }
}
