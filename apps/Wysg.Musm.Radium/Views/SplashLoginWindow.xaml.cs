using System.Windows;
using System.Windows.Input;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views
{
    public partial class SplashLoginWindow : Window
    {
        public SplashLoginWindow()
        {
            InitializeComponent();
        }

        private void OnOpenSettings(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow(databaseOnly: true) { Owner = this };
            win.ShowDialog();
        }

        private void OnCloseApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}