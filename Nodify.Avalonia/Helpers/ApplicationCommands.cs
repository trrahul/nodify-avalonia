namespace Nodify.Avalonia.Helpers;

public static class ApplicationCommands
{
    /// <summary>Gets the value that represents the Cancel Print command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> No gesture defined.</description></item><item><term> UI Text</term><description> Cancel Print</description></item></list></returns>
    public static RoutedUICommand CancelPrint { get; }

    /// <summary>Gets the value that represents the Close command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> No gesture defined.</description></item><item><term> UI Text</term><description> Close</description></item></list></returns>
    public static RoutedUICommand Close { get; }

    /// <summary>Gets the value that represents the Context Menu command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Shift+F10
    /// 
    /// Apps</description></item><item><term> Mouse Gesture</term><description> A Mouse Gesture is not attached to this command, but most applications follow the convention of using the Right Click gesture to invoke the context menu.</description></item><item><term> UI Text</term><description> Context Menu</description></item></list></returns>
    public static RoutedUICommand ContextMenu { get; }

    /// <summary>Gets the value that represents the Copy command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+C
    /// 
    /// Ctrl+Insert</description></item><item><term> UI Text</term><description> Copy</description></item></list></returns>
    public static RoutedUICommand Copy { get; }

    /// <summary>Gets the value that represents the Correction List command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> No gesture defined.</description></item><item><term> UI Text</term><description> Correction List</description></item></list></returns>
    public static RoutedUICommand CorrectionList { get; }

    /// <summary>Gets the value that represents the Cut command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+X
    /// 
    /// Shift+Delete</description></item><item><term> UI Text</term><description> Cut</description></item></list></returns>
    public static RoutedUICommand Cut { get; }

    /// <summary>Gets the value that represents the Delete command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Del</description></item><item><term> UI Text</term><description> Delete</description></item></list></returns>
    public static RoutedUICommand Delete { get; }

    /// <summary>Gets the value that represents the Find command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+F</description></item><item><term> UI Text</term><description> Find</description></item></list></returns>
    public static RoutedUICommand Find { get; }

    /// <summary>Gets the value that represents the Help command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> F1</description></item><item><term> UI Text</term><description> Help</description></item></list></returns>
    public static RoutedUICommand Help { get; }

    /// <summary>Gets the value that represents the New command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+N</description></item><item><term> UI Text</term><description> New</description></item></list></returns>
    public static RoutedUICommand New { get; }

    /// <summary>Represents a command which is always ignored.</summary>
    /// <returns>The command.</returns>
    public static RoutedUICommand NotACommand { get; }

    /// <summary>Gets the value that represents the Open command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+O</description></item><item><term> UI Text</term><description> Open</description></item></list></returns>
    public static RoutedUICommand Open { get; }

    /// <summary>Gets the value that represents the Paste command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+V
    /// 
    /// Shift+Insert</description></item><item><term> UI Text</term><description> Paste</description></item></list></returns>
    public static RoutedUICommand Paste { get; }

    /// <summary>Gets the value that represents the Print command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+P</description></item><item><term> UI Text</term><description> Print</description></item></list></returns>
    public static RoutedUICommand Print { get; }

    /// <summary>Gets the value that represents the Print Preview command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+F2</description></item><item><term> UI Text</term><description> Print Preview</description></item></list></returns>
    public static RoutedUICommand PrintPreview { get; }

    /// <summary>Gets the value that represents the Properties command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> F4</description></item><item><term> UI Text</term><description> Properties</description></item></list></returns>
    public static RoutedUICommand Properties { get; }

    /// <summary>Gets the value that represents the Redo command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+Y</description></item><item><term> UI Text</term><description> Redo</description></item></list></returns>
    public static RoutedUICommand Redo { get; }

    /// <summary>Gets the value that represents the Replace command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+H</description></item><item><term> UI Text</term><description> Replace</description></item></list></returns>
    public static RoutedUICommand Replace { get; }

    /// <summary>Gets the value that represents the Save command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+S</description></item><item><term> UI Text</term><description> Save</description></item></list></returns>
    public static RoutedUICommand Save { get; }

    /// <summary>Gets the value that represents the Save As command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> No gesture defined.</description></item><item><term> UI Text</term><description> Save As</description></item></list></returns>
    public static RoutedUICommand SaveAs { get; }

    /// <summary>Gets the value that represents the Select All command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl+A</description></item><item><term> UI Text</term><description> Select All</description></item></list></returns>
    public static RoutedUICommand SelectAll { get; }

    /// <summary>Gets the value that represents the Stop command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Esc</description></item><item><term> UI Text</term><description> Stop</description></item></list></returns>
    public static RoutedUICommand Stop { get; }

    /// <summary>Gets the value that represents the Undo command.</summary>
    /// <returns>The command.
    /// 
    /// <list type="table"><listheader><term> Default Values</term><description></description></listheader><item><term> Key Gesture</term><description> Ctrl-Z</description></item><item><term> UI Text</term><description> Undo</description></item></list></returns>
    public static RoutedUICommand Undo { get; }
}