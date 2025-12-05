using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Wysg.Musm.Radium.Models;

namespace Wysg.Musm.Radium.Converters
{
    /// <summary>
    /// Converter that creates a syntax-colored TextBlock for custom module display.
    /// Handles multi-line display for "Set X to Y" patterns and applies color coding.
    /// Built-in modules (without special keywords at the start) are displayed in:
    /// - Orange for normal modules
    /// - Grey for obsolete modules (containing "(obs)")
    /// </summary>
    public class CustomModuleSyntaxConverter : IValueConverter
    {
        // Color scheme
        private static readonly Brush KeywordBrush = new SolidColorBrush(Color.FromRgb(255, 160, 0)); // Orange
        private static readonly Brush ObsoleteBrush = new SolidColorBrush(Color.FromRgb(128, 128, 128)); // Grey for obsolete modules
        private static readonly Brush PropertyBrush = new SolidColorBrush(Color.FromRgb(106, 190, 48)); // Green
        private static readonly Brush BookmarkBrush = new SolidColorBrush(Color.FromRgb(78, 201, 176)); // Mint/Cyan
        private static readonly Brush DefaultBrush = new SolidColorBrush(Color.FromRgb(208, 208, 208)); // Light gray
        
        // Keywords for syntax coloring (ordered by length descending for proper matching)
        private static readonly string[] Keywords = { "Abort if", "If not", "Set", "Run", "If", "to" };
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string moduleName)
                return new TextBlock { Text = string.Empty };
            
            var textBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 180
            };
            
            // Check if this is a custom module (starts with special keywords) or built-in module
            // Custom modules start with: "Set ", "Run ", "Abort if ", "If ", "If not "
            bool isCustomModule = moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase) ||
                                  moduleName.StartsWith("Run ", StringComparison.OrdinalIgnoreCase) ||
                                  moduleName.StartsWith("Abort if ", StringComparison.OrdinalIgnoreCase) ||
                                  moduleName.StartsWith("If not ", StringComparison.OrdinalIgnoreCase) ||
                                  moduleName.StartsWith("If ", StringComparison.OrdinalIgnoreCase);
            
            if (!isCustomModule)
            {
                // Built-in module - check if obsolete (contains "(obs)")
                bool isObsolete = moduleName.IndexOf("(obs)", StringComparison.OrdinalIgnoreCase) >= 0;
                var brush = isObsolete ? ObsoleteBrush : KeywordBrush;
                
                textBlock.Inlines.Add(new Run(moduleName) { Foreground = brush });
                return textBlock;
            }
            
            // Custom module with keywords - check if this is a "Set X to Y" pattern - add line break
            var displayText = moduleName;
            if (moduleName.StartsWith("Set ", StringComparison.OrdinalIgnoreCase))
            {
                var toIndex = moduleName.IndexOf(" to ", StringComparison.OrdinalIgnoreCase);
                if (toIndex > 0)
                {
                    // Insert newline before " to "
                    displayText = moduleName.Substring(0, toIndex) + "\n" + moduleName.Substring(toIndex);
                }
            }
            
            // Apply syntax coloring for custom modules
            ApplySyntaxColoring(textBlock, displayText);
            
            return textBlock;
        }
        
        private void ApplySyntaxColoring(TextBlock textBlock, string text)
        {
            int currentIndex = 0;
            
            while (currentIndex < text.Length)
            {
                bool foundMatch = false;
                
                // Check for newlines
                if (text[currentIndex] == '\n')
                {
                    textBlock.Inlines.Add(new LineBreak());
                    currentIndex++;
                    continue;
                }
                
                // Check for keywords (longest first to avoid partial matches like "If" matching before "If not")
                foreach (var keyword in Keywords.OrderByDescending(k => k.Length))
                {
                    if (currentIndex + keyword.Length <= text.Length &&
                        text.Substring(currentIndex, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        textBlock.Inlines.Add(new Run(keyword) { Foreground = KeywordBrush });
                        currentIndex += keyword.Length;
                        foundMatch = true;
                        break;
                    }
                }
                
                if (foundMatch) continue;
                
                // Check for properties
                var propertyMatch = CustomModuleProperties.AllProperties
                    .OrderByDescending(p => p.Length)
                    .FirstOrDefault(p => currentIndex + p.Length <= text.Length &&
                                        text.Substring(currentIndex, p.Length).Equals(p, StringComparison.OrdinalIgnoreCase));
                
                if (propertyMatch != null)
                {
                    textBlock.Inlines.Add(new Run(propertyMatch) { Foreground = PropertyBrush });
                    currentIndex += propertyMatch.Length;
                    continue;
                }
                
                // Find next keyword or property boundary
                int nextBoundary = text.Length;
                
                foreach (var keyword in Keywords)
                {
                    var idx = text.IndexOf(keyword, currentIndex + 1, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < nextBoundary)
                        nextBoundary = idx;
                }
                
                foreach (var prop in CustomModuleProperties.AllProperties)
                {
                    var idx = text.IndexOf(prop, currentIndex + 1, StringComparison.OrdinalIgnoreCase);
                    if (idx >= 0 && idx < nextBoundary)
                        nextBoundary = idx;
                }
                
                // Extract segment until next boundary
                var segmentLength = nextBoundary - currentIndex;
                if (segmentLength > 0)
                {
                    var segment = text.Substring(currentIndex, segmentLength);
                    
                    // Determine color: bookmarks/procedures vs whitespace/punctuation
                    var trimmed = segment.Trim();
                    var brush = string.IsNullOrWhiteSpace(trimmed) ? DefaultBrush : BookmarkBrush;
                    
                    textBlock.Inlines.Add(new Run(segment) { Foreground = brush });
                    currentIndex += segmentLength;
                }
                else
                {
                    // Fallback: single character
                    textBlock.Inlines.Add(new Run(text[currentIndex].ToString()) { Foreground = DefaultBrush });
                    currentIndex++;
                }
            }
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
