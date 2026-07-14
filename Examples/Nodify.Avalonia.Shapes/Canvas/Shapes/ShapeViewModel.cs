using Avalonia;
using Avalonia.Media;
using Nodify.Avalonia.Shared;
using Nodify.Avalonia.Shared.UndoRedo;
using System;
using System.Collections.Generic;

namespace Nodify.Avalonia.Shapes.Canvas
{
    public abstract class ShapeViewModel : Undoable
    {
        public ShapeViewModel(IActionsHistory history) : base(history)
        {
            RecordProperty(nameof(Color));
            RecordProperty(nameof(Text));
        }

        public ShapeViewModel() : this(ActionsHistory.Global)
        {
        }

        public static readonly IReadOnlyList<Color> Colors = new Color[]
        {
            Color.FromRgb(207, 76, 44),
            Color.FromRgb(234, 156, 65),
            Color.FromRgb(235, 195, 71),
            Color.FromRgb(67, 141, 87),
            Color.FromRgb(63, 138, 226),
            Color.FromRgb(128, 61, 236),
        };

        private Point _location;
        public Point Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        private double _width;
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        private double _height;
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value).Then(x => OnPropertyChanged(nameof(BorderColor)));
        }

        public Color BorderColor => MultiplyScRgb(Color, 1.5f);

        /// <summary>
        /// Multiplies a color by a scalar in linear scRGB space: sRGB bytes → linear scRGB, scale
        /// (including alpha), clamp, gamma-encode back to sRGB bytes.
        /// </summary>
        private static Color MultiplyScRgb(Color c, float scalar)
        {
            static float ToLinear(byte channel)
            {
                float s = channel / 255f;
                return s <= 0.04045f ? s / 12.92f : MathF.Pow((s + 0.055f) / 1.055f, 2.4f);
            }

            static byte ToSrgbByte(float linear)
            {
                linear = Math.Clamp(linear, 0f, 1f);
                float s = linear <= 0.0031308f ? linear * 12.92f : 1.055f * MathF.Pow(linear, 1f / 2.4f) - 0.055f;
                // Round half up (Floor(x + 0.5)), not round-half-to-even like MathF.Round.
                return (byte)Math.Clamp(MathF.Floor(s * 255f + 0.5f), 0f, 255f);
            }

            float a = Math.Clamp(c.A / 255f * scalar, 0f, 1f);   // alpha is linear (no gamma)
            return Color.FromArgb(
                (byte)Math.Clamp(MathF.Floor(a * 255f + 0.5f), 0f, 255f),   // round half up
                ToSrgbByte(ToLinear(c.R) * scalar),
                ToSrgbByte(ToLinear(c.G) * scalar),
                ToSrgbByte(ToLinear(c.B) * scalar));
        }

        private string? _text;
        public string? Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public ConnectorViewModel LeftConnector { get; } = new ConnectorViewModel(Connections.ConnectorPosition.Left);
        public ConnectorViewModel RightConnector { get; } = new ConnectorViewModel(Connections.ConnectorPosition.Right);
        public ConnectorViewModel TopConnector { get; } = new ConnectorViewModel(Connections.ConnectorPosition.Top);
        public ConnectorViewModel BottomConnector { get; } = new ConnectorViewModel(Connections.ConnectorPosition.Bottom);
    }
}
