using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace StockGuard.Converters
{
    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType,
                              object? parameter, CultureInfo culture)
            => value?.ToString() switch
            {
                "Available" => Color.FromArgb("#10b981"),
                "Borrowed" => Color.FromArgb("#3b82f6"),
                "PendingPause" => Color.FromArgb("#f59e0b"),
                "OnHold" => Color.FromArgb("#8b5cf6"),
                "Damaged" => Color.FromArgb("#ef4444"),
                "UnderRepair" => Color.FromArgb("#f97316"),
                "Lost" => Color.FromArgb("#6b7280"),
                _ => Color.FromArgb("#94a3b8")
            };

        public object ConvertBack(object? value, Type targetType,
                                  object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
