using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Wysg.Musm.SnomedTools.ViewModels;

namespace Wysg.Musm.SnomedTools.Views
{
    public partial class CacheReviewWindow : Window
    {
        private readonly CacheReviewViewModel _viewModel;

        public CacheReviewWindow(CacheReviewViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Cleanup when window closes
            Closing += OnWindowClosing;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Dispose ViewModel to unsubscribe from background fetcher events
            _viewModel?.Dispose();
        }
    }

    /// <summary>
    /// Converter to show visibility when value is null
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
