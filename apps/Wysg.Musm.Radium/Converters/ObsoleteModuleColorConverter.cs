using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Converter that returns grey color for modules containing "(obs)" (obsolete),
    /// and orange color for other built-in modules.
    /// Used in automation settings tab to visually distinguish obsolete modules.
    /// </summary>
    public class ObsoleteModuleColorConverter : IValueConverter
    {
        // Orange for normal built-in modules
        private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromRgb(255, 160, 0));
        
        // Grey for obsolete modules
        private static readonly Brush ObsoleteBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128));
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string moduleName)
            {
                // Check if module contains "(obs)" - case insensitive
                if (moduleName.IndexOf("(obs)", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ObsoleteBrush;
                }
            }
            
            return NormalBrush;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
