# Editor

`NodifyEditor` is the graph surface. It generates containers for items, renders connections
and decorators in separate layers, and owns the viewport and interaction state.

## Content layers

The editor renders three collections:

| Layer | Property | Purpose |
| --- | --- | --- |
| Connections | `Connections` | Links rendered behind items by default |
| Items | `ItemsSource` | Nodes and other selectable graph content |
| Decorators | `Decorators` | Graph-positioned overlays rendered above items |

Each item is wrapped in an [ItemContainer](item-containers.md). Connections and decorators use
their own templates and containers. Set `DisplayConnectionsOnTop="True"` when connections
must render above items.

## Viewport coordinates

The graph uses an unscaled coordinate system. These properties describe the visible area:

- `ViewportLocation` is the graph coordinate at the viewport's top-left corner.
- `ViewportSize` is the visible graph size after accounting for zoom.
- `ViewportZoom` is the current scale, constrained by `MinViewportZoom` and
  `MaxViewportZoom`.
- `ViewportTransform` is the transform applied to graph content. It is useful for a grid or
  overlay that must follow the editor.
- `ItemsExtent` and `DecoratorsExtent` contain the bounds of their respective layers.

`ViewportLocation` and `ViewportZoom` use two-way binding by default:

```xml
<nodify:NodifyEditor ViewportLocation="{Binding ViewportLocation}"
                     ViewportZoom="{Binding ViewportZoom}"
                     MinViewportZoom="0.2"
                     MaxViewportZoom="4" />
```

The `ViewportUpdated` routed event fires when the viewport changes.

## Panning

Drag with the right or middle pointer button to pan. Set `DisablePanning="True"` to turn off
interactive panning. The programmatic operation has four stages:

```csharp
editor.BeginPanning();
editor.UpdatePanning(new Vector(20, 0));
editor.EndPanning();
// editor.CancelPanning() restores the initial position when cancellation is enabled.
```

The editor can auto-pan while a selection or drag approaches an edge. Control this with
`DisableAutoPanning`, `AutoPanEdgeDistance`, and `AutoPanSpeed`.

## Zooming

The pointer wheel zooms around the pointer position. `DisableZooming="True"` disables this
input. You can also call:

```csharp
editor.ZoomIn();
editor.ZoomOut();
editor.ZoomAtPosition(1.2, graphPoint);
await editor.ResetViewport();
editor.FitToScreen();
```

`FitToScreen(Rect?)` fits either a supplied graph rectangle or the complete `ItemsExtent`.
`BringIntoView` moves a point or area into the visible viewport.

## Scrolling

`NodifyEditor` implements Avalonia's logical scrolling contract. Put it inside a
`ScrollViewer` to display scrollbars backed by `ItemsExtent`, `ViewportLocation`, and
`ViewportSize`:

```xml
<ScrollViewer HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto">
    <nodify:NodifyEditor ItemsSource="{Binding Nodes}" />
</ScrollViewer>
```

Panning and scrollbars change the same viewport location.

## Selecting items and connections

Bind item selection through `SelectedItem` or `SelectedItems`:

```xml
<nodify:NodifyEditor ItemsSource="{Binding Nodes}"
                     SelectedItems="{Binding SelectedNodes}"
                     Connections="{Binding Connections}"
                     SelectedConnections="{Binding SelectedConnections}" />
```

The editor supports replace, append, remove, and invert selection strategies. Set
`EnableRealtimeSelection="True"` to update selection while the selection rectangle changes;
otherwise the update occurs when the gesture completes.

Connection selection uses the attached `BaseConnection.IsSelectable` property and the
editor's `SelectedConnections` list. See [Connections](connections.md).

## Pushing items

Hold `Ctrl`+`Shift` and drag with the left pointer button to push items away from a horizontal
or vertical boundary. `PushedArea`, `PushedAreaOrientation`, and `IsPushingItems` expose the
current operation. The programmatic API is:

```csharp
editor.BeginPushingItems(start, Orientation.Horizontal);
editor.UpdatePushedArea(delta);
editor.EndPushingItems();
```

Call `CancelPushingItems()` to restore the previous positions when
`NodifyEditor.AllowPushItemsCancellation` is enabled.

## Snapping to a grid

Set `GridCellSize` to make dragged locations align to a graph grid:

```xml
<nodify:NodifyEditor GridCellSize="20" />
```

`SnapToGrid(double)` applies the editor's current cell size to an arbitrary value.
`NodifyEditor.EnableSnappingCorrection` controls whether completed moves receive a final
correction to the grid.

## Commands and events

`EditorCommands` exposes routed commands for `ZoomIn`, `ZoomOut`, `SelectAll`,
`BringIntoView`, `FitToScreen`, `Align`, `LockSelection`, and `UnlockSelection`. Target a
specific editor with `TargetedRoutedCommand` when the command source is outside its visual
tree.

The editor also accepts view-model commands:

- `ConnectionStartedCommand` and `ConnectionCompletedCommand`
- `DisconnectConnectorCommand` and `RemoveConnectionCommand`
- `ItemsDragStartedCommand` and `ItemsDragCompletedCommand`
- `ItemsSelectStartedCommand` and `ItemsSelectCompletedCommand`
- `CuttingStartedCommand` and `CuttingCompletedCommand`

`ItemsMoved` reports moved data items and their common offset. `ViewportUpdated` reports a
viewport change. Individual containers and connections expose additional routed events.

Continue with the core concepts before customizing input:
[Item containers](item-containers.md) · [Nodes](nodes.md) · [Connectors](connectors.md) ·
[Connections](connections.md) · [Editor gestures](editor-gestures.md)
