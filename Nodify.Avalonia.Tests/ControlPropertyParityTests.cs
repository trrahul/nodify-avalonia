using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Nodify.Avalonia.Nodes;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class ControlPropertyParityTests
{
    [AvaloniaFact]
    public void ActualSize_SizeChangePreservesBinding()
    {
        var bindingTarget = new SizeBindingTarget();
        var container = new DecoratorContainer();
        using var binding = container.Bind(
            DecoratorContainer.ActualSizeProperty,
            new Binding(nameof(SizeBindingTarget.Value))
            {
                Source = bindingTarget,
                Mode = BindingMode.TwoWay,
                Priority = BindingPriority.Style
            });

        Arrange(container, new Size(120, 80));
        Assert.Equal(new Size(120, 80), bindingTarget.Value);

        Arrange(container, new Size(240, 160));
        Assert.Equal(new Size(240, 160), bindingTarget.Value);

        bindingTarget.Value = new Size(360, 240);
        Assert.Equal(new Size(360, 240), container.ActualSize);
    }

    [AvaloniaFact]
    public void ResizeCommands_CanBeProvidedByControlTheme()
    {
        object? startedParameter = null;
        object? completedParameter = null;
        var startedCommand = new TestCommand(parameter => startedParameter = parameter);
        var completedCommand = new TestCommand(parameter => completedParameter = parameter);
        var theme = new ControlTheme(typeof(GroupingNode))
        {
            Setters =
            {
                new Setter(GroupingNode.ResizeStartedCommandProperty, startedCommand),
                new Setter(GroupingNode.ResizeCompletedCommandProperty, completedCommand)
            }
        };
        var node = new GroupingNode
        {
            Theme = theme,
            Width = 120,
            Height = 80
        };

        Arrange(node, new Size(120, 80));
        Assert.Same(startedCommand, node.ResizeStartedCommand);
        Assert.Same(completedCommand, node.ResizeCompletedCommand);

        Assert.Equal(
            BindingPriority.Style,
            node.GetDiagnostic(GroupingNode.ResizeStartedCommandProperty).Priority);
        Assert.Equal(
            BindingPriority.Style,
            node.GetDiagnostic(GroupingNode.ResizeCompletedCommandProperty).Priority);

        node.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragStartedEvent });
        node.Width = 240;
        node.Height = 160;
        Arrange(node, new Size(240, 160));
        node.RaiseEvent(new VectorEventArgs { RoutedEvent = Thumb.DragCompletedEvent });

        Assert.Equal(new Size(120, 80), startedParameter);
        Assert.Equal(new Size(240, 160), completedParameter);
    }

    private static void Arrange(Control control, Size size)
    {
        control.Measure(size);
        control.Arrange(new Rect(size));
    }

    private sealed class SizeBindingTarget : AvaloniaObject
    {
        public static readonly StyledProperty<Size> ValueProperty =
            AvaloniaProperty.Register<SizeBindingTarget, Size>(nameof(Value));

        public Size Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }

}
