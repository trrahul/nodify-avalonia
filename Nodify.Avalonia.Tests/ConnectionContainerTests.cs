using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Interactivity;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class ConnectionContainerTests
{
    [AvaloniaFact]
    public void KeyboardBounds_ThrowsForCustomConnectionWithoutFocusTarget()
    {
        var custom = new ContentControl();
        var selector = new ConnectionsMultiSelector();
        var container = new ConnectionContainer(selector) { Content = custom };
        var window = new Window
        {
            Width = 300,
            Height = 200,
            Content = container
        };

        try
        {
            window.Show();
            container.ApplyTemplate();
            container.Measure(new Size(300, 200));
            container.Arrange(new Rect(0, 0, 300, 200));
            Dispatcher.UIThread.RunJobs();

            var exception = Assert.Throws<NotSupportedException>(
                () => _ = ((IKeyboardFocusTarget<ConnectionContainer>)container).Bounds);

            Assert.Contains("IKeyboardFocusTarget<Control>", exception.Message);
            Assert.Contains("keyboard navigation may be disabled", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void CuttingAndPendingPreview_ReadTheSameAttachedValue()
    {
        var control = new Control();

        CuttingLine.SetIsOverElement(control, true);
        Assert.True(PendingConnection.GetIsOverElement(control));

        PendingConnection.SetIsOverElement(control, false);
        Assert.False(CuttingLine.GetIsOverElement(control));
    }

    [AvaloniaFact]
    public void DirectFocus_ElevatesConnectionOnlyAmongItsSiblings()
    {
        var selector = new ConnectionsMultiSelector();
        var connection = CreateSelectableConnection();
        var container = new ConnectionContainer(selector)
        {
            Width = 200,
            Height = 100,
            ZIndex = 42,
            Content = connection
        };
        var other = new Button();
        var window = new Window
        {
            Width = 300,
            Height = 200,
            Content = new Grid
            {
                Children = { container, other }
            }
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.True(container.Focus());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(int.MaxValue, container.ZIndex);

            Assert.True(other.Focus());
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(42, container.ZIndex);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MousePress_FocusesSelectableConnection()
    {
        var selector = new ConnectionsMultiSelector();
        var connection = CreateSelectableConnection();
        connection.Width = 200;
        connection.Height = 100;
        connection.Stroke = Brushes.Black;
        connection.StrokeThickness = 6;
        var container = new ConnectionContainer(selector)
        {
            Width = 200,
            Height = 100,
            Content = connection
        };
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = container
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.MouseDown(
                new Point(50, 10),
                MouseButton.Left,
                RawInputModifiers.LeftMouseButton);

            Assert.True(container.IsFocused);
        }
        finally
        {
            window.MouseUp(new Point(50, 10), MouseButton.Left, RawInputModifiers.None);
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PointerHover_UsesTransparentOutlineCorridorLikeWpf()
    {
        var selector = new ConnectionsMultiSelector();
        var connection = CreateSelectableConnection();
        connection.Width = 200;
        connection.Height = 100;
        connection.Stroke = Brushes.Black;
        connection.StrokeThickness = 3;
        connection.OutlineBrush = Brushes.Transparent;
        connection.OutlineThickness = 5;
        var container = new ConnectionContainer(selector)
        {
            Width = 200,
            Height = 100,
            Content = connection
        };
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = container
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            // Five pixels from the centerline is outside the visible 3 px stroke,
            // but inside the 13 px transparent outline pen.
            window.MouseMove(new Point(50, 15));
            Assert.True(connection.IsPointerOver);

            // Eight pixels from the centerline is outside the outline corridor.
            window.MouseMove(new Point(50, 18));
            Assert.False(connection.IsPointerOver);

            // Without an outline, hit-testing falls back to the visible stroke area.
            connection.OutlineBrush = null;
            window.MouseMove(new Point(50, 15));
            Assert.False(connection.IsPointerOver);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PointerHover_UsesTransparentOutlineCorridorWithCanvasLayout()
    {
        var selector = new ConnectionsMultiSelector();
        var connection = CreateSelectableConnection();
        connection.Stroke = Brushes.Black;
        connection.StrokeThickness = 3;
        connection.OutlineBrush = Brushes.Transparent;
        connection.OutlineThickness = 5;
        var container = new ConnectionContainer(selector) { Content = connection };
        var canvas = new Canvas { Children = { container } };
        var window = new Window
        {
            Width = 200,
            Height = 100,
            Content = canvas
        };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.MouseMove(new Point(50, 15));

            Assert.True(connection.IsPointerOver);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void PointerHover_StepOutlineDoesNotFillImplicitlyClosedRoute()
    {
        var connection = new StepConnection
        {
            Width = 150,
            Height = 100,
            Source = new Point(10, 10),
            Target = new Point(110, 60),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            SourcePosition = ConnectorPosition.Right,
            TargetPosition = ConnectorPosition.Left,
            Spacing = 20,
            ArrowEnds = ArrowHeadEnds.None,
            Fill = null,
            Stroke = Brushes.Black,
            StrokeThickness = 3,
            OutlineBrush = Brushes.Transparent,
            OutlineThickness = 5
        };
        var window = new Window { Width = 150, Height = 100, Content = connection };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            window.MouseMove(new Point(40, 15));
            Assert.True(connection.IsPointerOver);

            window.MouseMove(new Point(40, 20));
            Assert.False(connection.IsPointerOver);
        }
        finally
        {
            window.Close();
        }
    }

    private static LineConnection CreateSelectableConnection()
    {
        var connection = new LineConnection
        {
            Source = new Point(10, 10),
            Target = new Point(100, 10),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None
        };
        BaseConnection.SetIsSelectable(connection, true);
        return connection;
    }
}
