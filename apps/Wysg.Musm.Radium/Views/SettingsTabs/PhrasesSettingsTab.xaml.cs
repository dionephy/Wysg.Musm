using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views.SettingsTabs
{
    public partial class PhrasesSettingsTab : UserControl
    {
        public PhrasesSettingsTab()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Get ViewModel from parent SettingsWindow's ViewModel or DI
            if (Application.Current is App app)
            {
                // First try to get from parent's SettingsViewModel
                var settingsWindow = Window.GetWindow(this) as SettingsWindow;
                if (settingsWindow?.DataContext is SettingsViewModel svm && svm.Phrases != null)
                {
                    DataContext = svm.Phrases;
                }
                else
                {
                    // Fallback to DI
                    var phrasesVm = app.Services.GetService<PhrasesViewModel>();
                    if (phrasesVm != null)
                    {
                        DataContext = phrasesVm;
                    }
                }
            }
        }
    }
}
