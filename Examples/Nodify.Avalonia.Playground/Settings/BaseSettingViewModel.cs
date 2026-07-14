using System;
using Nodify.Avalonia.Playground.ViewModels;

namespace Nodify.Avalonia.Playground
{
    /// <summary>
    /// Base class for all setting view models. The Value property is abstract so
    /// subclasses can override it with getter/setter lambdas — this ensures that
    /// compiled XAML bindings ({Binding Value} with x:DataType=ISettingViewModel)
    /// resolve to the actual overridden Value rather than being bypassed by a
    /// shadowed 'new' member.
    /// </summary>
    public abstract class BaseSettingViewModel<T> : ViewModelBase, ISettingViewModel
    {
        public string Name { get; }
        public string? Description { get; }
        public SettingsType Type { get; }

        protected BaseSettingViewModel(string name, string? description = null)
        {
            Name = name;
            Description = description;
            Type = typeof(T) switch
            {
                { } t when t == typeof(string) => SettingsType.Text,
                { } t when t == typeof(bool) => SettingsType.Boolean,
                { } t when t == typeof(uint) || t == typeof(double) => SettingsType.Number,
                { } t when t == typeof(PointEditor) => SettingsType.Point,
                { IsEnum: true } => SettingsType.Option,
                _ => throw new InvalidOperationException($"Type {typeof(T).Name} does not have a matching {nameof(SettingsType)}.")
            };
        }

        /// <summary>
        /// Overridable so compiled XAML bindings to ISettingViewModel.Value hit the
        /// actual implementation in the subclass (not a shadowed member).
        /// </summary>
        public abstract object? Value { get; set; }
    }
}
