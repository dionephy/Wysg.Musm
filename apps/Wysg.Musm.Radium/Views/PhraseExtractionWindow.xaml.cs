using System.Windows;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class PhraseExtractionWindow : Window
    {
        public PhraseExtractionWindow()
        {
            InitializeComponent();
        }

        public void Load(string header, string findings, string conclusion)
        {
            if (DataContext is PhraseExtractionViewModel vm)
                vm.LoadFromDeReportified(header, findings, conclusion);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
    }
}
