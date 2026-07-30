using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Services
{
    public class ThemeService
    {
        public event Action<bool>? ThemeChanged;

        public bool IsDark { get; private set; } = true; // dark default

        public void Initialize()
        {
            Apply(IsDark);
        }

        public void Toggle()
        {
            IsDark = !IsDark;
            Apply(IsDark);
            ThemeChanged?.Invoke(IsDark);
        }

        public void Apply(bool dark)
        {
            var dict = Application.Current?.Resources;
            if (dict is null) return;

            if (dark)
            {
                dict["BgBase"] = Color.FromArgb("#080f1e");
                dict["BgSurface"] = Color.FromArgb("#0d1526");
                dict["BgElevated"] = Color.FromArgb("#111d33");
                dict["BgHover"] = Color.FromArgb("#0f1c35");
                dict["BgCard"] = Color.FromArgb("#0d1526");
                dict["InputBg"] = Color.FromArgb("#111d33");
                dict["InputBorder"] = Color.FromArgb("#1e3a5f");
                dict["Text1"] = Color.FromArgb("#f0f4ff");
                dict["Text2"] = Color.FromArgb("#94a3b8");
                dict["Text3"] = Color.FromArgb("#475569");
                dict["BorderColor"] = Color.FromArgb("#1e3a5f");
                dict["BorderSoft"] = Color.FromArgb("#1a2e4a");
                dict["Blue"] = Color.FromArgb("#3b82f6");
                dict["BlueHover"] = Color.FromArgb("#2563eb");
                dict["BlueLight"] = Color.FromArgb("#60a5fa");
                dict["BlueSubtle"] = Color.FromArgb("#0f1c35");
                dict["Green"] = Color.FromArgb("#10b981");
                dict["GreenBg"] = Color.FromArgb("#0a2a1e");
                dict["Red"] = Color.FromArgb("#ef4444");
                dict["RedBg"] = Color.FromArgb("#2a0a0a");
                dict["Amber"] = Color.FromArgb("#f59e0b");
            }
            else
            {
                dict["BgBase"] = Color.FromArgb("#f1f5f9");
                dict["BgSurface"] = Color.FromArgb("#ffffff");
                dict["BgElevated"] = Color.FromArgb("#e2e8f0");
                dict["BgHover"] = Color.FromArgb("#f1f5f9");
                dict["BgCard"] = Color.FromArgb("#ffffff");
                dict["InputBg"] = Color.FromArgb("#f8fafc");
                dict["InputBorder"] = Color.FromArgb("#cbd5e1");
                dict["Text1"] = Color.FromArgb("#0f172a");
                dict["Text2"] = Color.FromArgb("#475569");
                dict["Text3"] = Color.FromArgb("#94a3b8");
                dict["BorderColor"] = Color.FromArgb("#cbd5e1");
                dict["BorderSoft"] = Color.FromArgb("#e2e8f0");
                dict["Blue"] = Color.FromArgb("#3b82f6");
                dict["BlueHover"] = Color.FromArgb("#2563eb");
                dict["BlueLight"] = Color.FromArgb("#2563eb");
                dict["BlueSubtle"] = Color.FromArgb("#eff6ff");
                dict["Green"] = Color.FromArgb("#16a34a");
                dict["GreenBg"] = Color.FromArgb("#dcfce7");
                dict["Red"] = Color.FromArgb("#dc2626");
                dict["RedBg"] = Color.FromArgb("#fee2e2");
                dict["Amber"] = Color.FromArgb("#d97706");
            }
        }
    }
}


