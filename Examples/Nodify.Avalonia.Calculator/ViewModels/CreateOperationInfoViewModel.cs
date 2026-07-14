using Avalonia;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class CreateOperationInfoViewModel
    {
        public CreateOperationInfoViewModel(OperationInfoViewModel info, Point location)
        {
            Info = info;
            Location = location;
        }

        public OperationInfoViewModel Info { get; }
        public Point Location { get; }
    }
}
