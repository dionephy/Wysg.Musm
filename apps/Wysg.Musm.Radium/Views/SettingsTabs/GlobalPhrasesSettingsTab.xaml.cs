using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Wysg.Musm.Radium.ViewModels;
using Wysg.Musm.Radium.Services;
using Wysg.Musm.Radium.Services.Adapters;
using Wysg.Musm.SnomedTools.ViewModels;
using Wysg.Musm.SnomedTools.Views;

namespace Wysg.Musm.Radium.Views.SettingsTabs
{
    public partial class GlobalPhrasesSettingsTab : UserControl
    {
        public GlobalPhrasesSettingsTab()
        {
      InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
 if (Application.Current is App app)
       {
     var vm = app.Services.GetService<GlobalPhrasesViewModel>();
      if (vm != null)
    {
           DataContext = vm;
     }
       }
        }

        private void OnLinkSnomedFromGlobal(object sender, RoutedEventArgs e)
    {
      try
       {
    if (sender is not Button btn) return;
  
  // Find the DataGridRow ancestor
  var row = FindAncestor<DataGridRow>(btn);
                if (row?.Item is GlobalPhraseItem item)
    {
       if (Application.Current is App app)
              {
     var svc = app.Services.GetService<ISnomedMapService>();
   var snow = app.Services.GetService<ISnowstormClient>();
                if (svc == null || snow == null)
      {
           MessageBox.Show("SNOMED services not available.", "SNOMED", MessageBoxButton.OK, MessageBoxImage.Error);
         return;
              }
             
            var win = new PhraseSnomedLinkWindow(item.Id, item.Text, null, svc, snow)
        {
        Owner = Window.GetWindow(this)
  };
              win.ShowDialog();
      }
    }
            }
         catch (Exception ex)
        {
       Debug.WriteLine("[GlobalPhrasesSettingsTab] Link SNOMED error: " + ex.Message);
            }
        }

        private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
          while (current != null)
      {
    if (current is T t) return t;
 current = VisualTreeHelper.GetParent(current);
      }
    return null;
      }

 private void OnOpenSnomedBrowserClick(object sender, RoutedEventArgs e)
        {
            try
  {
           if (Application.Current is not App app) return;
    
       // Get dependencies from DI
    var snowstormClient = app.Services.GetService<ISnowstormClient>();
      var phraseService = app.Services.GetService<IPhraseService>();
     var snomedMapService = app.Services.GetService<ISnomedMapService>();

if (snowstormClient == null || phraseService == null || snomedMapService == null)
    {
  MessageBox.Show(
 "SNOMED services not available. Check that Snowstorm is configured.",
           "SNOMED Browser",
                MessageBoxButton.OK,
              MessageBoxImage.Warning);
  return;
        }

              // Create ViewModel
       var vm = new SnomedBrowserViewModel(snowstormClient, phraseService, snomedMapService);

                // Create and show window
      var window = new SnomedBrowserWindow(vm)
           {
           Owner = Window.GetWindow(this)
    };

    window.ShowDialog();

       // Refresh the global phrases list after window closes
    if (DataContext is GlobalPhrasesViewModel globalVm)
  {
     _ = globalVm.RefreshPhrasesAsync();
      }
    }
          catch (Exception ex)
       {
  Debug.WriteLine("[GlobalPhrasesSettingsTab] Open SNOMED Browser error: " + ex.Message);
          MessageBox.Show(
          $"Error opening SNOMED Browser:\n{ex.Message}",
     "Error",
  MessageBoxButton.OK,
      MessageBoxImage.Error);
            }
        }

 private void OnOpenWordCountImporterClick(object sender, RoutedEventArgs e)
        {
    try
       {
              if (Application.Current is not App app) return;
     
     // Get Radium services from DI
    var radiumSnowstormClient = app.Services.GetService<Wysg.Musm.Radium.Services.ISnowstormClient>();
          var radiumPhraseService = app.Services.GetService<Wysg.Musm.Radium.Services.IPhraseService>();
          var radiumSnomedMapService = app.Services.GetService<Wysg.Musm.Radium.Services.ISnomedMapService>();

 if (radiumSnowstormClient == null || radiumPhraseService == null || radiumSnomedMapService == null)
     {
           MessageBox.Show(
     "SNOMED services not available. Check that Snowstorm is configured.",
        "Word Count Importer",
     MessageBoxButton.OK,
        MessageBoxImage.Warning);
                return;
  }

      // Create adapters to bridge Radium services to SnomedTools interfaces
         var snowstormAdapter = new SnowstormClientAdapter(radiumSnowstormClient);
         var phraseAdapter = new PhraseServiceAdapter(radiumPhraseService);
                var snomedMapAdapter = new SnomedMapServiceAdapter(radiumSnomedMapService);

    // Create ViewModel using SnomedTools library
           var vm = new SnomedWordCountImporterViewModel(
        snowstormAdapter,
   phraseAdapter,
      snomedMapAdapter,
    sessionFilePath: System.IO.Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
             "Wysg.Musm.Radium",
                    "snomed_wordcount_session.json")
      );

    // Create and show window using SnomedTools library
    var window = new SnomedWordCountImporterWindow(vm)
      {
   Owner = Window.GetWindow(this)
   };

           window.ShowDialog();

       // Refresh the global phrases list after window closes
                if (DataContext is GlobalPhrasesViewModel globalVm)
    {
     _ = globalVm.RefreshPhrasesAsync();
    }
            }
            catch (Exception ex)
            {
             Debug.WriteLine("[GlobalPhrasesSettingsTab] Open Word Count Importer error: " + ex.Message);
  MessageBox.Show(
          $"Error opening Word Count Importer:\n{ex.Message}",
    "Error",
              MessageBoxButton.OK,
           MessageBoxImage.Error);
   }
        }
    }
}
