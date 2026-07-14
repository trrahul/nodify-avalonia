# Item containers

`NodifyEditor` wraps every item in an `ItemContainer`. The container gives arbitrary content
a graph location, selection state, drag behavior, size, and focus visuals.

## Why containers exist

The object in `ItemsSource` is graph data; its data template can render any Avalonia control.
The surrounding `ItemContainer` supplies the behavior that all editor items share. Configure
it with `NodifyEditor.ItemContainerTheme`:

```xml
<nodify:NodifyEditor.ItemContainerTheme>
    <ControlTheme TargetType="nodify:ItemContainer"
                  BasedOn="{StaticResource {x:Type nodify:ItemContainer}}">
        <Setter Property="Location" Value="{Binding Location}" />
        <Setter Property="IsSelectable" Value="{Binding CanSelect}" />
        <Setter Property="IsDraggable" Value="{Binding CanMove}" />
    </ControlTheme>
</nodify:NodifyEditor.ItemContainerTheme>
```

## Location and size

`Location` stores the top-left point in graph coordinates and binds two-way by default.
`ActualSize` reports the rendered size. `PreviewLocation` and `DesiredSizeForSelection` expose
temporary values used during interaction.

```xml
<Setter Property="Location" Value="{Binding Location}" />
<Setter Property="ActualSize" Value="{Binding Size, Mode=OneWayToSource}" />
```

`LocationChanged` fires after the effective location changes. Use the view-model binding for
persistence and the event for view-only reactions.

## Selection

`IsSelectable` controls whether the editor may select a container. `IsSelected` reflects its
current state and can be styled:

```xml
<Style Selector="^[IsSelected=True]">
    <Setter Property="ZIndex" Value="10" />
</Style>
```

Bind the editor's `SelectedItems` collection for MVVM selection. `IsPreviewingSelection`
indicates how a real-time or pending area selection would affect the container.

## Moving and dragging

`IsDraggable` controls pointer and keyboard movement. The default optimized drag updates
`PreviewLocation` while the item moves and commits `Location` when the drag ends. If the
static `NodifyEditor.EnableDraggingContainersOptimizations` property is `false`, `Location`
changes during the drag instead. The container exposes the same operation programmatically:

```csharp
container.BeginDragging();
container.UpdateDragging(new Vector(20, 10));
container.EndDragging();
```

`CancelDragging()` restores the original location when
`NodifyEditor.AllowDraggingCancellation` is enabled. Dragging near an editor edge can trigger
auto-panning.

## Locking selection

`NodifyEditor.LockSelection()` sets `IsDraggable` to `false` on selected containers;
`UnlockSelection()` sets it to `true`. These methods do not remember each container's previous
value. The matching `EditorCommands.LockSelection` and `EditorCommands.UnlockSelection`
commands support menus and toolbars.

If your view model owns a persistent lock flag, bind `IsDraggable` in the container theme and
change the flag through a view-model command instead of using the editor's locking methods.

## Container events

Each container raises routed events for:

- `DragStarted`
- `DragDelta`
- `DragCompleted`
- `LocationChanged`
- `IsSelectedChanged`

`PreviewLocationChanged` reports preview updates before the final position is committed. At
editor level, `ItemsDragStartedCommand`, `ItemsDragCompletedCommand`, and `ItemsMoved` are
usually more convenient for undo and redo systems that operate on multiple selected items.

[Documentation index](README.md) · [Editor](editor.md) · [Nodes](nodes.md)
