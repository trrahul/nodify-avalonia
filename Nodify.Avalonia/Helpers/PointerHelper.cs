using System.Runtime.CompilerServices;
using Avalonia.Input;
using Avalonia.Styling;

namespace Nodify.Avalonia.Helpers;

public static class PointerHelper
{
    public const int PointerWheelDeltaForOneLine = 120;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointerUpdateKind GetPointerUpdateKind(this PointerEventArgs args)
    {
        return args.GetCurrentPoint(null).Properties.PointerUpdateKind;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointerPointProperties GetPointerPointProperties(this PointerEventArgs args)
    {
        return args.GetCurrentPoint(null).Properties;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MouseButton GetChangedButton(this PointerEventArgs args)
    {
        return args.GetCurrentPoint(null).Properties.PointerUpdateKind.GetMouseButton();
    }

}