# Editor gestures

Nodify.Avalonia routes built-in interactions through `InputProcessor`. Gesture mappings are
grouped in `EditorGestures` and can be supplied per editor, so one application can host
editors with different input schemes.

## Default gestures

### Editor

| Action | Default input |
| --- | --- |
| Pan | Right-button drag or middle-button drag |
| Zoom | Pointer wheel |
| Zoom in | `Ctrl`+`+` or `Ctrl`+numpad `+` |
| Zoom out | `Ctrl`+`-` or `Ctrl`+numpad `-` |
| Select area | Left-button drag |
| Add to selection | `Shift`+left-button drag |
| Remove from selection | `Alt`+left-button drag |
| Invert selection | `Ctrl`+left-button drag |
| Select all | `Ctrl`+`A` |
| Push items | `Ctrl`+`Shift`+left-button drag |
| Cut connections | `Alt`+`Shift`+left-button drag |
| Reset viewport | `Home` |
| Fit items to viewport | `Shift`+`Home` |
| Cancel the active operation | Right click or `Escape` |

### Item containers

Left click selects and drags an item. Add `Shift`, `Alt`, or `Ctrl` to append, remove, or
invert its selection. Right click or `Escape` cancels a drag when cancellation is enabled.

### Connectors and connections

| Action | Default input |
| --- | --- |
| Start or complete a pending connection | Left click or `Space` on a connector |
| Cancel a pending connection | Right click or `Escape` |
| Disconnect all links from a connector | `Alt`+left click or `Delete` |
| Select a connection | Left click |
| Add a connection to selection | `Shift`+left click |
| Remove a connection from selection | `Alt`+left click |
| Invert a connection's selection | `Ctrl`+left click |
| Split a connection | Left double-click |
| Remove a connection | `Alt`+left click |

### Grouping nodes

Hold `Shift` while dragging a grouping node by its header to switch its movement mode for that
drag. When the grouping node is selected and focused, `Ctrl`+`Space` or `Ctrl`+`Enter` toggles
the selection state of its content.

### Minimap

| Action | Default input |
| --- | --- |
| Move the viewport | Left-button drag |
| Pan | Arrow keys |
| Zoom | Pointer wheel |
| Zoom in | `Ctrl`+`+` or `Ctrl`+numpad `+` |
| Zoom out | `Ctrl`+`-` or `Ctrl`+numpad `-` |
| Reset viewport | `Home` |
| Cancel panning | Right click or `Escape` |

### Keyboard navigation

| Action | Default input |
| --- | --- |
| Move focus | Arrow keys |
| Toggle focused selection | `Space` or `Enter` |
| Move selected items | `Ctrl`+arrow keys |
| Pan | Hold `Space`, then press an arrow key |
| Clear selection | `Escape` |
| Next navigation layer | `Ctrl`+`]` |
| Previous navigation layer | `Ctrl`+`[` |

These defaults come from the matching groups on `EditorGestures`: `Editor`, `ItemContainer`,
`Connector`, `Connection`, `GroupingNode`, and `Minimap`.

## Per-editor gesture mappings

Create a new mapping rather than modifying the global `EditorGestures.Mappings` singleton:

```csharp
using Avalonia.Input;
using Nodify.Avalonia;
using Nodify.Avalonia.Helpers.Gestures;

public EditorGestures Gestures { get; } = CreateGestures();

private static EditorGestures CreateGestures()
{
    var gestures = new EditorGestures();
    gestures.Editor.ZoomModifierKey = KeyModifiers.Control;
    gestures.Editor.PanWithMouseWheel = true;
    gestures.Editor.PanHorizontalModifierKey = KeyModifiers.Shift;
    return gestures;
}
```

Bind the mapping to one editor:

```xml
<nodify:NodifyEditor InputGestures="{Binding Gestures}" />
```

`ActualGestures` returns the per-editor mapping when `InputGestures` is set and the global
mapping otherwise.

## Disable a gesture

Every `InputGestureRef` can be unbound. For example, disable connection cutting and area
selection in a read-only editor:

```csharp
var gestures = new EditorGestures();
gestures.Editor.Cutting.Unbind();
gestures.Editor.Selection.Unbind();
```

Use `Unbind()` on a gesture group to disable all gestures in that group. Disabling a gesture
does not disable the corresponding public methods or routed commands.

## Toggled interactions

The default drag interactions start on pointer press and finish on release. These static
switches allow the same gesture to start and stop an operation instead:

```csharp
NodifyEditor.EnableToggledPanningMode = true;
NodifyEditor.EnableToggledSelectingMode = true;
NodifyEditor.EnableToggledPushingItemsMode = true;
NodifyEditor.EnableToggledCuttingMode = true;
```

These settings affect every editor in the process. Set them once during application startup.

## Keyboard navigation

The editor always registers the nodes layer and registers the connections layer by default.
Set the static `NodifyEditor.AutoRegisterDecoratorsLayer` property to `true` before creating
editors to register the decorators layer automatically. Only one registered layer is active at
a time. Arrow keys move focus within that layer; `Ctrl`+`[` and `Ctrl`+`]` switch layers.

The editor exposes `ActiveNavigationLayer`, `ActivateNextNavigationLayer()`,
`ActivatePreviousNavigationLayer()`, and `ActivateNavigationLayer(...)` for applications that
need explicit control. The static `AutoFocusFirstElement` property controls whether activating
a layer focuses its first element.

Keyboard dragging raises the same movement events and updates the same location bindings as a
pointer drag. The static `MinimumNavigationStepSize` and `PanViewportOnKeyboardDrag`
properties control keyboard movement behavior for every editor.

[Documentation index](README.md) · [Editor](editor.md)
