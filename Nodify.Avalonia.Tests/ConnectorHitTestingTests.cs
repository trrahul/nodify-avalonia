using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Nodify.Avalonia.Connections;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class ConnectorHitTestingTests
{
    [AvaloniaFact]
    public void HitTest_UsesConnectorLocalCoordinatesBelowTranslatedAndScaledAncestors()
    {
        var connector = new Connector
        {
            Width = 20,
            Height = 20
        };
        Canvas.SetLeft(connector, 30);
        Canvas.SetTop(connector, 40);

        var scaledCanvas = new Canvas
        {
            Width = 200,
            Height = 200,
            RenderTransform = new ScaleTransform(2, 2),
            Children = { connector }
        };

        var translatedCanvas = new Canvas
        {
            Width = 400,
            Height = 300,
            RenderTransform = new TranslateTransform(100, 50),
            Children = { scaledCanvas }
        };

        var window = new Window
        {
            Width = 500,
            Height = 400,
            Content = translatedCanvas
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Point? globalCenter = connector.TranslatePoint(new Point(10, 10), window);
            Assert.True(globalCenter.HasValue, "Could not translate the connector center to the test window.");

            Assert.True(connector.HitTest(globalCenter.Value));
            Assert.False(connector.HitTest(globalCenter.Value + new Vector(100, 100)));
        }
        finally
        {
            window.Close();
        }
    }
}
