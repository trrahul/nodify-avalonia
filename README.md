# Nodify.Avalonia

**A powerful, feature-rich node editor control for Avalonia UI applications**

[![NuGet](https://img.shields.io/nuget/v/Nodify.Avalonia?style=for-the-badge&logo=nuget&label=release)](https://www.nuget.org/packages/Nodify.Avalonia/)
[![NuGet](https://img.shields.io/nuget/dt/Nodify.Avalonia?label=downloads&style=for-the-badge&logo=nuget)](https://www.nuget.org/packages/Nodify.Avalonia)
[![License](https://img.shields.io/github/license/trrahul/nodify-avalonia?style=for-the-badge)](https://github.com/trrahul/nodify-avalonia/blob/master/LICENSE)
[![Docs](https://img.shields.io/static/v1?label=docs&message=WIKI&color=blue&style=for-the-badge)](https://github.com/trrahul/nodify-avalonia/wiki)

Nodify.Avalonia is a comprehensive node editor library that enables you to create visual programming interfaces, data flow diagrams, and interactive graph-based applications in Avalonia UI. Built with performance and extensibility in mind, it provides a rich set of controls for creating professional node-based editors.

## 🚀 Quick Start

### Installation
```xml
<PackageReference Include="Nodify.Avalonia" Version="1.0.2" />
```

Or via Package Manager Console:
```
Install-Package Nodify.Avalonia
```

### Basic Usage
```xml
<nodify:NodifyEditor ItemsSource="{Binding Nodes}"
                     Connections="{Binding Connections}"
                     PendingConnection="{Binding PendingConnection}"
                     SelectedItems="{Binding SelectedNodes}" />
```

## ✨ Features

### 🎯 Core Capabilities
- **MVVM-First Design** - Built from the ground up to work seamlessly with MVVM patterns
- **Zero External Dependencies** - Only depends on Avalonia UI
- **High Performance** - Optimized rendering and interaction handling for large node graphs
- **Extensible Architecture** - Highly customizable with dependency properties and styling

### 🎨 Visual Features
- **Built-in Themes** - Professional dark and light themes included
- **Flexible Styling** - Comprehensive theming system with customizable templates
- **Connection Types** - Multiple connection styles: straight lines, bezier curves, and circuit-style
- **Node Shapes** - Support for various node shapes and custom templates

### 🖱️ Interaction & Navigation
- **Multi-Selection** - Select multiple nodes with rectangle selection or Ctrl+click
- **Zooming & Panning** - Smooth zooming with mouse wheel and panning with middle mouse button
- **Auto-Panning** - Automatic panning when dragging near viewport edges
- **Keyboard Shortcuts** - Full keyboard support for common operations
- **Undo/Redo Ready** - Command-based architecture supports undo/redo implementations

### 🔗 Connection System
- **Visual Connection Creation** - Drag from connectors to create connections visually
- **Pending Connections** - Preview connections while dragging
- **Connection Validation** - Customizable connection rules and validation
- **Multiple Connection Types** - Support for different connection styles and behaviors

### 🎛️ Advanced Features
- **Grouping & Comments** - Group nodes together and add comment nodes
- **Decorators Layer** - Overlay custom UI elements on the editor
- **State Management** - Built-in state system for handling different interaction modes
- **Performance Optimizations** - Virtualization and rendering optimizations for large graphs
- **Grid Snapping** - Optional grid snapping for precise node placement

## 📱 Live Demo

See Nodify.Avalonia in action:

[![Demo Video](https://img.youtube.com/vi/vJu3wMTGsGU/0.jpg)](https://www.youtube.com/watch?v=vJu3wMTGsGU)

## 🖼️ Screenshots

### Calculator Demo
A functional calculator built with visual programming nodes:

![Calculator Demo](https://github.com/user-attachments/assets/4c67386c-5ef4-4ebf-b627-de794d037ddc)

### Playground Application
Interactive playground showcasing all features:

![Playground Demo](https://github.com/user-attachments/assets/7664c675-3e2d-451a-b07b-f009d6ab121d)

### Node Graph Examples
![Node Graph 1](https://github.com/trrahul/nodify-avalonia/assets/7353840/ad8543f5-15c2-4506-93ca-2c40933bef26)

![Node Graph 2](https://github.com/trrahul/nodify-avalonia/assets/7353840/11a10880-a8e3-4923-b26e-0feeeb1a7b73)

## 📚 Example Applications

This repository includes two comprehensive example applications:

### 🧮 [Calculator](Nodify.Avalonia.Calculator)
A visual calculator where you can:
- Create mathematical operation nodes
- Connect operations to build complex calculations
- Group operations for organization
- Use various mathematical functions (Add, Multiply, Divide, Pow, etc.)

**Key Controls:**
- **Right Click**: Show operations menu (create nodes)
- **Delete**: Delete selected nodes
- **C**: Group selected operations
- **Ctrl+T**: Toggle theme

### 🎮 [Playground](Nodify.Avalonia.Playground)
An interactive demonstration featuring:
- All editor features and capabilities
- Customizable settings panel
- Theme switching
- Performance testing with large node graphs
- Various node types and connection styles

## 🛠️ Key Components

### NodifyEditor
The main editor control that hosts nodes and connections:
```xml
<nodify:NodifyEditor ItemsSource="{Binding Nodes}"
                     Connections="{Binding Connections}"
                     ViewportZoom="{Binding ZoomLevel}"
                     ViewportLocation="{Binding PanOffset}" />
```

### ItemContainer
Represents individual nodes in the editor:
```xml
<Style Selector="nodify|ItemContainer">
    <Setter Property="Location" Value="{Binding Position}" />
    <Setter Property="IsSelected" Value="{Binding IsSelected}" />
</Style>
```

### Connection Types
- **Connection** - Basic connection between nodes
- **LineConnection** - Straight line connections
- **CircuitConnection** - Circuit-style right-angle connections
- **PendingConnection** - Temporary connections while dragging

### Connectors
Input and output ports for nodes:
```xml
<nodify:Connector IsConnected="{Binding IsConnected}"
                  Anchor="{Binding Position}" />
```

## 🔧 Configuration Options

Nodify.Avalonia offers extensive customization through dependency properties:

### Viewport Control
- `ViewportZoom` - Current zoom level
- `MinViewportZoom` / `MaxViewportZoom` - Zoom limits
- `ViewportLocation` - Pan offset
- `DisablePanning` / `DisableZooming` - Disable interactions

### Selection & Interaction
- `EnableRealtimeSelection` - Update selection while dragging
- `SelectedItems` - Currently selected items
- `DisableAutoPanning` - Disable auto-pan at edges

### Visual Customization
- `GridCellSize` - Grid snap size
- `ConnectionTemplate` - Custom connection appearance
- `DisplayConnectionsOnTop` - Z-order for connections

### Performance Options
- `EnableRenderingOptimizations` - Optimize for large graphs
- `EnableDraggingOptimizations` - Optimize dragging performance

## 🎨 Theming

Built-in themes with full customization support:

```xml
<Application.Styles>
    <FluentTheme />
    <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml"/>
</Application.Styles>
```

### Custom Styling
```xml
<Style Selector="nodify|NodifyEditor">
    <Setter Property="Background" Value="#1E1E1E" />
    <Setter Property="ConnectionTemplate" Value="{StaticResource CustomConnectionTemplate}" />
</Style>
```

## 🤝 Contributing


### Development Setup
1. Clone the repository
2. Open `Nodify.Avalonia.sln` in your IDE
3. Build and run the example applications

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🌟 Show Your Support

If you find Nodify.Avalonia useful, please consider:
- ⭐ Starring the repository
- 🐛 Reporting issues
- 💡 Suggesting new features
- 🔄 Contributing code improvements

---

**Made with ❤️ for the Avalonia community**


