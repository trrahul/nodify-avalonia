using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using Nodify.Avalonia.Helpers;

namespace Nodify.Avalonia
{
    public partial class NodifyEditor
    {
        // The trackpad magnify delta is ~0.6 per step (observed on macOS), whereas the
        // mouse-wheel zoom curve is calibrated for 120-unit notches. This rescales the
        // magnify delta onto the same exponent that curve expects.
        private const double MouseWheelAvaloniaToWpfScale = 120.0 / 0.6;

        // Empirical feel factor so a pinch covers a comfortable zoom range.
        private const double PointerTouchGestureMagnifyScale = 4.0;

        internal bool IsZoomingAllowed()
            => !DisableZooming
                && (AllowZoomingWhileSelecting || !IsSelecting)
                && (AllowZoomingWhileCutting || !IsCutting)
                && (AllowZoomingWhilePushingItems || !IsPushingItems)
                && (AllowZoomingWhilePanning || !IsPanning);

        private void OnPointerTouchPadGestureMagnify(object? sender, PointerDeltaEventArgs e)
        {
            if (ActualGestures.Editor.ZoomModifierKey == e.KeyModifiers && IsZoomingAllowed())
            {
                double zoom = Math.Pow(2.0, e.Delta.Y / 3.0 / PointerHelper.PointerWheelDeltaForOneLine
                    * MouseWheelAvaloniaToWpfScale * PointerTouchGestureMagnifyScale);
                ZoomAtPosition(zoom, e.GetPosition(ItemsHost));

                if (e.Source is NodifyEditor
                    || (e.Source is Visual v && ReferenceEquals(v.FindAncestorOfType<NodifyEditor>(), this)))
                {
                    e.Handled = true;
                }
            }
        }
    }
}
