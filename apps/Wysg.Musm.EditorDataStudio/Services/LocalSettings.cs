using System;
using System.IO;
using System.Text.Json;

namespace Wysg.Musm.EditorDataStudio.Services
{
    public interface ILocalSettings
    {
        string? LastEdition { get; set; }
        string? ConnectionString { get; set; }
    }

    internal sealed class LocalSettings : ILocalSettings
    {
        private readonly string _filePath;
        private SettingsData _data;

        private sealed class SettingsData
        {
            public string? LastEdition { get; set; }
            public string? ConnectionString { get; set; }
        }

        public LocalSettings()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wysg.Musm", "EditorDataStudio");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
            _data = Load();
        }

        public string? LastEdition
        {
            get => _data.LastEdition;
            set { _data.LastEdition = value; Save(); }
        }

        public string? ConnectionString
        {
            get => _data.ConnectionString;
            set { _data.ConnectionString = value; Save(); }
        }

        private SettingsData Load()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
                }
            }
            catch { /* ignore */ }
            return new SettingsData();
        }

        private void Save()
        {
        try
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch { /* ignore */ }
        }
    }
}
