using Avalonia;
using ReactiveUI;

namespace Nodify.Avalonia.Calculator.ViewModels
{
    public class OperationGroupViewModel : OperationViewModel
    {
        private Size _size;
        public Size GroupSize
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }
    }
}