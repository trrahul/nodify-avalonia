# Connections

Connections render links between two `Avalonia.Point` values. An established connection
usually binds those values to connector anchors. `NodifyEditor` renders the objects in its
`Connections` collection through `ConnectionTemplate` and wraps each visual in a
`ConnectionContainer` for selection and keyboard focus.

The snippets below assume these XML namespaces:

```xml
xmlns:nodify="clr-namespace:Nodify.Avalonia;assembly=Nodify.Avalonia"
xmlns:connections="clr-namespace:Nodify.Avalonia.Connections;assembly=Nodify.Avalonia"
```

## Connection data

Keep references to the endpoint view models. Their anchors change when nodes move, so the
connection redraws without replacing the connection data item.

```csharp
public sealed record ConnectionViewModel(
    ConnectorViewModel Source,
    ConnectorViewModel Target,
    string? Label = null);
```

Expose an observable collection and bind it to the editor:

```xml
<nodify:NodifyEditor Connections="{Binding Connections}" />
```

## Connection templates

`ConnectionTemplate` maps each data item to one of the concrete connection controls:

```xml
<nodify:NodifyEditor Connections="{Binding Connections}">
    <nodify:NodifyEditor.ConnectionTemplate>
        <DataTemplate DataType="vm:ConnectionViewModel">
            <connections:Connection Source="{Binding Source.Anchor}"
                                    Target="{Binding Target.Anchor}"
                                    Text="{Binding Label}"
                                    connections:BaseConnection.IsSelectable="True" />
        </DataTemplate>
    </nodify:NodifyEditor.ConnectionTemplate>
</nodify:NodifyEditor>
```

All built-in established connections share `BaseConnection` properties. The most useful are:

- `Source` and `Target` for endpoints.
- `SourceOrientation` and `TargetOrientation` for horizontal or vertical routing.
- `SourceOffset`, `TargetOffset`, `SourceOffsetMode`, `TargetOffsetMode`, and `Spacing` for
  clearance around nodes.
- `Direction`, `ArrowEnds`, `ArrowShape`, and `ArrowSize` for direction markers.
- `Stroke`, `StrokeThickness`, `Fill`, `OutlineBrush`, and `OutlineThickness` for appearance.
- `Text`, `Foreground`, `TextBackground`, `TextPadding`, and `TextCornerRadius` for labels.
- `DirectionalArrowsCount`, `DirectionalArrowsOffset`, `IsAnimatingDirectionalArrows`, and
  `DirectionalArrowsAnimationDuration` for flow indicators along the path.

Set `ArrowEnds="None"` when a connection should have no endpoint arrow. Use a `ControlTheme`
based on the concrete connection type when many connections share the same settings.
`SourceOffsetMode` and `TargetOffsetMode` accept `None`, `Static`, `Circle`, `Rectangle`, or
`Edge`; choose a geometric mode when the offset size represents the endpoint node's bounds.
`Direction` accepts `Forward` or `Backward`.

## Connection

`Connection` draws a smooth Bezier path. It works well for general node editors and can use
the source and target orientations to shape the curve:

```xml
<connections:Connection Source="{Binding Source.Anchor}"
                        Target="{Binding Target.Anchor}"
                        SourceOrientation="Horizontal"
                        TargetOrientation="Horizontal" />
```

The Playground application demonstrates this control and exposes its common shape settings.

## LineConnection

`LineConnection` draws a straight path between the endpoints. A positive `Spacing` adds
short clearance segments at the endpoints; set `CornerRadius` when those bends should be
rounded. Vertical orientations are useful for top-to-bottom flows:

```xml
<connections:LineConnection Source="{Binding Source.Anchor}"
                            Target="{Binding Target.Anchor}"
                            SourceOrientation="Vertical"
                            TargetOrientation="Vertical" />
```

The Workflow and State Machine applications use `LineConnection`.

## CircuitConnection

`CircuitConnection` draws a segmented path controlled by `Angle`, in degrees. Its default
angle is 45 degrees:

```xml
<connections:CircuitConnection Source="{Binding Source.Anchor}"
                               Target="{Binding Target.Anchor}"
                               Angle="45"
                               CornerRadius="4" />
```

The Calculator application uses this variation for established links.

## StepConnection

`StepConnection` routes through horizontal and vertical segments. Set `SourcePosition` and
`TargetPosition` to `Top`, `Left`, `Bottom`, or `Right`; the control derives orientation and
direction from those sides.

