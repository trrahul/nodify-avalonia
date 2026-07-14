using Avalonia;
using Nodify.Avalonia.Extensions;
using Nodify.Avalonia.Shared;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class OperationGraphViewModel : CalculatorOperationViewModel
    {
        private Size _size;
        public Size DesiredSize
        {
            get => _size;
            set
            {
                // Size equality uses == where NaN never equals itself.
                // Without this guard the TwoWay ActualSize binding echoes the collapsed
                // (NaN, NaN) value back forever and overflows the stack.
                if (_size.Width.Equals(value.Width) && _size.Height.Equals(value.Height))
                {
                    return;
                }

                this.RaiseAndSetIfChanged(ref _size, value);
            }
        }
        
        private Size _prevSize;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (this.IsRaiseAndSetIfChanged(ref _isExpanded, value))
                {
                    if (_isExpanded)
                    {
                        DesiredSize = _prevSize;
                    }
                    else
                    {
                        _prevSize = Size;
                        // Fit content
                        DesiredSize = new Size(double.NaN, double.NaN);
                    }
                }
            }
        }

        public OperationGraphViewModel()
        {
            InnerCalculator.Operations[0].Location = new Point(50, 50);
            InnerCalculator.Operations[1].Location = new Point(200, 50);
        }
    }
}