# Cutting connections

`NodifyEditor` can draw a cutting line through one or more connections and remove every
connection that intersects it. Bind `RemoveConnectionCommand` to make the operation update
your view model.

## Use the cutting gesture

Hold `Alt`+`Shift`, press the left pointer button, drag across the connections, and release.
The default comes from `EditorGestures.NodifyEditorGestures.Cutting` and applies to the
editor's four built-in connection controls: `Connection`, `LineConnection`,
`CircuitConnection`, and `StepConnection`.

You can also drive the interaction from code. All points use graph coordinates:

```csharp
using Avalonia;

editor.BeginCutting(new Point(100, 80));
editor.UpdateCuttingLine(new Point(420, 260));
editor.EndCutting();
```

`IsCutting`, `CuttingLineStart`, and `CuttingLineEnd` expose the current state for bindings
and diagnostics.

## Handle removed connections

Set the same `RemoveConnectionCommand` used for a connection's disconnect gesture:

```xml
<nodify:NodifyEditor Connections="{Binding Connections}"
                     RemoveConnectionCommand="{Binding RemoveConnectionCommand}"
                     CuttingStartedCommand="{Binding CuttingStartedCommand}"
                     CuttingCompletedCommand="{Binding CuttingCompletedCommand}" />
```

At the end of a cut, `RemoveConnectionCommand` runs once for each intersected control. Its
parameter is that control's `DataContext`, so a typical command removes the received
connection view model from the collection bound to `Connections`.

`CuttingStartedCommand` and `CuttingCompletedCommand` receive the editor's `DataContext`.
The completed command runs whenever cutting ends, including cancellation; use it as an
interaction-lifecycle notification rather than a count of removed connections.

If `RemoveConnectionCommand` is not set, a built-in `BaseConnection` raises its `Disconnect`
event and then tries its own `DisconnectCommand`. An MVVM editor should normally bind the
editor command so all removal paths update the same collection.

Custom connection classes must opt in because cutting uses an exact type set:

```csharp
NodifyEditor.CuttingConnectionTypes.Add(typeof(MyConnection));
```

Add only controls whose rendered geometry represents the connection. The set is static and
affects every editor in the process. Clearing it disables cutting for all registered types.

## Preview intersections

Previewing dims each intersected built-in connection while the line moves:

```csharp
NodifyEditor.EnableCuttingLinePreview = true;
```

The editor sets the attached `CuttingLine.IsOverElement` property on each current
intersection. The default `BaseConnection` theme responds by setting `Opacity` to `0.4`.
Custom connection themes can respond to the same property:

```xml
<ControlTheme x:Key="MyConnectionTheme"
              TargetType="local:MyConnection"
              BasedOn="{StaticResource {x:Type connections:BaseConnection}}">
    <Style Selector="^[(connections|CuttingLine.IsOverElement)=true]">
        <Setter Property="Opacity" Value="0.25" />
    </Style>
</ControlTheme>
```

Previewing recalculates intersections on pointer movement. Leave it disabled for graphs
where the connection count or custom geometry makes that work noticeable.

## Cancel or toggle cutting

Right click or press `Escape` before releasing the cutting gesture to cancel without
removing connections. These inputs come from `EditorGestures.NodifyEditorGestures.CancelAction`.

The following settings are static and apply to every editor:

```csharp
NodifyEditor.AllowCuttingCancellation = true;
NodifyEditor.EnableToggledCuttingMode = false;
NodifyEditor.AllowPanningWhileCutting = false;
NodifyEditor.AllowZoomingWhileCutting = true;
```

When toggled mode is enabled, one cutting gesture starts the line and the next completes it.
When cancellation is disabled, the cancel gesture completes the cut instead. Remap or
disable the cutting and cancel gestures through a per-editor `InputGestures` mapping; see
[Editor gestures](editor-gestures.md).

## Customize the cutting line

Assign a `ControlTheme` to `NodifyEditor.CuttingLineTheme`. Base it on the supplied theme to
retain the default brushes and endpoint rendering:

```xml
<UserControl.Resources>
    <ControlTheme x:Key="DashedCuttingLineTheme"
                  TargetType="connections:CuttingLine"
                  BasedOn="{StaticResource NodifyEditor.CuttingLineTheme}">
        <Setter Property="Stroke" Value="#FFFFB020" />
        <Setter Property="Fill" Value="#FFFFB020" />
        <Setter Property="StrokeThickness" Value="2" />
        <Setter Property="StrokeDashArray" Value="4,2" />
    </ControlTheme>
</UserControl.Resources>

<nodify:NodifyEditor CuttingLineTheme="{StaticResource DashedCuttingLineTheme}" />
```

The Workflow example uses the same property for an accent-colored cutting line.

[Documentation index](README.md) · [Connections](connections.md) ·
[Editor gestures](editor-gestures.md)
