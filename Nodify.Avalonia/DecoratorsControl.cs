using Avalonia;
using Avalonia.Controls;

namespace Nodify.Avalonia
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that works with <see cref="DecoratorContainer"/>s.
    /// </summary>
    public class DecoratorsControl : ItemsControl
    {
        /// <inheritdoc />
        //protected override bool IsItemItsOwnContainerOverride(Control item) => item is DecoratorContainer; //todo

        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new DecoratorContainer();

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }
    }
}
