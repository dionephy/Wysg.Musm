using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace Wysg.Musm.SnomedTools
{
    public partial class SettingsWindow : Window
    {
     private readonly SnomedToolsLocalSettings _settings;

      public string? AzureSqlConnectionString
 {
   get => _settings.AzureSqlConnectionString;
      set => _settings.AzureSqlConnectionString = value;
    }

  public string? SnowstormBaseUrl
{
      get => _settings.SnowstormBaseUrl;
  set => _settings.SnowstormBaseUrl = value;
 }

        public SettingsWindow(SnomedToolsLocalSettings settings)
  {
    InitializeComponent();
   _settings = settings ?? throw new ArgumentNullException(nameof(settings));
      DataContext = this;

            // Show config path
   var configDir = Path.Combine(
Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
  "Wysg.Musm.SnomedTools");
        var configFile = Path.Combine(configDir, "settings.json");
  txtConfigPath.Text = configFile;
  }

      private async void OnTestAzureSql(object sender, RoutedEventArgs e)
 {
  if (string.IsNullOrWhiteSpace(AzureSqlConnectionString))
   {
       txtAzureSqlStatus.Text = "? Connection string is empty";
    return;
   }

 txtAzureSqlStatus.Text = "Testing...";
     try
   {
    await using var con = new SqlConnection(AzureSqlConnectionString);
 await con.OpenAsync();
        await using var cmd = new SqlCommand("SELECT @@VERSION", con);
       var version = (string?)await cmd.ExecuteScalarAsync();
       var versionDisplay = version != null ? version.Substring(0, Math.Min(30, version.Length)) : "unknown";
    txtAzureSqlStatus.Text = $"? Connected ({versionDisplay})";
       txtAzureSqlStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x90, 0xEE, 0x90));
}
   catch (Exception ex)
   {
          txtAzureSqlStatus.Text = $"? Failed: {ex.Message}";
 txtAzureSqlStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xB3, 0xB3));
            }
   }

    private async void OnTestSnowstorm(object sender, RoutedEventArgs e)
        {
      if (string.IsNullOrWhiteSpace(SnowstormBaseUrl))
    {
         txtSnowstormStatus.Text = "? Base URL is empty";
      return;
    }

      txtSnowstormStatus.Text = "Testing...";
 try
      {
   using var http = new HttpClient();
     var url = $"{SnowstormBaseUrl.TrimEnd('/')}/version";
          using var resp = await http.GetAsync(url);
    resp.EnsureSuccessStatusCode();
      var json = await resp.Content.ReadAsStringAsync();
      using var doc = JsonDocument.Parse(json);
 var version = doc.RootElement.TryGetProperty("version", out var v) ? v.GetString() : "unknown";
      txtSnowstormStatus.Text = $"? Connected (version {version})";
        txtSnowstormStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x90, 0xEE, 0x90));
      }
catch (Exception ex)
     {
  txtSnowstormStatus.Text = $"? Failed: {ex.Message}";
      txtSnowstormStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xB3, 0xB3));
      }
 }

 private void OnSave(object sender, RoutedEventArgs e)
 {
    // Settings are automatically saved by the properties
     MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
      Close();
  }

 private void OnClose(object sender, RoutedEventArgs e)
  {
  Close();
   }
    }
}
