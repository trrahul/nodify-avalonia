using System;
using System.Globalization;
using ReactiveUI;

namespace Nodify.Avalonia.Playground
{
    /// <summary>
    /// A setting view model that delegates get/set to lambdas, allowing it to
    /// proxy a property on another object (e.g. PlaygroundSettings.Instance).
    /// </summary>
    public sealed class ProxySettingViewModel<T> : BaseSettingViewModel<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        public ProxySettingViewModel(Func<T> getter, Action<T> setter, string name, string? description = null)
            : base(name, description)
        {
            _getter = getter;
            _setter = setter;
        }

        /// <summary>
        /// Overrides the abstract Value from BaseSettingViewModel so compiled
        /// bindings resolve here. The setter uses a robust conversion: if the
        /// incoming value is already T it is used directly; numeric/string types
        /// go through Convert.ChangeType; enums use Enum.ToObject.
        /// </summary>
        public override object? Value
        {
            get => _getter();
            set
            {
                T converted;
                if (value is T t)
                {
                    converted = t;
                }
                else if (typeof(T).IsEnum && value != null)
                {
                    converted = (T)Enum.ToObject(typeof(T), value);
                }
                else
                {
                    // Invariant culture so number rows like "1.5" parse to 1.5 regardless of
                    // OS locale (under CurrentCulture de-DE it became 15).
                    converted = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture)!;
                }

                _setter(converted);
                this.RaisePropertyChanged(nameof(Value));
            }
        }
    }
}
