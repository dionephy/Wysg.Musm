using System.IO;
using System.Text.Json;
using Wysg.Musm.LlamaClient.Models;

namespace Wysg.Musm.LlamaClient.Services;

/// <summary>
/// Service for managing saved presets.
/// </summary>
public class PresetService
{
    private readonly string _presetsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public PresetService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Wysg.Musm.LlamaClient");

        Directory.CreateDirectory(appDataPath);
        _presetsFilePath = Path.Combine(appDataPath, "presets.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Loads all presets from disk.
    /// </summary>
    public async Task<PresetCollection> LoadPresetsAsync()
    {
        if (!File.Exists(_presetsFilePath))
        {
            var defaultCollection = new PresetCollection
            {
                Presets = [Preset.CreateDefault()]
            };
            defaultCollection.ActivePresetId = defaultCollection.Presets[0].Id;
            await SavePresetsAsync(defaultCollection);
            return defaultCollection;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_presetsFilePath);
            var collection = JsonSerializer.Deserialize<PresetCollection>(json, _jsonOptions);
            return collection ?? new PresetCollection { Presets = [Preset.CreateDefault()] };
        }
        catch
        {
            return new PresetCollection { Presets = [Preset.CreateDefault()] };
        }
    }

    /// <summary>
    /// Saves all presets to disk.
    /// </summary>
    public async Task SavePresetsAsync(PresetCollection collection)
    {
        var json = JsonSerializer.Serialize(collection, _jsonOptions);
        await File.WriteAllTextAsync(_presetsFilePath, json);
    }

    /// <summary>
    /// Adds a new preset.
    /// </summary>
    public async Task<Preset> AddPresetAsync(PresetCollection collection, Preset preset)
    {
        collection.Presets.Add(preset);
        await SavePresetsAsync(collection);
        return preset;
    }

    /// <summary>
    /// Updates an existing preset.
    /// </summary>
    public async Task UpdatePresetAsync(PresetCollection collection, Preset preset)
    {
        var index = collection.Presets.FindIndex(p => p.Id == preset.Id);
        if (index >= 0)
        {
            preset.ModifiedAt = DateTime.UtcNow;
            collection.Presets[index] = preset;
            await SavePresetsAsync(collection);
        }
    }

    /// <summary>
    /// Deletes a preset.
    /// </summary>
    public async Task DeletePresetAsync(PresetCollection collection, string presetId)
    {
        collection.Presets.RemoveAll(p => p.Id == presetId);
        if (collection.ActivePresetId == presetId)
        {
            collection.ActivePresetId = collection.Presets.FirstOrDefault()?.Id;
        }
        await SavePresetsAsync(collection);
    }

    /// <summary>
    /// Duplicates a preset.
    /// </summary>
    public async Task<Preset> DuplicatePresetAsync(PresetCollection collection, string presetId)
    {
        var original = collection.Presets.FirstOrDefault(p => p.Id == presetId);
        if (original == null)
            throw new InvalidOperationException("Preset not found");

        var clone = original.Clone();
        return await AddPresetAsync(collection, clone);
    }

    /// <summary>
    /// Exports presets to a JSON string.
    /// </summary>
    public string ExportPresets(PresetCollection collection)
    {
        return JsonSerializer.Serialize(collection, _jsonOptions);
    }

    /// <summary>
    /// Imports presets from a JSON string.
    /// </summary>
    public async Task<int> ImportPresetsAsync(PresetCollection collection, string json)
    {
        var imported = JsonSerializer.Deserialize<PresetCollection>(json, _jsonOptions);
        if (imported?.Presets == null || imported.Presets.Count == 0)
            return 0;

        var count = 0;
        foreach (var preset in imported.Presets)
        {
            // Generate new ID to avoid conflicts
            preset.Id = Guid.NewGuid().ToString();
            preset.Name = $"{preset.Name} (Imported)";
            collection.Presets.Add(preset);
            count++;
        }

        await SavePresetsAsync(collection);
        return count;
    }
}
