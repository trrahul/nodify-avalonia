# Minimap

`Minimap` displays a scaled view of editor items and a rectangle for the visible viewport.
It can also pan and zoom the editor.

## Add a minimap

Place the editor and minimap in the same layout. Give the minimap a constrained size so its
`Viewbox` does not grow to fit the graph:

```xml
<Grid>
    <nodify:NodifyEditor x:Name="Editor"
                         ItemsSource="{Binding Nodes}" />

    <nodify:Minimap ItemsSource="{Binding ItemsSource, ElementName=Editor}"
                    Width="300"
                    Height="200"
                    Margin="12"
                    HorizontalAlignment="Right"
                    VerticalAlignment="Bottom">
        <nodify:Minimap.ItemTemplate>
            <DataTemplate>
                <Grid />
            </DataTemplate>
        </nodify:Minimap.ItemTemplate>

        <nodify:Minimap.Styles>
            <Style Selector="nodify|MinimapItem">
                <Setter Property="Location" Value="{Binding Location}" />
                <Setter Property="Width" Value="150" />
                <Setter Property="Height" Value="100" />
            </Style>
        </nodify:Minimap.Styles>
    </nodify:Minimap>
</Grid>
```

`Minimap` wraps every data item in a `MinimapItem`. Bind its `Location` to the same graph
position used by `ItemContainer`, and set its `Width` and `Height` to representative node
dimensions. You can bind those dimensions when your view models store actual node sizes.
The empty item template avoids rendering each view model's `ToString()` value; replace it
with a simple preview when node identity matters.

By default, optimized editor dragging commits model locations when a drag ends. If the
minimap must follow every pointer move, set this once during application startup:

```csharp
NodifyEditor.EnableDraggingContainersOptimizations = false;
```

This changes dragging for every editor, so keep the default when end-of-drag updates are
acceptable.

## Bind the viewport

Bind the editor's graph-space viewport to the minimap:

```xml
<nodify:Minimap ItemsSource="{Binding ItemsSource, ElementName=Editor}"
                ViewportLocation="{Binding ViewportLocation,
                                           ElementName=Editor,
                                           Mode=TwoWay}"
                ViewportSize="{Binding ViewportSize, ElementName=Editor}" />
```

`ViewportLocation` must update the editor when a user drags the minimap rectangle. Its
default binding mode is two-way, but writing the mode explicitly makes the requirement
clear. `ViewportSize` describes the editor's visible graph-space area.

`ResizeToViewport="True"` makes the minimap extent include both the items and the complete
viewport. With the default `False`, the minimap scales to its items and the viewport can move
outside their bounds. `MaxViewportOffset` limits how far drag-panning can move beyond the
item extent:

```xml
<nodify:Minimap ResizeToViewport="True"
                MaxViewportOffset="500,500" />
```

## Pan and zoom

The default pointer interactions are:

- Left-button drag moves the viewport.
- Right click or `Escape` cancels a drag and restores its initial position.
- The pointer wheel raises `Zoom` at the pointer location.

Handle `Zoom` and forward its factor and graph-space focus point to the editor:

```xml
<nodify:Minimap Zoom="OnMinimapZoom" />
```

```csharp
using Nodify.Avalonia.Events;

private void OnMinimapZoom(object? sender, ZoomEventArgs e)
{
    Editor.ZoomAtPosition(e.Zoom, e.Location);
}
```

When focused, the minimap also supports arrow-key panning, `Home` to reset the viewport, and
`Ctrl`+plus or `Ctrl`+minus to raise zoom events. `IsReadOnly="True"` blocks drag-panning,
wheel zooming, arrow panning, and the keyboard zoom gestures. The `Home` reset remains
available; unbind `ResetViewport` as shown below if a read-only minimap must never move the
editor.

The public `BeginPanning`, `UpdatePanning`, `EndPanning`, `CancelPanning`, `ResetViewport`,
`ZoomIn`, `ZoomOut`, and `ZoomAtPosition` methods support toolbar or accessibility commands.

## Configure gestures

Supply `EditorGestures.MinimapGestures` through `InputGestures` to change one minimap without
changing the global defaults:

```csharp
using Avalonia.Input;
using Nodify.Avalonia;
using Nodify.Avalonia.Helpers.Gestures;

public EditorGestures.MinimapGestures MinimapGestures { get; } = CreateMinimapGestures();

private static EditorGestures.MinimapGestures CreateMinimapGestures()
{
    var gestures = new EditorGestures.MinimapGestures();
    gestures.DragViewport.Value = new PointerGesture(MouseAction.MiddleClick);
    gestures.ZoomModifierKey = KeyModifiers.Control;
    gestures.ResetViewport.Unbind();
    return gestures;
}
```

```xml
<nodify:Minimap InputGestures="{Binding MinimapGestures}" />
```

`ActualGestures` returns this mapping when set and `EditorGestures.Mappings.Minimap`
otherwise. `Minimap.AllowPanningCancellation`, `Minimap.EnableToggledPanningMode`, and
`Minimap.NavigationStepSize` are static settings shared by all minimaps.

## Customize the viewport and items

`ViewportStyle` accepts a `ControlTheme` for the viewport rectangle:

```xml
<UserControl.Resources>
    <ControlTheme x:Key="MinimapViewportTheme" TargetType="Rectangle">
        <Setter Property="Fill" Value="#334DA3FF" />
        <Setter Property="Stroke" Value="#FF4DA3FF" />
        <Setter Property="StrokeThickness" Value="3" />
    </ControlTheme>
</UserControl.Resources>

<nodify:Minimap ViewportStyle="{StaticResource MinimapViewportTheme}" />
```

Style `MinimapItem` inside `Minimap.Styles` to change all item previews, or bind its
`Background`, `BorderBrush`, `Width`, and `Height` to item-specific data. The built-in color
resources `Minimap.BackgroundColor`, `Minimap.ViewportStrokeColor`,
`Minimap.ViewportBackgroundColor`, and `MinimapItem.BackgroundColor` provide palette-wide
customization; see [Theming](theming.md).

[Documentation index](README.md) · [Editor](editor.md) · [Item containers](item-containers.md)
