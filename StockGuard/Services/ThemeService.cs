using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace StockGuard.Services
{
    public class ThemeService
    {
        public event Action<bool>? ThemeChanged;

        public bool IsDark { get; private set; } = true;


        // =========================================================
        // INITIALIZE
        // =========================================================

        public void Initialize()
        {
            Apply(IsDark);
        }


        // =========================================================
        // TOGGLE
        // =========================================================

        public void Toggle()
        {
            IsDark = !IsDark;

            Apply(IsDark);

            ThemeChanged?.Invoke(IsDark);
        }


        // =========================================================
        // APPLY THEME
        // =========================================================

        public void Apply(bool dark)
        {
            IsDark = dark;

            if (Application.Current == null)
                return;


            // -----------------------------------------------------
            // IMPORTANT:
            // Also switch MAUI's native application theme.
            //
            // This helps controls such as:
            // Picker
            // Entry
            // dialogs
            // native Android/iOS controls
            // -----------------------------------------------------

            Application.Current.UserAppTheme =
                dark
                    ? AppTheme.Dark
                    : AppTheme.Light;


            var dict = Application.Current.Resources;

            if (dict == null)
                return;


            // =====================================================
            // DARK THEME
            // =====================================================

            if (dark)
            {
                // Backgrounds
                SetColor(dict, "BgBase", "#080f1e");
                SetColor(dict, "BgSurface", "#0d1526");
                SetColor(dict, "BgElevated", "#111d33");
                SetColor(dict, "BgHover", "#0f1c35");
                SetColor(dict, "BgCard", "#0d1526");

                // Inputs
                SetColor(dict, "InputBg", "#111d33");
                SetColor(dict, "InputBorder", "#1e3a5f");

                // Text
                SetColor(dict, "Text1", "#f0f4ff");
                SetColor(dict, "Text2", "#94a3b8");
                SetColor(dict, "Text3", "#64748b");

                // Borders
                SetColor(dict, "BorderColor", "#1e3a5f");
                SetColor(dict, "BorderSoft", "#1a2e4a");

                // Blue
                SetColor(dict, "Blue", "#3b82f6");
                SetColor(dict, "BlueHover", "#2563eb");
                SetColor(dict, "BlueLight", "#60a5fa");
                SetColor(dict, "BlueSubtle", "#0f1c35");

                // Green
                SetColor(dict, "Green", "#10b981");
                SetColor(dict, "GreenBg", "#0a2a1e");

                // Red
                SetColor(dict, "Red", "#ef4444");
                SetColor(dict, "RedBg", "#2a0a0a");

                // Amber
                SetColor(dict, "Amber", "#f59e0b");
            }


            // =====================================================
            // LIGHT THEME
            // =====================================================

            else
            {
                // Backgrounds
                SetColor(dict, "BgBase", "#f1f5f9");
                SetColor(dict, "BgSurface", "#ffffff");
                SetColor(dict, "BgElevated", "#f1f5f9");
                SetColor(dict, "BgHover", "#f8fafc");
                SetColor(dict, "BgCard", "#ffffff");

                // Inputs
                SetColor(dict, "InputBg", "#ffffff");
                SetColor(dict, "InputBorder", "#cbd5e1");

                // Text
                SetColor(dict, "Text1", "#0f172a");
                SetColor(dict, "Text2", "#475569");
                SetColor(dict, "Text3", "#64748b");

                // Borders
                SetColor(dict, "BorderColor", "#dbe3ec");
                SetColor(dict, "BorderSoft", "#e2e8f0");

                // Blue
                SetColor(dict, "Blue", "#2563eb");
                SetColor(dict, "BlueHover", "#1d4ed8");
                SetColor(dict, "BlueLight", "#2563eb");
                SetColor(dict, "BlueSubtle", "#eff6ff");

                // Green
                SetColor(dict, "Green", "#16a34a");
                SetColor(dict, "GreenBg", "#dcfce7");

                // Red
                SetColor(dict, "Red", "#dc2626");
                SetColor(dict, "RedBg", "#fee2e2");

                // Amber
                SetColor(dict, "Amber", "#d97706");
            }
        }


        // =========================================================
        // COLOR HELPER
        // =========================================================

        private static void SetColor(
            ResourceDictionary dictionary,
            string key,
            string hex)
        {
            dictionary[key] =
                Color.FromArgb(hex);
        }
    }
}