# Getting started

This guide builds a small MVVM node editor with movable nodes and interactive connections.
The view models are deliberately framework-neutral except for `Avalonia.Point`, which stores
graph coordinates.

## Requirements

- .NET 8 SDK
- An Avalonia 12 application
- Familiarity with XAML data binding and `ICommand`

## Install the package

Install Nodify.Avalonia from NuGet:

```powershell
dotnet add package Nodify.Avalonia
```

Or add the package reference directly:

```xml
<PackageReference Include="Nodify.Avalonia" Version="2.0.0" />
```

## Register the styles

Include the control styles after your Avalonia theme in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.App">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml" />
    </Application.Styles>
</Application>
```

If the styles are missing, the controls exist but do not have their default templates.

## Declare the editor host

The editor, containers, nodes, and connections use separate CLR namespaces. This complete
host uses reflection bindings so every snippet in this guide remains portable when copied
into a project whose view-model namespace differs:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:nodify="clr-namespace:Nodify.Avalonia;assembly=Nodify.Avalonia"
             xmlns:nodes="clr-namespace:Nodify.Avalonia.Nodes;assembly=Nodify.Avalonia"
             xmlns:connections="clr-namespace:Nodify.Avalonia.Connections;assembly=Nodify.Avalonia"
             x:Class="MyApp.Views.EditorView"
             x:CompileBindings="False">
    <nodify:NodifyEditor />
</UserControl>
```

Replace `MyApp.Views.EditorView` with the view's class name. To use compiled bindings,
replace `x:CompileBindings="False"` with `x:CompileBindings="True"`, declare the `vm`
namespace, set `x:DataType="vm:EditorViewModel"` on the root, and add the correct
`x:DataType` to each nested data-template binding scope. Compiled bindings validate each
path against the data type for that scope.

## Create an editor

An empty editor is enough to test style registration, selection, panning, and zooming:

```xml
<nodify:NodifyEditor />
```

The editor has three data layers:

1. `ItemsSource` contains nodes and other selectable items.
2. `Connections` contains links rendered behind the item layer.
3. `Decorators` contains graph-positioned overlays rendered above the item layer.

See [Editor](editor.md) for the complete layer and viewport model.

## Display nodes

Create a node view model with a title and graph position. `Location` must notify when it
changes because dragging an item updates the source through a two-way binding.

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;

public sealed class NodeViewModel : INotifyPropertyChanged
{
    private Point _location;

    public required string Title { get; init; }
    public ObservableCollection<ConnectorViewModel> Inputs { get; } = [];
    public ObservableCollection<ConnectorViewModel> Outputs { get; } = [];

