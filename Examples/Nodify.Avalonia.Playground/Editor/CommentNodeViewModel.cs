using Avalonia;
using ReactiveUI;

namespace Nodify.Avalonia.Playground.Editor
{
    public class CommentNodeViewModel : NodeViewModel
    {
        private string? _title;
        public string? Title
        {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private Size _size;
        public Size Size
        {
            get => _size;
            set => this.RaiseAndSetIfChanged(ref _size, value);
        }
    }
}
