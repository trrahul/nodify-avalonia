using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
using ReactiveUI;

namespace Nodify.Avalonia.Shared
{
    /// <summary>
    /// Cycles the application theme through Dark → Light → Nodify (purple).
    /// </summary>
    public static class ThemeManager
    {
        private static readonly List<string> _availableThemes = new List<string> { "Dark", "Light", "Nodify" };

        public static IReadOnlyCollection<string> AvailableThemes => _availableThemes;

        /// <summary>
        /// Custom variant for the purple Nodify palette (inherits Dark fallbacks). Declared in
        /// the library (<see cref="global::Nodify.Avalonia.NodifyThemeVariants"/>) because the
        /// dependency direction is Shared → library, never the reverse; this delegates rather
        /// than re-declaring so there is a single source of truth.
        /// </summary>
        public static ThemeVariant NodifyVariant => global::Nodify.Avalonia.NodifyThemeVariants.Nodify;

        private static string? _activeTheme;

        /// <summary>Detected from the application's actual variant on first use.</summary>
        public static string ActiveTheme => _activeTheme ??= DetectTheme();

        private static string DetectTheme()
        {
            var variant = Application.Current?.ActualThemeVariant;
            if (variant == NodifyVariant) return "Nodify";
            if (variant == ThemeVariant.Light) return "Light";
            return "Dark";
        }

        public static ICommand SetNextThemeCommand { get; } = ReactiveCommand.Create(SetNextTheme);

        public static void SetNextTheme()
        {
            var i = _availableThemes.IndexOf(ActiveTheme);
            var next = (i + 1) % _availableThemes.Count;
            SetTheme(_availableThemes[next]);
        }

        public static void SetTheme(string themeName)
        {
            _activeTheme = themeName;

            Application.Current!.RequestedThemeVariant = themeName switch
            {
                "Dark" => ThemeVariant.Dark,
                "Light" => ThemeVariant.Light,
                _ => NodifyVariant
            };
        }
    }
}
