using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;

namespace Wysg.Musm.Radium.Views.SettingsTabs
{
    public partial class HotkeysSettingsTab : UserControl
    {
        public HotkeysSettingsTab()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                var vm = app.Services.GetService<HotkeysViewModel>();
                if (vm != null)
                {
                    DataContext = vm;
                }
            }
        }
    }
}
