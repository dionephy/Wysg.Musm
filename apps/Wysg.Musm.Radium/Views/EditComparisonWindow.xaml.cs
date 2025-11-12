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
            
            // Subscribe to Closed event to refresh previous studies in MainViewModel
            Closed += OnWindowClosed;
        }
        
        private async void OnWindowClosed(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[EditComparisonWindow] Window closed - refreshing previous studies in MainViewModel");
                
                // Get the ACTUAL MainViewModel from MainWindow's DataContext
                var mainWindow = Application.Current?.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVm && !string.IsNullOrWhiteSpace(_patientNumber))
                {
                    // Reload previous studies to pick up any modality changes from LOINC mappings
                    await mainVm.LoadPreviousStudiesAsync(_patientNumber);
                    System.Diagnostics.Debug.WriteLine("[EditComparisonWindow] Previous studies refresh completed");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[EditComparisonWindow] WARN: Could not refresh - MainViewModel not found or no patient number");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EditComparisonWindow] Error refreshing previous studies on window close: {ex.Message}");
            }
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
