# Theming

Nodify.Avalonia supplies control themes, dynamic brushes, and three color palettes. The
active Avalonia theme variant selects the palette, so controls follow application-wide theme
changes.

## Include the control styles

Include the library styles after your application theme in `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="MyApp.App"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml" />
    </Application.Styles>
</Application>
```

`Controls.xaml` includes every default `ControlTheme`, the dynamic brush resources used by
those themes, and the palette dictionaries. Keep the include after `FluentTheme` so later
application styles can override either theme.

## Choose a color palette

The built-in palettes map to these theme variants:

| Palette | `RequestedThemeVariant` value |
| --- | --- |
| Dark | `Dark` |
| Light | `Light` |
| Purple Nodify palette | `NodifyThemeVariants.Nodify` |

Select Dark or Light directly in XAML. To select the Nodify palette, add the CLR namespace
and reference its public variant:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:nodify="clr-namespace:Nodify.Avalonia;assembly=Nodify.Avalonia"
             x:Class="MyApp.App"
             RequestedThemeVariant="{x:Static nodify:NodifyThemeVariants.Nodify}">
    <Application.Styles>
        <FluentTheme />
        <StyleInclude Source="avares://Nodify.Avalonia/Themes/Controls.xaml" />
    </Application.Styles>
</Application>
```

The library also keeps the Nodify palette in its `Default` resource dictionary as a lookup
fallback. Set a named variant when you want deterministic runtime colors.

## Override theme resources

The control themes consume brushes such as `Node.BackgroundBrush`. Those brushes use
dynamic references to color keys, so overriding a color updates every consumer:

```xml
<Application.Resources>
    <ResourceDictionary>
        <Color x:Key="NodifyEditor.BackgroundColor">#181B20</Color>
        <Color x:Key="NodifyEditor.ForegroundColor">#F4F7FB</Color>
        <Color x:Key="Node.BackgroundColor">#2A3038</Color>
        <Color x:Key="Node.HeaderColor">#1F252D</Color>
        <Color x:Key="ItemContainer.SelectedColor">#FFB020</Color>
        <Color x:Key="Connection.StrokeColor">#4DA3FF</Color>
        <Color x:Key="PendingConnection.StrokeColor">#4DA3FF</Color>
    </ResourceDictionary>
</Application.Resources>
```

Common key groups are:

- `NodifyEditor.*Color` for the background, foreground, selection rectangle, pushed area,
  cutting line, and focus visual.
- `ItemContainer.*Color` for borders and selection.
- `Node.*Color`, `GroupingNode.*Color`, `KnotNode.*Color`, and `StateNode.*Color` for nodes.
- `Connector.*Color`, `NodeInput.*Color`, and `NodeOutput.*Color` for connectors.
- `Connection.StrokeColor`, `LineConnection.StrokeColor`,
  `CircuitConnection.StrokeColor`, and `StepConnection.StrokeColor` for completed links.
- `PendingConnection.*Color`, `Minimap.*Color`, `MinimapItem.BackgroundColor`, and
  `HotKey.*Color` for auxiliary controls.

Use Avalonia theme dictionaries when an override should differ between Light and Dark:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <Color x:Key="Connection.StrokeColor">#0067C0</Color>
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <Color x:Key="Connection.StrokeColor">#60CDFF</Color>
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Prefer color-key overrides for palette changes. Override a brush key only when you need a
different brush type, opacity, or gradient.

## Customize a control theme

Base a named `ControlTheme` on the supplied type theme to keep the control template and
interaction states:

```xml
<UserControl.Resources>
    <ControlTheme x:Key="CompactNodeTheme"
                  TargetType="nodes:Node"
                  BasedOn="{StaticResource {x:Type nodes:Node}}">
        <Setter Property="ContentPadding" Value="10,4" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="BorderBrush" Value="#FF4DA3FF" />
    </ControlTheme>
</UserControl.Resources>

<nodes:Node Header="{Binding Title}"
            Theme="{StaticResource CompactNodeTheme}" />
```

Controls with editor-owned visuals expose dedicated theme properties. For example,
`NodifyEditor.CuttingLineTheme`, `SelectionRectangleTheme`, `PushedAreaTheme`, and
`ItemContainerTheme` accept `ControlTheme` values. `Minimap.ViewportStyle` themes its
viewport rectangle. See [Cutting connections](cutting-connections.md) and
[Minimap](minimap.md) for complete examples.

If you replace a template instead of basing on the supplied theme, preserve the named parts
used by that control. The source themes under `Nodify.Avalonia/Themes/Styles` are the
authoritative template reference.

## Switch themes at runtime

Set the current application's `RequestedThemeVariant`. Dynamic resources refresh the
Nodify.Avalonia palette and the rest of the Avalonia application:

```csharp
using Avalonia;
using Avalonia.Styling;
using Nodify.Avalonia;

public static void SetTheme(string name)
{
    if (Application.Current is not { } app)
        return;

    app.RequestedThemeVariant = name switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        _ => NodifyThemeVariants.Nodify
    };
}
```

You do not need to replace the style include or rebuild control themes. Keep custom colors
in theme dictionaries if they must change with the selected variant.

[Documentation index](README.md) · [Getting started](getting-started.md)
