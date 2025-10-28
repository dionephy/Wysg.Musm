using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    /// <summary>
    /// Interaction logic for EditComparisonWindow.xaml
    /// </summary>
    public partial class EditComparisonWindow : Window
    {
        public EditComparisonWindow()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// Opens the Edit Comparison window with the given patient context and previous studies.
        /// </summary>
        public static string? Open(string patientNumber, string patientName, string patientSex, List<MainViewModel.PreviousStudyTab> previousStudies, string currentComparison)
        {
            try
            {
                var app = (App)Application.Current;
                var studynameRepo = app.Services.GetRequiredService<Services.IStudynameLoincRepository>();
                
                var vm = new EditComparisonViewModel(studynameRepo, patientNumber, patientName, patientSex, previousStudies, currentComparison);
                
                var window = new EditComparisonWindow
                {
                    DataContext = vm,
                    Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive)
                };
                
                if (window.ShowDialog() == true)
                {
                    return vm.ComparisonString;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditComparisonWindow] Error: {ex.Message}");
                MessageBox.Show($"Failed to open Edit Comparison window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
        
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
    
    /// <summary>
    /// Converter to invert boolean to visibility for UI elements.
    /// </summary>
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
