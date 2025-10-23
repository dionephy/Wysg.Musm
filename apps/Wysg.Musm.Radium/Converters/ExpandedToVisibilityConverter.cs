using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Converter that returns Visibility.Collapsed when IsExpanded is false, otherwise Visibility.Visible.
    /// Used to show/hide term lists in the SNOMED browser based on expansion state.
    /// </summary>
    public sealed class ExpandedToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
