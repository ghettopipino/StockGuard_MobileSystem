using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockGuard.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType,
            object? parameter, CultureInfo culture)
            => value is int n && n > 0;

        public object ConvertBack(object? value, Type targetType,
            object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}