using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Xunit;

namespace Nodify.Avalonia.Tests;

public sealed class ConnectionCuttingTests
{
    [AvaloniaFact]
    public void CuttingConnectionTypes_StartWithAllBuiltInTypes()
    {
        MethodInfo? factory = typeof(NodifyEditor).GetMethod(
            "CreateDefaultCuttingConnectionTypes",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(factory);
        var defaults = Assert.IsType<HashSet<Type>>(factory.Invoke(null, null));
        Assert.Equal(
            new[]
            {
                typeof(CircuitConnection),
                typeof(Connection),
                typeof(LineConnection),
                typeof(StepConnection)
            },
            defaults.OrderBy(type => type.Name));
        Assert.All(defaults, type => Assert.Contains(type, NodifyEditor.CuttingConnectionTypes));

        FieldInfo registry = typeof(NodifyEditor).GetField(nameof(NodifyEditor.CuttingConnectionTypes))!;
        Assert.All(defaults, type => Assert.False(
            ReferencesMetadataToken(type.TypeInitializer, registry.MetadataToken),
            $"{type.Name}'s static constructor must not mutate the cutting registry."));
    }

    [AvaloniaFact]
    public void EmptyCuttingConnectionTypes_MatchesNoConnections()
    {
        var previousTypes = new HashSet<Type>(NodifyEditor.CuttingConnectionTypes);
        object dataContext = new();
        object? removed = null;
        var custom = new UnregisteredConnection
        {
            Width = 100,
            Height = 100,
            Source = new Point(0, 50),
            Target = new Point(100, 50),
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            DataContext = dataContext
        };
        var editor = new NodifyEditor
        {
            Width = 300,
            Height = 200,
            ConnectionTemplate = new SingleControlTemplate(custom),
            RemoveConnectionCommand = new TestCommand(parameter => removed = parameter)
        };
        var window = new Window
        {
            Width = 300,
            Height = 200,
            Content = editor
        };

        try
        {
            NodifyEditor.CuttingConnectionTypes.Clear();
            window.Show();
            editor.ApplyTemplate();
            var connectionsHost = Assert.IsType<ConnectionsMultiSelector>(editor.ConnectionsHost);
            connectionsHost.Template = new FuncControlTemplate<ConnectionsMultiSelector>((_, _) => new ItemsPresenter());
            editor.Connections = new[] { dataContext };
            connectionsHost.ApplyTemplate();
            connectionsHost.Measure(new Size(300, 200));
            connectionsHost.Arrange(new Rect(0, 0, 300, 200));
            editor.Measure(new Size(300, 200));
            editor.Arrange(new Rect(0, 0, 300, 200));
            Dispatcher.UIThread.RunJobs();

            editor.BeginCutting(new Point(50, 40));
            editor.UpdateCuttingLine(new Point(50, 60));
            editor.EndCutting();

            Assert.Null(removed);
        }
        finally
        {
            window.Close();
            NodifyEditor.CuttingConnectionTypes.Clear();
            NodifyEditor.CuttingConnectionTypes.UnionWith(previousTypes);
        }
    }

    [AvaloniaFact]
    public void ShapeCandidates_SkipPrivateLayeredGeometryHostButKeepUnrelatedDescendant()
    {
        var owner = new TestLayeredShape();
        var unrelated = new Line
        {
            StartPoint = new Point(0, 10),
            EndPoint = new Point(20, 10),
            Stroke = Brushes.Black
        };
        owner.Children.Add(unrelated);
        MethodInfo? getCandidates = typeof(BaseConnection).Assembly
            .GetType("Nodify.Avalonia.Connections.ConnectionCutting")?
            .GetMethod("GetShapeCandidates", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(getCandidates);
        var candidates = Assert.IsAssignableFrom<IEnumerable<Visual>>(
            getCandidates.Invoke(null, new object[] { owner }));

        Assert.Equal(new Visual[] { owner, unrelated }, candidates);
    }

    [AvaloniaFact]
    public void RegisteredTemplatedCustomConnection_ParticipatesInPreviewAndCommandRemoval()
    {
        var previousTypes = new HashSet<Type>(NodifyEditor.CuttingConnectionTypes);
        bool previousPreview = NodifyEditor.EnableCuttingLinePreview;
        object dataContext = new();
        object? removed = null;
        var custom = new CustomConnection
        {
            Width = 100,
            Height = 100,
            Theme = new ControlTheme(typeof(CustomConnection))
            {
                Setters =
                {
                    new Setter(
                        CustomConnection.TemplateProperty,
                        new FuncControlTemplate<CustomConnection>((_, _) => new Line
                        {
                            StartPoint = new Point(0, 50),
                            EndPoint = new Point(100, 50),
                            Stroke = Brushes.Black,
                            StrokeThickness = 1
                        }))
                }
            },
            DataContext = dataContext
        };
        var editor = new NodifyEditor
        {
            Width = 300,
            Height = 200,
            ConnectionTemplate = new SingleControlTemplate(custom),
            RemoveConnectionCommand = new TestCommand(parameter => removed = parameter)
        };
        var window = new Window
        {
            Width = 300,
            Height = 200,
            Content = editor
        };

        try
        {
            NodifyEditor.CuttingConnectionTypes.Clear();
            NodifyEditor.CuttingConnectionTypes.Add(typeof(CustomConnection));
            NodifyEditor.EnableCuttingLinePreview = true;

            window.Show();
            editor.ApplyTemplate();
            var connectionsHost = Assert.IsType<ConnectionsMultiSelector>(editor.ConnectionsHost);
            connectionsHost.Template = new FuncControlTemplate<ConnectionsMultiSelector>((_, _) => new ItemsPresenter());
            editor.Connections = new[] { dataContext };
            connectionsHost.ApplyTemplate();
            connectionsHost.Measure(new Size(300, 200));
            connectionsHost.Arrange(new Rect(0, 0, 300, 200));
            editor.Measure(new Size(300, 200));
            editor.Arrange(new Rect(0, 0, 300, 200));
            Dispatcher.UIThread.RunJobs();

            Assert.True(connectionsHost.Items.Count == 1,
                "The editor's connections host did not receive the custom connection data.");
            Assert.True(custom.IsAttachedToVisualTree(), "The custom connection was not realized.");
            Assert.Single(custom.GetVisualDescendants().OfType<Line>());

            editor.BeginCutting(new Point(50, 40));
            editor.UpdateCuttingLine(new Point(50, 60));

            Assert.True(CuttingLine.GetIsOverElement(custom));

            editor.EndCutting();

            Assert.Same(dataContext, removed);
        }
        finally
        {
            window.Close();
            NodifyEditor.CuttingConnectionTypes.Clear();
            NodifyEditor.CuttingConnectionTypes.UnionWith(previousTypes);
            NodifyEditor.EnableCuttingLinePreview = previousPreview;
        }
    }

    [AvaloniaFact]
    public void IsCutByLine_RejectsSharpWaypointRemovedByRoundedRendering()
    {
        var connection = new InspectableLineConnection
        {
            Source = new Point(0, 0),
            Target = new Point(100, -100),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            SourceOrientation = Orientation.Horizontal,
            TargetOrientation = Orientation.Vertical,
            Spacing = 40,
            CornerRadius = 20,
            StrokeThickness = 1,
            ArrowEnds = ArrowHeadEnds.None
        };

        Assert.False(connection.IsCutByLine(
            new Point(100.4267459067941, -138.45714941389826),
            new Point(98.58845584675798, -139.2449880110566)));
    }

    [AvaloniaFact]
    public void IsCutByLine_DetectsShortCutAcrossCubicBezierBetweenLegacySamples()
    {
        var connection = new InspectableConnection
        {
            Source = new Point(0, 0),
            Target = new Point(200, 100),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            SourceOrientation = Orientation.Horizontal,
            TargetOrientation = Orientation.Horizontal,
            StrokeThickness = 1
        };

        Assert.True(connection.IsCutByLine(
            new Point(9.088134765625, 0.236865234375),
            new Point(9.088134765625, 0.336865234375)));
    }

    [AvaloniaFact]
    public void IsCutByLine_DetectsCollinearOverlap()
    {
        var connection = CreateStraightLineConnection();

        Assert.True(connection.IsCutByLine(new Point(40, 0), new Point(60, 0)));
    }

    [AvaloniaFact]
    public void IsCutByLine_IncludesTwoPixelStrokeTolerance()
    {
        var connection = CreateStraightLineConnection();

        Assert.True(connection.IsCutByLine(new Point(50, 2.25), new Point(50, 2.4)));
    }

    [AvaloniaFact]
    public void IsCutByLine_DetectsZeroLengthCutAtOrigin()
    {
        var connection = CreateStraightLineConnection();

        Assert.True(connection.IsCutByLine(default, default));
    }

    private static InspectableLineConnection CreateStraightLineConnection()
        => new()
        {
            Source = new Point(0, 0),
            Target = new Point(100, 0),
            SourceOffsetMode = ConnectionOffsetMode.None,
            TargetOffsetMode = ConnectionOffsetMode.None,
            Spacing = 0,
            StrokeThickness = 1
        };

    private sealed class InspectableConnection : Connection
    {
        public Geometry RenderedGeometry
            => CreateDefiningGeometry()
               ?? throw new InvalidOperationException("Rendered geometry was not created.");
    }

    private sealed class InspectableLineConnection : LineConnection
    {
        public Geometry RenderedGeometry
            => CreateDefiningGeometry()
               ?? throw new InvalidOperationException("Rendered geometry was not created.");
    }

    private sealed class CustomConnection : ContentControl;

    private sealed class TestLayeredShape : LayeredShape
    {
        protected override Geometry? CreateDefiningGeometry()
            => new LineGeometry(new Point(0, 0), new Point(20, 0));
    }

    private sealed class UnregisteredConnection : BaseConnection
    {
        protected override ((Point ArrowStartSource, Point ArrowStartTarget), (Point ArrowEndSource, Point ArrowEndTarget)) DrawLineGeometry(
            StreamGeometryContext context,
            Point source,
            Point target)
        {
            context.BeginFigure(source, false);
            context.LineTo(target);
            return ((target, source), (source, target));
        }
    }

    private sealed class SingleControlTemplate(Control control) : IDataTemplate
    {
        public Control Build(object? parameter) => control;

        public bool Match(object? data) => true;
    }

    private static bool ReferencesMetadataToken(MethodBase? method, int metadataToken)
    {
        byte[]? body = method?.GetMethodBody()?.GetILAsByteArray();
        if (body is null)
        {
            return false;
        }

        ReadOnlySpan<byte> token = BitConverter.GetBytes(metadataToken);
        return body.AsSpan().IndexOf(token) >= 0;
    }

}
