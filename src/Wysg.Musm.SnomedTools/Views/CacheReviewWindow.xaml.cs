using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
            
            // Subscribe to ListBox SelectionChanged events to sync with ViewModel
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // Find the three ListBoxes and wire up selection sync
            var organismListBox = this.FindName("OrganismListBox") as ListBox;
            var substanceListBox = this.FindName("SubstanceListBox") as ListBox;
            var otherListBox = this.FindName("OtherListBox") as ListBox;

            if (organismListBox != null)
                organismListBox.SelectionChanged += OnListBoxSelectionChanged;
            if (substanceListBox != null)
                substanceListBox.SelectionChanged += OnListBoxSelectionChanged;
            if (otherListBox != null)
                otherListBox.SelectionChanged += OnListBoxSelectionChanged;
        }

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox) return;

            // Sync ViewModel IsSelected with ListBox selection
            // When user selects items via click/shift+click, update ViewModel
            foreach (var item in e.AddedItems)
            {
                if (item is SelectableCachedCandidate candidate)
                    candidate.IsSelected = true;
            }

            foreach (var item in e.RemovedItems)
            {
                if (item is SelectableCachedCandidate candidate)
                    candidate.IsSelected = false;
            }
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
