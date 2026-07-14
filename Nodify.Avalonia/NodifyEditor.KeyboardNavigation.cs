using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Nodify.Avalonia.Connections;
using Nodify.Avalonia.Interactivity;

namespace Nodify.Avalonia
{
    public partial class NodifyEditor : IKeyboardNavigationLayer, IKeyboardNavigationLayerGroup
    {
        #region Static Configuration

        /// <summary>
        /// Gets or sets the default viewport edge offset applied when bringing an item into view as a result of keyboard focus.
        /// </summary>
        public static double BringIntoViewEdgeOffset { get; set; } = 32d;

        /// <summary>
        /// Automatically focus the first container when the navigation layer changes or the editor gets focused.
        /// </summary>
        public static bool AutoFocusFirstElement { get; set; } = true;

        /// <summary>
        /// Automatically pan the viewport when a node is focused via keyboard navigation.
        /// </summary>
        public static bool AutoPanOnNodeFocus { get; set; } = true;

        /// <summary>
        /// Automatically registers the decorators layer for keyboard navigation.
        /// </summary>
        public static bool AutoRegisterDecoratorsLayer { get; set; } = false;

        /// <summary>
        /// Automatically registers the connections layer for keyboard navigation.
        /// </summary>
        public static bool AutoRegisterConnectionsLayer { get; set; } = true;

        /// <summary>
        /// Indicates whether the viewport should automatically pan to follow elements moved via keyboard dragging.
        /// </summary>
        public static bool PanViewportOnKeyboardDrag { get; set; } = true;

        /// <summary>
        /// Defines the minimum distance to move or navigate when using directional input (such as arrow keys).
        /// </summary>
        public static double MinimumNavigationStepSize { get; set; } = 10d;

        #endregion

        #region Layer Group

        private readonly List<IKeyboardNavigationLayer> _navigationLayers = new List<IKeyboardNavigationLayer>();
        private IKeyboardNavigationLayer? _activeKeyboardNavigationLayer;

        /// <summary>Gets the current active keyboard navigation layer, if any.</summary>
        public IKeyboardNavigationLayer? ActiveNavigationLayer => _activeKeyboardNavigationLayer;

        /// <summary>Occurs when the active keyboard navigation layer changes.</summary>
        public event Action<KeyboardNavigationLayerId>? ActiveNavigationLayerChanged;

        int IReadOnlyCollection<IKeyboardNavigationLayer>.Count => _navigationLayers.Count;

        /// <inheritdoc />
        public bool RegisterNavigationLayer(IKeyboardNavigationLayer layer)
        {
            if (_navigationLayers.Any(l => l.Id == layer.Id))
            {
                return false;
            }

            _navigationLayers.Add(layer);

            if (_activeKeyboardNavigationLayer == null)
            {
                ActivateNavigationLayer(layer.Id);
            }

            return true;
        }

