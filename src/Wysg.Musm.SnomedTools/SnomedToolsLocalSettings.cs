using System;
using System.IO;
using System.Text.Json;

namespace Wysg.Musm.SnomedTools
{
    /// <summary>
    /// Local configuration for standalone SnomedTools app.
    /// Saves connection strings and API endpoints to local AppData.
    /// </summary>
    public sealed class SnomedToolsLocalSettings
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm.SnomedTools");
        
  private static readonly string ConfigFile = Path.Combine(ConfigDir, "settings.json");

        private sealed class ConfigDto
        {
  public string? AzureSqlConnectionString { get; set; }
            public string? SnowstormBaseUrl { get; set; }
      }

        private ConfigDto _config = new();

public string? AzureSqlConnectionString
        {
            get => _config.AzureSqlConnectionString;
    set
       {
    _config.AzureSqlConnectionString = value;
    Save();
  }
      }

        public string? SnowstormBaseUrl
        {
     get => _config.SnowstormBaseUrl;
     set
          {
   _config.SnowstormBaseUrl = value;
      Save();
    }
    }

        public SnomedToolsLocalSettings()
    {
            Load();
        }

   private void Load()
        {
     try
 {
      if (!File.Exists(ConfigFile))
   {
        // Initialize with sensible defaults
             _config = new ConfigDto
     {
      AzureSqlConnectionString = "Server=tcp:musm-server.database.windows.net,1433;Initial Catalog=musmdb;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;",
             SnowstormBaseUrl = "https://snowstorm.ihtsdotools.org/snowstorm"
          };
              Save(); // Persist defaults
          return;
    }

                var json = File.ReadAllText(ConfigFile);
                _config = JsonSerializer.Deserialize<ConfigDto>(json) ?? new ConfigDto();
       }
            catch (Exception ex)
            {
    System.Diagnostics.Debug.WriteLine($"[SnomedToolsLocalSettings] Error loading config: {ex.Message}");
           _config = new ConfigDto();
        }
        }

  private void Save()
        {
            try
            {
      if (!Directory.Exists(ConfigDir))
         Directory.CreateDirectory(ConfigDir);

              var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions
    {
         WriteIndented = true
        });

    File.WriteAllText(ConfigFile, json);
    System.Diagnostics.Debug.WriteLine($"[SnomedToolsLocalSettings] Settings saved to {ConfigFile}");
         }
       catch (Exception ex)
     {
      System.Diagnostics.Debug.WriteLine($"[SnomedToolsLocalSettings] Error saving config: {ex.Message}");
            }
        }
    }
}
