using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Converter that returns a highlight background color if a synonym term contains "structure", "entire", or "(" (case-insensitive).
    /// Returns a light yellow/amber background color for matching terms.
    /// </summary>
    public class SynonymStructureHighlightConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush HighlightBrush = new SolidColorBrush(Color.FromArgb(80, 255, 193, 7)); // Semi-transparent amber
        private static readonly SolidColorBrush DefaultBrush = Brushes.Transparent;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = Term (string)
            // values[1] = TermType (string)
            
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return DefaultBrush;

            string term = values[0] as string ?? string.Empty;
            string termType = values[1] as string ?? string.Empty;

            // Only highlight Synonym terms (not FSN or PT)
            if (!string.Equals(termType, "Synonym", StringComparison.OrdinalIgnoreCase))
                return DefaultBrush;

            // Check if term contains "structure", "entire" (case-insensitive), or "("
            if (term.IndexOf("structure", StringComparison.OrdinalIgnoreCase) >= 0 ||
                term.IndexOf("entire", StringComparison.OrdinalIgnoreCase) >= 0 ||
                term.Contains('('))
            {
                return HighlightBrush;
            }

            return DefaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