```xml
<connections:StepConnection Source="{Binding Source.Anchor}"
                            Target="{Binding Target.Anchor}"
                            SourcePosition="Right"
                            TargetPosition="Left"
                            CornerRadius="6" />
```

The Shapes and Workflow applications demonstrate side-aware step routing. This control is a
library-level variation of `LineConnection`; use it when orthogonal paths communicate the
layout more clearly.

## PendingConnection

`PendingConnection` displays the in-progress drag from a connector. Bind one data item to
`NodifyEditor.PendingConnection`, then supply a `PendingConnectionTemplate`:

```xml
<nodify:NodifyEditor PendingConnection="{Binding PendingConnection}">
    <nodify:NodifyEditor.PendingConnectionTemplate>
        <DataTemplate>
            <connections:PendingConnection IsVisible="{Binding IsVisible}"
                                           Source="{Binding Source, Mode=OneWayToSource}"
                                           Target="{Binding Target, Mode=OneWayToSource}"
                                           TargetAnchor="{Binding TargetLocation, Mode=OneWayToSource}"
                                           AllowOnlyConnectors="True"
                                           EnablePreview="True"
                                           EnableSnapping="True"
                                           StartedCommand="{Binding StartCommand}"
                                           CompletedCommand="{Binding CompleteCommand}" />
        </DataTemplate>
    </nodify:NodifyEditor.PendingConnectionTemplate>
</nodify:NodifyEditor>
```

`SourceAnchor` and `TargetAnchor` drive its visual. `Source` is the initiating connector's
data context; `Target` becomes the accepted target's data context. The interaction produces
`Source`, `Target`, and `TargetAnchor`, so use `OneWayToSource` when the view model only
records them. Use `TwoWay` only when the view model must also drive those control values.
`StartedCommand` receives `Source`, while `CompletedCommand` receives `Target`.

Editor-level `ConnectionStartedCommand` and `ConnectionCompletedCommand` are alternative
configuration choices to the pending connection's `StartedCommand` and `CompletedCommand`.
Configuring commands at both levels may invoke both for one interaction, so choose one level
for each workflow.

The default template draws a dashed pending path. Replace its `Template` with an Avalonia
`ControlTemplate` containing any built-in connection control when the pending path should
match established links. Bind that control's endpoints to `SourceAnchor` and `TargetAnchor`
with `TemplateBinding`.

`AllowOnlyConnectors`, `EnablePreview`, `PreviewTarget`, and `EnableSnapping` control target
discovery. `IsVisible`, `Stroke`, `StrokeThickness`, `StrokeDashArray`, and `Direction`
control the pending visual. See [Connectors](connectors.md) for target details.

## Selecting connections

Built-in connection controls opt into selection through the attached
`BaseConnection.IsSelectable` property. Bind the attached `IsSelected` property when the
connection view model owns selection state:

```xml
<connections:StepConnection Source="{Binding Source.Anchor}"
                            Target="{Binding Target.Anchor}"
                            connections:BaseConnection.IsSelectable="True"
                            connections:BaseConnection.IsSelected="{Binding IsSelected}" />
```

At editor level, bind `SelectedConnection` for one selected item or `SelectedConnections`
for a two-way-synchronized list. `CanSelectMultipleConnections` switches between single and
multiple selection. `SelectAllConnections()` and `UnselectAllConnections()` provide
programmatic selection operations.

The default selection gesture is a left click, with the same append, remove, and invert
modifiers used for item selection. A selectable connection can also receive keyboard focus.

## Removing and splitting connections

Bind `RemoveConnectionCommand` on the editor to remove the supplied connection data item:

```xml
<nodify:NodifyEditor RemoveConnectionCommand="{Binding RemoveConnectionCommand}" />
```

The command runs for a connection's unhandled `Disconnect` event and for every connection
removed by the cutting workflow. A built-in connection also exposes `DisconnectCommand`; its
fallback parameter is `null`. The default connection disconnect gesture is Alt+left click.

Double-clicking a connection raises its `Split` event with `SplitLocation` in graph
coordinates. If the event remains unhandled, `SplitCommand` receives that
`Avalonia.Point`. A typical command removes the original item, creates a knot node at the
split location, and adds two replacement connections. Code can initiate the same workflow
with `SplitAtLocation(point)` or remove the visual through `Remove()`.

For bulk removal by drawing across paths, see [Cutting connections](cutting-connections.md).

[Documentation index](README.md) · [Connectors](connectors.md) · [Cutting connections](cutting-connections.md)
