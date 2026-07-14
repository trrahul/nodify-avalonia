using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Nodify.Avalonia.Nodes;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class NodeContainerStyleTests
{
    [AvaloniaFact]
    public void HeaderContainerStyle_OverridesPaddingAndCornerRadius()
    {
        var expectedPadding = new Thickness(11, 12, 13, 14);
        var expectedCornerRadius = new CornerRadius(15, 16, 17, 18);
        var node = new Node
        {
            Header = "Header",
            HeaderContainerStyle = CreateBorderTheme(
                new Setter(Border.PaddingProperty, expectedPadding),
                new Setter(Border.CornerRadiusProperty, expectedCornerRadius))
        };

        WithRealizedNode(node, parts =>
        {
            var header = parts.HeaderContainer;

            Assert.NotNull(header);
            Assert.Equal(expectedPadding, header.Padding);
            Assert.Equal(expectedCornerRadius, header.CornerRadius);
        });
    }

    [AvaloniaFact]
    public void FooterContainerStyle_OverridesPaddingAndCornerRadius()
    {
        var expectedPadding = new Thickness(21, 22, 23, 24);
        var expectedCornerRadius = new CornerRadius(25, 26, 27, 28);
        var node = new Node
        {
            Footer = "Footer",
            FooterContainerStyle = CreateBorderTheme(
                new Setter(Border.PaddingProperty, expectedPadding),
                new Setter(Border.CornerRadiusProperty, expectedCornerRadius))
        };

        WithRealizedNode(node, parts =>
        {
            var footer = parts.FooterContainer;

            Assert.NotNull(footer);
            Assert.Equal(expectedPadding, footer.Padding);
            Assert.Equal(expectedCornerRadius, footer.CornerRadius);
        });
    }

    [AvaloniaFact]
    public void ContentContainerStyle_AppliesToWholeBody()
    {
        var bodyBorderThickness = new Thickness(3, 4, 5, 6);
        var bodyCornerRadius = new CornerRadius(7);
        var contentPadding = new Thickness(31, 32, 33, 34);
        var contentBrush = new SolidColorBrush(Colors.CornflowerBlue);
        var content = new object();
        var contentTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock());
        var node = new Node
        {
            Content = content,
            ContentTemplate = contentTemplate,
            Input = new[] { new object() },
            Output = new[] { new object() },
            ContentPadding = contentPadding,
            ContentBrush = contentBrush,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Bottom,
            ContentContainerStyle = CreateBorderTheme(
                new Setter(Border.BorderThicknessProperty, bodyBorderThickness),
                new Setter(Border.CornerRadiusProperty, bodyCornerRadius))
        };

        WithRealizedNode(node, parts =>
        {
            var body = parts.ContentContainer;

            Assert.NotNull(body);
            Assert.Equal(bodyBorderThickness, body.BorderThickness);
            Assert.Equal(bodyCornerRadius, body.CornerRadius);

            var bodyGrid = Assert.IsType<Grid>(body.Child);
            Assert.Equal(3, bodyGrid.ColumnDefinitions.Count);
            var input = Assert.Single(bodyGrid.Children.OfType<ItemsControl>(), item => Grid.GetColumn(item) == 0);
            var output = Assert.Single(bodyGrid.Children.OfType<ItemsControl>(), item => Grid.GetColumn(item) == 2);
            var center = Assert.Single(bodyGrid.Children, control => Grid.GetColumn(control) == 1);

            Assert.Null(center.Name);
            var centerBorder = Assert.IsType<Border>(center);
            Assert.Equal(contentPadding, centerBorder.Padding);
            Assert.Same(contentBrush, centerBorder.Background);
            Assert.Equal(default, centerBorder.BorderThickness);
            Assert.Equal(default, centerBorder.CornerRadius);
            var centerPresenter = Assert.IsType<ContentPresenter>(centerBorder.Child);
            Assert.Same(content, centerPresenter.Content);
            Assert.Same(contentTemplate, centerPresenter.ContentTemplate);
            Assert.Equal(HorizontalAlignment.Right, centerPresenter.HorizontalAlignment);
            Assert.Equal(VerticalAlignment.Bottom, centerPresenter.VerticalAlignment);
            Assert.DoesNotContain(input, centerBorder.GetLogicalDescendants());
            Assert.DoesNotContain(output, centerBorder.GetLogicalDescendants());
            Assert.Equal(default, body.Padding);
            Assert.Null(body.Background);
        });
    }

    [AvaloniaFact]
    public void ContainerThemes_CanBeReusedAndReplaced()
    {
        var sharedPadding = new Thickness(41);
        var replacementPadding = new Thickness(42);
        var sharedTheme = CreateBorderTheme(new Setter(Border.PaddingProperty, sharedPadding));
        var replacementTheme = CreateBorderTheme(new Setter(Border.PaddingProperty, replacementPadding));
        var node = new Node
        {
            Header = "Header",
            Footer = "Footer",
            HeaderContainerStyle = sharedTheme,
            FooterContainerStyle = sharedTheme
        };

        WithRealizedNode(node, parts =>
        {
            Assert.Equal(sharedPadding, parts.HeaderContainer?.Padding);
            Assert.Equal(sharedPadding, parts.FooterContainer?.Padding);

            node.HeaderContainerStyle = replacementTheme;
            Dispatcher.UIThread.RunJobs();

            Assert.Equal(replacementPadding, parts.HeaderContainer?.Padding);
            Assert.Equal(sharedPadding, parts.FooterContainer?.Padding);
        });
    }

    private static ControlTheme CreateBorderTheme(params Setter[] setters)
    {
        var theme = new ControlTheme(typeof(Border));
        foreach (var setter in setters)
        {
            theme.Setters.Add(setter);
        }

        return theme;
    }

    private static void WithRealizedNode(Node node, Action<RealizedNodeParts> assertions)
    {
        var parts = new RealizedNodeParts();
        node.TemplateApplied += (_, e) =>
        {
            parts.HeaderContainer = e.NameScope.Find<Border>("PART_HeaderContainer") ?? parts.HeaderContainer;
            parts.ContentContainer = e.NameScope.Find<Border>("PART_ContentContainer") ?? parts.ContentContainer;
            parts.FooterContainer = e.NameScope.Find<Border>("PART_FooterContainer") ?? parts.FooterContainer;
        };
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Content = node
        };

        try
        {
            window.Show();
            node.ApplyTemplate();
            window.Measure(new Size(400, 300));
            window.Arrange(new Rect(0, 0, 400, 300));
            Dispatcher.UIThread.RunJobs();
            assertions(parts);
        }
        finally
        {
            window.Close();
        }
    }

    private sealed class RealizedNodeParts
    {
        public Border? HeaderContainer { get; set; }

        public Border? ContentContainer { get; set; }

        public Border? FooterContainer { get; set; }
    }
}
