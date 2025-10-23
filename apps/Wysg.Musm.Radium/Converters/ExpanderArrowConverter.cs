using System;
using System.Globalization;
using System.Windows.Data;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Converter that returns "¡å" when expanded (true) and "¢º" when collapsed (false).
    /// Used for expand/collapse toggle buttons in the SNOMED browser.
    /// </summary>
    public sealed class ExpanderArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? "¡å" : "¢º";
            }
            return "¢º";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
