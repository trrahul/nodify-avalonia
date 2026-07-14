# Connectors

A `Connector` is an interactive endpoint. It calculates an `Avalonia.Point` anchor in graph
coordinates, starts pending connections, identifies drop targets, and raises disconnect
requests. `NodeInput`, `NodeOutput`, and `StateNode` provide ready-made connector controls.

Start with the base connector view model, XML namespaces, and node templates in
[Getting started](getting-started.md). This page keeps only connector-specific variations and
behavior.

## Connector data

Store the anchor on the connector view model shown in
[Getting started](getting-started.md). `IsConnected` may mean "participates in anchor
tracking" rather than merely "has one established connection," so set it true while the
application needs live connection geometry.

A connection data item then holds references to its two endpoint objects rather than copies
of their coordinates.

## NodeInput and NodeOutput

`NodeInput` and `NodeOutput` add `Header`, `HeaderTemplate`, `ConnectorTemplate`, and
`Orientation` to the base connector. For example, make the input template from Getting
started vertical and replace its endpoint shape:

```xml
<DataTemplate DataType="vm:ConnectorViewModel">
    <nodes:NodeInput Header="{Binding Title}"
                     Anchor="{Binding Anchor, Mode=OneWayToSource}"
                     IsConnected="{Binding IsConnected}"
                     Orientation="Vertical">
        <nodes:NodeInput.ConnectorTemplate>
            <DataTemplate>
                <Ellipse Width="10" Height="10" Fill="DodgerBlue" />
            </DataTemplate>
        </nodes:NodeInput.ConnectorTemplate>
    </nodes:NodeInput>
</DataTemplate>
```

`Orientation` defaults to `Horizontal`; use `Vertical` for top and bottom connector layouts.
Replace `HeaderTemplate` when the label needs an editor or status indicator. Replace
`ConnectorTemplate` when the endpoint itself needs another shape.

## Anchor binding

`Connector.Anchor` is calculated from the center of the template part named
`PART_Connector`, or from the connector control's center when that part is absent. The
control writes the result, so bind it with `Mode=OneWayToSource`:

```xml
Anchor="{Binding Anchor, Mode=OneWayToSource}"
```

The anchor uses editor graph coordinates. Bind established connections to
`Source.Anchor` and `Target.Anchor`; the control updates those values as item containers move
or resize. Call `UpdateAnchor()` only when code changes layout in a way that bypasses the
normal size, location, or viewport notifications.

## Connected state

`IsConnected` enables anchor tracking and the `:isconnected` visual state. When false, the
connector stops updating `Anchor` and ignores disconnect interaction. Set it true for every
connector used by a visible established connection. Applications may also keep it true for
available endpoints so a new connection receives a current anchor immediately.

`IsPendingConnection` is read-only to consumers and reports whether that connector is
currently creating a connection.

## Connection targets

`PendingConnection.AllowOnlyConnectors` defaults to true. In that mode only connector
controls can become a target. Set it false to allow an `ItemContainer` as a target too.

```xml
<connections:PendingConnection AllowOnlyConnectors="True"
                               EnablePreview="True"
                               EnableSnapping="True"
                               PreviewTarget="{Binding HoverTarget, Mode=OneWayToSource}" />
```

`EnableSnapping` moves `TargetAnchor` onto the connector under the pointer.
`EnablePreview` updates `PreviewTarget` with the candidate connector or item data context and
sets the `PendingConnection.IsOverElement` attached state on the candidate. A custom control
theme can react to that state to highlight valid targets. `Target` receives the accepted
target's data context only when the interaction completes.

## Connector commands and events

The connector raises four bubbling routed events:

- `PendingConnectionStarted` when a connect gesture begins.
- `PendingConnectionDrag` while the captured pointer moves.
- `PendingConnectionCompleted` when the gesture ends over a target.
- `Disconnect` when the disconnect gesture runs.

Handle these events when the view needs direct control. `PendingConnectionEventArgs` exposes
the source and target connector data, anchor, pointer offsets, and cancellation state.

For MVVM, bind editor-level commands:

```xml
<nodify:NodifyEditor ConnectionStartedCommand="{Binding StartConnectionCommand}"
                     ConnectionCompletedCommand="{Binding CreateConnectionCommand}"
                     DisconnectConnectorCommand="{Binding DisconnectConnectorCommand}" />
```

`ConnectionStartedCommand` receives the source connector data. `ConnectionCompletedCommand`
receives a value tuple containing the source and target data. `DisconnectConnectorCommand`
receives the connector data and typically removes all established connections that refer to
it. A connector-level `DisconnectCommand` is also available; it receives that connector's
data context when the `Disconnect` event remains unhandled.

Code can call `BeginConnecting()`, `UpdatePendingConnection(...)`, `EndConnecting()`, or
`CancelConnecting()` for non-pointer workflows. `EndConnecting(Connector)` completes against
a known target control.

The default gestures are documented in [Editor gestures](editor-gestures.md).

[Documentation index](README.md) · [Nodes](nodes.md) · [Connections](connections.md)
