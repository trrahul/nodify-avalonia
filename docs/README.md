# Nodify.Avalonia documentation

Nodify.Avalonia provides controls for building node editors in Avalonia applications. The
library handles the editor surface, nodes, connectors, connections, selection, navigation,
and input while your view models hold the graph data.

This library is an Avalonia port of [Nodify](https://github.com/miroiu/nodify), created by
Miroiu Emanuel. The guides here describe the Avalonia implementation in this repository.

## Start here

- [Getting started](getting-started.md) — install the package and build a working node editor.
- [Editor](editor.md) — understand layers, viewport movement, selection, and commands.

## Core concepts

- [Item containers](item-containers.md) position and select items on the editor surface.
- [Nodes](nodes.md) display content and arrange input and output connectors.
- [Connectors](connectors.md) expose connection points and report their graph coordinates.
- [Connections](connections.md) render links between connector anchors.

## Controls and interactions

- [Editor gestures](editor-gestures.md) — learn and customize pointer and keyboard input.
- [Cutting connections](cutting-connections.md)
- [Minimap](minimap.md)
- [Theming](theming.md)
- [Frequently asked questions](faq.md)

## Examples

The repository includes five runnable applications:

- [Workflow designer](../Examples/Nodify.Avalonia.Workflow)
- [Shapes canvas](../Examples/Nodify.Avalonia.Shapes)
- [Playground](../Examples/Nodify.Avalonia.Playground)
- [State machine](../Examples/Nodify.Avalonia.StateMachine)
- [Calculator](../Examples/Nodify.Avalonia.Calculator)

Start an example from the repository root with:

```powershell
dotnet run --project Examples/Nodify.Avalonia.Playground
```

## Version requirements

Nodify.Avalonia 2.0 targets **.NET 8** and depends on **Avalonia 12.0.5**. The package has no
runtime dependency other than Avalonia.

[Back to the project README](../README.md)
