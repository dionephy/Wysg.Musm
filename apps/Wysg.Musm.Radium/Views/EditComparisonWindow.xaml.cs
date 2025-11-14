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
        private readonly string _patientNumber;
        
        public EditComparisonWindow(string patientNumber)
        {
            InitializeComponent();
            _patientNumber = patientNumber;
            
            // REMOVED: Don't subscribe to Closed event - no need to reload studies
            // The EditComparisonViewModel already handles modality updates in real-time
            // Reloading causes toggle buttons to disappear/reappear and editors to clear
            // Closed += OnWindowClosed;
        }
        
        // REMOVED: OnWindowClosed method that was causing unnecessary reload
        // Previous studies don't need to be reloaded when window closes because:
        // 1. Modality updates happen in real-time via RefreshModalityForStudyAsync
        // 2. Full reload clears and rebuilds PreviousStudies collection
        // 3. This causes toggle buttons to disappear/reappear
        // 4. Editor text gets cleared during rebuild
        // 5. The fix in SelectedPreviousStudy setter helps but doesn't eliminate the visual glitch
        
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
                
                var window = new EditComparisonWindow(patientNumber)
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
