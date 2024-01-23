using Avalonia.Input;

namespace Nodify.Avalonia.EditorStates;

public class DragCompletedEventArgs : VectorEventArgs
{
    public bool Canceled { get; set; } 
}