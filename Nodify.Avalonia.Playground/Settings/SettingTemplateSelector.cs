using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Nodify.Avalonia.Playground
{
    /// <summary>
    /// A dependency-free IDataTemplate that selects the appropriate editor
    /// template based on the ISettingViewModel.Type property.
    /// Used as the ContentTemplate on the ContentControl in SettingsView.
    /// </summary>
    public class SettingTemplateSelector : IDataTemplate
    {
        public IDataTemplate? Boolean { get; set; }
        public IDataTemplate? Number { get; set; }
        public IDataTemplate? Option { get; set; }
        public IDataTemplate? Point { get; set; }
        public IDataTemplate? Text { get; set; }

        public bool Match(object? data) => data is ISettingViewModel;

        public Control? Build(object? param)
        {
            if (param is not ISettingViewModel vm)
                return null;

            var template = vm.Type switch
            {
                SettingsType.Boolean => Boolean,
                SettingsType.Number => Number,
                SettingsType.Option => Option,
                SettingsType.Point => Point,
                _ => Text
            };

            return template?.Build(param);
        }
    }
}
