# Frequently asked questions

The AXAML fragments below use the namespaces and `x:CompileBindings="False"` host from
[Getting started](getting-started.md). Add explicit `x:DataType` values before enabling
compiled bindings in these fragments.

## Why do the controls appear blank or unstyled?

The control templates are loaded from the package's style include. Add it after your
Avalonia application theme in `App.axaml`:

```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml" />
</Application.Styles>
```

Confirm that the project references `Nodify.Avalonia` and that the URI contains the same
assembly name. See [Getting started](getting-started.md) and [Theming](theming.md).

## Why is the editor empty when `ItemsSource` has data?

`ItemsSource` supplies data, but each item still needs a visual and a graph location. Set an
`ItemTemplate` that creates a node control, then bind `ItemContainer.Location` in
`ItemContainerTheme`:

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

Make the source collection observable when items can be added or removed, and raise property
change notifications when a node's location changes.

## Why do connections stay still when nodes move?

Store each connector's calculated `Anchor` in its view model and bind it back with
`OneWayToSource`. Set `IsConnected` to `True` while the connector participates in a
connection, then bind the connection endpoints to those anchors:

```xml
<connections:Connector Anchor="{Binding Anchor, Mode=OneWayToSource}"
                       IsConnected="True" />

<connections:Connection Source="{Binding Source.Anchor}"
                        Target="{Binding Target.Anchor}" />
```

The anchor source property must notify when it changes. `NodeInput`, `NodeOutput`, and
`StateNode` expose the same connector properties.

## Which binding modes should I use?

Use the direction in which each value is produced:

| Value | Typical mode | Reason |
| --- | --- | --- |
| `ItemContainer.Location` | `TwoWay` | The model positions the node; dragging updates the model. |
| `Connector.Anchor` | `OneWayToSource` | The arranged control calculates the anchor for the model. |
| Connection `Source` and `Target` | `OneWay` | The model supplies endpoint points to the connection. |
| `Minimap.ViewportLocation` | `TwoWay` | Either editor or minimap can pan the viewport. |
| `Minimap.ViewportSize` | `OneWay` | The editor calculates the visible graph-space size. |
| Pending connection `Source`, `Target`, and `TargetAnchor` | `OneWayToSource` | The interaction reports connector data and the pointer anchor to the model. |

Several Nodify.Avalonia properties declare these defaults, but explicit modes make templates
easier to review. With compiled bindings enabled, also give templates the correct data type
or use a valid binding path for their actual `DataContext`.

Use `TwoWay` for pending-connection values only when the view model must also drive them back
into the control.

## Why can I see a connector but not target it?

Pending connection targeting uses Avalonia hit testing. Check these conditions:

- `PendingConnection.EnableHitTesting` is `true`; this is the default and is a static setting.
- The target is a `Connector`, `NodeInput`, `NodeOutput`, or another connector-derived
  control inside the same editor.
- A custom connector has nonzero bounds and a hit-testable visual. The built-in themes use a
  transparent background or fill so the visible connector area participates in hit testing.
- `PendingConnection.AllowOnlyConnectors` matches your intent. Its default `true` excludes
  item containers as targets.

Set `EnablePreview="True"` to show the `:isoverelement` state while diagnosing which target
the pending connection sees.

## Why does one pointer or keyboard action trigger the wrong interaction?

The editor, item containers, connectors, connections, and minimap each have gesture groups.
Overlapping gestures can both match when custom mappings reuse the same input. Create an
`EditorGestures` instance for one editor, unbind the conflicting action, and assign the
mapping through `NodifyEditor.InputGestures`:

```csharp
public EditorGestures Gestures { get; } = CreateGestures();

private static EditorGestures CreateGestures()
{
    var gestures = new EditorGestures();
    gestures.Editor.Cutting.Unbind();
    return gestures;
}
```

```xml
<nodify:NodifyEditor InputGestures="{Binding Gestures}" />
```

Prefer per-editor mappings over changing `EditorGestures.Mappings`, which is the global
singleton. See [Editor gestures](editor-gestures.md) for the complete defaults.

## How should I tune a large graph?

Start with the built-in defaults and measure the interaction that is slow:

- Keep `NodifyEditor.EnableRenderingContainersOptimizations` enabled. It bitmap-caches a
  sufficiently large item panel while zoomed out; the default threshold is 700 containers
  and the default zoom-out threshold is `0.3`.
- Keep `NodifyEditor.EnableDraggingContainersOptimizations` enabled when model and minimap
  updates can wait until a drag completes.
- Set `EnableRealtimeSelection="False"` if updating a large selected set on every pointer
  move is expensive.
- Leave `NodifyEditor.EnableCuttingLinePreview` disabled if repeated connection-geometry
  intersection checks are expensive.
- Keep node and connection templates small. The editor does not promise data virtualization,
  so every visual and binding still contributes cost.

The optimization thresholds are static and affect all editors. Change them only after
profiling a representative graph.

## Where should I report a defect?

Open an issue in the repository's
[issue tracker](https://github.com/trrahul/nodify-avalonia/issues). Include the
Nodify.Avalonia and Avalonia versions, target platform, a minimal AXAML and view-model sample,
the expected and actual behavior, and whether one of the runnable examples reproduces the
problem.

[Documentation index](README.md)
