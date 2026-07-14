# Nodes

Nodes are the visible items in a graph. `NodifyEditor` creates an `ItemContainer` for each
item in `ItemsSource`; the item template inside that container chooses the node control. The
container owns graph position and selection, while the node control owns its content and
connectors.

Start with the base view models, XML namespaces, templates, and reflection-binding host in
[Getting started](getting-started.md). The snippets below show node-specific variations and
assume that setup, including its `x:CompileBindings="False"` binding mode.

## Node

`Node` is the general-purpose control for a titled node with input and output connector
lists. Bind `Input` and `Output` to enumerable properties, then use the connector templates
from [Getting started](getting-started.md) to turn each item into a connector. The content and
footer are independent of those connector lists:

```xml
<DataTemplate DataType="vm:OperationViewModel">
    <nodes:Node Header="{Binding Title}"
                Input="{Binding Inputs}"
                Output="{Binding Outputs}"
                Footer="{Binding Status}">
        <TextBlock Text="{Binding Description}" />
    </nodes:Node>
</DataTemplate>
```

`HeaderTemplate`, `ContentTemplate`, and `FooterTemplate` customize the three content areas.
Set `Footer` to display the footer. `HeaderBrush`, `ContentBrush`, `FooterBrush`, and
`ContentPadding` provide direct visual customization. Read-only `HasHeader` and `HasFooter`
values track whether those areas contain data.

For finer layout changes, set `HeaderContainerStyle`, `ContentContainerStyle`, or
`FooterContainerStyle` to a `ControlTheme` targeting `Border`. The brush properties remain
the source for each area's fill.

## GroupingNode

`GroupingNode` represents a resizable region. Dragging its header selects and moves the
items inside the group's bounds. Its default `MovementMode` is `Group`; use `Self` when the
group should move without its contents. The `SwitchMovementMode` gesture temporarily flips
the configured mode while the header is dragged.

```xml
<DataTemplate DataType="vm:GroupViewModel">
    <nodes:GroupingNode Header="{Binding Title}"
                        ActualSize="{Binding Size, Mode=TwoWay}"
                        CanResize="{Binding CanResize}"
                        MovementMode="Group"
                        IsContentHitTestVisible="False">
        <TextBlock Margin="12" Text="{Binding Description}" />
    </nodes:GroupingNode>
</DataTemplate>
```

`ActualSize` is an `Avalonia.Size` and binds two-way by default. `ResizeStarted` marks the
lifecycle start before any resize deltas are applied. After the last delta commits the final
size to `ActualSize`, `ResizeCompleted` reports that committed size. The matching commands
run when their events remain unhandled. Use `ResizeThumbTemplate` to replace the resize
handle.

Set `IsContentHitTestVisible` to `False` when pointer input should pass through the group's
body to contained editor items. `ToggleContentSelection()` selects all contained items when
none are selected, or clears the contained selection when any are selected. See
[Editor gestures](editor-gestures.md) for the movement-mode modifier.

## KnotNode

`KnotNode` is a small content host used to reroute a connection. The control does not create
a connector automatically; place one connector data item in `Content`. The Playground
application uses this pattern:

```xml
<DataTemplate DataType="vm:KnotNodeViewModel">
    <nodes:KnotNode Content="{Binding Connector}" />
</DataTemplate>
```

Register a data template for the connector data type, or place a `Connector` directly in
the knot's `Content`. A knot commonly has one incoming connection and one outgoing
connection, both sharing its connector's `Anchor`.

## StateNode

`StateNode` combines node content with connector behavior. The outer ring starts and
completes pending connections; pointer interaction over the content remains available to
buttons, text editors, and other controls inside the node.

Because `StateNode` is a connector, bind `Anchor` back to the view model and keep
`IsConnected` true when connections must follow the node:

```xml
<DataTemplate DataType="vm:StateViewModel">
    <nodes:StateNode Content="{Binding}"
                     Anchor="{Binding Anchor, Mode=OneWayToSource}"
                     IsConnected="True">
        <nodes:StateNode.ContentTemplate>
            <DataTemplate>
                <TextBlock Margin="16" Text="{Binding Name}" />
            </DataTemplate>
        </nodes:StateNode.ContentTemplate>
    </nodes:StateNode>
</DataTemplate>
```

`ContentTemplate` controls the center content. `HighlightBrush` controls the highlight used
when a pending connection is over the state node.

## Custom node templates

An editor item can render any Avalonia control. Use `NodifyEditor.ItemTemplate` for one item
type, or add typed data templates when a graph contains several node types:

```xml
<nodify:NodifyEditor ItemsSource="{Binding Items}">
    <nodify:NodifyEditor.DataTemplates>
        <DataTemplate DataType="vm:OperationViewModel">
            <nodes:Node Header="{Binding Title}" />
        </DataTemplate>
        <DataTemplate DataType="vm:GroupViewModel">
            <nodes:GroupingNode Header="{Binding Title}"
                                ActualSize="{Binding Size}" />
        </DataTemplate>
    </nodify:NodifyEditor.DataTemplates>
</nodify:NodifyEditor>
```

Keep position, selection, and drag settings in `ItemContainerTheme`; keep the node's visual
structure in its data template or control theme. This separation lets several node controls
share the same editor behavior. See [Item containers](item-containers.md) and
[Connectors](connectors.md) for the two surrounding layers.

[Documentation index](README.md) · [Item containers](item-containers.md) · [Connectors](connectors.md)
