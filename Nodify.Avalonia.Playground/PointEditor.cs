using Avalonia;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    public class PointEditor : ReactiveObject
    {
        public double X
        {
            get => Value.X;
            set
            {
                Value = new Point(value, Value.Y);
                if (value >= 0)
                {
                    Size = new Size(value, Size.Height);
                }
            }
        }

        public double Y
        {
            get => Value.Y;
            set
            {
                Value = new Point(Value.X, value);
                if (value >= 0)
                {
                    Size = new Size(Size.Width, value);
                }
            }
        }

        private Point _value;
        public Point Value
        {
            get => _value;
            set
            {
                this.RaiseAndSetIfChanged(ref _value, value);
                this.RaisePropertyChanged(nameof(X));
                this.RaisePropertyChanged(nameof(Y));
            }
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set
            {
                this.RaiseAndSetIfChanged(ref _size, value);
                this.RaisePropertyChanged(nameof(X));
                this.RaisePropertyChanged(nameof(Y));
            }
        }

        public static implicit operator PointEditor(Point point)
        {
            return new PointEditor
            {
                X = point.X,
                Y = point.Y
            };
        }

        public static implicit operator PointEditor(Size size)
        {
            return new PointEditor
            {
                X = size.Width,
                Y = size.Height
            };
        }
    }
}
