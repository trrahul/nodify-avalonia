using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Nodify.Avalonia.Shared;

public static class ReactiveObjectCustomExtensions
{
    public static bool IsRaiseAndSetIfChanged<TObj, TRet>(
        this TObj reactiveObject,
        ref TRet backingField,
        TRet newValue,
        [CallerMemberName] string? propertyName = null)
        where TObj : IReactiveObject
    {
        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
        {
            return false;
        }
        reactiveObject.RaisePropertyChanging(propertyName);
        backingField = newValue;
        reactiveObject.RaisePropertyChanged(propertyName);
        return true;
    }
}