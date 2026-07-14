using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia
{
    /// <summary>
    /// An <see cref="ItemsControl"/> that works with <see cref="DecoratorContainer"/>s.
    /// </summary>
    public class DecoratorsControl : ItemsControl, IKeyboardNavigationLayer
    {
        /// <summary>Gets the <see cref="NodifyEditor"/> that owns this <see cref="DecoratorsControl"/>.</summary>
        public NodifyEditor? Editor { get; private set; }

        private readonly StatefulFocusNavigator<DecoratorContainer> _focusNavigator;

        public DecoratorsControl()
        {
            _focusNavigator = new StatefulFocusNavigator<DecoratorContainer>(
                getCurrentFocus: () => global::Avalonia.Controls.TopLevel.GetTopLevel(this)
                    ?.FocusManager?.GetFocusedElement() as DecoratorContainer,
                onFocus: OnElementFocused);
        }

        /// <inheritdoc />
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new DecoratorContainer();

        /// <inheritdoc />
        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            recycleKey = null;
            return item is not DecoratorContainer;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return base.ArrangeOverride(finalSize);
        }

        /// <inheritdoc />
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            Editor = this.FindAncestorOfType<NodifyEditor>();

            if (NodifyEditor.AutoRegisterDecoratorsLayer)
            {
                Editor?.RegisterNavigationLayer(this);
            }
        }

        #region Keyboard Navigation (IKeyboardNavigationLayer)

        /// <inheritdoc />
        public KeyboardNavigationLayerId Id => KeyboardNavigationLayerId.Decorators;

        /// <inheritdoc />
        public IKeyboardFocusTarget<Control>? LastFocusedElement => _focusNavigator.LastFocusedElement;

        /// <inheritdoc />
        public bool TryMoveFocus(TraversalRequest request)
            => _focusNavigator.TryMoveFocus(request, TryFindContainerToFocus);

        /// <inheritdoc />
        public bool TryRestoreFocus()
            => _focusNavigator.TryRestoreFocus();

        /// <inheritdoc />
        public void OnActivated() => TryRestoreFocus();

        /// <inheritdoc />
        public void OnDeactivated() { }

        /// <summary>
        /// Iterates all realized <see cref="DecoratorContainer"/>s.
        /// </summary>
        protected internal IReadOnlyCollection<DecoratorContainer> DecoratorContainers
        {
            get
            {
                var list = new List<DecoratorContainer>(Items.Count);
                for (int i = 0; i < Items.Count; i++)
                {
                    if (ContainerFromIndex(i) is DecoratorContainer dc)
                        list.Add(dc);
                }
                return list;
            }
        }

        private bool TryFindContainerToFocus(DecoratorContainer? currentElement, TraversalRequest request, out DecoratorContainer? containerToFocus)
        {
            containerToFocus = null;

            if (currentElement is DecoratorContainer focusedContainer)
            {
                containerToFocus = FindNextFocusTarget(focusedContainer, request);
            }
            else if (currentElement is Control elem && elem.FindAncestorOfType<DecoratorContainer>() is DecoratorContainer parentContainer)
            {
                containerToFocus = parentContainer;
            }
            else if (Items.Count > 0 && Editor != null)
            {
                var viewport = new Rect(Editor.ViewportLocation, Editor.ViewportSize);
                var containers = DecoratorContainers;
                containerToFocus = containers.FirstOrDefault(c =>
                    viewport.Intersects(((IKeyboardFocusTarget<DecoratorContainer>)c).Bounds))
                    ?? containers.FirstOrDefault();
            }

            return containerToFocus != null;
        }

        /// <summary>
        /// Finds the next decorator container to focus using directional navigation.
        /// </summary>
        protected virtual DecoratorContainer? FindNextFocusTarget(DecoratorContainer currentContainer, TraversalRequest request)
        {
            var nav = new DirectionalFocusNavigator<DecoratorContainer>(DecoratorContainers);
            return nav.FindNextFocusTarget(currentContainer, request)?.Element;
        }

        /// <summary>
        /// Called when a decorator container is keyboard-focused.
        /// </summary>
        protected virtual void OnElementFocused(IKeyboardFocusTarget<DecoratorContainer> target)
        {
            if (NodifyEditor.AutoPanOnNodeFocus)
            {
                Editor?.BringIntoView(target.Bounds, NodifyEditor.BringIntoViewEdgeOffset);
            }
        }

        #endregion
    }
}