    public Point Location
    {
        get => _location;
        set
        {
            if (_location == value)
                return;

            _location = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

The complete `EditorViewModel` later in this guide exposes these initial nodes:

```csharp
public ObservableCollection<NodeViewModel> Nodes { get; } =
[
    new NodeViewModel { Title = "Source", Location = new Point(80, 100) },
    new NodeViewModel { Title = "Target", Location = new Point(360, 180) }
];
```

Bind the collection and provide an item template:

```xml
<nodify:NodifyEditor ItemsSource="{Binding Nodes}">
    <nodify:NodifyEditor.ItemTemplate>
        <DataTemplate>
            <nodes:Node Header="{Binding Title}" />
        </DataTemplate>
    </nodify:NodifyEditor.ItemTemplate>

    <nodify:NodifyEditor.ItemContainerTheme>
        <ControlTheme TargetType="nodify:ItemContainer"
                      BasedOn="{StaticResource {x:Type nodify:ItemContainer}}">
            <Setter Property="Location" Value="{Binding Location}" />
        </ControlTheme>
    </nodify:NodifyEditor.ItemContainerTheme>
</nodify:NodifyEditor>
```

`NodifyEditor` wraps each data item in an `ItemContainer`. The control theme binds each
container's graph position back to its view model.

## Add connectors

Add connector collections to the node model. A connector stores the anchor point produced by
its control. Keep `IsConnected` true when the application needs anchor updates.

```csharp
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;

public sealed class ConnectorViewModel : INotifyPropertyChanged
{
    private Point _anchor;

    public string Title { get; init; } = string.Empty;
    public bool IsConnected { get; set; } = true;

    public Point Anchor
    {
        get => _anchor;
        set
        {
            if (_anchor == value)
                return;

            _anchor = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Anchor)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
```

The assembled `EditorViewModel` below adds one output to the source node and one input to the
target node.

Then update the node template:

```xml
<nodes:Node Header="{Binding Title}"
            Input="{Binding Inputs}"
            Output="{Binding Outputs}">
    <nodes:Node.InputConnectorTemplate>
        <DataTemplate>
            <nodes:NodeInput Header="{Binding Title}"
                             Anchor="{Binding Anchor, Mode=OneWayToSource}"
                             IsConnected="{Binding IsConnected}" />
        </DataTemplate>
    </nodes:Node.InputConnectorTemplate>

    <nodes:Node.OutputConnectorTemplate>
        <DataTemplate>
            <nodes:NodeOutput Header="{Binding Title}"
                              Anchor="{Binding Anchor, Mode=OneWayToSource}"
                              IsConnected="{Binding IsConnected}" />
        </DataTemplate>
    </nodes:Node.OutputConnectorTemplate>
</nodes:Node>
```

## Display connections

A connection view model refers to its source and target connectors:

```csharp
public sealed record ConnectionViewModel(
    ConnectorViewModel Source,
    ConnectorViewModel Target);
```

Use this minimal generic command to expose strongly typed handlers through `ICommand`:

```csharp
using System.Windows.Input;

public sealed class RelayCommand<T>(Action<T> execute) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) => parameter is T;

    public void Execute(object? parameter)
    {
        if (parameter is T value)
            execute(value);
    }
}
```

Here is the complete editor view model. It defines and initializes both commands and keeps
their mutation logic in explicit handlers:

```csharp
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;

public sealed class EditorViewModel
{
    public EditorViewModel()
    {
        Nodes[0].Outputs.Add(new ConnectorViewModel { Title = "Output" });
        Nodes[1].Inputs.Add(new ConnectorViewModel { Title = "Input" });

        CreateConnectionCommand =
            new RelayCommand<(object Source, object? Target)>(CreateConnection);
        RemoveConnectionCommand =
            new RelayCommand<ConnectionViewModel>(RemoveConnection);
    }

    public ObservableCollection<NodeViewModel> Nodes { get; } =
    [
        new NodeViewModel { Title = "Source", Location = new Point(80, 100) },
        new NodeViewModel { Title = "Target", Location = new Point(360, 180) }
    ];

    public ObservableCollection<ConnectionViewModel> Connections { get; } = [];
    public object PendingConnection { get; } = new();

    public ICommand CreateConnectionCommand { get; }
    public ICommand RemoveConnectionCommand { get; }

    private void CreateConnection((object Source, object? Target) endpoints)
    {
        if (endpoints.Source is ConnectorViewModel source &&
            endpoints.Target is ConnectorViewModel target)
        {
            Connections.Add(new ConnectionViewModel(source, target));
        }
    }

    private void RemoveConnection(ConnectionViewModel connection)
    {
        Connections.Remove(connection);
    }
}
```

Bind it to the editor and map each item to a connection control:

```xml
<nodify:NodifyEditor ItemsSource="{Binding Nodes}"
                     Connections="{Binding Connections}">
    <nodify:NodifyEditor.ConnectionTemplate>
        <DataTemplate>
            <connections:Connection Source="{Binding Source.Anchor}"
                                    Target="{Binding Target.Anchor}" />
        </DataTemplate>
    </nodify:NodifyEditor.ConnectionTemplate>
</nodify:NodifyEditor>
```

The anchor bindings update when a connector moves. The connection then redraws between the
new points.

## Create connections interactively

Give the editor the pending-connection data item and command from the assembled view model:

```xml
<nodify:NodifyEditor PendingConnection="{Binding PendingConnection}"
                     ConnectionCompletedCommand="{Binding CreateConnectionCommand}">
    <nodify:NodifyEditor.PendingConnectionTemplate>
        <DataTemplate>
            <connections:PendingConnection AllowOnlyConnectors="True"
                                           EnablePreview="True" />
        </DataTemplate>
    </nodify:NodifyEditor.PendingConnectionTemplate>
</nodify:NodifyEditor>
```

Dragging from one connector to another invokes `ConnectionCompletedCommand` with a C# value
tuple containing the source and target data items. The target is nullable because a pending
connection can finish without a target when its template permits that behavior. Validate both
values before adding a connection. The `CreateConnection` handler above performs that check.

## Remove connections

Bind `RemoveConnectionCommand` to a command that removes its
`ConnectionViewModel` parameter. The editor uses the same command for explicit removal and
[cutting connections](cutting-connections.md). Splitting uses `BaseConnection.SplitCommand`
instead.

`DisconnectConnectorCommand` receives a connector data item. Use it when a gesture should
remove every connection attached to that connector.

## Assemble the interactive editor

The following AXAML combines the node, connector, established-connection, pending-connection,
creation, and removal setup. It retains `x:CompileBindings="False"`, so every binding in the
assembled example uses the portable reflection-binding mode described above:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:nodify="clr-namespace:Nodify.Avalonia;assembly=Nodify.Avalonia"
             xmlns:nodes="clr-namespace:Nodify.Avalonia.Nodes;assembly=Nodify.Avalonia"
             xmlns:connections="clr-namespace:Nodify.Avalonia.Connections;assembly=Nodify.Avalonia"
             x:Class="MyApp.Views.EditorView"
             x:CompileBindings="False">
    <nodify:NodifyEditor ItemsSource="{Binding Nodes}"
                         Connections="{Binding Connections}"
                         PendingConnection="{Binding PendingConnection}"
                         ConnectionCompletedCommand="{Binding CreateConnectionCommand}"
                         RemoveConnectionCommand="{Binding RemoveConnectionCommand}">
        <nodify:NodifyEditor.ItemTemplate>
            <DataTemplate>
                <nodes:Node Header="{Binding Title}"
                            Input="{Binding Inputs}"
                            Output="{Binding Outputs}">
                    <nodes:Node.InputConnectorTemplate>
                        <DataTemplate>
                            <nodes:NodeInput Header="{Binding Title}"
                                             Anchor="{Binding Anchor, Mode=OneWayToSource}"
                                             IsConnected="{Binding IsConnected}" />
                        </DataTemplate>
                    </nodes:Node.InputConnectorTemplate>
                    <nodes:Node.OutputConnectorTemplate>
                        <DataTemplate>
                            <nodes:NodeOutput Header="{Binding Title}"
                                              Anchor="{Binding Anchor, Mode=OneWayToSource}"
                                              IsConnected="{Binding IsConnected}" />
                        </DataTemplate>
                    </nodes:Node.OutputConnectorTemplate>
                </nodes:Node>
            </DataTemplate>
        </nodify:NodifyEditor.ItemTemplate>

        <nodify:NodifyEditor.ItemContainerTheme>
            <ControlTheme TargetType="nodify:ItemContainer"
                          BasedOn="{StaticResource {x:Type nodify:ItemContainer}}">
                <Setter Property="Location"
                        Value="{Binding Location, Mode=TwoWay}" />
            </ControlTheme>
        </nodify:NodifyEditor.ItemContainerTheme>

        <nodify:NodifyEditor.ConnectionTemplate>
            <DataTemplate>
                <connections:Connection Source="{Binding Source.Anchor}"
                                        Target="{Binding Target.Anchor}"
                                        connections:BaseConnection.IsSelectable="True" />
            </DataTemplate>
        </nodify:NodifyEditor.ConnectionTemplate>

        <nodify:NodifyEditor.PendingConnectionTemplate>
            <DataTemplate>
                <connections:PendingConnection AllowOnlyConnectors="True"
                                               EnablePreview="True" />
            </DataTemplate>
        </nodify:NodifyEditor.PendingConnectionTemplate>
    </nodify:NodifyEditor>
</UserControl>
```

Set the view's data context after `InitializeComponent()` (or supply it through your usual
dependency-injection setup):

```csharp
using Avalonia.Controls;

namespace MyApp.Views;

public partial class EditorView : UserControl
{
    public EditorView()
    {
        InitializeComponent();
        DataContext = new EditorViewModel();
    }
}
```

## Position nodes

`ItemContainer.Location` uses graph coordinates, independent of the current viewport pan and
zoom. Its binding mode is two-way by default:

```xml
<Setter Property="Location" Value="{Binding Location}" />
```

Set the per-editor `GridCellSize` property to change the grid interval. The static
`NodifyEditor.EnableSnappingCorrection` property controls final snap correction for every
editor. See [Item containers](item-containers.md) for drag events, preview locations, and
locking.

## Draw a background grid

A tiled `VisualBrush` can follow `ViewportTransform`, keeping the grid aligned while the user
pans and zooms. A brush stored in resources is outside the visual tree, so assign its transform
after loading the AXAML instead of using an `ElementName` binding:

```xml
<UserControl.Resources>
    <VisualBrush x:Key="GridBrush"
                 TileMode="Tile"
                 DestinationRect="0,0,30,30"
                 SourceRect="0,0,30,30">
        <VisualBrush.Visual>
            <Rectangle Width="1" Height="1" Fill="#40FFFFFF" />
        </VisualBrush.Visual>
    </VisualBrush>
</UserControl.Resources>

<nodify:NodifyEditor x:Name="Editor"
                     Background="{StaticResource GridBrush}" />
```

In the view constructor, after `InitializeComponent()`, connect the brush to the editor's
transform. The editor mutates this transform as the viewport changes, so you assign it only
once:

```csharp
using Avalonia.Media;

var gridBrush = (VisualBrush)Resources["GridBrush"]!;
gridBrush.Transform = Editor.ViewportTransform;
```

## Next steps

- Learn the [editor and its content layers](editor.md).
- Customize [gestures](editor-gestures.md).
- Choose among the available [node](nodes.md) and [connection](connections.md) controls.
- Apply built-in or custom [themes](theming.md).

[Documentation index](README.md)
