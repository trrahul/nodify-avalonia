# Nodify.Avalonia

Node-editor controls for Avalonia, designed for MVVM applications.

[![NuGet](https://img.shields.io/nuget/v/Nodify.Avalonia.svg)](https://www.nuget.org/packages/Nodify.Avalonia/)
[![Downloads](https://img.shields.io/nuget/dt/Nodify.Avalonia.svg)](https://www.nuget.org/packages/Nodify.Avalonia/)
[![License](https://img.shields.io/github/license/trrahul/nodify-avalonia.svg)](LICENSE)

This project ports [Nodify](https://github.com/miroiu/nodify) from WPF to **Avalonia 12 and .NET 8**.
It follows the upstream API and behavior while adapting input, rendering, and
theming to Avalonia.

## Why this port?

- **Avalonia 12.0.5** and **.NET 8**
- Nodify v7-style `InputProcessor` architecture
- Per-editor input gesture mappings
- Keyboard navigation and pending-connection hotkeys
- Connection multi-selection, cutting lines, grouping nodes, and minimap support


[Installation](#-installation) · [Examples](#-examples-of-node-based-applications) ·
[Documentation](docs/README.md) ·
[Contributing](CONTRIBUTING.md)

## 🚀 Examples of node-based applications

The repository ships five runnable sample apps, ported from the upstream examples.

🧩 A workflow designer app where you can visualize CI/CD pipelines and their dependencies.

> [Examples/Nodify.Avalonia.Workflow](Examples/Nodify.Avalonia.Workflow)

![Workflow designer](https://raw.githubusercontent.com/trrahul/nodify-avalonia/main/docs/images/workflow-designer.png)

🔶 A canvas application where you can draw and connect shapes.

> [Examples/Nodify.Avalonia.Shapes](Examples/Nodify.Avalonia.Shapes)

![Shapes](https://raw.githubusercontent.com/trrahul/nodify-avalonia/main/docs/images/shapes.png)

🎨 A playground application where you can try all the available settings.

> [Examples/Nodify.Avalonia.Playground](Examples/Nodify.Avalonia.Playground)

![Playground](https://raw.githubusercontent.com/trrahul/nodify-avalonia/main/docs/images/playground.png)

🌓 A state machine where each state represents an executable action, and each transition represents a condition for executing the next action.

> [Examples/Nodify.Avalonia.StateMachine](Examples/Nodify.Avalonia.StateMachine)

![State machine](https://raw.githubusercontent.com/trrahul/nodify-avalonia/main/docs/images/state-machine.png)

💻 A simple "real-time" calculator where each node represents an operation that takes input and feeds its output into other node's input.

> [Examples/Nodify.Avalonia.Calculator](Examples/Nodify.Avalonia.Calculator)

![Calculator](https://raw.githubusercontent.com/trrahul/nodify-avalonia/main/docs/images/calculator.png)

A short walkthrough is also available on [YouTube](https://www.youtube.com/watch?v=vJu3wMTGsGU).

## 📥 Installation

Install via the NuGet package manager:

```
Install-Package Nodify.Avalonia
```

Or add a package reference:

```xml
<PackageReference Include="Nodify.Avalonia" Version="2.0.0" />
```

Register the theme in `App.axaml`:

```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml" />
</Application.Styles>
```

Then host the editor:

```xml
<nodify:NodifyEditor xmlns:nodify="clr-namespace:Nodify.Avalonia;assembly=Nodify.Avalonia"
                     ItemsSource="{Binding Nodes}"
                     Connections="{Binding Connections}"
                     PendingConnection="{Binding PendingConnection}"
                     SelectedItems="{Binding SelectedNodes}" />
```

## ⭐️ Features

- Designed from the start to work with **MVVM**
- **No dependencies** other than Avalonia
- **Optimized** for interactions with hundreds of nodes at once
- Built-in dark and light **themes**
- **Selecting**, **zooming**, and **panning** with **auto-panning** near the edges
- **Select**, **move**, and **connect** nodes
- Multiple connection styles: straight lines, bezier curves, and circuit-style
- Ready for undo/redo
- Configurable input gestures for each action
- Built-in keyboard navigation system
- Grouping and comment nodes, and a decorators overlay layer
- Lots of **configurable** styled properties

## 📚 Documentation

Read the [documentation](docs/README.md) and the [changelog](CHANGELOG.md).

## ❤️ [Contributing](CONTRIBUTING.md)

Documentation, bug reports, and pull requests are very welcome.

1. Clone the repository
2. Open `Nodify.Avalonia.sln`
3. Build and run any of the sample apps to smoke-test your changes

## 📄 License

Licensed under the MIT License — see [LICENSE](LICENSE). This port retains the original Nodify
copyright; all credit for the original library goes to [Miroiu Emanuel](https://github.com/miroiu/nodify).
