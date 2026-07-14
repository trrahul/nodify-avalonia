using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;

namespace Nodify.Avalonia.Shared.Controls
{
    public class Swatches : TemplatedControl
    {
        public static readonly StyledProperty<Color> SelectedColorProperty
            = AvaloniaProperty.Register<Swatches, Color>(nameof(SelectedColor), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<IEnumerable<Color>> ColorsProperty
            = AvaloniaProperty.Register<Swatches, IEnumerable<Color>>(nameof(Colors), defaultValue: Array.Empty<Color>());

        public Color SelectedColor
        {
            get => GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public IEnumerable<Color> Colors
        {
            get => GetValue(ColorsProperty);
            set => SetValue(ColorsProperty, value);
        }

        static Swatches()
        {
            FocusableProperty.OverrideDefaultValue<Swatches>(true);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            var color = e.Source is global::Avalonia.Controls.Control fe && fe.DataContext is Color c ? c : (Color?)null;
            if (color.HasValue)
            {
                SelectedColor = color.Value;
            }

            e.Handled = true;
        }
    }
}
