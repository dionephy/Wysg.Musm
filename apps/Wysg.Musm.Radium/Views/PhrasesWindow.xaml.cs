using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class PhrasesWindow : Window
    {
        public PhrasesWindow(PhrasesViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void OnClose(object sender, RoutedEventArgs e) => Close();
        private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void OnDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        public static void Open()
        {
            var app = (App)Application.Current;
            var vm = app.Services.GetRequiredService<PhrasesViewModel>();
            var win = new PhrasesWindow(vm);
            win.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            win.Show();
        }
    }
}
