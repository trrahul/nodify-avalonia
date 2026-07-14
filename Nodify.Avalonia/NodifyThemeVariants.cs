using Avalonia.Styling;

namespace Nodify.Avalonia
{
    /// <summary>
    /// Declares the custom "Nodify" (purple) <see cref="ThemeVariant"/> used by the
    /// <c>Themes/Controls.xaml</c> ThemeDictionaries.
    /// </summary>
    /// <remarks>
    /// This lives in the library rather than in <c>Nodify.Avalonia.Shared</c> because
    /// the dependency direction between the two projects is one-way (Shared references
    /// the library, never the reverse). <c>Nodify.Avalonia.Shared.ThemeManager.NodifyVariant</c>
    /// delegates to <see cref="Nodify"/> so consumers keep a single source of truth.
    /// </remarks>
    public static class NodifyThemeVariants
    {
        /// <summary>Custom variant for the purple Nodify palette (inherits Dark fallbacks).</summary>
        public static ThemeVariant Nodify { get; } = new ThemeVariant("Nodify", ThemeVariant.Dark);
    }
}
