using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class OperationManualWindow : Window
    {
        private readonly ICollectionView _entriesView;

        public OperationManualWindow()
        {
            InitializeComponent();

            _entriesView = CollectionViewSource.GetDefaultView(OperationManualCatalog.Entries);
            _entriesView.SortDescriptions.Add(new SortDescription(nameof(OperationManualEntry.Name), ListSortDirection.Ascending));
            _entriesView.Filter = FilterEntry;
            ManualList.ItemsSource = _entriesView;
        }

        private bool FilterEntry(object? item)
        {
            if (item is not OperationManualEntry entry)
            {
                return false;
            }

            var text = SearchBox.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            return entry.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.Summary.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.OutputNotes.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.Category.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.Arguments.Any(arg =>
                    arg.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || arg.AcceptedTypes.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || arg.Description.Contains(text, StringComparison.OrdinalIgnoreCase)
                    || arg.Requirement.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _entriesView.Refresh();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