        /// <inheritdoc />
        public bool RemoveNavigationLayer(KeyboardNavigationLayerId layerId)
        {
            var layerToRemove = _navigationLayers.FirstOrDefault(layer => layer.Id == layerId);
            if (layerToRemove != null && _navigationLayers.Remove(layerToRemove))
            {
                // Unconditionally rotate to the index-relative predecessor after ANY removal, not
                // just when the active layer itself was removed. Deliberately do NOT null out
                // _activeKeyboardNavigationLayer here even when the removed layer was active:
                // leaving the field pointing at the (now-detached) removed layer makes
                // ActivatePreviousNavigationLayer's IndexOf(...) return -1 and rotate to
                // (count - 2 + count) % count -- the post-removal list's second-to-last layer, NOT
                // index 0's predecessor (which would be the LAST layer, an off-by-one).
                ActivatePreviousNavigationLayer();

                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool ActivateNavigationLayer(KeyboardNavigationLayerId layerId)
        {
            var newLayer = _navigationLayers.FirstOrDefault(x => x.Id == layerId);
            if (newLayer != null)
            {
                var prevLayer = _activeKeyboardNavigationLayer;
                _activeKeyboardNavigationLayer = newLayer;
                prevLayer?.OnDeactivated();
                newLayer.OnActivated();
                OnKeyboardNavigationLayerActivated(newLayer);
                ActiveNavigationLayerChanged?.Invoke(layerId);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool ActivateNextNavigationLayer()
        {
            if (_navigationLayers.Count > 0)
            {
                int currentIndex = _activeKeyboardNavigationLayer != null
                    ? _navigationLayers.IndexOf(_activeKeyboardNavigationLayer)
                    : -1;
                int nextIndex = (currentIndex + 1) % _navigationLayers.Count;
                return ActivateNavigationLayer(_navigationLayers[nextIndex].Id);
            }

            return false;
        }

        /// <inheritdoc />
        public bool ActivatePreviousNavigationLayer()
        {
            if (_navigationLayers.Count > 0)
            {
                int currentIndex = _activeKeyboardNavigationLayer != null
                    ? _navigationLayers.IndexOf(_activeKeyboardNavigationLayer)
                    : 0;
                int prevIndex = (currentIndex - 1 + _navigationLayers.Count) % _navigationLayers.Count;
                return ActivateNavigationLayer(_navigationLayers[prevIndex].Id);
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerator<IKeyboardNavigationLayer> GetEnumerator()
            => _navigationLayers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        /// Called when a navigation layer is activated. If <see cref="AutoFocusFirstElement"/> is true and
        /// restore fails, tries to move focus to the first element (only when a parent editor has focus or there
        /// is no parent editor).
        /// </summary>
        protected virtual void OnKeyboardNavigationLayerActivated(IKeyboardNavigationLayer activeLayer)
        {
            if (AutoFocusFirstElement && !activeLayer.TryRestoreFocus() && HandleNestedEditor())
            {
                activeLayer.TryMoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }

            bool HandleNestedEditor()
            {
                var parentEditor = this.FindAncestorOfType<NodifyEditor>();
                return parentEditor is null || parentEditor.IsKeyboardFocusWithin;
            }
        }

        #endregion

        #region Nodes IKeyboardNavigationLayer

        // The nodes layer is implemented by the editor itself.
        KeyboardNavigationLayerId IKeyboardNavigationLayer.Id => KeyboardNavigationLayerId.Nodes;

        IKeyboardFocusTarget<Control>? IKeyboardNavigationLayer.LastFocusedElement
            => _nodesNavigator?.LastFocusedElement;

        // Built lazily in InitializeKeyboardNavigation() which is called from the NodifyEditor ctor.
        private StatefulFocusNavigator<ItemContainer>? _nodesNavigator;

        internal void InitializeKeyboardNavigation()
        {
            _nodesNavigator = new StatefulFocusNavigator<ItemContainer>(
                getCurrentFocus: () => global::Avalonia.Controls.TopLevel.GetTopLevel(this)
                    ?.FocusManager?.GetFocusedElement() as ItemContainer,
                onFocus: OnElementFocused);

            RegisterNavigationLayer(this);
            // ActivateNavigationLayer(Nodes) is called automatically inside RegisterNavigationLayer
            // because there is no active layer yet.
        }

        bool IKeyboardNavigationLayer.TryMoveFocus(TraversalRequest request)
            => _nodesNavigator?.TryMoveFocus(request, TryFindContainerToFocus) ?? false;

        bool IKeyboardNavigationLayer.TryRestoreFocus()
            => _nodesNavigator?.TryRestoreFocus() ?? false;

        void IKeyboardNavigationLayer.OnActivated()
        {
            ((IKeyboardNavigationLayer)this).TryRestoreFocus();
        }

        void IKeyboardNavigationLayer.OnDeactivated()
        {
        }

        private bool TryFindContainerToFocus(ItemContainer? currentElement, TraversalRequest request, out ItemContainer? containerToFocus)
        {
            containerToFocus = null;

            if (currentElement is ItemContainer focusedContainer)
            {
                containerToFocus = FindNextFocusTarget(focusedContainer, request);
            }
            // The current focus is a control inside an ItemContainer (e.g. a TextBox inside a node)
            else if (currentElement is Control elem && elem != this
                     && elem.FindAncestorOfType<ItemContainer>() is ItemContainer parentContainer)
            {
                containerToFocus = parentContainer;
            }
            else if (Items.Count > 0)
            {
                var viewport = new global::Avalonia.Rect(ViewportLocation, ViewportSize);
                var containers = ItemContainers;
                containerToFocus = containers.FirstOrDefault(c => c?.IsSelectableInArea(viewport, isContained: false) is true)
                    ?? containers.FirstOrDefault();
            }

            return containerToFocus != null;
        }

        /// <summary>
        /// Finds the next <see cref="ItemContainer"/> to focus using directional navigation.
        /// </summary>
        protected virtual ItemContainer? FindNextFocusTarget(ItemContainer currentContainer, TraversalRequest request)
        {
            var nav = new DirectionalFocusNavigator<ItemContainer>(ItemContainers);
            return nav.FindNextFocusTarget(currentContainer, request)?.Element;
        }

        /// <summary>
        /// Called when an <see cref="ItemContainer"/> is focused by keyboard navigation.
        /// Pans the viewport to bring the container into view when <see cref="AutoPanOnNodeFocus"/> is true.
        /// </summary>
        protected virtual void OnElementFocused(IKeyboardFocusTarget<ItemContainer> target)
        {
            if (AutoPanOnNodeFocus)
            {
                BringIntoView(target.Bounds, BringIntoViewEdgeOffset);
            }
        }

        #endregion

        #region Public Navigation API

        /// <summary>
        /// Moves focus in the specified <paramref name="direction"/> within the active navigation layer.
        /// </summary>
        public bool MoveFocus(FocusNavigationDirection direction)
            => ActiveNavigationLayer?.TryMoveFocus(new TraversalRequest(direction)) ?? false;

        /// <summary>
        /// Returns true when <paramref name="o"/> is a control that acts as a navigation-trigger
        /// boundary (editor, container, connection container, decorator container).
        /// </summary>
        protected internal virtual bool IsNavigationTrigger(object? o)
            => o is NodifyEditor or ItemContainer or ConnectionContainer or DecoratorContainer;

        #endregion

        #region Focus Override

        protected override void OnGotFocus(global::Avalonia.Input.FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);

            if (_lastInputWasKeyboard && ActiveNavigationLayer != null)
            {
                // Determine whether focus is arriving from outside the editor.
                // OldFocusedElement == null means focus came from outside the application/editor.
                bool isFocusComingFromOutside = e.OldFocusedElement is null
                    || (e.OldFocusedElement is Control oldCtrl && !this.IsVisualAncestorOf(oldCtrl));

                if (isFocusComingFromOutside && ActiveNavigationLayer.TryRestoreFocus())
                {
                    e.Handled = true;
                }
                else if (ActiveNavigationLayer.LastFocusedElement is null
                         && ReferenceEquals(e.NewFocusedElement, this)
                         && AutoFocusFirstElement)
                {
                    // Guard with NewFocusedElement == this: without it, the TryMoveFocus call below
                    // focuses a child container, whose GotFocus bubbles back here and re-enters this
                    // branch (LastFocusedElement is still null until .Focus() returns) → unbounded
                    // recursion → StackOverflow.
                    e.Handled = ActiveNavigationLayer.TryMoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }

        /// <summary>
        /// Focus-trap: when keyboard focus moves away from a non-navigation-trigger element
        /// inside the editor (e.g. a TextBox inside a node) to an element outside the editor,
        /// redirect focus back into the editor.
        /// </summary>
        protected override void OnLostFocus(global::Avalonia.Input.FocusChangedEventArgs e)
        {
            base.OnLostFocus(e);

            if (!_lastInputWasKeyboard)
            {
                return;
            }

            // e.OldFocusedElement is the control that just lost focus (was inside the editor).
            // e.NewFocusedElement is the control that is gaining focus.
            if (e.OldFocusedElement is not Control oldFocus)
            {
                return;
            }

            // Only intercept when:
            // 1. The element that lost focus is inside THIS editor.
            // 2. The element that lost focus is NOT itself a navigation trigger (e.g. it is a
            //    TextBox or other focusable inside a node, not an ItemContainer/editor).
            // 3. The new focus target is OUTSIDE this editor (or null).
            bool oldFocusIsInsideEditor = this.IsVisualAncestorOf(oldFocus);
            if (!oldFocusIsInsideEditor)
            {
                return;
            }

            if (IsNavigationTrigger(oldFocus))
            {
                // Focus moved away from a navigation trigger directly — let it go normally.
                return;
            }

            bool newFocusIsOutsideEditor = e.NewFocusedElement is null
                || (e.NewFocusedElement is Control newCtrl && !this.IsVisualAncestorOf(newCtrl));

            if (!newFocusIsOutsideEditor)
            {
                return;
            }

            // Focus is escaping the editor from inside a non-trigger element.
            // Redirect: try to focus the nearest navigation-trigger ancestor (e.g. the
            // ItemContainer that contains the TextBox), or fall back to the editor itself.
            Control? container = FindNavigationTriggerAncestor(oldFocus);
            if (container is Control elem && elem != this && elem.Focusable)
            {
                elem.Focus();
                e.Handled = true;
            }
            else
            {
                e.Handled = Focus();
            }
        }

        private Control? FindNavigationTriggerAncestor(Visual element)
        {
            var current = element.GetVisualParent();
            while (current != null && current != this)
            {
                if (IsNavigationTrigger(current) && current is Control ctrl)
                {
                    return ctrl;
                }
                current = current.GetVisualParent();
            }
            return null;
        }

        #endregion
    }
}
